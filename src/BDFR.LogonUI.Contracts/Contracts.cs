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
