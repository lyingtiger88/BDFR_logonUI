using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BDFR.LogonUI.Demo;

public sealed class BDFRGauge : FrameworkElement
{
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

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(170, 145);

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var width = ActualWidth <= 0 ? 170 : ActualWidth;
        var height = ActualHeight <= 0 ? 145 : ActualHeight;
        var radius = Math.Max(34, Math.Min(width * 0.31, height * 0.37));
        var center = new Point(width / 2, height * 0.53);
        const double start = 145;
        const double sweep = 250;

        var trackPen = NewPen(Color.FromArgb(80, 230, 236, 244), 10);
        var greenPen = NewPen(Color.FromRgb(91, 221, 165), 10);
        var orangePen = NewPen(Color.FromRgb(255, 165, 76), 10);
        var redPen = NewPen(Color.FromRgb(255, 91, 76), 10);

        DrawArc(dc, center, radius, start, sweep, trackPen);
        DrawArc(dc, center, radius, start, sweep * .60, greenPen);
        DrawArc(dc, center, radius, start + sweep * .60, sweep * .20, orangePen);
        DrawArc(dc, center, radius, start + sweep * .80, sweep * .20, redPen);

        for (var tick = 0; tick <= 100; tick += 20)
        {
            var angle = start + sweep * (tick / 100d);
            var p = PointOnCircle(center, radius + 21, angle);
            var ft = Text(tick.ToString(CultureInfo.InvariantCulture), 10, Brushes.LightGray, dpi);
            dc.DrawText(ft, new Point(p.X - ft.Width / 2, p.Y - ft.Height / 2));
        }

        var normalized = Math.Clamp(Value, 0, 100);
        var markerAngle = start + sweep * (normalized / 100d);
        var markerInner = PointOnCircle(center, radius - 9, markerAngle);
        var markerOuter = PointOnCircle(center, radius + 9, markerAngle);
        dc.DrawLine(NewPen(Color.FromRgb(17, 35, 62), 4), markerInner, markerOuter);

        var valueText = Text($"{normalized:0}%", Math.Max(22, radius * .48), Brushes.White, dpi, FontWeights.SemiBold);
        dc.DrawText(valueText, new Point(center.X - valueText.Width / 2, center.Y - valueText.Height * .33));

        var labelText = Text(Label, 12, Brushes.Gainsboro, dpi);
        dc.DrawText(labelText, new Point(center.X - labelText.Width / 2, center.Y + radius * .58));
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
        new(value, CultureInfo.GetCultureInfo("fa-IR"), FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI Variable Display"), FontStyles.Normal, weight ?? FontWeights.Normal, FontStretches.Normal),
            size, brush, dpi);

    private static Point PointOnCircle(Point center, double radius, double degrees)
    {
        var r = degrees * Math.PI / 180d;
        return new Point(center.X + Math.Cos(r) * radius, center.Y + Math.Sin(r) * radius);
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
            ctx.ArcTo(end, new Size(radius, radius), 0, Math.Abs(sweepAngle) > 180,
                sweepAngle >= 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise,
                true, false);
        }
        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }
}
