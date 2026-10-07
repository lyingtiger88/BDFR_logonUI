using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using BDFR.LogonUI.Contracts;

namespace BDFR.LogonUI.Broker;

public sealed class BrokerPipeServer
{
    public const string PipeName = "BDFR.LogonUI.Broker.v1";
    private const int MaxMessageBytes = 64 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly CalendarSnapshotRegistry _calendar;

    public BrokerPipeServer(CalendarSnapshotRegistry calendar)
    {
        _calendar = calendar;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var pipe = new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut,
                4,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
                4096,
                4096);

            await pipe.WaitForConnectionAsync(cancellationToken);

            try
            {
                await HandleClientAsync(pipe, cancellationToken);
            }
            catch
            {
                // Per-client failures must never tear down the broker.
            }
        }
    }

    private async Task HandleClientAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            stream,
            new UTF8Encoding(false),
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);

        using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(false),
            bufferSize: 4096,
            leaveOpen: true)
        {
            AutoFlush = true
        };

        var line = await ReadBoundedLineAsync(reader, cancellationToken);
        if (line is null)
            return;

        BrokerResponse response;

        try
        {
            var request = JsonSerializer.Deserialize<BrokerRequest>(line, JsonOptions)
                          ?? throw new InvalidOperationException("Request body was empty.");

            response = Handle(request);
        }
        catch (Exception ex)
        {
            response = new BrokerResponse(false, ex.Message);
        }

        await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private BrokerResponse Handle(BrokerRequest request)
    {
        if (request.ProtocolVersion != 1)
            return new BrokerResponse(false, "Unsupported protocol version.");

        return request.Operation switch
        {
            "publish-calendar" => PublishCalendar(request),
            "read-calendar" => ReadCalendar(request),
            "ping" => new BrokerResponse(true),
            _ => new BrokerResponse(false, "Unsupported broker operation.")
        };
    }

    private BrokerResponse PublishCalendar(BrokerRequest request)
    {
        if (request.Calendar is null)
            return new BrokerResponse(false, "Calendar payload is required.");

        if (!string.Equals(request.ProviderId, request.Calendar.ProviderId, StringComparison.Ordinal))
            return new BrokerResponse(false, "Provider identity mismatch.");

        _calendar.Publish(request.Calendar);
        return new BrokerResponse(true);
    }

    private BrokerResponse ReadCalendar(BrokerRequest request)
    {
        var snapshot = _calendar.Read(DateTimeOffset.UtcNow, request.IsLocked);
        return new BrokerResponse(true, Calendar: snapshot);
    }

    private static async Task<string?> ReadBoundedLineAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        var buffer = new char[1024];
        var builder = new StringBuilder();

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
                return builder.Length == 0 ? null : builder.ToString();

            for (var i = 0; i < read; i++)
            {
                var ch = buffer[i];

                if (ch == '\n')
                    return builder.ToString();

                if (ch != '\r')
                    builder.Append(ch);

                if (Encoding.UTF8.GetByteCount(builder.ToString()) > MaxMessageBytes)
                    throw new InvalidOperationException("Broker message exceeds size limit.");
            }
        }
    }
}
