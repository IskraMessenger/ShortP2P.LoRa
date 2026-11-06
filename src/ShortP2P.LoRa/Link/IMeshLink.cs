namespace ShortP2P.LoRa.Link;

/// <summary>One raw radio payload delivered by the board link.</summary>
public readonly record struct MeshLinkFrame(uint From, uint To, byte[] Payload);

/// <summary>
///     Physical link to the LoRa board (REQ-TR-1). Implementations: Meshtastic serial link, fakes in tests.
///     Payloads are opaque to the link — chunking / ShortP2P framing live above it.
/// </summary>
public interface IMeshLink : IAsyncDisposable
{
    /// <summary>Local radio node number, known after start (board-reported).</summary>
    uint? LocalNodeNumber { get; }

    event Action<MeshLinkFrame>? Received;

    ValueTask StartAsync(CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Send one payload to a node (0xFFFFFFFF = broadcast). Throws on TX error → Send-Retry above.</summary>
    ValueTask SendAsync(uint to, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default);
}
