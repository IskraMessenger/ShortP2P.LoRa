using System.Buffers.Binary;

namespace ShortP2P.LoRa;

/// <summary>
///     Internal id of one Meshatastic/LoRa device: opaque bytes, length 1–8 (REQ-DIR-1).
///     When the device exposes a hardware address this is its MAC (6 bytes);
///     for Meshtastic boards it is the 4-byte node number, big-endian.
/// </summary>
public sealed class MeshId : IEquatable<MeshId>
{
    public const int MinLength = 1;
    public const int MaxLength = 8;

    private readonly byte[] _bytes;

    public MeshId(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is < MinLength or > MaxLength)
            throw new ArgumentOutOfRangeException(nameof(bytes), $"MeshId length must be {MinLength}..{MaxLength} bytes.");
        _bytes = bytes.ToArray();
    }

    public ReadOnlyMemory<byte> Bytes => _bytes;

    public int Length => _bytes.Length;

    /// <summary>MeshId from a Meshtastic 32-bit node number (big-endian on the wire).</summary>
    public static MeshId FromNodeNumber(uint nodeNumber)
    {
        var b = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(b, nodeNumber);
        return new MeshId(b);
    }

    /// <summary>Meshtastic node number. Valid only for 4-byte MeshIds.</summary>
    public uint ToNodeNumber()
    {
        if (_bytes.Length != 4)
            throw new InvalidOperationException("Only a 4-byte MeshId maps to a Meshtastic node number.");
        return BinaryPrimitives.ReadUInt32BigEndian(_bytes);
    }

    public bool TryToNodeNumber(out uint nodeNumber)
    {
        nodeNumber = 0;
        if (_bytes.Length != 4)
            return false;
        nodeNumber = BinaryPrimitives.ReadUInt32BigEndian(_bytes);
        return true;
    }

    public bool Equals(MeshId? other)
    {
        return other is not null && _bytes.AsSpan().SequenceEqual(other._bytes);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as MeshId);
    }

    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.AddBytes(_bytes);
        return hc.ToHashCode();
    }

    public string ToHex()
    {
        return Convert.ToHexString(_bytes);
    }

    public override string ToString()
    {
        return ToHex();
    }
}
