using ShortP2P.Transport.Abstractions;

namespace ShortP2P.LoRa;

/// <summary>
///     TransportAddress helpers for kind 5: one address is one MeshId (REQ-TR-1, REQ-DIR-1).
/// </summary>
public static class MeshatasticTransportAddress
{
    public static TransportAddress ToTransportAddress(MeshId meshId)
    {
        return new TransportAddress(TransportKind.Meshtastic, meshId.Bytes.ToArray());
    }

    public static MeshId FromTransportAddress(TransportAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (address.Kind != TransportKind.Meshtastic)
            throw new ArgumentException(
                $"Transport address kind {address.Kind} is not Meshatastic ({TransportKind.Meshtastic}).",
                nameof(address));
        return new MeshId(address.Data);
    }

    public static bool IsMeshatastic(TransportAddress address)
    {
        return address.Kind == TransportKind.Meshtastic;
    }
}
