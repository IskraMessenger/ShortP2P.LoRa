using System.Buffers.Binary;
using Microsoft.Extensions.Logging;
using ShortP2P.LoRa;
using ShortP2P.LoRa.Jurisdiction;

// Meshatastic air demo: sends 10-byte and 1-KB datagrams in a loop and prints
// every received datagram with an integrity check. Run on two boards and watch
// the other side print your datagrams.

if (args.Contains("--help") || args.Contains("-h"))
{
    PrintUsage();
    return 0;
}

var port = Arg(args, "port");
var regionCode = Arg(args, "region");
if (string.IsNullOrWhiteSpace(port) || string.IsNullOrWhiteSpace(regionCode))
{
    Console.Error.WriteLine("Error: --port and --region are required.");
    PrintUsage();
    return 2;
}

JurisdictionProfile region;
try
{
    region = JurisdictionCatalog.Require(regionCode);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine("Known regions: " + string.Join(", ", JurisdictionCatalog.Regions.Select(r => r.Code)));
    return 2;
}

var to = ParseNode(Arg(args, "to") ?? "broadcast");
var intervalSeconds = double.TryParse(Arg(args, "interval"), out var iv) ? iv : 5.0;

var logger = new ConsoleLogger();
var options = new MeshatasticTransportOptions
{
    ComPort = port,
    Jurisdiction = region
};
await using var transport = new MeshatasticTransport(options, logger);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await transport.StartAsync(cts.Token);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to start the transport on {port}: {ex.Message}");
    Console.Error.WriteLine("Check the port name, cable, and that the board runs Meshtastic firmware.");
    return 1;
}

Console.WriteLine($"Local node: {transport.LocalMeshId?.ToHex()} ({transport.LocalMeshId?.ToNodeNumber()})");
Console.WriteLine($"Region: {region.Code}; destination: 0x{to:X8}; interval: {intervalSeconds:0.#} s. Ctrl+C to stop.");

// Receiver: print every inbound datagram with an integrity check.
var rxTask = Task.Run(async () =>
{
    try
    {
        await foreach (var msg in transport.Inbound.ReadAllAsync(cts.Token))
        {
            string from;
            try
            {
                from = MeshatasticTransportAddress.FromTransportAddress(msg.RemoteAddress).ToHex();
            }
            catch
            {
                from = "?";
            }

            Console.WriteLine($"{Timestamp()} RX {msg.Payload.Length,5} B from {from}: {Describe(msg.Payload.Span)}");
        }
    }
    catch (OperationCanceledException)
    {
        // shutdown
    }
});

// Sender: alternate 10-byte and 1-KB datagrams.
var sizes = new[] { 10, 1024 };
uint seq = 0;
var round = 0;
while (!cts.IsCancellationRequested)
{
    var size = sizes[round++ % sizes.Length];
    var payload = BuildPayload(size, ++seq);
    try
    {
        await transport.SendAsync(payload,
            MeshatasticTransportAddress.ToTransportAddress(MeshId.FromNodeNumber(to)), cts.Token);
        var chunks = size <= 233 ? 1 : (size + 217) / 218;
        Console.WriteLine($"{Timestamp()} TX {size,5} B seq {seq} ({chunks} radio frame{(chunks > 1 ? "s" : "")})");
    }
    catch (OperationCanceledException)
    {
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{Timestamp()} TX {size,5} B seq {seq} FAILED: {ex.Message}");
    }

    try
    {
        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

await transport.StopAsync();
await rxTask;
return 0;

// ---- helpers ----

static string? Arg(string[] argv, string name)
{
    for (var i = 0; i < argv.Length; i++)
    {
        if (argv[i] == "--" + name && i + 1 < argv.Length)
            return argv[i + 1];
        if (argv[i].StartsWith("--" + name + "=", StringComparison.Ordinal))
            return argv[i][(name.Length + 3)..];
    }

    return null;
}

static uint ParseNode(string text)
{
    if (text.Equals("broadcast", StringComparison.OrdinalIgnoreCase))
        return MeshatasticTransport.BroadcastNode;
    if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        return Convert.ToUInt32(text[2..], 16);
    return uint.Parse(text);
}

static string Timestamp()
{
    return DateTime.Now.ToString("HH:mm:ss.fff");
}

static byte PatternByte(int offset, uint seq)
{
    return unchecked((byte)(offset * 31 + seq));
}

static byte[] BuildPayload(int size, uint seq)
{
    var p = new byte[size];
    p[0] = 0x02; // cipher frame marker — the transport carries ShortP2P chat frames only
    BinaryPrimitives.WriteUInt32BigEndian(p.AsSpan(1), seq);
    for (var i = 5; i < size; i++)
        p[i] = PatternByte(i, seq);
    return p;
}

static string Describe(ReadOnlySpan<byte> p)
{
    if (p.Length >= 5 && p[0] == 0x02)
    {
        var seq = BinaryPrimitives.ReadUInt32BigEndian(p[1..]);
        for (var k = 5; k < p.Length; k++)
            if (p[k] != PatternByte(k, seq))
                return $"seq {seq}: CORRUPT at offset {k} (0x{p[k]:X2}, expected 0x{PatternByte(k, seq):X2})";
        return $"seq {seq}: pattern OK ({p.Length} B)";
    }

    var head = Convert.ToHexString(p[..Math.Min(16, p.Length)]);
    return $"frame 0x{p[0]:X2}, {p.Length} B: {head}{(p.Length > 16 ? "…" : "")}";
}

static void PrintUsage()
{
    Console.WriteLine("""
                      Meshatastic air demo — sends 10-byte and 1-KB datagrams in a loop,
                      prints every received datagram with an integrity check.

                      Usage:
                        meshatastic-demo --port COM3 --region EU_868 [--to broadcast] [--interval 5]

                      Options:
                        --port       COM port of the Meshtastic board (required), e.g. COM3 or /dev/ttyUSB0
                        --region     Jurisdiction/region code (required; TX stays blocked without it)
                        --to         Destination node number: decimal, 0x hex, or "broadcast" (default)
                        --interval   Seconds between datagrams (default 5)

                      Run the same command on the second board and watch each side print
                      the other side's datagrams.

                      Known regions:
                      """
                      + string.Join(", ", JurisdictionCatalog.Regions.Select(r => r.Code)));
}

/// <summary>Minimal console logger for transport internals (retries, drops, resends).</summary>
internal sealed class ConsoleLogger : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Information;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = logLevel >= LogLevel.Error
            ? ConsoleColor.Red
            : logLevel >= LogLevel.Warning
                ? ConsoleColor.Yellow
                : ConsoleColor.DarkGray;
        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{logLevel}] {formatter(state, exception)}");
        Console.ForegroundColor = prev;
    }
}
