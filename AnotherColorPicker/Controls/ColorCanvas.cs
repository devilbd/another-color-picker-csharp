using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AnotherColorPicker.Controls;

/// <summary>
/// A custom Saturation-Value canvas control for color picking.
/// Displays a gradient from white (left) to the full-saturation hue (right),
/// and from the color (top) to black (bottom).
/// The user can click/drag to select a Saturation and Value.
/// </summary>
public class ColorCanvas : Control
{
    // ========================
    // Dependency Properties
    // ========================

    public static readonly StyledProperty<double> HueProperty =
        AvaloniaProperty.Register<ColorCanvas, double>(nameof(Hue), 0);

    public static readonly StyledProperty<double> SaturationProperty =
        AvaloniaProperty.Register<ColorCanvas, double>(nameof(Saturation), 100);

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ColorCanvas, double>(nameof(Value), 100);

    /// <summary>
    /// The hue angle (0-360) to display on the canvas background.
    /// </summary>
    public double Hue
    {
        get => GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    /// <summary>
    /// The selected saturation (0-100), mapped to X position.
    /// </summary>
    public double Saturation
    {
        get => GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    /// <summary>
    /// The selected value/brightness (0-100), mapped to Y position (inverted: top=100, bottom=0).
    /// </summary>
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private bool _isDragging;

    public ColorCanvas()
    {
        ClipToBounds = true;
    }

    static ColorCanvas()
    {
        AffectsRender<ColorCanvas>(HueProperty, SaturationProperty, ValueProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        var w = bounds.Width;
        var h = bounds.Height;

        if (w <= 0 || h <= 0) return;

        // 1. Draw the hue background
        Models.ColorModel.HsvToRgb(Hue, 100, 100, out var hr, out var hg, out var hb);
        var hueColor = Color.FromRgb(hr, hg, hb);
        context.FillRectangle(new SolidColorBrush(hueColor), new Rect(0, 0, w, h));

        // 2. Overlay: white-to-transparent gradient (left to right = saturation)
        var whiteBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Colors.White, 0),
                new GradientStop(Color.FromArgb(0, 255, 255, 255), 1),
            }
        };
        context.FillRectangle(whiteBrush, new Rect(0, 0, w, h));

        // 3. Overlay: transparent-to-black gradient (top to bottom = value)
        var blackBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(0, 0, 0, 0), 0),
                new GradientStop(Colors.Black, 1),
            }
        };
        context.FillRectangle(blackBrush, new Rect(0, 0, w, h));

        // 4. Draw the selector circle at (Saturation, Value)
        double cx = (Saturation / 100.0) * w;
        double cy = (1.0 - Value / 100.0) * h;
        double radius = 7;

        // Outer circle (white border)
        var outerPen = new Pen(Brushes.White, 2.5);
        context.DrawEllipse(null, outerPen, new Point(cx, cy), radius, radius);

        // Inner circle (dark border for contrast)
        var innerPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)), 1);
        context.DrawEllipse(null, innerPen, new Point(cx, cy), radius - 1.5, radius - 1.5);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isDragging = true;
        e.Pointer.Capture(this);
        UpdateFromPointer(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging)
        {
            UpdateFromPointer(e);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
        e.Pointer.Capture(null);
    }

    private void UpdateFromPointer(PointerEventArgs e)
    {
        var pos = e.GetPosition(this);
        var w = Bounds.Width;
        var h = Bounds.Height;

        if (w <= 0 || h <= 0) return;

        Saturation = Math.Clamp(pos.X / w * 100, 0, 100);
        Value = Math.Clamp((1.0 - pos.Y / h) * 100, 0, 100);
    }
}
