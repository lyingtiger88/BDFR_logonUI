using BDFR.LogonUI.Contracts;

namespace BDFR.LogonUI.Broker;

public sealed class CalendarSnapshotRegistry
{
    private static readonly TimeSpan MaxTtl = TimeSpan.FromMinutes(30);
    private const int MaxAgendaItems = 64;
    private const int MaxReminders = 64;
    private const int MaxTextLength = 256;

    private readonly object _gate = new();
    private CalendarLockSnapshot? _latest;

    public void Publish(CalendarLockSnapshot snapshot)
    {
        Validate(snapshot);

        lock (_gate)
            _latest = snapshot;
    }

    public CalendarLockSnapshot? Read(DateTimeOffset nowUtc, bool isLocked)
    {
        CalendarLockSnapshot? snapshot;

        lock (_gate)
            snapshot = _latest;

        if (snapshot is null || snapshot.ExpiresAtUtc <= nowUtc)
            return null;

        if (!isLocked)
            return snapshot;

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
