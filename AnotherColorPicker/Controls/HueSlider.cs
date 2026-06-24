using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AnotherColorPicker.Controls;

/// <summary>
/// A horizontal hue slider control that displays the full 0-360° hue spectrum
/// and allows the user to select a hue value by clicking/dragging.
/// </summary>
public class HueSlider : Control
{
    public static readonly StyledProperty<double> HueProperty =
        AvaloniaProperty.Register<HueSlider, double>(nameof(Hue), 0);

    /// <summary>
    /// The selected hue value (0-360).
    /// </summary>
    public double Hue
    {
        get => GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    private bool _isDragging;

    public HueSlider()
    {
        ClipToBounds = true;
        Height = 20;
    }

    static HueSlider()
    {
        AffectsRender<HueSlider>(HueProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        var w = bounds.Width;
        var h = bounds.Height;

        if (w <= 0 || h <= 0) return;

        // Draw hue rainbow gradient
        var stops = new GradientStops();
        for (int i = 0; i <= 6; i++)
        {
            double hue = i * 60;
            Models.ColorModel.HsvToRgb(hue, 100, 100, out var r, out var g, out var b);
            stops.Add(new GradientStop(Color.FromRgb(r, g, b), i / 6.0));
        }

        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops = stops,
        };

        // Draw rounded rectangle for the hue bar
        var geo = new RectangleGeometry(new Rect(0, 0, w, h)) { RadiusX = h / 2, RadiusY = h / 2 };
        context.DrawGeometry(brush, null, geo);

        // Draw the thumb indicator
        double thumbX = (Hue / 360.0) * w;
        double thumbRadius = h / 2.0 + 1;

        // White circle with shadow
        var shadowPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), 2);
        context.DrawEllipse(Brushes.White, shadowPen, new Point(thumbX, h / 2.0), thumbRadius, thumbRadius);

        // Inner circle showing current hue color
        Models.ColorModel.HsvToRgb(Hue, 100, 100, out var hr, out var hg, out var hb);
        var hueColor = Color.FromRgb(hr, hg, hb);
        context.DrawEllipse(new SolidColorBrush(hueColor), null, new Point(thumbX, h / 2.0), thumbRadius - 3, thumbRadius - 3);
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
        if (w <= 0) return;

        Hue = Math.Clamp(pos.X / w * 360, 0, 360);
    }
}
