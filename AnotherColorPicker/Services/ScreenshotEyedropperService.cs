using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace AnotherColorPicker.Services;

/// <summary>
/// Eyedropper implementation using a fullscreen overlay approach.
/// Takes a screenshot of the screen and displays it in a fullscreen borderless window.
/// The user clicks to pick a pixel color. Works on both X11 and Wayland.
/// </summary>
public class ScreenshotEyedropperService : IEyedropperService
{
    public event Action<Color>? ColorPreview;

    public bool IsAvailable => true;

    public async Task<Color?> PickColorAsync()
    {
        // Step 1: Capture screenshot using platform tools
        var screenshotPath = Path.Combine(Path.GetTempPath(), $"acp_screenshot_{Guid.NewGuid()}.png");
        bool captured = await CaptureScreenshotAsync(screenshotPath);

        if (!captured || !File.Exists(screenshotPath))
        {
            return null;
        }

        try
        {
            // Step 2: Load the screenshot bitmap
            var bitmap = new Bitmap(screenshotPath);

            // Step 3: Show fullscreen overlay with the screenshot
            var pickedColor = await ShowPickerOverlayAsync(bitmap);

            bitmap.Dispose();

            return pickedColor;
        }
        finally
        {
            // Clean up temp file
            try { File.Delete(screenshotPath); } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// Captures a screenshot using available Linux tools (gnome-screenshot, scrot, grim for Wayland, import from ImageMagick).
    /// </summary>
    private static async Task<bool> CaptureScreenshotAsync(string outputPath)
    {
        // Try multiple screenshot tools in order of preference
        var tools = new (string command, string args)[]
        {
            // Wayland-compatible via XDG portal
            ("gnome-screenshot", $"-f \"{outputPath}\""),
            // Wayland via grim
            ("grim", $"\"{outputPath}\""),
            // X11 via scrot
            ("scrot", $"\"{outputPath}\""),
            // X11 via ImageMagick
            ("import", $"-window root \"{outputPath}\""),
            // Spectacle (KDE)
            ("spectacle", $"-b -n -f -o \"{outputPath}\""),
        };

        foreach (var (command, args) in tools)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = Process.Start(psi);
                if (process == null) continue;

                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && File.Exists(outputPath))
                {
                    return true;
                }
            }
            catch
            {
                // Tool not available, try next
            }
        }

        return false;
    }

    /// <summary>
    /// Shows the screenshot in a fullscreen borderless window and lets the user pick a pixel.
    /// </summary>
    private async Task<Color?> ShowPickerOverlayAsync(Bitmap screenshot)
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
            Background = Brushes.Black,
            Cursor = new Cursor(StandardCursorType.Cross),
        };

        var imageControl = new Avalonia.Controls.Image
        {
            Source = screenshot,
            Stretch = Stretch.UniformToFill,
        };

        overlayWindow.Content = imageControl;

        // Use WriteableBitmap to read pixel colors
        WriteableBitmap? writeable = null;

        overlayWindow.Opened += (_, _) =>
        {
            // Convert screenshot to WriteableBitmap for pixel reading
            writeable = ConvertToWriteableBitmap(screenshot);
        };

        overlayWindow.PointerMoved += (_, e) =>
        {
            if (writeable == null) return;

            var pos = e.GetPosition(imageControl);
            var color = GetPixelColor(writeable, (int)pos.X, (int)pos.Y);
            if (color.HasValue)
            {
                ColorPreview?.Invoke(color.Value);
            }
        };

        overlayWindow.PointerPressed += (_, e) =>
        {
            if (writeable == null)
            {
                tcs.TrySetResult(null);
                overlayWindow.Close();
                return;
            }

            var pos = e.GetPosition(imageControl);
            var color = GetPixelColor(writeable, (int)pos.X, (int)pos.Y);
            tcs.TrySetResult(color);
            writeable.Dispose();
            overlayWindow.Close();
        };

        overlayWindow.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                tcs.TrySetResult(null);
                writeable?.Dispose();
                overlayWindow.Close();
            }
        };

        overlayWindow.Show();

        return await tcs.Task;
    }

    private static WriteableBitmap ConvertToWriteableBitmap(Bitmap source)
    {
        var size = source.PixelSize;
        var writeable = new WriteableBitmap(size, source.Dpi, Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul);

        using (var fb = writeable.Lock())
        {
            var rect = new Avalonia.PixelRect(0, 0, size.Width, size.Height);
            source.CopyPixels(rect, fb.Address, fb.RowBytes * size.Height, fb.RowBytes);
        }

        return writeable;
    }

    private static Color? GetPixelColor(WriteableBitmap bitmap, int x, int y)
    {
        var size = bitmap.PixelSize;
        if (x < 0 || x >= size.Width || y < 0 || y >= size.Height)
            return null;

        using var fb = bitmap.Lock();
        unsafe
        {
            var ptr = (byte*)fb.Address;
            int stride = fb.RowBytes;
            int offset = y * stride + x * 4; // BGRA8888

            byte b = ptr[offset];
            byte g = ptr[offset + 1];
            byte r = ptr[offset + 2];
            byte a = ptr[offset + 3];

            return Color.FromArgb(a == 0 ? (byte)255 : a, r, g, b);
        }
    }
}
