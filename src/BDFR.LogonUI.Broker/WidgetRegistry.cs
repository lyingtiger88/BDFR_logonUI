using System.Collections.Concurrent;
using BDFR.LogonUI.Contracts;

namespace BDFR.LogonUI.Broker;

public sealed class WidgetRegistry
{
    private static readonly TimeSpan MaxTtl = TimeSpan.FromHours(1);
    private const int MaxFields = 32;
    private const int MaxFieldLength = 256;

    private readonly ConcurrentDictionary<(string ProviderId, string WidgetId), WidgetSnapshot> _widgets = new();

    public void Upsert(WidgetSnapshot snapshot)
    {
        Validate(snapshot);
        _widgets[(snapshot.ProviderId, snapshot.WidgetId)] = snapshot;
    }

    public IReadOnlyList<WidgetSnapshot> GetActive(DateTimeOffset nowUtc, bool isLocked)
    {
        var result = new List<WidgetSnapshot>();

        foreach (var pair in _widgets)
        {
            var snapshot = pair.Value;

            if (snapshot.ExpiresAtUtc <= nowUtc)
            {
                _widgets.TryRemove(pair.Key, out _);
                continue;
            }

            if (!isLocked)
            {
                result.Add(snapshot);
                continue;
            }

            switch (snapshot.Privacy)
            {
                case PrivacyLevel.Public:
                    result.Add(snapshot);
                    break;

                case PrivacyLevel.Private:
                    result.Add(snapshot with
                    {
                        Fields = new Dictionary<string, string>
                        {
                            ["status"] = "Content hidden while locked"
                        }
                    });
                    break;

                case PrivacyLevel.Secret:
                    break;

                default:
                    throw new InvalidOperationException("Unknown privacy level.");
            }
        }

        return result
            .OrderBy(x => x.ProviderId, StringComparer.Ordinal)
            .ThenBy(x => x.WidgetId, StringComparer.Ordinal)
            .ToArray();
    }

    private static void Validate(WidgetSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.ProviderId))
            throw new ArgumentException("ProviderId is required.", nameof(snapshot));

        if (string.IsNullOrWhiteSpace(snapshot.WidgetId))
            throw new ArgumentException("WidgetId is required.", nameof(snapshot));

        if (snapshot.ExpiresAtUtc <= snapshot.UpdatedAtUtc)
            throw new ArgumentException("Snapshot expiry must be after its update time.", nameof(snapshot));

        if (snapshot.ExpiresAtUtc - snapshot.UpdatedAtUtc > MaxTtl)
            throw new ArgumentException("Snapshot TTL exceeds the broker limit.", nameof(snapshot));

        if (snapshot.Fields.Count > MaxFields)
            throw new ArgumentException("Snapshot contains too many fields.", nameof(snapshot));

        foreach (var (key, value) in snapshot.Fields)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > MaxFieldLength)
                throw new ArgumentException("Invalid field key.", nameof(snapshot));

            if (value.Length > MaxFieldLength)
                throw new ArgumentException("Field value exceeds the broker limit.", nameof(snapshot));
        }
    }
}
