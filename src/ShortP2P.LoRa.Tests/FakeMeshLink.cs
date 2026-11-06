using ShortP2P.LoRa.Link;

namespace ShortP2P.LoRa.Tests;

/// <summary>
///     In-memory board link for tests. Pair two instances to get a radio path.
///     Supports TX failure injection and per-frame drop/mutation hooks.
/// </summary>
internal sealed class FakeMeshLink : IMeshLink
{
    private readonly uint _node;

    public FakeMeshLink(uint node)
    {
        _node = node;
    }

    public FakeMeshLink? Peer { get; set; }

    /// <summary>Next N SendAsync calls throw (Send-Retry tests).</summary>
    public int FailNextSends;

    /// <summary>Total SendAsync calls, including failed ones.</summary>
    public int Attempts;

    /// <summary>Hook before delivery: return false to drop the frame (loss), or mutate it (corruption).</summary>
    public Func<uint, byte[], bool>? BeforeDeliver;

    public List<(uint To, byte[] Payload)> Sent { get; } = [];

    public uint? LocalNodeNumber { get; private set; }

    public event Action<MeshLinkFrame>? Received;

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        LocalNodeNumber = _node;
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask SendAsync(uint to, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        Attempts++;
        if (FailNextSends > 0)
        {
            FailNextSends--;
            throw new IOException("injected TX failure");
        }

        var bytes = payload.ToArray();
        Sent.Add((to, bytes));

        var peer = Peer;
        if (peer != null && (BeforeDeliver == null || BeforeDeliver(to, bytes)))
            peer.Received?.Invoke(new MeshLinkFrame(_node, to, bytes));

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public static (FakeMeshLink A, FakeMeshLink B) CreatePair(uint nodeA, uint nodeB)
    {
        var a = new FakeMeshLink(nodeA);
        var b = new FakeMeshLink(nodeB);
        a.Peer = b;
        b.Peer = a;
        return (a, b);
    }
}
