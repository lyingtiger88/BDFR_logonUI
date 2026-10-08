using System.IO;
using System.Text.Json;

namespace BDFR.LogonUI.Demo;

public sealed class WelcomeAnimationSettings
{
    public string Style { get; set; } = nameof(WelcomeAnimationStyle.ElegantFade);
    public bool Enabled { get; set; } = true;
    public double DurationSeconds { get; set; } = 3.2;

    public WelcomeAnimationStyle Resolve() =>
        WelcomeAnimationCatalog.Parse(Style);

    public WelcomeAnimationSettings Clone() =>
        new()
        {
            Style = Style,
            Enabled = Enabled,
            DurationSeconds = DurationSeconds
        };
}

public sealed class WelcomeAnimationSettingsService
{
    private readonly string _path;

    public WelcomeAnimationSettingsService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BDFR",
            "LogonUI");

        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "welcome-animation.demo.json");
    }

    public WelcomeAnimationSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
                return new WelcomeAnimationSettings();

            return JsonSerializer.Deserialize<WelcomeAnimationSettings>(
                       File.ReadAllText(_path))
                   ?? new WelcomeAnimationSettings();
        }
        catch
        {
            return new WelcomeAnimationSettings();
        }
    }

    public void Save(WelcomeAnimationSettings settings)
    {
        settings.DurationSeconds = Math.Clamp(
            settings.DurationSeconds,
            1.5,
            8.0);

        File.WriteAllText(
            _path,
            JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions { WriteIndented = true }));
    }
}
