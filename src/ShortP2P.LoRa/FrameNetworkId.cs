namespace ShortP2P.LoRa;

/// <summary>
///     Extracts the sender NetworkId from cleartext ShortP2P frames that carry it (REQ-DIR-3):
///     invite 0x30 ("SP2I" v1: NetworkId 12 at offset 6) and session setup 0x04 (NetworkId 12 at offset 1).
/// </summary>
internal static class FrameNetworkId
{
    internal static bool TryExtract(ReadOnlySpan<byte> datagram, out byte[] networkId)
    {
        networkId = [];

        if (datagram.Length >= 18 && datagram[0] == 0x30 &&
            datagram[1] == 'S' && datagram[2] == 'P' && datagram[3] == '2' && datagram[4] == 'I')
        {
            networkId = datagram.Slice(6, 12).ToArray();
            return true;
        }

        if (datagram.Length >= 13 && datagram[0] == 0x04)
        {
            networkId = datagram.Slice(1, 12).ToArray();
            return true;
        }

        return false;
    }
}
