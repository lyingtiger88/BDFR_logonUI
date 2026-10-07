using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace BDFR.LogonUI.Demo;

public sealed record ThemeSettings(
    string Preset,
    string CardSurface,
    string CardBorder,
    string Accent,
    string ToolbarSurface,
    string GaugeTrack,
    string GaugeNormal,
    string GaugeWarning,
    string GaugeCritical,
    string GaugeMarker,
    string GaugeValue,
    string GaugeScale,
    string GaugeLabel);

public sealed class ThemeSettingsService
{
    private readonly string _path;

    public ThemeSettingsService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BDFR",
            "LogonUI");

        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "theme.demo.json");
    }

    public ThemeSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var value = JsonSerializer.Deserialize<ThemeSettings>(File.ReadAllText(_path));
                if (value is not null)
                    return value;
            }
        }
        catch
        {
        }

        return Preset("Fluent");
    }

    public void Save(ThemeSettings settings) =>
        File.WriteAllText(
            _path,
            JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));

    public static ThemeSettings Preset(string preset) => preset switch
    {
        "Emerald" => new(
            "Emerald", "#A8142825", "#554FE0B7", "#42D6A4", "#D0162927",
            "#35E8EEF4", "#46D8A0", "#F2B84B", "#F35E55", "#173147", "#F4FFFB", "#B8D8CF", "#E3F7F1"),

        "Sunset" => new(
            "Sunset", "#A8291B25", "#55FFC7AA", "#FF8A5C", "#D02A1B25",
            "#35F3E7E2", "#F0B55D", "#FF8A4C", "#F05353", "#321D2F", "#FFF4EE", "#D9B8AA", "#F6DCD0"),

        "Cyber" => new(
            "Cyber", "#B0101224", "#667B61FF", "#7B61FF", "#DB101329",
            "#353B3F62", "#24E3B5", "#F5D547", "#FF4F7B", "#111A38", "#F4F2FF", "#9FA8D8", "#DAD6FF"),

        "Monochrome" => new(
            "Monochrome", "#B0191B1F", "#55FFFFFF", "#D6D7DA", "#D01B1D21",
            "#356A6D72", "#D4D5D7", "#B3B5BA", "#8E9197", "#20242A", "#FFFFFF", "#BFC2C7", "#E5E6E8"),

        "Light Gauge" => new(
            "Light Gauge", "#A8182433", "#52FFFFFF", "#2F8CF0", "#D0182433",
            "#50E6ECF4", "#79DDB0", "#FF9F43", "#F4513A", "#20324D", "#20324D", "#AEB8C6", "#D6DCE5"),

        _ => new(
            "Fluent", "#A8182433", "#52FFFFFF", "#2F8CF0", "#D0182433",
            "#50E6ECF4", "#5BDDA5", "#FFA54C", "#FF5B4C", "#11233E", "#FFFFFF", "#D3DBE5", "#E2E7ED")
    };

    public static SolidColorBrush Brush(string value, string fallback)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(value);
            var brush = new SolidColorBrush(color);
            if (brush.CanFreeze)
                brush.Freeze();
            return brush;
        }
        catch
        {
            var color = (Color)ColorConverter.ConvertFromString(fallback);
            var brush = new SolidColorBrush(color);
            if (brush.CanFreeze)
                brush.Freeze();
            return brush;
        }
    }

    public static void ApplyAppResources(ThemeSettings settings)
    {
        var resources = Application.Current.Resources;
        resources["GlassSurfaceBrush"] = Brush(settings.CardSurface, "#A8182433");
        resources["GlassBorderBrush"] = Brush(settings.CardBorder, "#52FFFFFF");
        resources["AccentBrush"] = Brush(settings.Accent, "#2F8CF0");
        resources["ToolbarSurfaceBrush"] = Brush(settings.ToolbarSurface, "#D0182433");
    }
}
