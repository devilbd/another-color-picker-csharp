using AnotherColorPicker.Helpers;
using AnotherColorPicker.Models;

namespace AnotherColorPicker.Tests;

/// <summary>
/// Tests for ColorHarmonyHelper to validate color harmony generation.
/// </summary>
public class ColorHarmonyTests
{
    private readonly ColorModel _baseColor = ColorModel.FromHsv(120, 80, 90);

    [Fact]
    public void GetComplementary_Returns180DegreeOpposite()
    {
        var comp = ColorHarmonyHelper.GetComplementary(_baseColor);
        var expectedHue = (_baseColor.HsvH + 180) % 360;
        Assert.Equal(expectedHue, comp.HsvH, 1.0);
    }

    [Fact]
    public void GetAnalogous_ReturnsThreeColors()
    {
        var colors = ColorHarmonyHelper.GetAnalogous(_baseColor);
        Assert.Equal(3, colors.Count);
    }

    [Fact]
    public void GetAnalogous_ColorsAre30DegreeApart()
    {
        var colors = ColorHarmonyHelper.GetAnalogous(_baseColor, 30);
        var expectedH0 = (_baseColor.HsvH - 30 + 360) % 360;
        var expectedH2 = (_baseColor.HsvH + 30) % 360;

        Assert.Equal(expectedH0, colors[0].HsvH, 1.0);
        Assert.Equal(expectedH2, colors[2].HsvH, 1.0);
    }

    [Fact]
    public void GetTriadic_ReturnsThreeColors()
    {
        var colors = ColorHarmonyHelper.GetTriadic(_baseColor);
        Assert.Equal(3, colors.Count);
    }

    [Fact]
    public void GetTriadic_ColorsAre120DegreeApart()
    {
        var colors = ColorHarmonyHelper.GetTriadic(_baseColor);
        Assert.Equal((_baseColor.HsvH + 120) % 360, colors[1].HsvH, 1.0);
        Assert.Equal((_baseColor.HsvH + 240) % 360, colors[2].HsvH, 1.0);
    }

    [Fact]
    public void GetTetradic_ReturnsFourColors()
    {
        var colors = ColorHarmonyHelper.GetTetradic(_baseColor);
        Assert.Equal(4, colors.Count);
    }

    [Fact]
    public void GetTetradic_ColorsAre90DegreeApart()
    {
        var colors = ColorHarmonyHelper.GetTetradic(_baseColor);
        Assert.Equal((_baseColor.HsvH + 90) % 360, colors[1].HsvH, 1.0);
        Assert.Equal((_baseColor.HsvH + 180) % 360, colors[2].HsvH, 1.0);
        Assert.Equal((_baseColor.HsvH + 270) % 360, colors[3].HsvH, 1.0);
    }

    [Fact]
    public void GetSplitComplementary_ReturnsThreeColors()
    {
        var colors = ColorHarmonyHelper.GetSplitComplementary(_baseColor);
        Assert.Equal(3, colors.Count);
    }

    [Fact]
    public void GetMonochromatic_ReturnsRequestedCount()
    {
        var colors = ColorHarmonyHelper.GetMonochromatic(_baseColor, 7);
        Assert.Equal(7, colors.Count);
    }

    [Fact]
    public void GetMonochromatic_AllSameHue()
    {
        var colors = ColorHarmonyHelper.GetMonochromatic(_baseColor, 5);
        foreach (var c in colors)
        {
            Assert.Equal(_baseColor.HsvH, c.HsvH, 1.0);
        }
    }

    [Fact]
    public void GetShades_ReturnsRequestedCount()
    {
        var colors = ColorHarmonyHelper.GetShades(_baseColor, 5);
        Assert.Equal(5, colors.Count);
    }

    [Fact]
    public void GetShades_AllSameHue()
    {
        var colors = ColorHarmonyHelper.GetShades(_baseColor, 5);
        foreach (var c in colors)
        {
            Assert.Equal(_baseColor.HsvH, c.HsvH, 1.0);
        }
    }

    [Fact]
    public void GetTints_ReturnsRequestedCount()
    {
        var colors = ColorHarmonyHelper.GetTints(_baseColor, 5);
        Assert.Equal(5, colors.Count);
    }

    [Fact]
    public void GetTints_AllSameHue()
    {
        var colors = ColorHarmonyHelper.GetTints(_baseColor, 5);
        foreach (var c in colors)
        {
            Assert.Equal(_baseColor.HsvH, c.HsvH, 1.0);
        }
    }

    // Edge cases
    [Fact]
    public void GetComplementary_RedGivesCorrectComplement()
    {
        var red = ColorModel.FromHsv(0, 100, 100);
        var comp = ColorHarmonyHelper.GetComplementary(red);
        Assert.Equal(180, comp.HsvH, 1.0); // Cyan
    }

    [Fact]
    public void GetComplementary_AtBoundary_WrapsCorrectly()
    {
        var color = ColorModel.FromHsv(350, 100, 100);
        var comp = ColorHarmonyHelper.GetComplementary(color);
        Assert.Equal(170, comp.HsvH, 1.0);
    }
}
