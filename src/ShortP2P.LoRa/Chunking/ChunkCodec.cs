using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace ShortP2P.LoRa.Chunking;

/// <summary>
///     Chunk framing for datagrams larger than one radio payload (REQ-CHUNK-1).
///     Header: 6-char id | chunk number uint16 BE (1-based) | total uint16 BE |
///             CRC type uint8 | CRC of the whole datagram uint32 BE.
///     A chunk starts with a letter so it never collides with ShortP2P frame bytes
///     (0x30/0x31/0x32 are ASCII '0'..'2').
/// </summary>
public static class ChunkCodec
{
    public const short IdLength = 6;
    public const short HeaderLength = 15;

    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const string LettersAndDigits = Letters + "0123456789";

    public static bool IsLetter(byte b) => b is >= (byte)'A' and <= (byte)'Z' or >= (byte)'a' and <= (byte)'z';

    /// <summary>New compressed id: [A-Za-z][A-Za-z0-9]{5}.</summary>
    public static string NewId()
    {
        Span<byte> random = stackalloc byte[IdLength];
        RandomNumberGenerator.Fill(random);
        var chars = new char[IdLength];
        chars[0] = Letters[random[0] % Letters.Length];
        for (var i = 1; i < IdLength; i++)
            chars[i] = LettersAndDigits[random[i] % LettersAndDigits.Length];
        return new string(chars);
    }

    public static void ValidateId(string id)
    {
        if (id.Length != IdLength || !IsLetter((byte)id[0]))
            throw new ArgumentException("Compressed id must be 6 chars, first one a letter.", nameof(id));
        
        foreach (var c in id)
            if (!IsLetter((byte)c) && c is < '0' or > '9')
                throw new ArgumentException("Compressed id may contain only A-Za-z0-9.", nameof(id));
    }

    /// <summary>Write one chunk. Returns bytes written (HeaderLength + data.Length).</summary>
    public static int WriteChunk(Span<byte> destination, string id, ushort number, ushort total, uint crc,
        ReadOnlySpan<byte> data)
    {
        ValidateId(id);
        if (number == 0 || total < 2 || number > total)
            throw new ArgumentOutOfRangeException(nameof(number), "Number must be 1..total and total >= 2.");
        if (destination.Length < HeaderLength + data.Length)
            throw new ArgumentException("Destination too small.", nameof(destination));

        Encoding.ASCII.GetBytes(id.AsSpan(), destination);
        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(6, 2), number);
        BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(8, 2), total);
        destination[10] = Crc32.WireType;
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(11, 4), crc);
        data.CopyTo(destination[HeaderLength..]);
        return HeaderLength + data.Length;
    }

    public static bool TryParse(ReadOnlySpan<byte> frame, out MeshChunk chunk)
    {
        chunk = default;
        if (frame.Length <= HeaderLength || !IsLetter(frame[0]))
            return false;

        for (var i = 0; i < IdLength; i++)
            if (!IsLetter(frame[i]) && frame[i] is < (byte)'0' or > (byte)'9')
                return false;

        var number = BinaryPrimitives.ReadUInt16BigEndian(frame.Slice(6, 2));
        var total = BinaryPrimitives.ReadUInt16BigEndian(frame.Slice(8, 2));
        if (total < 2 || number == 0 || number > total)
            return false;

        var id = Encoding.ASCII.GetString(frame[..IdLength]);
        var crcType = frame[10];
        var crc = BinaryPrimitives.ReadUInt32BigEndian(frame.Slice(11, 4));
        chunk = new MeshChunk(id, number, total, crcType, crc, frame[HeaderLength..].ToArray());
        
        return true;
    }
}
