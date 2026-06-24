using System;

namespace AnotherColorPicker.Models;

/// <summary>
/// Represents a color with multiple color space representations.
/// Provides bidirectional conversions between RGB, HSV, HSL, CMYK, and HEX.
/// </summary>
public class ColorModel : IEquatable<ColorModel>
{
    // --- RGB (0–255) ---
    public byte R { get; private set; }
    public byte G { get; private set; }
    public byte B { get; private set; }
    public byte A { get; private set; } = 255;

    // --- HSV (H: 0–360, S: 0–100, V: 0–100) ---
    public double HsvH { get; private set; }
    public double HsvS { get; private set; }
    public double HsvV { get; private set; }

    // --- HSL (H: 0–360, S: 0–100, L: 0–100) ---
    public double HslH { get; private set; }
    public double HslS { get; private set; }
    public double HslL { get; private set; }

    // --- CMYK (0–100) ---
    public double CmykC { get; private set; }
    public double CmykM { get; private set; }
    public double CmykY { get; private set; }
    public double CmykK { get; private set; }

    // --- HEX ---
    public string Hex => $"#{R:X2}{G:X2}{B:X2}";
    public string HexWithAlpha => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

    private ColorModel() { }

    /// <summary>
    /// Creates a ColorModel from RGB values (0–255).
    /// </summary>
    public static ColorModel FromRgb(byte r, byte g, byte b, byte a = 255)
    {
        var color = new ColorModel { R = r, G = g, B = b, A = a };
        color.RecalculateFromRgb();
        return color;
    }

    /// <summary>
    /// Creates a ColorModel from HSV values (H: 0–360, S: 0–100, V: 0–100).
    /// </summary>
    public static ColorModel FromHsv(double h, double s, double v, byte a = 255)
    {
        h = ClampAngle(h);
        s = Math.Clamp(s, 0, 100);
        v = Math.Clamp(v, 0, 100);

        var color = new ColorModel { HsvH = h, HsvS = s, HsvV = v, A = a };
        HsvToRgb(h, s, v, out var r, out var g, out var b);
        color.R = r;
        color.G = g;
        color.B = b;
        color.RecalculateHslFromRgb();
        color.RecalculateCmykFromRgb();
        return color;
    }

    /// <summary>
    /// Creates a ColorModel from HSL values (H: 0–360, S: 0–100, L: 0–100).
    /// </summary>
    public static ColorModel FromHsl(double h, double s, double l, byte a = 255)
    {
        h = ClampAngle(h);
        s = Math.Clamp(s, 0, 100);
        l = Math.Clamp(l, 0, 100);

        var color = new ColorModel { HslH = h, HslS = s, HslL = l, A = a };
        HslToRgb(h, s, l, out var r, out var g, out var b);
        color.R = r;
        color.G = g;
        color.B = b;
        color.RecalculateHsvFromRgb();
        color.RecalculateCmykFromRgb();
        return color;
    }

    /// <summary>
    /// Creates a ColorModel from CMYK values (0–100).
    /// </summary>
    public static ColorModel FromCmyk(double c, double m, double y, double k, byte a = 255)
    {
        c = Math.Clamp(c, 0, 100);
        m = Math.Clamp(m, 0, 100);
        y = Math.Clamp(y, 0, 100);
        k = Math.Clamp(k, 0, 100);

        var color = new ColorModel { CmykC = c, CmykM = m, CmykY = y, CmykK = k, A = a };
        CmykToRgb(c, m, y, k, out var r, out var g, out var b);
        color.R = r;
        color.G = g;
        color.B = b;
        color.RecalculateHsvFromRgb();
        color.RecalculateHslFromRgb();
        return color;
    }

    /// <summary>
    /// Creates a ColorModel from a HEX string (e.g., "#FF5733" or "FF5733").
    /// </summary>
    public static ColorModel FromHex(string hex)
    {
        hex = hex.TrimStart('#');

        byte a = 255, r, g, b;

        switch (hex.Length)
        {
            case 6:
                r = Convert.ToByte(hex[..2], 16);
                g = Convert.ToByte(hex[2..4], 16);
                b = Convert.ToByte(hex[4..6], 16);
                break;
            case 8:
                a = Convert.ToByte(hex[..2], 16);
                r = Convert.ToByte(hex[2..4], 16);
                g = Convert.ToByte(hex[4..6], 16);
                b = Convert.ToByte(hex[6..8], 16);
                break;
            case 3:
                r = Convert.ToByte(new string(hex[0], 2), 16);
                g = Convert.ToByte(new string(hex[1], 2), 16);
                b = Convert.ToByte(new string(hex[2], 2), 16);
                break;
            default:
                throw new ArgumentException($"Invalid HEX color string: #{hex}", nameof(hex));
        }

        return FromRgb(r, g, b, a);
    }

    // ========================
    // Conversion Core Methods
    // ========================

    private void RecalculateFromRgb()
    {
        RecalculateHsvFromRgb();
        RecalculateHslFromRgb();
        RecalculateCmykFromRgb();
    }

    private void RecalculateHsvFromRgb()
    {
        RgbToHsv(R, G, B, out var h, out var s, out var v);
        HsvH = h;
        HsvS = s;
        HsvV = v;
    }

    private void RecalculateHslFromRgb()
    {
        RgbToHsl(R, G, B, out var h, out var s, out var l);
        HslH = h;
        HslS = s;
        HslL = l;
    }

    private void RecalculateCmykFromRgb()
    {
        RgbToCmyk(R, G, B, out var c, out var m, out var y, out var k);
        CmykC = c;
        CmykM = m;
        CmykY = y;
        CmykK = k;
    }

    // ========================
    // Static Conversion Utils
    // ========================

    /// <summary>
    /// Converts RGB (0–255) to HSV (H: 0–360, S: 0–100, V: 0–100).
    /// </summary>
    public static void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
    {
        double rNorm = r / 255.0;
        double gNorm = g / 255.0;
        double bNorm = b / 255.0;

        double max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
        double min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
        double delta = max - min;

        // Hue
        if (delta == 0)
            h = 0;
        else if (max == rNorm)
            h = 60 * (((gNorm - bNorm) / delta) % 6);
        else if (max == gNorm)
            h = 60 * (((bNorm - rNorm) / delta) + 2);
        else
            h = 60 * (((rNorm - gNorm) / delta) + 4);

        if (h < 0) h += 360;

        // Saturation
        s = max == 0 ? 0 : (delta / max) * 100;

        // Value
        v = max * 100;

        h = Math.Round(h, 2);
        s = Math.Round(s, 2);
        v = Math.Round(v, 2);
    }

    /// <summary>
    /// Converts HSV (H: 0–360, S: 0–100, V: 0–100) to RGB (0–255).
    /// </summary>
    public static void HsvToRgb(double h, double s, double v, out byte r, out byte g, out byte b)
    {
        double sNorm = s / 100.0;
        double vNorm = v / 100.0;

        double c = vNorm * sNorm;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = vNorm - c;

        double rPrime, gPrime, bPrime;

        switch (h)
        {
            case < 60:
                rPrime = c; gPrime = x; bPrime = 0;
                break;
            case < 120:
                rPrime = x; gPrime = c; bPrime = 0;
                break;
            case < 180:
                rPrime = 0; gPrime = c; bPrime = x;
                break;
            case < 240:
                rPrime = 0; gPrime = x; bPrime = c;
                break;
            case < 300:
                rPrime = x; gPrime = 0; bPrime = c;
                break;
            default:
                rPrime = c; gPrime = 0; bPrime = x;
                break;
        }

        r = (byte)Math.Round((rPrime + m) * 255);
        g = (byte)Math.Round((gPrime + m) * 255);
        b = (byte)Math.Round((bPrime + m) * 255);
    }

    /// <summary>
    /// Converts RGB (0–255) to HSL (H: 0–360, S: 0–100, L: 0–100).
    /// </summary>
    public static void RgbToHsl(byte r, byte g, byte b, out double h, out double s, out double l)
    {
        double rNorm = r / 255.0;
        double gNorm = g / 255.0;
        double bNorm = b / 255.0;

        double max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
        double min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
        double delta = max - min;

        // Lightness
        l = (max + min) / 2.0;

        // Saturation
        if (delta == 0)
        {
            h = 0;
            s = 0;
        }
        else
        {
            s = delta / (1 - Math.Abs(2 * l - 1));

            if (max == rNorm)
                h = 60 * (((gNorm - bNorm) / delta) % 6);
            else if (max == gNorm)
                h = 60 * (((bNorm - rNorm) / delta) + 2);
            else
                h = 60 * (((rNorm - gNorm) / delta) + 4);

            if (h < 0) h += 360;
        }

        h = Math.Round(h, 2);
        s = Math.Round(s * 100, 2);
        l = Math.Round(l * 100, 2);
    }

    /// <summary>
    /// Converts HSL (H: 0–360, S: 0–100, L: 0–100) to RGB (0–255).
    /// </summary>
    public static void HslToRgb(double h, double s, double l, out byte r, out byte g, out byte b)
    {
        double sNorm = s / 100.0;
        double lNorm = l / 100.0;

        double c = (1 - Math.Abs(2 * lNorm - 1)) * sNorm;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = lNorm - c / 2;

        double rPrime, gPrime, bPrime;

        switch (h)
        {
            case < 60:
                rPrime = c; gPrime = x; bPrime = 0;
                break;
            case < 120:
                rPrime = x; gPrime = c; bPrime = 0;
                break;
            case < 180:
                rPrime = 0; gPrime = c; bPrime = x;
                break;
            case < 240:
                rPrime = 0; gPrime = x; bPrime = c;
                break;
            case < 300:
                rPrime = x; gPrime = 0; bPrime = c;
                break;
            default:
                rPrime = c; gPrime = 0; bPrime = x;
                break;
        }

        r = (byte)Math.Round((rPrime + m) * 255);
        g = (byte)Math.Round((gPrime + m) * 255);
        b = (byte)Math.Round((bPrime + m) * 255);
    }

    /// <summary>
    /// Converts RGB (0–255) to CMYK (0–100).
    /// </summary>
    public static void RgbToCmyk(byte r, byte g, byte b, out double c, out double m, out double y, out double k)
    {
        double rNorm = r / 255.0;
        double gNorm = g / 255.0;
        double bNorm = b / 255.0;

        k = 1 - Math.Max(rNorm, Math.Max(gNorm, bNorm));

        if (Math.Abs(k - 1) < 1e-10)
        {
            c = 0;
            m = 0;
            y = 0;
            k = 100;
            return;
        }

        c = Math.Round((1 - rNorm - k) / (1 - k) * 100, 2);
        m = Math.Round((1 - gNorm - k) / (1 - k) * 100, 2);
        y = Math.Round((1 - bNorm - k) / (1 - k) * 100, 2);
        k = Math.Round(k * 100, 2);
    }

    /// <summary>
    /// Converts CMYK (0–100) to RGB (0–255).
    /// </summary>
    public static void CmykToRgb(double c, double m, double y, double k, out byte r, out byte g, out byte b)
    {
        double cNorm = c / 100.0;
        double mNorm = m / 100.0;
        double yNorm = y / 100.0;
        double kNorm = k / 100.0;

        r = (byte)Math.Round(255 * (1 - cNorm) * (1 - kNorm));
        g = (byte)Math.Round(255 * (1 - mNorm) * (1 - kNorm));
        b = (byte)Math.Round(255 * (1 - yNorm) * (1 - kNorm));
    }

    /// <summary>
    /// Parses a HEX string to RGB components.
    /// </summary>
    public static bool TryParseHex(string hex, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        hex = hex.TrimStart('#');

        if (hex.Length == 6)
        {
            try
            {
                r = Convert.ToByte(hex[..2], 16);
                g = Convert.ToByte(hex[2..4], 16);
                b = Convert.ToByte(hex[4..6], 16);
                return true;
            }
            catch
            {
                return false;
            }
        }

        if (hex.Length == 3)
        {
            try
            {
                r = Convert.ToByte(new string(hex[0], 2), 16);
                g = Convert.ToByte(new string(hex[1], 2), 16);
                b = Convert.ToByte(new string(hex[2], 2), 16);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    // ========================
    // Helpers
    // ========================

    private static double ClampAngle(double angle)
    {
        angle %= 360;
        if (angle < 0) angle += 360;
        return angle;
    }

    // ========================
    // Equality
    // ========================

    public bool Equals(ColorModel? other)
    {
        if (other is null) return false;
        return R == other.R && G == other.G && B == other.B && A == other.A;
    }

    public override bool Equals(object? obj) => Equals(obj as ColorModel);

    public override int GetHashCode() => HashCode.Combine(R, G, B, A);

    public override string ToString() => $"ColorModel({Hex}, RGB({R},{G},{B}), A={A})";

    /// <summary>
    /// Creates a deep copy of this ColorModel.
    /// </summary>
    public ColorModel Clone() => FromRgb(R, G, B, A);
}
