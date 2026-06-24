using System;
using System.Collections.Generic;
using AnotherColorPicker.Models;

namespace AnotherColorPicker.Helpers;

/// <summary>
/// Generates color harmonies based on color theory relationships.
/// All methods return lists of ColorModel instances derived from a base color.
/// </summary>
public static class ColorHarmonyHelper
{
    /// <summary>
    /// Returns the complementary color (180° opposite on the color wheel).
    /// </summary>
    public static ColorModel GetComplementary(ColorModel baseColor)
    {
        double newH = (baseColor.HsvH + 180) % 360;
        return ColorModel.FromHsv(newH, baseColor.HsvS, baseColor.HsvV, baseColor.A);
    }

    /// <summary>
    /// Returns analogous colors (colors adjacent on the wheel, ±30° by default).
    /// </summary>
    public static List<ColorModel> GetAnalogous(ColorModel baseColor, double angle = 30)
    {
        return new List<ColorModel>
        {
            ColorModel.FromHsv((baseColor.HsvH - angle + 360) % 360, baseColor.HsvS, baseColor.HsvV, baseColor.A),
            baseColor.Clone(),
            ColorModel.FromHsv((baseColor.HsvH + angle) % 360, baseColor.HsvS, baseColor.HsvV, baseColor.A),
        };
    }

    /// <summary>
    /// Returns triadic colors (three colors equally spaced, 120° apart).
    /// </summary>
    public static List<ColorModel> GetTriadic(ColorModel baseColor)
    {
        return new List<ColorModel>
        {
            baseColor.Clone(),
            ColorModel.FromHsv((baseColor.HsvH + 120) % 360, baseColor.HsvS, baseColor.HsvV, baseColor.A),
            ColorModel.FromHsv((baseColor.HsvH + 240) % 360, baseColor.HsvS, baseColor.HsvV, baseColor.A),
        };
    }

    /// <summary>
    /// Returns tetradic (rectangle) colors (four colors, two complementary pairs at 60° and 180° offsets).
    /// </summary>
    public static List<ColorModel> GetTetradic(ColorModel baseColor)
    {
        return new List<ColorModel>
        {
            baseColor.Clone(),
            ColorModel.FromHsv((baseColor.HsvH + 90) % 360, baseColor.HsvS, baseColor.HsvV, baseColor.A),
            ColorModel.FromHsv((baseColor.HsvH + 180) % 360, baseColor.HsvS, baseColor.HsvV, baseColor.A),
            ColorModel.FromHsv((baseColor.HsvH + 270) % 360, baseColor.HsvS, baseColor.HsvV, baseColor.A),
        };
    }

    /// <summary>
    /// Returns split-complementary colors (base + two colors adjacent to its complement).
    /// </summary>
    public static List<ColorModel> GetSplitComplementary(ColorModel baseColor, double splitAngle = 30)
    {
        double comp = (baseColor.HsvH + 180) % 360;
        return new List<ColorModel>
        {
            baseColor.Clone(),
            ColorModel.FromHsv((comp - splitAngle + 360) % 360, baseColor.HsvS, baseColor.HsvV, baseColor.A),
            ColorModel.FromHsv((comp + splitAngle) % 360, baseColor.HsvS, baseColor.HsvV, baseColor.A),
        };
    }

    /// <summary>
    /// Returns monochromatic shades and tints by varying Value (brightness) in HSV.
    /// </summary>
    public static List<ColorModel> GetMonochromatic(ColorModel baseColor, int count = 5)
    {
        var results = new List<ColorModel>();
        if (count < 2) count = 2;

        for (int i = 0; i < count; i++)
        {
            double v = 100.0 * (i + 1) / (count + 1);
            results.Add(ColorModel.FromHsv(baseColor.HsvH, baseColor.HsvS, Math.Clamp(v, 0, 100), baseColor.A));
        }

        return results;
    }

    /// <summary>
    /// Returns shades of a color by reducing Value (darkening).
    /// </summary>
    public static List<ColorModel> GetShades(ColorModel baseColor, int count = 5)
    {
        var results = new List<ColorModel>();

        for (int i = 0; i < count; i++)
        {
            double factor = 1.0 - ((double)(i + 1) / (count + 1));
            double v = baseColor.HsvV * factor;
            results.Add(ColorModel.FromHsv(baseColor.HsvH, baseColor.HsvS, Math.Clamp(v, 0, 100), baseColor.A));
        }

        return results;
    }

    /// <summary>
    /// Returns tints of a color by reducing Saturation and increasing Value (lightening).
    /// </summary>
    public static List<ColorModel> GetTints(ColorModel baseColor, int count = 5)
    {
        var results = new List<ColorModel>();

        for (int i = 0; i < count; i++)
        {
            double factor = (double)(i + 1) / (count + 1);
            double s = baseColor.HsvS * (1.0 - factor);
            double v = baseColor.HsvV + (100 - baseColor.HsvV) * factor;
            results.Add(ColorModel.FromHsv(baseColor.HsvH, Math.Clamp(s, 0, 100), Math.Clamp(v, 0, 100), baseColor.A));
        }

        return results;
    }
}
