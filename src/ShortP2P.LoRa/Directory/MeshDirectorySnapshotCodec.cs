using System.Buffers.Binary;

namespace ShortP2P.LoRa.Directory;

/// <summary>
///     Directory snapshot frame 0x32 "SP2M" v1 (REQ-DIR-4). Cleartext, so the map is usable
///     before a crypto session exists. Consumed inside this component; never delivered to the messenger.
///     Layout per record: NetworkId 12 | subject uint8 | meshCount uint16 BE |
///     per MeshId: meshIdLen uint8 | MeshId bytes | IsActive uint8 | Confirmed uint8.
/// </summary>
public static class MeshDirectorySnapshotCodec
{
    public const byte FrameByte = 0x32;

    private const byte WireVersion = 1;
    private const int HeaderLength = 20; // 0x32 + SP2M + ver + announcer 12 + count 2

    private static ReadOnlySpan<byte> Magic => "SP2M"u8;

    public static byte[] Encode(byte[] announcerNetworkId, IReadOnlyList<MeshBinding> bindings)
    {
        if (announcerNetworkId.Length != MeshBinding.NetworkIdLength)
            throw new ArgumentException("Announcer NetworkId must be 12 bytes.", nameof(announcerNetworkId));

        var grouped = bindings
            .GroupBy(b => Convert.ToHexString(b.NetworkId))
            .ToList();
        if (grouped.Count > ushort.MaxValue)
            throw new ArgumentException("Too many records.", nameof(bindings));

        using var ms = new MemoryStream();
        ms.WriteByte(FrameByte);
        ms.Write(Magic);
        ms.WriteByte(WireVersion);
        ms.Write(announcerNetworkId);
        Span<byte> u16 = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(u16, (ushort)grouped.Count);
        ms.Write(u16);

        foreach (var group in grouped)
        {
            var first = group.First();
            ms.Write(first.NetworkId);
            ms.WriteByte((byte)first.Subject);
            BinaryPrimitives.WriteUInt16BigEndian(u16, (ushort)group.Count());
            ms.Write(u16);
            foreach (var b in group)
            {
                ms.WriteByte((byte)b.MeshId.Length);
                ms.Write(b.MeshId);
                ms.WriteByte(b.IsActive ? (byte)1 : (byte)0);
                ms.WriteByte(b.Confirmed ? (byte)1 : (byte)0);
            }
        }

        return ms.ToArray();
    }

    public static bool TryParse(ReadOnlySpan<byte> frame, out byte[] announcerNetworkId,
        out IReadOnlyList<MeshBinding> bindings)
    {
        announcerNetworkId = [];
        bindings = [];

        if (frame.Length < HeaderLength || frame[0] != FrameByte || !frame[1..].StartsWith(Magic) ||
            frame[5] != WireVersion)
            return false;

        announcerNetworkId = frame.Slice(6, MeshBinding.NetworkIdLength).ToArray();
        var recordCount = BinaryPrimitives.ReadUInt16BigEndian(frame.Slice(18, 2));
        var o = HeaderLength;
        var result = new List<MeshBinding>();

        for (var r = 0; r < recordCount; r++)
        {
            if (frame.Length < o + MeshBinding.NetworkIdLength + 3)
                return false;
            var net = frame.Slice(o, MeshBinding.NetworkIdLength).ToArray();
            o += MeshBinding.NetworkIdLength;
            var subject = frame[o++];
            if (subject is < (byte)MeshSubject.Self or > (byte)MeshSubject.Server)
                return false;
            var meshCount = BinaryPrimitives.ReadUInt16BigEndian(frame.Slice(o, 2));
            o += 2;

            for (var m = 0; m < meshCount; m++)
            {
                if (frame.Length < o + 1)
                    return false;
                var len = frame[o++];
                if (len is < MeshId.MinLength or > MeshId.MaxLength || frame.Length < o + len + 2)
                    return false;
                var mesh = frame.Slice(o, len).ToArray();
                o += len;
                var isActive = frame[o++] != 0;
                var confirmed = frame[o++] != 0;
                result.Add(new MeshBinding(net, mesh, (MeshSubject)subject, isActive, confirmed, DateTime.MinValue));
            }
        }

        if (o != frame.Length)
            return false;

        bindings = result;
        return true;
    }
}
