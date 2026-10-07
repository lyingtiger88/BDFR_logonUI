using BDFR.LogonUI.Broker;
using BDFR.LogonUI.Contracts;

var registry = new WidgetRegistry();
var now = DateTimeOffset.UtcNow;

registry.Upsert(new WidgetSnapshot(
    ProviderId: "bdfr.demo",
    WidgetId: "system-status",
    UpdatedAtUtc: now,
    ExpiresAtUtc: now.AddMinutes(5),
    Privacy: PrivacyLevel.Public,
    Fields: new Dictionary<string, string>
    {
        ["cpu"] = "7%",
        ["ram"] = "38%",
        ["network"] = "Connected"
    }));

Console.WriteLine("BDFR LogonUI broker prototype");
foreach (var widget in registry.GetActive(DateTimeOffset.UtcNow, isLocked: true))
{
    Console.WriteLine($"{widget.ProviderId}/{widget.WidgetId}");
    foreach (var field in widget.Fields)
        Console.WriteLine($"  {field.Key}: {field.Value}");
}
