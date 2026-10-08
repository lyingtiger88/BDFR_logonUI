using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace BDFR.LogonUI.Demo;

public sealed class ColorWheelControl : FrameworkElement
{
    private enum DragTarget
    {
        None,
        Hue,
        SaturationValue
    }

    private DragTarget _dragTarget;
    private double _hue;
    private double _saturation = 1;
    private double _value = 1;

    public event EventHandler? SelectedColorChanged;

    public Color SelectedColor => HsvToColor(_hue, _saturation, _value);

    public void SetColor(Color color)
    {
        ColorToHsv(color, out _hue, out _saturation, out _value);
        InvalidateVisual();
        SelectedColorChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var side = Math.Min(
            double.IsInfinity(availableSize.Width) ? 340 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 340 : availableSize.Height);

        side = Math.Clamp(side, 220, 440);
        return new Size(side, side);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var side = Math.Min(ActualWidth, ActualHeight);
        if (side <= 0)
            return;

        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var outerRadius = side * .46;
        var ringThickness = Math.Max(18, side * .095);
        var ringRadius = outerRadius - ringThickness / 2;

        // Hue ring, drawn in small arc segments.
        const int segments = 180;
        for (var i = 0; i < segments; i++)
        {
            var a0 = i * 360d / segments;
            var a1 = (i + 1) * 360d / segments + .35;
            var color = HsvToColor(a0, 1, 1);
            DrawArc(
                dc,
                center,
                ringRadius,
                a0 - 90,
                a1 - a0,
                new Pen(new SolidColorBrush(color), ringThickness)
                {
                    StartLineCap = PenLineCap.Flat,
                    EndLineCap = PenLineCap.Flat
                });
        }

        // Inner saturation/value palette, circular like the supplied visual reference.
        var innerRadius = outerRadius - ringThickness - side * .045;
        var innerBounds = new Rect(
            center.X - innerRadius,
            center.Y - innerRadius,
            innerRadius * 2,
            innerRadius * 2);

        dc.PushClip(new EllipseGeometry(center, innerRadius, innerRadius));

        var hueBrush = new SolidColorBrush(HsvToColor(_hue, 1, 1));
        dc.DrawRectangle(hueBrush, null, innerBounds);

        var whiteOverlay = new LinearGradientBrush(
            Colors.White,
            Color.FromArgb(0, 255, 255, 255),
            new Point(0, .5),
            new Point(1, .5));
        dc.DrawRectangle(whiteOverlay, null, innerBounds);

        var blackOverlay = new LinearGradientBrush(
            Color.FromArgb(0, 0, 0, 0),
            Colors.Black,
            new Point(.5, 0),
            new Point(.5, 1));
        dc.DrawRectangle(blackOverlay, null, innerBounds);

        dc.Pop();

        // Inner boundary.
        dc.DrawEllipse(
            null,
            new Pen(new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)), 1),
            center,
            innerRadius,
            innerRadius);

        // Hue marker.
        var hueAngle = (_hue - 90) * Math.PI / 180d;
        var huePoint = new Point(
            center.X + Math.Cos(hueAngle) * ringRadius,
            center.Y + Math.Sin(hueAngle) * ringRadius);

        DrawMarker(dc, huePoint, 7);

        // Saturation/value marker.
        var svX = innerBounds.Left + _saturation * innerBounds.Width;
        var svY = innerBounds.Top + (1 - _value) * innerBounds.Height;
        var vector = new Vector(svX - center.X, svY - center.Y);
        if (vector.Length > innerRadius - 5)
        {
            vector.Normalize();
            vector *= innerRadius - 5;
            svX = center.X + vector.X;
            svY = center.Y + vector.Y;
        }

        DrawMarker(dc, new Point(svX, svY), 6);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        _dragTarget = HitTarget(e.GetPosition(this));
        if (_dragTarget == DragTarget.None)
            return;

        CaptureMouse();
        UpdateFromPoint(e.GetPosition(this), _dragTarget);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_dragTarget == DragTarget.None || e.LeftButton != MouseButtonState.Pressed)
            return;

        UpdateFromPoint(e.GetPosition(this), _dragTarget);
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_dragTarget == DragTarget.None)
            return;

        UpdateFromPoint(e.GetPosition(this), _dragTarget);
        _dragTarget = DragTarget.None;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    private DragTarget HitTarget(Point point)
    {
        var side = Math.Min(ActualWidth, ActualHeight);
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var outerRadius = side * .46;
        var ringThickness = Math.Max(18, side * .095);
        var innerRadius = outerRadius - ringThickness - side * .045;
        var distance = (point - center).Length;

        if (distance >= outerRadius - ringThickness - 5 && distance <= outerRadius + 5)
            return DragTarget.Hue;

        if (distance <= innerRadius + 2)
            return DragTarget.SaturationValue;

        return DragTarget.None;
    }

    private void UpdateFromPoint(Point point, DragTarget target)
    {
        var side = Math.Min(ActualWidth, ActualHeight);
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var outerRadius = side * .46;
        var ringThickness = Math.Max(18, side * .095);
        var innerRadius = outerRadius - ringThickness - side * .045;

        if (target == DragTarget.Hue)
        {
            var angle = Math.Atan2(point.Y - center.Y, point.X - center.X) * 180d / Math.PI + 90;
            if (angle < 0)
                angle += 360;
            _hue = angle % 360;
        }
        else
        {
            var bounds = new Rect(
                center.X - innerRadius,
                center.Y - innerRadius,
                innerRadius * 2,
                innerRadius * 2);

            _saturation = Math.Clamp((point.X - bounds.Left) / bounds.Width, 0, 1);
            _value = Math.Clamp(1 - (point.Y - bounds.Top) / bounds.Height, 0, 1);
        }

        InvalidateVisual();
        SelectedColorChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void DrawMarker(DrawingContext dc, Point point, double radius)
    {
        dc.DrawEllipse(
            null,
            new Pen(Brushes.White, 2.5),
            point,
            radius,
            radius);

        dc.DrawEllipse(
            null,
            new Pen(new SolidColorBrush(Color.FromArgb(190, 0, 0, 0)), 1),
            point,
            radius + 2,
            radius + 2);
    }

    public static Color HsvToColor(double hue, double saturation, double value)
    {
        hue = ((hue % 360) + 360) % 360;
        saturation = Math.Clamp(saturation, 0, 1);
        value = Math.Clamp(value, 0, 1);

        var c = value * saturation;
        var x = c * (1 - Math.Abs((hue / 60d) % 2 - 1));
        var m = value - c;

        var (r1, g1, b1) = hue switch
        {
            < 60 => (c, x, 0d),
            < 120 => (x, c, 0d),
            < 180 => (0d, c, x),
            < 240 => (0d, x, c),
            < 300 => (x, 0d, c),
            _ => (c, 0d, x)
        };

        return Color.FromRgb(
            (byte)Math.Round((r1 + m) * 255),
            (byte)Math.Round((g1 + m) * 255),
            (byte)Math.Round((b1 + m) * 255));
    }

    public static void ColorToHsv(Color color, out double hue, out double saturation, out double value)
    {
        var r = color.R / 255d;
        var g = color.G / 255d;
        var b = color.B / 255d;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        if (delta == 0)
            hue = 0;
        else if (max == r)
            hue = 60 * (((g - b) / delta) % 6);
        else if (max == g)
            hue = 60 * (((b - r) / delta) + 2);
        else
            hue = 60 * (((r - g) / delta) + 4);

        if (hue < 0)
            hue += 360;

        saturation = max == 0 ? 0 : delta / max;
        value = max;
    }

    private static void DrawArc(
        DrawingContext dc,
        Point center,
        double radius,
        double startAngle,
        double sweepAngle,
        Pen pen)
    {
        var start = PointOnCircle(center, radius, startAngle);
        var end = PointOnCircle(center, radius, startAngle + sweepAngle);

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(start, false, false);
            ctx.ArcTo(
                end,
                new Size(radius, radius),
                0,
                Math.Abs(sweepAngle) > 180,
                sweepAngle >= 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise,
                true,
                false);
        }

        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }

    private static Point PointOnCircle(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180d;
        return new Point(
            center.X + Math.Cos(radians) * radius,
            center.Y + Math.Sin(radians) * radius);
    }
}
