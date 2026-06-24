using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace AnotherColorPicker.Services;

/// <summary>
/// Eyedropper implementation that reads screen pixels directly using native OS APIs.
/// Falls back to ScreenshotEyedropperService on Wayland due to strict security policies.
/// </summary>
public class NativeEyedropperService : IEyedropperService
{
    private readonly ScreenshotEyedropperService _screenshotFallbackService;
    private readonly WaylandEyedropperService _portalFallbackService;
    private const int MagnifierRadius = 5;
    private const int MagnifierSize = 11; // 2 * Radius + 1
    private const int ZoomScale = 10;

    public NativeEyedropperService()
    {
        _screenshotFallbackService = new ScreenshotEyedropperService();
        _portalFallbackService = new WaylandEyedropperService();
    }

    public event Action<Color>? ColorPreview;

    public bool IsAvailable => true;

    private bool IsWayland => Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null;

    public async Task<Color?> PickColorAsync()
    {
        if (IsWayland)
        {
            // Wayland strictly prevents direct pixel reading. Use Screenshot fallback first
            // to provide the custom magnifier experience.
            _screenshotFallbackService.ColorPreview += OnFallbackPreview;
            var screenshotResult = await _screenshotFallbackService.PickColorAsync();
            _screenshotFallbackService.ColorPreview -= OnFallbackPreview;

            if (screenshotResult.HasValue)
            {
                return screenshotResult.Value;
            }

            // If taking a screenshot fails (e.g., missing grim/scrot), use native portal which doesn't have our magnifier
            _portalFallbackService.ColorPreview += OnFallbackPreview;
            try
            {
                return await _portalFallbackService.PickColorAsync();
            }
            finally
            {
                _portalFallbackService.ColorPreview -= OnFallbackPreview;
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

        var canvas = new Canvas();

        var magnifierImage = new Avalonia.Controls.Image
        {
            Width = MagnifierSize * ZoomScale,
            Height = MagnifierSize * ZoomScale,
            Stretch = Stretch.UniformToFill
        };
        RenderOptions.SetBitmapInterpolationMode(magnifierImage, BitmapInterpolationMode.None);

        var hexText = new TextBlock
        {
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.Parse("#AA000000")),
            Padding = new Thickness(4, 2),
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var crosshair = new Border
        {
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1),
            Width = ZoomScale,
            Height = ZoomScale,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var magnifierBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#333333")),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(10),
            Background = Brushes.Black,
            ClipToBounds = true,
            Width = MagnifierSize * ZoomScale,
            Height = MagnifierSize * ZoomScale,
            IsVisible = false
        };

        var magnifierGrid = new Grid();
        magnifierGrid.Children.Add(magnifierImage);
        magnifierGrid.Children.Add(crosshair);
        magnifierGrid.Children.Add(hexText);

        magnifierBorder.Child = magnifierGrid;
        canvas.Children.Add(magnifierBorder);
        
        overlayWindow.Content = canvas;

        var writeableBitmap = new WriteableBitmap(
            new PixelSize(MagnifierSize, MagnifierSize),
            new Vector(96, 96),
            Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Premul);
        magnifierImage.Source = writeableBitmap;

        Point lastPointerPos = default;

        Action updateMagnifier = () =>
        {
            var colors = NativePixelReader.GetPixelsAroundCursor(MagnifierRadius, out int w, out int h);
            if (colors != null && w == MagnifierSize && h == MagnifierSize)
            {
                using (var fb = writeableBitmap.Lock())
                {
                    unsafe
                    {
                        byte* ptr = (byte*)fb.Address;
                        for (int y = 0; y < h; y++)
                        {
                            for (int x = 0; x < w; x++)
                            {
                                int idx = y * w + x;
                                var c = colors[idx];
                                int offset = y * fb.RowBytes + x * 4;
                                ptr[offset] = c.B;
                                ptr[offset + 1] = c.G;
                                ptr[offset + 2] = c.R;
                                ptr[offset + 3] = 255;
                            }
                        }
                    }
                }
                magnifierBorder.IsVisible = true;
                
                var centerColor = colors[(MagnifierSize / 2) * MagnifierSize + (MagnifierSize / 2)];
                hexText.Text = $"#{centerColor.R:X2}{centerColor.G:X2}{centerColor.B:X2}";
                ColorPreview?.Invoke(centerColor);
                
                Canvas.SetLeft(magnifierBorder, lastPointerPos.X + 20);
                Canvas.SetTop(magnifierBorder, lastPointerPos.Y + 20);
            }
            else
            {
                magnifierBorder.IsVisible = false;
                var color = NativePixelReader.GetColorUnderCursor();
                if (color.HasValue)
                {
                    ColorPreview?.Invoke(color.Value);
                }
            }
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
                updateMagnifier();
            };
            
            updateTimer.Start();
        };

        overlayWindow.PointerMoved += (_, e) =>
        {
            lastPointerPos = e.GetPosition(canvas);
            updateMagnifier();
        };

        overlayWindow.PointerPressed += (_, e) =>
        {
            updateTimer?.Stop();
            
            // Hide overlay momentarily to capture final color cleanly without our own UI
            overlayWindow.Opacity = 0;
            
            var finalColor = NativePixelReader.GetColorUnderCursor();
            tcs.TrySetResult(finalColor);
            
            writeableBitmap.Dispose();
            overlayWindow.Close();
        };

        overlayWindow.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                updateTimer?.Stop();
                tcs.TrySetResult(null);
                
                writeableBitmap.Dispose();
                overlayWindow.Close();
            }
        };

        overlayWindow.Show();

        return await tcs.Task;
    }
}
