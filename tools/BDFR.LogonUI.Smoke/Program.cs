using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using BDFR.LogonUI.Broker;
using BDFR.LogonUI.Contracts;

var registry = new CalendarSnapshotRegistry();
var server = new BrokerPipeServer(registry);
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

var serverTask = server.RunAsync(cts.Token);
await Task.Delay(150, cts.Token);

var now = DateTimeOffset.UtcNow;
var snapshot = new CalendarLockSnapshot(
    "bdfr.anahita",
    now,
    now.AddMinutes(5),
    "امروز",
    "2026-10-07",
    "١٤٤٨/٠٤/٢٥",
    new[]
    {
        new CalendarAgendaItem("pub", "occasion", "مناسبت عمومی", null, null, true, PrivacyLevel.Public),
        new CalendarAgendaItem("priv", "event", "جلسه محرمانه", now.AddHours(1), null, false, PrivacyLevel.Private),
        new CalendarAgendaItem("secret", "event", "نباید دیده شود", now.AddHours(2), null, false, PrivacyLevel.Secret)
    },
    new[]
    {
        new CalendarReminderItem("r1", "یادآوری شخصی", now.AddMinutes(30), PrivacyLevel.Private),
        new CalendarReminderItem("r2", "یادآوری مخفی", now.AddMinutes(45), PrivacyLevel.Secret)
    });

var publish = await SendAsync(
    new BrokerRequest("publish-calendar", "bdfr.anahita", snapshot),
    cts.Token);

Assert(publish.Success, "publish failed");

var locked = await SendAsync(
    new BrokerRequest("read-calendar", IsLocked: true),
    cts.Token);

Assert(locked.Success, "locked read failed");
Assert(locked.Calendar is not null, "locked calendar missing");
Assert(locked.Calendar!.Agenda.Count == 2, "secret agenda leaked or count mismatch");
Assert(locked.Calendar.Agenda.Single(x => x.Id == "pub").Title == "مناسبت عمومی", "public title changed");
Assert(locked.Calendar.Agenda.Single(x => x.Id == "priv").Title == "رویداد خصوصی", "private title was not masked");
Assert(locked.Calendar.Reminders.Count == 1, "secret reminder leaked");
Assert(locked.Calendar.Reminders[0].Title == "یادآوری خصوصی", "private reminder was not masked");

var unlocked = await SendAsync(
    new BrokerRequest("read-calendar", IsLocked: false),
    cts.Token);

Assert(unlocked.Success, "unlocked read failed");
Assert(unlocked.Calendar is not null, "unlocked calendar missing");
Assert(unlocked.Calendar!.Agenda.Count == 3, "unlocked agenda should contain all items");
Assert(unlocked.Calendar.Agenda.Single(x => x.Id == "priv").Title == "جلسه محرمانه", "unlocked private title changed");
Assert(unlocked.Calendar.Reminders.Count == 2, "unlocked reminders should contain all items");

cts.Cancel();

try
{
    await serverTask;
}
catch (OperationCanceledException)
{
}

Console.WriteLine("BDFR LogonUI broker smoke test passed.");

static async Task<BrokerResponse> SendAsync(BrokerRequest request, CancellationToken cancellationToken)
{
    await using var pipe = new NamedPipeClientStream(
        ".",
        BrokerPipeServer.PipeName,
        PipeDirection.InOut,
        PipeOptions.Asynchronous);

    await pipe.ConnectAsync(1000, cancellationToken);

    using var reader = new StreamReader(
        pipe,
        new UTF8Encoding(false),
        detectEncodingFromByteOrderMarks: false,
        bufferSize: 4096,
        leaveOpen: true);

    using var writer = new StreamWriter(
        pipe,
        new UTF8Encoding(false),
        bufferSize: 4096,
        leaveOpen: true)
    {
        AutoFlush = true
    };

    await writer.WriteLineAsync(JsonSerializer.Serialize(request));
    var line = await reader.ReadLineAsync(cancellationToken);

    if (string.IsNullOrWhiteSpace(line))
        throw new InvalidOperationException("Broker returned an empty response.");

    return JsonSerializer.Deserialize<BrokerResponse>(line)
           ?? throw new InvalidOperationException("Broker returned invalid JSON.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
