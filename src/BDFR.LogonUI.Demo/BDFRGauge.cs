using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BDFR.LogonUI.Demo;

public sealed class BDFRGauge : FrameworkElement
{
    private const double ReferenceWidth = 170d;
    private const double ReferenceHeight = 145d;

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(BDFRGauge),
            new FrameworkPropertyMetadata(
                0d,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(BDFRGauge),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsAvailableProperty =
        DependencyProperty.Register(
            nameof(IsAvailable),
            typeof(bool),
            typeof(BDFRGauge),
            new FrameworkPropertyMetadata(
                true,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty VisualStyleProperty =
        DependencyProperty.Register(
            nameof(VisualStyle),
            typeof(GaugeVisualStyle),
            typeof(BDFRGauge),
            new FrameworkPropertyMetadata(
                GaugeVisualStyle.ReferenceArc,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackBrushProperty =
        RegisterBrush(
            nameof(TrackBrush),
            new SolidColorBrush(Color.FromArgb(80, 230, 236, 244)));

    public static readonly DependencyProperty NormalBrushProperty =
        RegisterBrush(
            nameof(NormalBrush),
            new SolidColorBrush(Color.FromRgb(91, 221, 165)));

    public static readonly DependencyProperty WarningBrushProperty =
        RegisterBrush(
            nameof(WarningBrush),
            new SolidColorBrush(Color.FromRgb(255, 165, 76)));

    public static readonly DependencyProperty CriticalBrushProperty =
        RegisterBrush(
            nameof(CriticalBrush),
            new SolidColorBrush(Color.FromRgb(255, 91, 76)));

    public static readonly DependencyProperty MarkerBrushProperty =
        RegisterBrush(
            nameof(MarkerBrush),
            new SolidColorBrush(Color.FromRgb(17, 35, 62)));

    public static readonly DependencyProperty ValueBrushProperty =
        RegisterBrush(nameof(ValueBrush), Brushes.White);

    public static readonly DependencyProperty ScaleBrushProperty =
        RegisterBrush(nameof(ScaleBrush), Brushes.LightGray);

    public static readonly DependencyProperty LabelBrushProperty =
        RegisterBrush(nameof(LabelBrush), Brushes.Gainsboro);

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

    public bool IsAvailable
    {
        get => (bool)GetValue(IsAvailableProperty);
        set => SetValue(IsAvailableProperty, value);
    }

    public GaugeVisualStyle VisualStyle
    {
        get => (GaugeVisualStyle)GetValue(VisualStyleProperty);
        set => SetValue(VisualStyleProperty, value);
    }

    public Brush TrackBrush
    {
        get => (Brush)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public Brush NormalBrush
    {
        get => (Brush)GetValue(NormalBrushProperty);
        set => SetValue(NormalBrushProperty, value);
    }

    public Brush WarningBrush
    {
        get => (Brush)GetValue(WarningBrushProperty);
        set => SetValue(WarningBrushProperty, value);
    }

    public Brush CriticalBrush
    {
        get => (Brush)GetValue(CriticalBrushProperty);
        set => SetValue(CriticalBrushProperty, value);
    }

    public Brush MarkerBrush
    {
        get => (Brush)GetValue(MarkerBrushProperty);
        set => SetValue(MarkerBrushProperty, value);
    }

    public Brush ValueBrush
    {
        get => (Brush)GetValue(ValueBrushProperty);
        set => SetValue(ValueBrushProperty, value);
    }

    public Brush ScaleBrush
    {
        get => (Brush)GetValue(ScaleBrushProperty);
        set => SetValue(ScaleBrushProperty, value);
    }

    public Brush LabelBrush
    {
        get => (Brush)GetValue(LabelBrushProperty);
        set => SetValue(LabelBrushProperty, value);
    }

    private static DependencyProperty RegisterBrush(
        string name,
        Brush defaultValue)
        => DependencyProperty.Register(
            name,
            typeof(Brush),
            typeof(BDFRGauge),
            new FrameworkPropertyMetadata(
                defaultValue,
                FrameworkPropertyMetadataOptions.AffectsRender));

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width)
            ? ReferenceWidth
            : Math.Max(72, availableSize.Width);

        var height = double.IsInfinity(availableSize.Height)
            ? ReferenceHeight
            : Math.Max(64, availableSize.Height);

        return new Size(width, height);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var width = Math.Max(1, ActualWidth);
        var height = Math.Max(1, ActualHeight);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var value = Math.Clamp(Value, 0, 100);
        var scale = Math.Clamp(
            Math.Min(width / ReferenceWidth, height / ReferenceHeight),
            .34,
            3.4);

        switch (VisualStyle)
        {
            case GaugeVisualStyle.ReferenceArc:
                RenderReferenceArc(dc, width, height, dpi, scale, value);
                break;

            case GaugeVisualStyle.RoundDense:
                RenderRoundDial(dc, width, height, dpi, scale, value, 24, true, true, false);
                break;

            case GaugeVisualStyle.RoundBold:
                RenderRoundDial(dc, width, height, dpi, scale, value, 12, true, false, true);
                break;

            case GaugeVisualStyle.RoundMinimal:
                RenderRoundDial(dc, width, height, dpi, scale, value, 8, false, false, false);
                break;

            case GaugeVisualStyle.HalfClean:
                RenderHalfDial(dc, width, height, dpi, scale, value, 8, false, false, false);
                break;

            case GaugeVisualStyle.HalfSegmented:
                RenderHalfDial(dc, width, height, dpi, scale, value, 10, true, false, false);
                break;

            case GaugeVisualStyle.HalfBlocks:
                RenderHalfDial(dc, width, height, dpi, scale, value, 9, true, true, false);
                break;

            case GaugeVisualStyle.HalfMinimal:
                RenderHalfDial(dc, width, height, dpi, scale, value, 5, false, false, true);
                break;

            case GaugeVisualStyle.InsetArc:
                RenderInsetDial(dc, width, height, dpi, scale, value, false, false, true);
                break;

            case GaugeVisualStyle.DarkRadial:
                RenderInsetDial(dc, width, height, dpi, scale, value, true, true, true);
                break;

            case GaugeVisualStyle.DarkNeedle:
                RenderInsetDial(dc, width, height, dpi, scale, value, true, false, false);
                break;

            case GaugeVisualStyle.DarkCompact:
                RenderInsetDial(dc, width, height, dpi, scale, value, true, false, true);
                break;

            case GaugeVisualStyle.MiniDoubleRing:
                RenderMiniDial(dc, width, height, dpi, scale, value, true, false, false, false);
                break;

            case GaugeVisualStyle.MiniTicks:
                RenderMiniDial(dc, width, height, dpi, scale, value, false, true, false, false);
                break;

            case GaugeVisualStyle.MiniOpenArc:
                RenderMiniDial(dc, width, height, dpi, scale, value, false, false, true, false);
                break;

            case GaugeVisualStyle.MiniDisplay:
                RenderMiniDial(dc, width, height, dpi, scale, value, false, false, false, true);
                break;

            case GaugeVisualStyle.MiniDense:
                RenderMiniDial(dc, width, height, dpi, scale, value, true, true, false, true);
                break;

            default:
                RenderReferenceArc(dc, width, height, dpi, scale, value);
                break;
        }
    }

    private void RenderReferenceArc(
        DrawingContext dc,
        double width,
        double height,
        double dpi,
        double scale,
        double value)
    {
        var radius = Math.Max(22 * scale, Math.Min(width * .30, height * .36));
        var center = new Point(width / 2, height * .52);

        const double start = 145;
        const double sweep = 250;

        var arcThickness = Math.Clamp(10 * scale, 3.2, 24);
        var markerThickness = Math.Clamp(4 * scale, 1.7, 9);
        var markerHalfLength = Math.Clamp(9 * scale, 4, 22);
        var tickOffset = Math.Clamp(21 * scale, 9, 42);
        var tickFontSize = Math.Clamp(9.5 * scale, 6.2, 22);
        var labelFontSize = Math.Clamp(11.5 * scale, 7.2, 27);

        DrawArc(dc, center, radius, start, sweep, NewPen(TrackBrush, arcThickness));
        DrawArc(dc, center, radius, start, sweep * .60, NewPen(NormalBrush, arcThickness));
        DrawArc(dc, center, radius, start + sweep * .60, sweep * .20, NewPen(WarningBrush, arcThickness));
        DrawArc(dc, center, radius, start + sweep * .80, sweep * .20, NewPen(CriticalBrush, arcThickness));

        if (width >= 100 && height >= 78)
        {
            for (var tick = 0; tick <= 100; tick += 10)
            {
                var angle = start + sweep * (tick / 100d);
                var p = PointOnCircle(center, radius + tickOffset, angle);
                var ft = Text(
                    tick.ToString(CultureInfo.InvariantCulture),
                    tickFontSize,
                    ScaleBrush,
                    dpi);

                dc.DrawText(
                    ft,
                    new Point(p.X - ft.Width / 2, p.Y - ft.Height / 2));
            }
        }

        if (IsAvailable)
        {
            var markerAngle = start + sweep * (value / 100d);
            var markerInner = PointOnCircle(center, radius - markerHalfLength, markerAngle);
            var markerOuter = PointOnCircle(center, radius + markerHalfLength, markerAngle);

            dc.DrawLine(
                NewPen(MarkerBrush, markerThickness),
                markerInner,
                markerOuter);
        }

        DrawCenterValue(
            dc,
            center,
            radius,
            dpi,
            value,
            false,
            center.Y - radius * .12);

        DrawLabel(
            dc,
            center.X,
            center.Y + radius * .62,
            labelFontSize,
            dpi);
    }

    private void RenderRoundDial(
        DrawingContext dc,
        double width,
        double height,
        double dpi,
        double scale,
        double value,
        int tickCount,
        bool displayBox,
        bool denseMinorTicks,
        bool doubleFrame)
    {
        var radius = Math.Min(width, height) * .33;
        var center = new Point(width / 2, height * .48);
        var outer = Math.Clamp(2.4 * scale, 1.2, 6);

        dc.DrawEllipse(
            null,
            NewPen(ScaleBrush, outer),
            center,
            radius,
            radius);

        if (doubleFrame)
        {
            dc.DrawEllipse(
                null,
                NewPen(TrackBrush, Math.Clamp(5.5 * scale, 2, 13)),
                center,
                radius * .88,
                radius * .88);
        }

        const double start = 140;
        const double sweep = 260;

        DrawTicks(
            dc,
            center,
            radius,
            start,
            sweep,
            tickCount,
            denseMinorTicks ? 2 : 1,
            scale,
            doubleFrame ? MarkerBrush : ScaleBrush);

        DrawNeedle(
            dc,
            center,
            radius * .78,
            start,
            sweep,
            value,
            scale,
            doubleFrame ? MarkerBrush : ValueBrush,
            true);

        if (displayBox)
        {
            DrawDisplayBox(
                dc,
                center.X,
                center.Y + radius * .55,
                radius * .72,
                radius * .27,
                value,
                dpi,
                scale);
        }
        else
        {
            DrawCenterValue(
                dc,
                center,
                radius,
                dpi,
                value,
                false,
                center.Y + radius * .42);
        }

        DrawLabel(
            dc,
            center.X,
            Math.Min(height - 9 * scale, center.Y + radius + 9 * scale),
            Math.Clamp(9.5 * scale, 6.5, 20),
            dpi);
    }

    private void RenderHalfDial(
        DrawingContext dc,
        double width,
        double height,
        double dpi,
        double scale,
        double value,
        int segmentCount,
        bool segmented,
        bool blocks,
        bool minimal)
    {
        var radius = Math.Min(width * .39, height * .58);
        var center = new Point(width / 2, height * .70);
        const double start = 180;
        const double sweep = 180;

        var baseThickness = Math.Clamp((blocks ? 12 : 6) * scale, 2.3, 28);

        if (segmented)
        {
            DrawSegmentedArc(
                dc,
                center,
                radius,
                start,
                sweep,
                segmentCount,
                baseThickness,
                value);
        }
        else
        {
            DrawArc(
                dc,
                center,
                radius,
                start,
                sweep,
                NewPen(TrackBrush, baseThickness));

            if (!minimal)
            {
                DrawArc(
                    dc,
                    center,
                    radius,
                    start,
                    sweep * (value / 100d),
                    NewPen(ValueRangeBrush(value), baseThickness));
            }
        }

        if (!minimal)
        {
            DrawTicks(
                dc,
                center,
                radius - baseThickness * .7,
                start,
                sweep,
                Math.Max(5, segmentCount),
                1,
                scale,
                ScaleBrush);
        }

        DrawNeedle(
            dc,
            center,
            radius * .72,
            start,
            sweep,
            value,
            scale,
            MarkerBrush,
            true);

        DrawCenterValue(
            dc,
            center,
            radius,
            dpi,
            value,
            true,
            center.Y - radius * .20);

        DrawLabel(
            dc,
            center.X,
            Math.Min(height - 8 * scale, center.Y + 11 * scale),
            Math.Clamp(10 * scale, 7, 20),
            dpi);
    }

    private void RenderInsetDial(
        DrawingContext dc,
        double width,
        double height,
        double dpi,
        double scale,
        double value,
        bool dark,
        bool dense,
        bool innerRing)
    {
        var radius = Math.Min(width, height) * .34;
        var center = new Point(width / 2, height * .49);
        var background = dark
            ? WithOpacity(MarkerBrush, .82)
            : WithOpacity(TrackBrush, .45);

        dc.DrawEllipse(background, null, center, radius, radius);

        if (innerRing)
        {
            dc.DrawEllipse(
                null,
                NewPen(
                    dark ? WithOpacity(ValueBrush, .75) : ScaleBrush,
                    Math.Clamp(2 * scale, 1, 5)),
                center,
                radius * .58,
                radius * .58);
        }

        const double start = 135;
        const double sweep = 270;
        var tickBrush = dark ? WithOpacity(ValueBrush, .9) : ScaleBrush;

        DrawTicks(
            dc,
            center,
            radius * .88,
            start,
            sweep,
            dense ? 20 : 10,
            dense ? 2 : 1,
            scale,
            tickBrush);

        DrawArc(
            dc,
            center,
            radius * .74,
            start,
            sweep,
            NewPen(
                dark ? WithOpacity(ValueBrush, .20) : TrackBrush,
                Math.Clamp(5 * scale, 2, 14)));

        DrawArc(
            dc,
            center,
            radius * .74,
            start,
            sweep * (value / 100d),
            NewPen(
                ValueRangeBrush(value),
                Math.Clamp(5 * scale, 2, 14)));

        DrawNeedle(
            dc,
            center,
            radius * .66,
            start,
            sweep,
            value,
            scale,
            dark ? ValueBrush : MarkerBrush,
            true);

        DrawCenterValue(
            dc,
            center,
            radius,
            dpi,
            value,
            true,
            center.Y + radius * .54);

        DrawLabel(
            dc,
            center.X,
            Math.Min(height - 8 * scale, center.Y + radius + 9 * scale),
            Math.Clamp(9.5 * scale, 6.5, 20),
            dpi);
    }

    private void RenderMiniDial(
        DrawingContext dc,
        double width,
        double height,
        double dpi,
        double scale,
        double value,
        bool doubleRing,
        bool denseTicks,
        bool openArc,
        bool displayBox)
    {
        var radius = Math.Min(width, height) * .31;
        var center = new Point(width / 2, height * .47);
        var frameThickness = Math.Clamp(2.2 * scale, 1.1, 5.5);

        if (openArc)
        {
            DrawArc(
                dc,
                center,
                radius,
                140,
                260,
                NewPen(ScaleBrush, frameThickness));
        }
        else
        {
            dc.DrawEllipse(
                null,
                NewPen(ScaleBrush, frameThickness),
                center,
                radius,
                radius);
        }

        if (doubleRing)
        {
            dc.DrawEllipse(
                null,
                NewPen(TrackBrush, Math.Clamp(4.2 * scale, 1.5, 10)),
                center,
                radius * .75,
                radius * .75);
        }

        const double start = 140;
        const double sweep = 260;

        DrawTicks(
            dc,
            center,
            radius * .88,
            start,
            sweep,
            denseTicks ? 16 : 8,
            denseTicks ? 2 : 1,
            scale,
            ScaleBrush);

        DrawNeedle(
            dc,
            center,
            radius * .70,
            start,
            sweep,
            value,
            scale,
            MarkerBrush,
            true);

        if (displayBox)
        {
            DrawDisplayBox(
                dc,
                center.X,
                center.Y + radius * .52,
                radius * .72,
                radius * .26,
                value,
                dpi,
                scale);
        }
        else
        {
            DrawCenterValue(
                dc,
                center,
                radius,
                dpi,
                value,
                true,
                center.Y + radius * .52);
        }

        DrawLabel(
            dc,
            center.X,
            Math.Min(height - 6 * scale, center.Y + radius + 7 * scale),
            Math.Clamp(8.5 * scale, 6.2, 18),
            dpi);
    }

    private void DrawTicks(
        DrawingContext dc,
        Point center,
        double radius,
        double start,
        double sweep,
        int majorCount,
        int minorPerMajor,
        double scale,
        Brush brush)
    {
        var total = Math.Max(1, majorCount * Math.Max(1, minorPerMajor));

        for (var i = 0; i <= total; i++)
        {
            var isMajor = i % Math.Max(1, minorPerMajor) == 0;
            var angle = start + sweep * (i / (double)total);
            var outer = PointOnCircle(center, radius, angle);
            var length = Math.Clamp(
                (isMajor ? 10 : 5) * scale,
                isMajor ? 4 : 2,
                isMajor ? 22 : 12);

            var inner = PointOnCircle(center, radius - length, angle);

            dc.DrawLine(
                NewPen(
                    brush,
                    Math.Clamp(
                        (isMajor ? 2.1 : 1.1) * scale,
                        .7,
                        isMajor ? 5.5 : 3)),
                inner,
                outer);
        }
    }

    private void DrawSegmentedArc(
        DrawingContext dc,
        Point center,
        double radius,
        double start,
        double sweep,
        int segments,
        double thickness,
        double value)
    {
        segments = Math.Max(3, segments);
        var gap = sweep / segments * .16;
        var segmentSweep = sweep / segments - gap;

        for (var i = 0; i < segments; i++)
        {
            var segmentValue = (i + .5) / segments * 100;
            var brush = segmentValue <= value
                ? ValueRangeBrush(segmentValue)
                : TrackBrush;

            DrawArc(
                dc,
                center,
                radius,
                start + i * sweep / segments + gap / 2,
                segmentSweep,
                NewPen(brush, thickness));
        }
    }

    private void DrawNeedle(
        DrawingContext dc,
        Point center,
        double length,
        double start,
        double sweep,
        double value,
        double scale,
        Brush brush,
        bool drawHub)
    {
        if (!IsAvailable)
            return;

        var angle = start + sweep * (value / 100d);
        var end = PointOnCircle(center, length, angle);

        dc.DrawLine(
            NewPen(brush, Math.Clamp(3 * scale, 1.3, 7)),
            center,
            end);

        if (!drawHub)
            return;

        dc.DrawEllipse(
            brush,
            null,
            center,
            Math.Clamp(5.3 * scale, 2.8, 13),
            Math.Clamp(5.3 * scale, 2.8, 13));

        dc.DrawEllipse(
            ValueBrush,
            null,
            center,
            Math.Clamp(1.8 * scale, 1, 4),
            Math.Clamp(1.8 * scale, 1, 4));
    }

    private void DrawDisplayBox(
        DrawingContext dc,
        double centerX,
        double centerY,
        double boxWidth,
        double boxHeight,
        double value,
        double dpi,
        double scale)
    {
        var rect = new Rect(
            centerX - boxWidth / 2,
            centerY - boxHeight / 2,
            boxWidth,
            boxHeight);

        dc.DrawRoundedRectangle(
            WithOpacity(MarkerBrush, .18),
            NewPen(ScaleBrush, Math.Clamp(1.2 * scale, .7, 3)),
            rect,
            Math.Clamp(2.5 * scale, 1.5, 6),
            Math.Clamp(2.5 * scale, 1.5, 6));

        var text = Text(
            IsAvailable
                ? value.ToString("0", CultureInfo.InvariantCulture)
                : "—",
            Math.Clamp(10.5 * scale, 7, 24),
            ValueBrush,
            dpi,
            FontWeights.SemiBold);

        dc.DrawText(
            text,
            new Point(
                centerX - text.Width / 2,
                centerY - text.Height / 2));
    }

    private void DrawCenterValue(
        DrawingContext dc,
        Point center,
        double radius,
        double dpi,
        double value,
        bool compact,
        double y)
    {
        var size = compact
            ? Math.Clamp(radius * .25, 9, 28)
            : Math.Clamp(radius * .47, 13, 62);

        var text = Text(
            IsAvailable
                ? value.ToString("0", CultureInfo.InvariantCulture)
                : "—",
            size,
            ValueBrush,
            dpi,
            FontWeights.SemiBold);

        dc.DrawText(
            text,
            new Point(
                center.X - text.Width / 2,
                y - text.Height / 2));
    }

    private void DrawLabel(
        DrawingContext dc,
        double centerX,
        double y,
        double size,
        double dpi)
    {
        var label = Text(
            Label,
            size,
            LabelBrush,
            dpi,
            FontWeights.Normal);

        dc.DrawText(
            label,
            new Point(
                centerX - label.Width / 2,
                y - label.Height / 2));
    }

    private Brush ValueRangeBrush(double value)
        => value < 60
            ? NormalBrush
            : value < 80
                ? WarningBrush
                : CriticalBrush;

    private static Brush WithOpacity(Brush brush, double opacity)
    {
        var clone = brush.CloneCurrentValue();
        clone.Opacity = Math.Clamp(opacity, 0, 1);

        if (clone.CanFreeze)
            clone.Freeze();

        return clone;
    }

    private static Pen NewPen(Brush brush, double thickness)
        => new(brush, thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };

    private static FormattedText Text(
        string value,
        double size,
        Brush brush,
        double dpi,
        FontWeight? weight = null)
        => new(
            value,
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

    private static Point PointOnCircle(
        Point center,
        double radius,
        double degrees)
    {
        var radians = degrees * Math.PI / 180d;

        return new Point(
            center.X + Math.Cos(radians) * radius,
            center.Y + Math.Sin(radians) * radius);
    }

    private static void DrawArc(
        DrawingContext dc,
        Point center,
        double radius,
        double startAngle,
        double sweepAngle,
        Pen pen)
    {
        if (Math.Abs(sweepAngle) < .01)
            return;

        var geometry = new StreamGeometry();

        using (var ctx = geometry.Open())
        {
            var start = PointOnCircle(center, radius, startAngle);
            var end = PointOnCircle(
                center,
                radius,
                startAngle + sweepAngle);

            ctx.BeginFigure(start, false, false);
            ctx.ArcTo(
                end,
                new Size(radius, radius),
                0,
                Math.Abs(sweepAngle) > 180,
                sweepAngle >= 0
                    ? SweepDirection.Clockwise
                    : SweepDirection.Counterclockwise,
                true,
                false);
        }

        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }
}
