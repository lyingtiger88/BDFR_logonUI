using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace BDFR.LogonUI.Demo;

public sealed class SeasonalBackgroundService
{
    private readonly string _root;
    private readonly string _settingsPath;
    private BackgroundSettings _settings;

    public SeasonalBackgroundService()
    {
        _root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BDFR", "LogonUI", "Backgrounds");

        Directory.CreateDirectory(_root);
        _settingsPath = Path.Combine(_root, "background.settings.json");
        _settings = LoadSettings();
    }

    public bool ElenaMode => _settings.ElenaMode;
    public string BackgroundFolder => _root;
    public string? ActivePath { get; private set; }

    public void SetElenaMode(bool enabled)
    {
        _settings = _settings with { ElenaMode = enabled };
        SaveSettings();
    }

    public string ImportCustom(string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Background image was not found.", sourcePath);

        var extension = Path.GetExtension(sourcePath);
        if (!SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unsupported background format.");

        var target = Path.Combine(_root, $"Custom{extension.ToLowerInvariant()}");
        File.Copy(sourcePath, target, overwrite: true);

        _settings = _settings with { CustomPath = target, ElenaMode = false };
        SaveSettings();
        return target;
    }

    public BitmapImage? Resolve(DateTime now)
    {
        var path = ResolvePath(now);
        ActivePath = path;

        if (path is null)
            return null;

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.DecodePixelWidth = 2560;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    public string CurrentModeLabel(DateTime now)
    {
        if (_settings.ElenaMode)
            return $"Elena • {GetSeason(now)}";

        return string.IsNullOrWhiteSpace(_settings.CustomPath)
            ? "پس‌زمینه پیش‌فرض"
            : "پس‌زمینه سفارشی";
    }

    private string? ResolvePath(DateTime now)
    {
        if (_settings.ElenaMode)
        {
            var seasonal = FindSeasonal(GetSeason(now));
            if (seasonal is not null)
                return seasonal;
        }

        if (!string.IsNullOrWhiteSpace(_settings.CustomPath) && File.Exists(_settings.CustomPath))
            return _settings.CustomPath;

        return FindSeasonal(GetSeason(now));
    }

    private string? FindSeasonal(string season)
    {
        var roots = new[]
        {
            _root,
            Path.Combine(AppContext.BaseDirectory, "assets", "backgrounds")
        };

        foreach (var root in roots)
        {
            foreach (var extension in SupportedExtensions)
            {
                var candidates = new[]
                {
                    Path.Combine(root, $"{season}_16x9{extension}"),
                    Path.Combine(root, $"{season}{extension}")
                };

                var match = candidates.FirstOrDefault(File.Exists);
                if (match is not null)
                    return match;
            }
        }

        return null;
    }

    private static string GetSeason(DateTime date)
    {
        var calendar = new PersianCalendar();
        var month = calendar.GetMonth(date);

        return month switch
        {
            <= 3 => "Spring",
            <= 6 => "Summer",
            <= 9 => "Autumn",
            _ => "Winter"
        };
    }

    private BackgroundSettings LoadSettings()
    {
        if (!File.Exists(_settingsPath))
            return new BackgroundSettings(false, null);

        try
        {
            return JsonSerializer.Deserialize<BackgroundSettings>(File.ReadAllText(_settingsPath))
                   ?? new BackgroundSettings(false, null);
        }
        catch
        {
            return new BackgroundSettings(false, null);
        }
    }

    private void SaveSettings()
    {
        File.WriteAllText(
            _settingsPath,
            JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static readonly string[] SupportedExtensions =
    {
        ".jpg", ".jpeg", ".png", ".bmp"
    };

    private sealed record BackgroundSettings(bool ElenaMode, string? CustomPath);
}
