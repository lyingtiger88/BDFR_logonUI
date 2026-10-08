using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace BDFR.LogonUI.Demo;

public sealed record WallpaperSourceItem(
    string Path,
    string DisplayName,
    string SourceLabel);

public sealed class SeasonalBackgroundService
{
    private readonly string _settingsRoot;
    private readonly string _settingsPath;
    private readonly string _appWallpaperFolder;
    private readonly string _userWallpaperFolder;
    private readonly string _anahitaSeasonalGalleryFolder;
    private BackgroundSettings _settings;
    private int _seasonCarouselIndex;
    private string? _seasonCarouselKey;

    public SeasonalBackgroundService()
    {
        _settingsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BDFR",
            "LogonUI");

        Directory.CreateDirectory(_settingsRoot);
        _settingsPath = Path.Combine(_settingsRoot, "background.settings.json");

        _appWallpaperFolder = Path.Combine(AppContext.BaseDirectory, "wallpaper");

        _userWallpaperFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "LockScreen");

        Directory.CreateDirectory(_userWallpaperFolder);

        // This is the same runtime gallery used by Anahita's ThemeService.
        // Installer default:
        // %LOCALAPPDATA%\Programs\Anahita\picture\theme\season backgrounds
        var anahitaOverride = Environment.GetEnvironmentVariable("BDFR_ANAHITA_SEASON_GALLERY");
        _anahitaSeasonalGalleryFolder = !string.IsNullOrWhiteSpace(anahitaOverride)
            ? Path.GetFullPath(anahitaOverride)
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "Anahita",
                "picture",
                "theme",
                "season backgrounds");

        _settings = LoadSettings();
    }

    public bool SeasonCarouselMode => _settings.SeasonCarouselMode;
    public string AppWallpaperFolder => _appWallpaperFolder;
    public string UserWallpaperFolder => _userWallpaperFolder;
    public string AnahitaSeasonalGalleryFolder => _anahitaSeasonalGalleryFolder;
    public string? ActivePath { get; private set; }

    public void SetSeasonCarouselMode(bool enabled)
    {
        _settings = _settings with { SeasonCarouselMode = enabled };
        ResetSeasonCarousel();
        SaveSettings();
    }

    public IReadOnlyList<WallpaperSourceItem> GetAvailableWallpapers()
    {
        var result = new List<WallpaperSourceItem>();

        AddManualWallpaperFolder(result, _appWallpaperFolder, "wallpaper");
        AddManualWallpaperFolder(result, _userWallpaperFolder, @"Pictures\LockScreen");

        return result
            .GroupBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.SourceLabel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public void SelectWallpaper(string path)
    {
        var fullPath = Path.GetFullPath(path);

        if (!IsAllowedManualWallpaperPath(fullPath))
        {
            throw new InvalidOperationException(
                "Wallpaper selection is restricted to the packaged wallpaper folder and Pictures\\LockScreen.");
        }

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Wallpaper image was not found.", fullPath);

        if (!IsManualWallpaperFormat(fullPath))
            throw new InvalidOperationException("Unsupported wallpaper format.");

        _settings = _settings with
        {
            SelectedWallpaperPath = fullPath,
            SeasonCarouselMode = false
        };

        ResetSeasonCarousel();
        SaveSettings();
    }

    public IReadOnlyList<string> GetSeasonCarouselPaths(DateTime date)
    {
        if (!Directory.Exists(_anahitaSeasonalGalleryFolder))
            return [];

        var season = GetSeasonKey(date);
        var numbered = new List<(int Index, string Path)>();
        string? fallback = null;

        IEnumerable<string> files;

        try
        {
            files = Directory.EnumerateFiles(
                _anahitaSeasonalGalleryFolder,
                "*.*",
                SearchOption.TopDirectoryOnly).ToArray();
        }
        catch
        {
            return [];
        }

        foreach (var path in files)
        {
            if (!IsSeasonGalleryFormat(path))
                continue;

            var name = Path.GetFileNameWithoutExtension(path);

            if (string.Equals(name, season, StringComparison.OrdinalIgnoreCase))
            {
                fallback ??= path;
                continue;
            }

            var match = Regex.Match(
                name,
                $@"^{Regex.Escape(season)}_(?<index>\d+)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (!match.Success
                || !int.TryParse(match.Groups["index"].Value, out var index)
                || index < 1)
            {
                continue;
            }

            numbered.Add((index, path));
        }

        if (numbered.Count > 0)
        {
            return numbered
                .OrderBy(x => x.Index)
                .ThenBy(x => Path.GetFileName(x.Path), StringComparer.OrdinalIgnoreCase)
                .GroupBy(x => x.Index)
                .Select(x => x.First().Path)
                .ToArray();
        }

        return fallback is null ? [] : [fallback];
    }

    public bool AdvanceSeasonCarousel(DateTime date)
    {
        if (!_settings.SeasonCarouselMode)
            return false;

        var files = GetSeasonCarouselPaths(date);
        if (files.Count <= 1)
            return false;

        var key = GetSeasonKey(date);

        if (!string.Equals(_seasonCarouselKey, key, StringComparison.OrdinalIgnoreCase))
        {
            _seasonCarouselKey = key;
            _seasonCarouselIndex = 0;
        }
        else
        {
            _seasonCarouselIndex = (_seasonCarouselIndex + 1) % files.Count;
        }

        return true;
    }

    public BitmapImage? Resolve(DateTime now)
    {
        var path = ResolvePath(now);
        ActivePath = path;

        if (path is null)
            return null;

        return LoadBitmap(path, 2560);
    }

    public BitmapImage? LoadThumbnail(string path, int decodeWidth = 360)
    {
        var fullPath = Path.GetFullPath(path);

        if (!IsAllowedManualWallpaperPath(fullPath)
            || !File.Exists(fullPath)
            || !IsManualWallpaperFormat(fullPath))
        {
            return null;
        }

        return LoadBitmap(fullPath, Math.Clamp(decodeWidth, 96, 720));
    }

    public string CurrentModeLabel(DateTime now)
    {
        if (_settings.SeasonCarouselMode)
        {
            var items = GetSeasonCarouselPaths(now);
            var index = items.Count == 0
                ? 0
                : Math.Clamp(_seasonCarouselIndex, 0, items.Count - 1) + 1;

            return items.Count == 0
                ? $"Season Carousel • {GetSeasonKey(now)} • Anahita gallery empty"
                : $"Season Carousel • {GetSeasonKey(now)} • {index}/{items.Count}";
        }

        return string.IsNullOrWhiteSpace(_settings.SelectedWallpaperPath)
            ? "پس‌زمینه پیش‌فرض"
            : "Wallpaper Gallery";
    }

    private string? ResolvePath(DateTime now)
    {
        if (_settings.SeasonCarouselMode)
        {
            var seasonalFiles = GetSeasonCarouselPaths(now);
            var seasonKey = GetSeasonKey(now);

            if (!string.Equals(_seasonCarouselKey, seasonKey, StringComparison.OrdinalIgnoreCase))
            {
                _seasonCarouselKey = seasonKey;
                _seasonCarouselIndex = 0;
            }

            if (seasonalFiles.Count > 0)
            {
                _seasonCarouselIndex = Math.Clamp(
                    _seasonCarouselIndex,
                    0,
                    seasonalFiles.Count - 1);

                // WPF BitmapImage renders the photographic/raster gallery directly.
                // Unsupported files are skipped by LoadBitmap and do not trigger
                // arbitrary file access.
                return seasonalFiles[_seasonCarouselIndex];
            }
        }

        if (!string.IsNullOrWhiteSpace(_settings.SelectedWallpaperPath))
        {
            var candidate = Path.GetFullPath(_settings.SelectedWallpaperPath);

            if (IsAllowedManualWallpaperPath(candidate)
                && File.Exists(candidate)
                && IsManualWallpaperFormat(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private void AddManualWallpaperFolder(
        ICollection<WallpaperSourceItem> destination,
        string folder,
        string sourceLabel)
    {
        if (!Directory.Exists(folder))
            return;

        IEnumerable<string> files;

        try
        {
            files = Directory.EnumerateFiles(
                folder,
                "*.*",
                SearchOption.TopDirectoryOnly).ToArray();
        }
        catch
        {
            return;
        }

        foreach (var path in files)
        {
            if (!IsManualWallpaperFormat(path))
                continue;

            string fullPath;

            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch
            {
                continue;
            }

            if (!IsAllowedManualWallpaperPath(fullPath))
                continue;

            destination.Add(new WallpaperSourceItem(
                fullPath,
                Path.GetFileNameWithoutExtension(fullPath),
                sourceLabel));
        }
    }

    private bool IsAllowedManualWallpaperPath(string path) =>
        IsPathInsideRoot(path, _appWallpaperFolder)
        || IsPathInsideRoot(path, _userWallpaperFolder);

    private static bool IsPathInsideRoot(string candidate, string root)
    {
        try
        {
            var fullCandidate = Path.GetFullPath(candidate);
            var fullRoot = Path.GetFullPath(root);
            var relative = Path.GetRelativePath(fullRoot, fullCandidate);

            return !Path.IsPathRooted(relative)
                   && relative != ".."
                   && !relative.StartsWith(
                       $"..{Path.DirectorySeparatorChar}",
                       StringComparison.Ordinal)
                   && !relative.StartsWith(
                       $"..{Path.AltDirectorySeparatorChar}",
                       StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsManualWallpaperFormat(string path) =>
        ManualWallpaperExtensions.Contains(
            Path.GetExtension(path),
            StringComparer.OrdinalIgnoreCase);

    private static bool IsSeasonGalleryFormat(string path) =>
        SeasonGalleryExtensions.Contains(
            Path.GetExtension(path),
            StringComparer.OrdinalIgnoreCase);

    private static BitmapImage? LoadBitmap(string path, int decodeWidth)
    {
        try
        {
            // BitmapImage covers PNG/JPEG/BMP and any WIC codec installed on Windows.
            // If a calendar gallery item uses an unsupported codec (e.g. SVG without
            // a WPF codec), it safely resolves as null instead of falling back to
            // unrestricted file access.
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.DecodePixelWidth = decodeWidth;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private static string GetSeasonKey(DateTime date)
    {
        var calendar = new PersianCalendar();

        return calendar.GetMonth(date) switch
        {
            <= 3 => "Spring",
            <= 6 => "Summer",
            <= 9 => "Autumn",
            _ => "Winter"
        };
    }

    private void ResetSeasonCarousel()
    {
        _seasonCarouselIndex = 0;
        _seasonCarouselKey = null;
    }

    private BackgroundSettings LoadSettings()
    {
        if (!File.Exists(_settingsPath))
            return new BackgroundSettings(false, null);

        try
        {
            var parsed = JsonSerializer.Deserialize<BackgroundSettings>(
                File.ReadAllText(_settingsPath));

            return parsed ?? new BackgroundSettings(false, null);
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
            JsonSerializer.Serialize(
                _settings,
                new JsonSerializerOptions { WriteIndented = true }));
    }

    private static readonly string[] ManualWallpaperExtensions =
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".bmp",
        ".webp"
    };

    // Mirrors the Anahita seasonal-gallery contract.
    private static readonly string[] SeasonGalleryExtensions =
    {
        ".svg",
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    private sealed record BackgroundSettings(
        bool SeasonCarouselMode,
        string? SelectedWallpaperPath);
}
