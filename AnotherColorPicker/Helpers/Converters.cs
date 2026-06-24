using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AnotherColorPicker.Helpers;

/// <summary>
/// Converts a HEX color string (e.g., "#FF5733") to an Avalonia SolidColorBrush.
/// </summary>
public class HexToBrushConverter : IValueConverter
{
    public static readonly HexToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                hex = hex.TrimStart('#');
                if (hex.Length == 6)
                {
                    var r = System.Convert.ToByte(hex[..2], 16);
                    var g = System.Convert.ToByte(hex[2..4], 16);
                    var b = System.Convert.ToByte(hex[4..6], 16);
                    return new SolidColorBrush(Color.FromRgb(r, g, b));
                }
            }
            catch
            {
                // Fall through
            }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts a boolean to an opacity value (true = 1.0, false = 0.0).
/// Used for toast visibility animation.
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public static readonly BoolToOpacityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? 1.0 : 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
