using BDFR.LogonUI.Broker;

using var singleInstance = new Mutex(
    initiallyOwned: true,
    name: @"Local\BDFR.LogonUI.Broker",
    createdNew: out var createdNew);

if (!createdNew)
    return;

using var shutdown = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    shutdown.Cancel();
};

var registry = new CalendarSnapshotRegistry();
var server = new BrokerPipeServer(registry);

try
{
    await server.RunAsync(shutdown.Token);
}
catch (OperationCanceledException)
{
    // Normal shutdown.
}
