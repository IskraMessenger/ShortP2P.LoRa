namespace ShortP2P.LoRa.Directory;

/// <summary>
///     One (NetworkId, MeshId) binding (REQ-DIR-2). NetworkId is the 12-byte ShortP2P wire id.
/// </summary>
public sealed record MeshBinding(
    byte[] NetworkId,
    byte[] MeshId,
    MeshSubject Subject,
    bool IsActive,
    bool Confirmed,
    DateTime LastSeenUtc)
{
    public const int NetworkIdLength = 12;

    public string NetworkIdHex
    {
        get => Convert.ToHexString(NetworkId);
    }

    public string MeshIdHex
    {
        get => Convert.ToHexString(MeshId);
    }
}
