using System.Text;

namespace ShortP2P.LoRa.Reliability;

/// <summary>
///     Retry request frame 0x33 "SP2R" v1 (REQ-RETRY-2): asks the TX node to resend
///     the whole message identified by (TX MeshId, compressed id).
///     Not a chunk and not a chat frame; consumed inside this component.
/// </summary>
public static class RetryRequestCodec
{
    public const byte FrameByte = 0x33;
    public const byte ReasonCrcMismatch = 1;
    public const byte ReasonChunkTimeout = 2;
    public const byte ReasonInconsistentCrc = 3;

    private const byte WireVersion = 1;

    private static ReadOnlySpan<byte> Magic => "SP2R"u8;

    public static byte[] Encode(MeshId txMeshId, string compressedId, byte reason, byte retry)
    {
        Chunking.ChunkCodec.ValidateId(compressedId);
        var mesh = txMeshId.Bytes.Span;
        var buf = new byte[7 + mesh.Length + 6 + 2];
        buf[0] = FrameByte;
        Magic.CopyTo(buf.AsSpan(1));
        buf[5] = WireVersion;
        buf[6] = (byte)mesh.Length;
        mesh.CopyTo(buf.AsSpan(7));
        var o = 7 + mesh.Length;
        Encoding.ASCII.GetBytes(compressedId).CopyTo(buf, o);
        buf[o + 6] = reason;
        buf[o + 7] = retry;
        return buf;
    }

    public static bool TryParse(ReadOnlySpan<byte> frame, out MeshId txMeshId, out string compressedId,
        out byte reason, out byte retry)
    {
        txMeshId = null!;
        compressedId = "";
        reason = 0;
        retry = 0;

        if (frame.Length < 8 || frame[0] != FrameByte || !frame[1..].StartsWith(Magic) || frame[5] != WireVersion)
            return false;

        var meshLen = frame[6];
        if (meshLen is < MeshId.MinLength or > MeshId.MaxLength)
            return false;
        if (frame.Length != 7 + meshLen + 6 + 2)
            return false;

        txMeshId = new MeshId(frame.Slice(7, meshLen));
        var o = 7 + meshLen;
        var id = frame.Slice(o, 6);
        if (!Chunking.ChunkCodec.IsLetter(id[0]))
            return false;
        compressedId = Encoding.ASCII.GetString(id);
        reason = frame[o + 6];
        retry = frame[o + 7];
        return true;
    }
}
