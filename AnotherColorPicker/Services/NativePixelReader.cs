using System;
using System.Runtime.InteropServices;
using Avalonia.Media;

namespace AnotherColorPicker.Services;

/// <summary>
/// Provides OS-native methods for reading the pixel color under the cursor directly from the screen.
/// </summary>
public static class NativePixelReader
{
    public static Color? GetColorUnderCursor()
    {
        try
        {
            if (OperatingSystem.IsWindows())
                return GetWindowsPixel();
            if (OperatingSystem.IsLinux())
                return GetLinuxPixel();
            if (OperatingSystem.IsMacOS())
                return GetMacPixel();
        }
        catch
        {
            // Ignore native interop failures
        }
        
        return null;
    }

    // ==========================================================
    // Windows Implementation
    // ==========================================================

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

    private static Color? GetWindowsPixel()
    {
        if (!GetCursorPos(out var p)) return null;

        IntPtr hdc = GetDC(IntPtr.Zero);
        if (hdc == IntPtr.Zero) return null;

        uint pixel = GetPixel(hdc, p.X, p.Y);
        ReleaseDC(IntPtr.Zero, hdc);

        if (pixel == 0xFFFFFFFF) // CLR_INVALID
            return null;

        byte r = (byte)(pixel & 0x000000FF);
        byte g = (byte)((pixel & 0x0000FF00) >> 8);
        byte b = (byte)((pixel & 0x00FF0000) >> 16);

        return Color.FromRgb(r, g, b);
    }

    // ==========================================================
    // Linux Implementation (X11)
    // ==========================================================

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern bool XQueryPointer(IntPtr display, IntPtr window, out IntPtr root, out IntPtr child, out int root_x, out int root_y, out int win_x, out int win_y, out uint mask);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XGetImage(IntPtr display, IntPtr drawable, int x, int y, uint width, uint height, ulong plane_mask, int format);

    [DllImport("libX11.so.6")]
    private static extern ulong XGetPixel(IntPtr image, int x, int y);

    [DllImport("libX11.so.6")]
    private static extern int XDestroyImage(IntPtr image);

    private static Color? GetLinuxPixel()
    {
        // Direct pixel reading on Wayland is not supported via X11 API
        if (Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null)
            return null;

        IntPtr display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero) return null;

        try
        {
            IntPtr root = XDefaultRootWindow(display);
            if (!XQueryPointer(display, root, out _, out _, out int rx, out int ry, out _, out _, out _))
            {
                return null;
            }

            IntPtr image = XGetImage(display, root, rx, ry, 1, 1, ~0ul, 2); // 2 = ZPixmap
            if (image == IntPtr.Zero)
            {
                return null;
            }

            ulong pixel = XGetPixel(image, 0, 0);

            byte b = (byte)(pixel & 0xFF);
            byte g = (byte)((pixel >> 8) & 0xFF);
            byte r = (byte)((pixel >> 16) & 0xFF);

            XDestroyImage(image);

            return Color.FromRgb(r, g, b);
        }
        finally
        {
            XCloseDisplay(display);
        }
    }

    // ==========================================================
    // macOS Implementation (CoreGraphics)
    // ==========================================================

    [StructLayout(LayoutKind.Sequential)]
    private struct CGPoint { public double X; public double Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect { public CGPoint Origin; public CGSize Size; }

    [StructLayout(LayoutKind.Sequential)]
    private struct CGSize { public double Width; public double Height; }

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGEventCreate(IntPtr source);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern CGPoint CGEventGetLocation(IntPtr @event);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGWindowListCreateImage(CGRect screenBounds, uint listOption, uint windowID, uint imageOption);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGImageGetDataProvider(IntPtr image);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGDataProviderCopyData(IntPtr provider);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFDataGetBytePtr(IntPtr data);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFDataGetLength(IntPtr data);

    private static Color? GetMacPixel()
    {
        IntPtr evt = CGEventCreate(IntPtr.Zero);
        if (evt == IntPtr.Zero) return null;

        CGPoint loc = CGEventGetLocation(evt);
        CFRelease(evt);

        CGRect rect = new CGRect
        {
            Origin = new CGPoint { X = loc.X, Y = loc.Y },
            Size = new CGSize { Width = 1, Height = 1 }
        };

        // kCGWindowListOptionOnScreenOnly = (1 << 0)
        // kCGNullWindowID = 0
        // kCGWindowImageDefault = 0
        IntPtr cgImage = CGWindowListCreateImage(rect, 1, 0, 0);
        if (cgImage == IntPtr.Zero) return null;

        IntPtr dataProvider = CGImageGetDataProvider(cgImage);
        IntPtr cfData = CGDataProviderCopyData(dataProvider);
        
        Color? result = null;
        if (cfData != IntPtr.Zero)
        {
            IntPtr bytePtr = CFDataGetBytePtr(cfData);
            nint length = CFDataGetLength(cfData);

            if (bytePtr != IntPtr.Zero && length >= 4)
            {
                unsafe
                {
                    byte* p = (byte*)bytePtr;
                    // Usually BGRA on Mac
                    byte b = p[0];
                    byte g = p[1];
                    byte r = p[2];
                    result = Color.FromRgb(r, g, b);
                }
            }
            CFRelease(cfData);
        }
        
        CGImageRelease(cgImage);
        return result;
    }

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGImageRelease(IntPtr image);
}
