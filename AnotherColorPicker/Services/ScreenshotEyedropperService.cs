using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
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

    private const int MagnifierRadius = 5;
    private const int MagnifierSize = 11; // 2 * Radius + 1
    private const int ZoomScale = 10;

    public async Task<Color?> PickColorAsync()
    {
        // Step 1: Capture screenshot using DBus or platform tools
        var screenshotPath = await CaptureScreenshotAsync();

        if (string.IsNullOrEmpty(screenshotPath) || !File.Exists(screenshotPath))
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
            // Clean up file to prevent cluttering the user's Pictures or temp folder
            try { File.Delete(screenshotPath); } catch { /* ignore */ }
        }
    }

    private static async Task<string?> CaptureScreenshotDbusAsync()
    {
        try
        {
            using var connection = new Tmds.DBus.Connection(Tmds.DBus.Address.Session);
            await connection.ConnectAsync();

            var portal = connection.CreateProxy<IScreenshotPortal>("org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop");
            
            var options = new System.Collections.Generic.Dictionary<string, object>
            {
                { "interactive", false }
            };

            var requestPath = await portal.ScreenshotAsync("", options);
            var request = connection.CreateProxy<IRequest>("org.freedesktop.portal.Desktop", requestPath);
            
            var tcs = new TaskCompletionSource<string?>();
            
            IDisposable? watcher = null;
            watcher = await request.WatchResponseAsync(
                args => 
                {
                    try
                    {
                        if (args.response == 0 && args.results != null && args.results.TryGetValue("uri", out var uriObj))
                        {
                            var uri = uriObj.ToString();
                            if (uri != null && uri.StartsWith("file://"))
                            {
                                tcs.TrySetResult(Uri.UnescapeDataString(uri.Substring(7)));
                            }
                            else
                            {
                                tcs.TrySetResult(null);
                            }
                        }
                        else
                        {
                            tcs.TrySetResult(null);
                        }
                    }
                    catch
                    {
                        tcs.TrySetResult(null);
                    }
                    finally
                    {
                        watcher?.Dispose();
                    }
                },
                onError: ex => 
                {
                    tcs.TrySetResult(null);
                    watcher?.Dispose();
                });
                
            return await tcs.Task;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Captures a screenshot using DBus portal or available Linux tools.
    /// </summary>
    private static async Task<string?> CaptureScreenshotAsync()
    {
        if (Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null)
        {
            var dbusPath = await CaptureScreenshotDbusAsync();
            if (!string.IsNullOrEmpty(dbusPath) && File.Exists(dbusPath))
            {
                return dbusPath;
            }
        }

        var outputPath = Path.Combine(Path.GetTempPath(), $"acp_screenshot_{Guid.NewGuid()}.png");

        // Try multiple screenshot tools in order of preference
        var tools = new (string command, string args)[]
        {
            ("gnome-screenshot", $"-f \"{outputPath}\""),
            ("grim", $"\"{outputPath}\""),
            ("scrot", $"\"{outputPath}\""),
            ("import", $"-window root \"{outputPath}\""),
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
                    return outputPath;
                }
            }
            catch
            {
                // Tool not available, try next
            }
        }

        return null;
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

        var rootGrid = new Grid();

        var imageControl = new Avalonia.Controls.Image
        {
            Source = screenshot,
            Stretch = Stretch.Fill,
        };
        rootGrid.Children.Add(imageControl);

        var canvas = new Canvas();
        rootGrid.Children.Add(canvas);

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

        overlayWindow.Content = rootGrid;

        var magnifierWriteable = new WriteableBitmap(
            new PixelSize(MagnifierSize, MagnifierSize),
            new Vector(96, 96),
            Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Premul);
        magnifierImage.Source = magnifierWriteable;

        // Use WriteableBitmap to read pixel colors
        WriteableBitmap? writeable = null;

        Action<Point> updateMagnifier = (pos) =>
        {
            if (writeable == null || imageControl.Bounds.Width == 0) return;
            
            double scaleX = writeable.PixelSize.Width / imageControl.Bounds.Width;
            double scaleY = writeable.PixelSize.Height / imageControl.Bounds.Height;

            var centerX = (int)(pos.X * scaleX);
            var centerY = (int)(pos.Y * scaleY);

            Color? centerColor = null;

            using (var srcFb = writeable.Lock())
            using (var dstFb = magnifierWriteable.Lock())
            {
                unsafe
                {
                    byte* srcPtr = (byte*)srcFb.Address;
                    byte* dstPtr = (byte*)dstFb.Address;

                    int srcWidth = writeable.PixelSize.Width;
                    int srcHeight = writeable.PixelSize.Height;
                    int srcStride = srcFb.RowBytes;
                    int dstStride = dstFb.RowBytes;

                    for (int y = 0; y < MagnifierSize; y++)
                    {
                        int sy = centerY - MagnifierRadius + y;
                        for (int x = 0; x < MagnifierSize; x++)
                        {
                            int sx = centerX - MagnifierRadius + x;

                            int dstOffset = y * dstStride + x * 4;

                            if (sx >= 0 && sx < srcWidth && sy >= 0 && sy < srcHeight)
                            {
                                int srcOffset = sy * srcStride + sx * 4;
                                byte b = srcPtr[srcOffset];
                                byte g = srcPtr[srcOffset + 1];
                                byte r = srcPtr[srcOffset + 2];
                                byte a = srcPtr[srcOffset + 3];

                                dstPtr[dstOffset] = b;
                                dstPtr[dstOffset + 1] = g;
                                dstPtr[dstOffset + 2] = r;
                                dstPtr[dstOffset + 3] = a;

                                if (x == MagnifierRadius && y == MagnifierRadius)
                                {
                                    centerColor = Color.FromArgb(a == 0 ? (byte)255 : a, r, g, b);
                                }
                            }
                            else
                            {
                                dstPtr[dstOffset] = 0;
                                dstPtr[dstOffset + 1] = 0;
                                dstPtr[dstOffset + 2] = 0;
                                dstPtr[dstOffset + 3] = 255;
                            }
                        }
                    }
                }
            }

            magnifierBorder.IsVisible = true;
            if (centerColor.HasValue)
            {
                hexText.Text = $"#{centerColor.Value.R:X2}{centerColor.Value.G:X2}{centerColor.Value.B:X2}";
                ColorPreview?.Invoke(centerColor.Value);
            }

            Canvas.SetLeft(magnifierBorder, pos.X + 20);
            Canvas.SetTop(magnifierBorder, pos.Y + 20);
        };

        overlayWindow.Opened += (_, _) =>
        {
            // Convert screenshot to WriteableBitmap for pixel reading
            writeable = ConvertToWriteableBitmap(screenshot);
        };

        overlayWindow.PointerMoved += (_, e) =>
        {
            var pos = e.GetPosition(imageControl);
            updateMagnifier(pos);
        };

        overlayWindow.PointerPressed += (_, e) =>
        {
            if (writeable == null || imageControl.Bounds.Width == 0)
            {
                tcs.TrySetResult(null);
                magnifierWriteable.Dispose();
                overlayWindow.Close();
                return;
            }

            double scaleX = writeable.PixelSize.Width / imageControl.Bounds.Width;
            double scaleY = writeable.PixelSize.Height / imageControl.Bounds.Height;

            var pos = e.GetPosition(imageControl);
            var color = GetPixelColor(writeable, (int)(pos.X * scaleX), (int)(pos.Y * scaleY));
            tcs.TrySetResult(color);
            writeable.Dispose();
            magnifierWriteable.Dispose();
            overlayWindow.Close();
        };

        overlayWindow.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                tcs.TrySetResult(null);
                writeable?.Dispose();
                magnifierWriteable.Dispose();
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
