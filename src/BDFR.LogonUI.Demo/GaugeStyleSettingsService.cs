using System.IO;
using System.Text.Json;

namespace BDFR.LogonUI.Demo;

public sealed class GaugeStyleSettings
{
    public string DefaultStyle { get; set; } = nameof(GaugeVisualStyle.ReferenceArc);

    public Dictionary<string, string> Overrides { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public GaugeVisualStyle Resolve(string gaugeId)
    {
        if (Overrides.TryGetValue(gaugeId, out var style))
            return GaugeStyleCatalog.Parse(style);

        return GaugeStyleCatalog.Parse(DefaultStyle);
    }

    public GaugeStyleSettings Clone()
    {
        var clone = new GaugeStyleSettings
        {
            DefaultStyle = DefaultStyle,
            Overrides = new Dictionary<string, string>(
                Overrides,
                StringComparer.OrdinalIgnoreCase)
        };

        return clone;
    }
}

public sealed class GaugeStyleSettingsService
{
    private readonly string _path;

    public GaugeStyleSettingsService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BDFR",
            "LogonUI");

        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "gauge-styles.demo.json");
    }

    public GaugeStyleSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
                return new GaugeStyleSettings();

            var value = JsonSerializer.Deserialize<GaugeStyleSettings>(
                File.ReadAllText(_path));

            if (value is null)
                return new GaugeStyleSettings();

            value.Overrides ??= new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            return value;
        }
        catch
        {
            return new GaugeStyleSettings();
        }
    }

    public void Save(GaugeStyleSettings settings)
    {
        File.WriteAllText(
            _path,
            JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions { WriteIndented = true }));
    }
}
