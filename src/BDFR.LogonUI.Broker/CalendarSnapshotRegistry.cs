using System.Text.Json;
using BDFR.LogonUI.Contracts;

namespace BDFR.LogonUI.Broker;

public sealed class CalendarSnapshotRegistry
{
    private static readonly TimeSpan MaxTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan MinPublishInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan SafeCacheLifetime = TimeSpan.FromHours(6);

    private const int MaxAgendaItems = 64;
    private const int MaxReminders = 64;
    private const int MaxTextLength = 256;

    private readonly object _gate = new();
    private readonly string _safeCachePath;

    private CalendarLockSnapshot? _latest;
    private CalendarLockSnapshot? _safeCache;
    private DateTimeOffset _lastPublishedAtUtc;

    public CalendarSnapshotRegistry()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BDFR",
            "LogonUI");

        Directory.CreateDirectory(root);
        _safeCachePath = Path.Combine(root, "broker.calendar.safe.json");
        _safeCache = TryLoadSafeCache();
    }

    public void Publish(CalendarLockSnapshot snapshot)
    {
        Validate(snapshot);

        var now = DateTimeOffset.UtcNow;
        CalendarLockSnapshot safe;

        lock (_gate)
        {
            if (now - _lastPublishedAtUtc < MinPublishInterval)
                throw new InvalidOperationException("Calendar provider is publishing too quickly.");

            _lastPublishedAtUtc = now;
            _latest = snapshot;

            safe = SanitizeForLock(snapshot) with
            {
                ExpiresAtUtc = now.Add(SafeCacheLifetime)
            };

            _safeCache = safe;
        }

        TrySaveSafeCache(safe);
    }

    public CalendarLockSnapshot? Read(DateTimeOffset nowUtc, bool isLocked)
    {
        CalendarLockSnapshot? live;
        CalendarLockSnapshot? cached;

        lock (_gate)
        {
            live = _latest;
            cached = _safeCache;
        }

        if (live is not null && live.ExpiresAtUtc > nowUtc)
            return isLocked ? SanitizeForLock(live) : live;

        if (isLocked && cached is not null && cached.ExpiresAtUtc > nowUtc)
            return cached;

        return null;
    }

    private static CalendarLockSnapshot SanitizeForLock(CalendarLockSnapshot snapshot)
    {
        var agenda = snapshot.Agenda
            .Where(x => x.Privacy != PrivacyLevel.Secret)
            .Select(x => x.Privacy == PrivacyLevel.Public
                ? x
                : x with { Title = "رویداد خصوصی" })
            .ToArray();

        var reminders = snapshot.Reminders
            .Where(x => x.Privacy != PrivacyLevel.Secret)
            .Select(x => x.Privacy == PrivacyLevel.Public
                ? x
                : x with { Title = "یادآوری خصوصی" })
            .ToArray();

        return snapshot with
        {
            Agenda = agenda,
            Reminders = reminders
        };
    }

    private CalendarLockSnapshot? TryLoadSafeCache()
    {
        if (!File.Exists(_safeCachePath))
            return null;

        try
        {
            var snapshot = JsonSerializer.Deserialize<CalendarLockSnapshot>(
                File.ReadAllText(_safeCachePath));

            if (snapshot is null ||
                snapshot.SchemaVersion != 1 ||
                !string.Equals(snapshot.ProviderId, "bdfr.anahita", StringComparison.Ordinal) ||
                snapshot.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                return null;
            }

            return snapshot;
        }
        catch
        {
            return null;
        }
    }

    private void TrySaveSafeCache(CalendarLockSnapshot snapshot)
    {
        try
        {
            var temp = _safeCachePath + ".tmp";
            File.WriteAllText(
                temp,
                JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, _safeCachePath, overwrite: true);
        }
        catch
        {
            // Cache persistence is best effort and must not break live broker IPC.
        }
    }

    private static void Validate(CalendarLockSnapshot snapshot)
    {
        if (snapshot.SchemaVersion != 1)
            throw new ArgumentException("Unsupported calendar schema version.", nameof(snapshot));

        if (!string.Equals(snapshot.ProviderId, "bdfr.anahita", StringComparison.Ordinal))
            throw new ArgumentException("Calendar provider is not authorized.", nameof(snapshot));

        if (snapshot.ExpiresAtUtc <= snapshot.GeneratedAtUtc)
            throw new ArgumentException("Calendar snapshot expiry is invalid.", nameof(snapshot));

        if (snapshot.ExpiresAtUtc - snapshot.GeneratedAtUtc > MaxTtl)
            throw new ArgumentException("Calendar snapshot TTL exceeds the broker limit.", nameof(snapshot));

        if (snapshot.Agenda.Count > MaxAgendaItems)
            throw new ArgumentException("Calendar snapshot contains too many agenda items.", nameof(snapshot));

        if (snapshot.Reminders.Count > MaxReminders)
            throw new ArgumentException("Calendar snapshot contains too many reminders.", nameof(snapshot));

        ValidateText(snapshot.PersianDate, nameof(snapshot.PersianDate));
        ValidateText(snapshot.GregorianDate, nameof(snapshot.GregorianDate));
        ValidateText(snapshot.HijriDate, nameof(snapshot.HijriDate));

        foreach (var item in snapshot.Agenda)
        {
            ValidateText(item.Id, "agenda id");
            ValidateText(item.Kind, "agenda kind");
            ValidateText(item.Title, "agenda title");
        }

        foreach (var reminder in snapshot.Reminders)
        {
            ValidateText(reminder.Id, "reminder id");
            ValidateText(reminder.Title, "reminder title");
        }
    }

    private static void ValidateText(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxTextLength)
            throw new ArgumentException($"Invalid {field}.");
    }
}
