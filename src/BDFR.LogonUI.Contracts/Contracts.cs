namespace BDFR.LogonUI.Contracts;

public enum PrivacyLevel
{
    Public = 0,
    Private = 1,
    Secret = 2
}

public sealed record WidgetManifest(
    string ProviderId,
    string WidgetId,
    string DisplayName,
    int SchemaVersion = 1);

public sealed record WidgetSnapshot(
    string ProviderId,
    string WidgetId,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    PrivacyLevel Privacy,
    IReadOnlyDictionary<string, string> Fields);

public sealed record LockNotification(
    string ProviderId,
    string NotificationId,
    string Title,
    string? Body,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    PrivacyLevel Privacy = PrivacyLevel.Private);

public sealed record CalendarAgendaItem(
    string Id,
    string Kind,
    string Title,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    bool AllDay,
    PrivacyLevel Privacy = PrivacyLevel.Private);

public sealed record CalendarReminderItem(
    string Id,
    string Title,
    DateTimeOffset FireAtUtc,
    PrivacyLevel Privacy = PrivacyLevel.Private);

public sealed record CalendarLockSnapshot(
    string ProviderId,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string PersianDate,
    string GregorianDate,
    string HijriDate,
    IReadOnlyList<CalendarAgendaItem> Agenda,
    IReadOnlyList<CalendarReminderItem> Reminders,
    int SchemaVersion = 1);

public sealed record BrokerRequest(
    string Operation,
    string? ProviderId = null,
    CalendarLockSnapshot? Calendar = null,
    bool IsLocked = true,
    int ProtocolVersion = 1);

public sealed record BrokerResponse(
    bool Success,
    string? Error = null,
    CalendarLockSnapshot? Calendar = null,
    int ProtocolVersion = 1);
