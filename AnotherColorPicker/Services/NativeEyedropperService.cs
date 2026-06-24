using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace AnotherColorPicker.Services;

/// <summary>
/// Eyedropper implementation that reads screen pixels directly using native OS APIs.
/// Falls back to ScreenshotEyedropperService on Wayland due to strict security policies.
/// </summary>
public class NativeEyedropperService : IEyedropperService
{
    private readonly WaylandEyedropperService _fallbackService;

    public NativeEyedropperService()
    {
        _fallbackService = new WaylandEyedropperService();
    }

    public event Action<Color>? ColorPreview;

    public bool IsAvailable => true;

    private bool IsWayland => Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null;

    public async Task<Color?> PickColorAsync()
    {
        if (IsWayland)
        {
            // Wayland strictly prevents direct pixel reading.
            _fallbackService.ColorPreview += OnFallbackPreview;
            try
            {
                return await _fallbackService.PickColorAsync();
            }
            finally
            {
                _fallbackService.ColorPreview -= OnFallbackPreview;
            }
        }

        return await ShowPickerOverlayAsync();
    }

    private void OnFallbackPreview(Color color)
    {
        ColorPreview?.Invoke(color);
    }

    private async Task<Color?> ShowPickerOverlayAsync()
    {
        var tcs = new TaskCompletionSource<Color?>();

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return null;
        }

        var overlayWindow = new Window
        {
            WindowDecorations = WindowDecorations.None,
            WindowState = WindowState.FullScreen,
            Topmost = true,
            ShowInTaskbar = false,
            Background = Brushes.Transparent,
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
            Cursor = new Cursor(StandardCursorType.Cross),
        };

        DispatcherTimer? updateTimer = null;

        overlayWindow.Opened += (_, _) =>
        {
            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60fps
            };

            updateTimer.Tick += (s, e) =>
            {
                var color = NativePixelReader.GetColorUnderCursor();
                if (color.HasValue)
                {
                    ColorPreview?.Invoke(color.Value);
                }
            };
            
            updateTimer.Start();
        };

        // Also update on pointer move for immediate feedback
        overlayWindow.PointerMoved += (_, e) =>
        {
            var color = NativePixelReader.GetColorUnderCursor();
            if (color.HasValue)
            {
                ColorPreview?.Invoke(color.Value);
            }
        };

        overlayWindow.PointerPressed += (_, e) =>
        {
            updateTimer?.Stop();
            
            // For the final pick, we temporarily hide the overlay to ensure we don't accidentally read its tint
            // if the platform composed it with a slight opacity.
            overlayWindow.Opacity = 0;
            
            var finalColor = NativePixelReader.GetColorUnderCursor();
            tcs.TrySetResult(finalColor);
            overlayWindow.Close();
        };

        overlayWindow.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                updateTimer?.Stop();
                tcs.TrySetResult(null);
                overlayWindow.Close();
            }
        };

        overlayWindow.Show();

        return await tcs.Task;
    }
}
