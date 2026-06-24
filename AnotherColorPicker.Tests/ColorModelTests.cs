using AnotherColorPicker.Models;

namespace AnotherColorPicker.Tests;

/// <summary>
/// Comprehensive tests for ColorModel conversions between RGB, HSV, HSL, CMYK, and HEX.
/// Tests roundtrip precision, boundary values, and known color values.
/// </summary>
public class ColorModelTests
{
    // ========================
    // RGB to HEX
    // ========================

    [Theory]
    [InlineData(0, 0, 0, "#000000")]
    [InlineData(255, 255, 255, "#FFFFFF")]
    [InlineData(255, 0, 0, "#FF0000")]
    [InlineData(0, 255, 0, "#00FF00")]
    [InlineData(0, 0, 255, "#0000FF")]
    [InlineData(108, 99, 255, "#6C63FF")]
    [InlineData(255, 87, 51, "#FF5733")]
    public void FromRgb_ProducesCorrectHex(byte r, byte g, byte b, string expectedHex)
    {
        var color = ColorModel.FromRgb(r, g, b);
        Assert.Equal(expectedHex, color.Hex);
    }

    // ========================
    // HEX to RGB
    // ========================

    [Theory]
    [InlineData("#000000", 0, 0, 0)]
    [InlineData("#FFFFFF", 255, 255, 255)]
    [InlineData("#FF0000", 255, 0, 0)]
    [InlineData("#00FF00", 0, 255, 0)]
    [InlineData("#0000FF", 0, 0, 255)]
    [InlineData("6C63FF", 108, 99, 255)]
    [InlineData("#ff5733", 255, 87, 51)]
    public void FromHex_ProducesCorrectRgb(string hex, byte expectedR, byte expectedG, byte expectedB)
    {
        var color = ColorModel.FromHex(hex);
        Assert.Equal(expectedR, color.R);
        Assert.Equal(expectedG, color.G);
        Assert.Equal(expectedB, color.B);
    }

    [Theory]
    [InlineData("#F00", 255, 0, 0)]
    [InlineData("0F0", 0, 255, 0)]
    [InlineData("#00F", 0, 0, 255)]
    public void FromHex_ShortFormat_ProducesCorrectRgb(string hex, byte expectedR, byte expectedG, byte expectedB)
    {
        var color = ColorModel.FromHex(hex);
        Assert.Equal(expectedR, color.R);
        Assert.Equal(expectedG, color.G);
        Assert.Equal(expectedB, color.B);
    }

    [Fact]
    public void FromHex_InvalidFormat_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() => ColorModel.FromHex("#GGGGGG"));
    }

    [Fact]
    public void FromHex_InvalidLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ColorModel.FromHex("#ABCDE"));
    }

    // ========================
    // RGB to HSV and back
    // ========================

    [Theory]
    [InlineData(255, 0, 0, 0, 100, 100)]       // Pure red
    [InlineData(0, 255, 0, 120, 100, 100)]      // Pure green
    [InlineData(0, 0, 255, 240, 100, 100)]      // Pure blue
    [InlineData(255, 255, 0, 60, 100, 100)]     // Yellow
    [InlineData(0, 255, 255, 180, 100, 100)]    // Cyan
    [InlineData(255, 0, 255, 300, 100, 100)]    // Magenta
    [InlineData(255, 255, 255, 0, 0, 100)]      // White
    [InlineData(0, 0, 0, 0, 0, 0)]              // Black
    [InlineData(128, 128, 128, 0, 0, 50.2)]     // Gray
    public void RgbToHsv_KnownColors(byte r, byte g, byte b, double expectedH, double expectedS, double expectedV)
    {
        ColorModel.RgbToHsv(r, g, b, out var h, out var s, out var v);
        Assert.Equal(expectedH, h, 1.0);  // 1 degree tolerance
        Assert.Equal(expectedS, s, 1.0);
        Assert.Equal(expectedV, v, 1.0);
    }

    [Theory]
    [InlineData(255, 0, 0)]
    [InlineData(0, 255, 0)]
    [InlineData(0, 0, 255)]
    [InlineData(128, 64, 32)]
    [InlineData(200, 100, 50)]
    [InlineData(0, 0, 0)]
    [InlineData(255, 255, 255)]
    [InlineData(123, 45, 67)]
    public void RgbToHsvToRgb_Roundtrip(byte r, byte g, byte b)
    {
        ColorModel.RgbToHsv(r, g, b, out var h, out var s, out var v);
        ColorModel.HsvToRgb(h, s, v, out var rBack, out var gBack, out var bBack);

        Assert.True(Math.Abs(r - rBack) <= 1, $"R: expected {r}, got {rBack}");
        Assert.True(Math.Abs(g - gBack) <= 1, $"G: expected {g}, got {gBack}");
        Assert.True(Math.Abs(b - bBack) <= 1, $"B: expected {b}, got {bBack}");
    }

    // ========================
    // RGB to HSL and back
    // ========================

    [Theory]
    [InlineData(255, 0, 0, 0, 100, 50)]       // Pure red
    [InlineData(0, 255, 0, 120, 100, 50)]      // Pure green
    [InlineData(0, 0, 255, 240, 100, 50)]      // Pure blue
    [InlineData(255, 255, 255, 0, 0, 100)]     // White
    [InlineData(0, 0, 0, 0, 0, 0)]             // Black
    public void RgbToHsl_KnownColors(byte r, byte g, byte b, double expectedH, double expectedS, double expectedL)
    {
        ColorModel.RgbToHsl(r, g, b, out var h, out var s, out var l);
        Assert.Equal(expectedH, h, 1.0);
        Assert.Equal(expectedS, s, 1.0);
        Assert.Equal(expectedL, l, 1.0);
    }

    [Theory]
    [InlineData(255, 0, 0)]
    [InlineData(0, 255, 0)]
    [InlineData(0, 0, 255)]
    [InlineData(128, 64, 32)]
    [InlineData(200, 100, 50)]
    [InlineData(0, 0, 0)]
    [InlineData(255, 255, 255)]
    [InlineData(42, 170, 220)]
    public void RgbToHslToRgb_Roundtrip(byte r, byte g, byte b)
    {
        ColorModel.RgbToHsl(r, g, b, out var h, out var s, out var l);
        ColorModel.HslToRgb(h, s, l, out var rBack, out var gBack, out var bBack);

        Assert.True(Math.Abs(r - rBack) <= 1, $"R: expected {r}, got {rBack}");
        Assert.True(Math.Abs(g - gBack) <= 1, $"G: expected {g}, got {gBack}");
        Assert.True(Math.Abs(b - bBack) <= 1, $"B: expected {b}, got {bBack}");
    }

    // ========================
    // RGB to CMYK and back
    // ========================

    [Theory]
    [InlineData(255, 0, 0, 0, 100, 100, 0)]      // Pure red
    [InlineData(0, 255, 0, 100, 0, 100, 0)]       // Pure green
    [InlineData(0, 0, 255, 100, 100, 0, 0)]       // Pure blue
    [InlineData(255, 255, 255, 0, 0, 0, 0)]       // White
    [InlineData(0, 0, 0, 0, 0, 0, 100)]           // Black
    public void RgbToCmyk_KnownColors(byte r, byte g, byte b, double expectedC, double expectedM, double expectedY, double expectedK)
    {
        ColorModel.RgbToCmyk(r, g, b, out var c, out var m, out var y, out var k);
        Assert.Equal(expectedC, c, 1.0);
        Assert.Equal(expectedM, m, 1.0);
        Assert.Equal(expectedY, y, 1.0);
        Assert.Equal(expectedK, k, 1.0);
    }

    [Theory]
    [InlineData(255, 0, 0)]
    [InlineData(0, 255, 0)]
    [InlineData(0, 0, 255)]
    [InlineData(128, 64, 32)]
    [InlineData(200, 100, 50)]
    [InlineData(255, 255, 255)]
    [InlineData(100, 150, 200)]
    public void RgbToCmykToRgb_Roundtrip(byte r, byte g, byte b)
    {
        ColorModel.RgbToCmyk(r, g, b, out var c, out var m, out var y, out var k);
        ColorModel.CmykToRgb(c, m, y, k, out var rBack, out var gBack, out var bBack);

        Assert.True(Math.Abs(r - rBack) <= 1, $"R: expected {r}, got {rBack}");
        Assert.True(Math.Abs(g - gBack) <= 1, $"G: expected {g}, got {gBack}");
        Assert.True(Math.Abs(b - bBack) <= 1, $"B: expected {b}, got {bBack}");
    }

    // Black is a special case for CMYK
    [Fact]
    public void RgbToCmykToRgb_Black()
    {
        ColorModel.RgbToCmyk(0, 0, 0, out var c, out var m, out var y, out var k);
        Assert.Equal(100, k, 0.1);
        ColorModel.CmykToRgb(c, m, y, k, out var rBack, out var gBack, out var bBack);
        Assert.Equal(0, rBack);
        Assert.Equal(0, gBack);
        Assert.Equal(0, bBack);
    }

    // ========================
    // ColorModel Factory Cross-Consistency
    // ========================

    [Fact]
    public void FromRgb_AllSpacesConsistent()
    {
        var color = ColorModel.FromRgb(108, 99, 255);

        // All should be populated
        Assert.Equal(108, color.R);
        Assert.Equal(99, color.G);
        Assert.Equal(255, color.B);
        Assert.Equal("#6C63FF", color.Hex);

        // HSV, HSL, CMYK should all be non-zero for this color
        Assert.True(color.HsvH > 0);
        Assert.True(color.HsvS > 0);
        Assert.True(color.HsvV > 0);
        Assert.True(color.HslH > 0);
        Assert.True(color.HslS > 0);
        Assert.True(color.HslL > 0);
    }

    [Fact]
    public void FromHsv_RgbConsistent()
    {
        var color = ColorModel.FromHsv(0, 100, 100); // Pure red
        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal("#FF0000", color.Hex);
    }

    [Fact]
    public void FromHsl_RgbConsistent()
    {
        var color = ColorModel.FromHsl(120, 100, 50); // Pure green
        Assert.Equal(0, color.R);
        Assert.Equal(255, color.G);
        Assert.Equal(0, color.B);
        Assert.Equal("#00FF00", color.Hex);
    }

    [Fact]
    public void FromCmyk_RgbConsistent()
    {
        var color = ColorModel.FromCmyk(100, 100, 0, 0); // Pure blue
        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(255, color.B);
        Assert.Equal("#0000FF", color.Hex);
    }

    // ========================
    // TryParseHex
    // ========================

    [Theory]
    [InlineData("#FF5733", true, 255, 87, 51)]
    [InlineData("FF5733", true, 255, 87, 51)]
    [InlineData("#F00", true, 255, 0, 0)]
    [InlineData("GGGGGG", false, 0, 0, 0)]
    [InlineData("#ABCDE", false, 0, 0, 0)]
    public void TryParseHex_VariousInputs(string hex, bool expectedSuccess, byte expectedR, byte expectedG, byte expectedB)
    {
        bool result = ColorModel.TryParseHex(hex, out var r, out var g, out var b);
        Assert.Equal(expectedSuccess, result);
        if (expectedSuccess)
        {
            Assert.Equal(expectedR, r);
            Assert.Equal(expectedG, g);
            Assert.Equal(expectedB, b);
        }
    }

    // ========================
    // Equality and Clone
    // ========================

    [Fact]
    public void Equality_SameColor()
    {
        var a = ColorModel.FromRgb(100, 150, 200);
        var b = ColorModel.FromRgb(100, 150, 200);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentColor()
    {
        var a = ColorModel.FromRgb(100, 150, 200);
        var b = ColorModel.FromRgb(100, 150, 201);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Clone_ProducesEqualCopy()
    {
        var original = ColorModel.FromRgb(42, 170, 220);
        var clone = original.Clone();
        Assert.Equal(original, clone);
        Assert.NotSame(original, clone);
    }

    // ========================
    // Boundary Values
    // ========================

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 0, 100)]
    [InlineData(0, 100, 0)]
    [InlineData(0, 100, 100)]
    [InlineData(360, 0, 0)]
    [InlineData(360, 100, 100)]
    [InlineData(180, 50, 50)]
    public void FromHsv_BoundaryValues_NoCrash(double h, double s, double v)
    {
        var color = ColorModel.FromHsv(h, s, v);
        Assert.NotNull(color);
        Assert.NotEmpty(color.Hex);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 0, 100)]
    [InlineData(0, 100, 0)]
    [InlineData(0, 100, 50)]
    [InlineData(360, 100, 50)]
    public void FromHsl_BoundaryValues_NoCrash(double h, double s, double l)
    {
        var color = ColorModel.FromHsl(h, s, l);
        Assert.NotNull(color);
        Assert.NotEmpty(color.Hex);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(100, 100, 100, 0)]
    [InlineData(0, 0, 0, 100)]
    [InlineData(50, 50, 50, 50)]
    public void FromCmyk_BoundaryValues_NoCrash(double c, double m, double y, double k)
    {
        var color = ColorModel.FromCmyk(c, m, y, k);
        Assert.NotNull(color);
        Assert.NotEmpty(color.Hex);
    }
}
