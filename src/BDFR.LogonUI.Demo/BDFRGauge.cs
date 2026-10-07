using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BDFR.LogonUI.Demo;

public sealed class BDFRGauge : FrameworkElement
{
    private const double ReferenceWidth = 170d;
    private const double ReferenceHeight = 145d;

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(BDFRGauge),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(BDFRGauge),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty IsAvailableProperty =
        DependencyProperty.Register(nameof(IsAvailable), typeof(bool), typeof(BDFRGauge),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool IsAvailable
    {
        get => (bool)GetValue(IsAvailableProperty);
        set => SetValue(IsAvailableProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? ReferenceWidth : Math.Max(72, availableSize.Width);
        var height = double.IsInfinity(availableSize.Height) ? ReferenceHeight : Math.Max(64, availableSize.Height);
        return new Size(width, height);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var width = Math.Max(1, ActualWidth);
        var height = Math.Max(1, ActualHeight);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        var scale = Math.Clamp(
            Math.Min(width / ReferenceWidth, height / ReferenceHeight),
            .38,
            3.25);

        var radius = Math.Max(22 * scale, Math.Min(width * .30, height * .36));
        var center = new Point(width / 2, height * .52);

        const double start = 145;
        const double sweep = 250;

        var arcThickness = Math.Clamp(10 * scale, 3.2, 24);
        var markerThickness = Math.Clamp(4 * scale, 1.7, 9);
        var markerHalfLength = Math.Clamp(9 * scale, 4, 22);
        var tickOffset = Math.Clamp(21 * scale, 9, 42);
        var tickFontSize = Math.Clamp(10 * scale, 6.5, 24);
        var labelFontSize = Math.Clamp(12 * scale, 7.5, 28);

        var trackPen = NewPen(Color.FromArgb(80, 230, 236, 244), arcThickness);
        var greenPen = NewPen(Color.FromRgb(91, 221, 165), arcThickness);
        var orangePen = NewPen(Color.FromRgb(255, 165, 76), arcThickness);
        var redPen = NewPen(Color.FromRgb(255, 91, 76), arcThickness);

        DrawArc(dc, center, radius, start, sweep, trackPen);
        DrawArc(dc, center, radius, start, sweep * .60, greenPen);
        DrawArc(dc, center, radius, start + sweep * .60, sweep * .20, orangePen);
        DrawArc(dc, center, radius, start + sweep * .80, sweep * .20, redPen);

        if (width >= 78 && height >= 68)
        {
            for (var tick = 0; tick <= 100; tick += 20)
            {
                var angle = start + sweep * (tick / 100d);
                var p = PointOnCircle(center, radius + tickOffset, angle);
                var ft = Text(tick.ToString(CultureInfo.InvariantCulture), tickFontSize, Brushes.LightGray, dpi);
                dc.DrawText(ft, new Point(p.X - ft.Width / 2, p.Y - ft.Height / 2));
            }
        }

        var normalized = Math.Clamp(Value, 0, 100);
        if (IsAvailable)
        {
            var markerAngle = start + sweep * (normalized / 100d);
            var markerInner = PointOnCircle(center, radius - markerHalfLength, markerAngle);
            var markerOuter = PointOnCircle(center, radius + markerHalfLength, markerAngle);
            dc.DrawLine(NewPen(Color.FromRgb(17, 35, 62), markerThickness), markerInner, markerOuter);
        }

        var valueFontSize = Math.Clamp(radius * .47, 13, 66);
        var valueText = Text(IsAvailable ? $"{normalized:0}%" : "—", valueFontSize, Brushes.White, dpi, FontWeights.SemiBold);
        dc.DrawText(valueText, new Point(center.X - valueText.Width / 2, center.Y - valueText.Height * .34));

        var labelText = Text(Label, labelFontSize, Brushes.Gainsboro, dpi);
        dc.DrawText(labelText, new Point(center.X - labelText.Width / 2, center.Y + radius * .59));
    }

    private static Pen NewPen(Color color, double thickness)
    {
        var pen = new Pen(new SolidColorBrush(color), thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

        pen.Freeze();
        return pen;
    }

    private static FormattedText Text(string value, double size, Brush brush, double dpi, FontWeight? weight = null) =>
        new(value,
            CultureInfo.GetCultureInfo("fa-IR"),
            FlowDirection.LeftToRight,
            new Typeface(
                new FontFamily("Segoe UI Variable Display"),
                FontStyles.Normal,
                weight ?? FontWeights.Normal,
                FontStretches.Normal),
            size,
            brush,
            dpi);

    private static Point PointOnCircle(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180d;
        return new Point(
            center.X + Math.Cos(radians) * radius,
            center.Y + Math.Sin(radians) * radius);
    }

    private static void DrawArc(DrawingContext dc, Point center, double radius, double startAngle, double sweepAngle, Pen pen)
    {
        if (Math.Abs(sweepAngle) < .01)
            return;

        var geometry = new StreamGeometry();

        using (var ctx = geometry.Open())
        {
            var start = PointOnCircle(center, radius, startAngle);
            var end = PointOnCircle(center, radius, startAngle + sweepAngle);

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
}
