namespace BDFR.LogonUI.Demo;

public enum GaugeVisualStyle
{
    ReferenceArc = 1,
    RoundDense = 2,
    RoundBold = 3,
    RoundMinimal = 4,
    HalfClean = 5,
    HalfSegmented = 6,
    HalfBlocks = 7,
    HalfMinimal = 8,
    InsetArc = 9,
    DarkRadial = 10,
    DarkNeedle = 11,
    DarkCompact = 12,
    MiniDoubleRing = 13,
    MiniTicks = 14,
    MiniOpenArc = 15,
    MiniDisplay = 16,
    MiniDense = 17
}

public sealed record GaugeStyleDefinition(
    GaugeVisualStyle Style,
    string DisplayName,
    string Group);

public static class GaugeStyleCatalog
{
    public static readonly GaugeStyleDefinition[] All =
    [
        new(GaugeVisualStyle.ReferenceArc, "01 · Reference Arc", "Round"),
        new(GaugeVisualStyle.RoundDense, "02 · Round Dense", "Round"),
        new(GaugeVisualStyle.RoundBold, "03 · Round Bold", "Round"),
        new(GaugeVisualStyle.RoundMinimal, "04 · Round Minimal", "Round"),
        new(GaugeVisualStyle.HalfClean, "05 · Half Clean", "Half"),
        new(GaugeVisualStyle.HalfSegmented, "06 · Half Segmented", "Half"),
        new(GaugeVisualStyle.HalfBlocks, "07 · Half Blocks", "Half"),
        new(GaugeVisualStyle.HalfMinimal, "08 · Half Minimal", "Half"),
        new(GaugeVisualStyle.InsetArc, "09 · Inset Arc", "Inset"),
        new(GaugeVisualStyle.DarkRadial, "10 · Dark Radial", "Inset"),
        new(GaugeVisualStyle.DarkNeedle, "11 · Dark Needle", "Inset"),
        new(GaugeVisualStyle.DarkCompact, "12 · Dark Compact", "Inset"),
        new(GaugeVisualStyle.MiniDoubleRing, "13 · Mini Double Ring", "Mini"),
        new(GaugeVisualStyle.MiniTicks, "14 · Mini Ticks", "Mini"),
        new(GaugeVisualStyle.MiniOpenArc, "15 · Mini Open Arc", "Mini"),
        new(GaugeVisualStyle.MiniDisplay, "16 · Mini Display", "Mini"),
        new(GaugeVisualStyle.MiniDense, "17 · Mini Dense", "Mini")
    ];

    public static GaugeVisualStyle Parse(string? value)
        => Enum.TryParse<GaugeVisualStyle>(value, true, out var parsed)
            && Enum.IsDefined(parsed)
                ? parsed
                : GaugeVisualStyle.ReferenceArc;

    public static string DisplayName(GaugeVisualStyle style)
        => All.FirstOrDefault(x => x.Style == style)?.DisplayName
           ?? style.ToString();
}
