using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using BDFR.LogonUI.Contracts;

namespace BDFR.LogonUI.Demo;

public sealed class CalendarBrokerClient
{
    public const string PipeName = "BDFR.LogonUI.Broker.v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<bool> EnsureBrokerAsync(CancellationToken cancellationToken = default)
    {
        if (await PingAsync(cancellationToken))
            return true;

        var brokerPath = Path.Combine(AppContext.BaseDirectory, "BDFR.LogonUI.Broker.exe");
        if (!File.Exists(brokerPath))
            return false;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = brokerPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory
            });
        }
        catch
        {
            return false;
        }

        for (var attempt = 0; attempt < 8; attempt++)
        {
            await Task.Delay(180, cancellationToken);
            if (await PingAsync(cancellationToken))
                return true;
        }

        return false;
    }

    public async Task<CalendarLockSnapshot?> ReadCalendarAsync(
        bool isLocked,
        CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(
            new BrokerRequest("read-calendar", IsLocked: isLocked),
            cancellationToken);

        return response?.Success == true
            ? response.Calendar
            : null;
    }

    private async Task<bool> PingAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await SendAsync(new BrokerRequest("ping"), cancellationToken);
            return response?.Success == true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<BrokerResponse?> SendAsync(
        BrokerRequest request,
        CancellationToken cancellationToken)
    {
        await using var pipe = new NamedPipeClientStream(
            ".",
            PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        await pipe.ConnectAsync(350, cancellationToken);

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

        await writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions));
        var responseLine = await reader.ReadLineAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(responseLine)
            ? null
            : JsonSerializer.Deserialize<BrokerResponse>(responseLine, JsonOptions);
    }
}
