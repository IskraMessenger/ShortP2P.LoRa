using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Crc32Arm = System.Runtime.Intrinsics.Arm.Crc32;

namespace ShortP2P.LoRa;

/// <summary>
///     CRC-32 (IEEE 802.3: polynomial 0xEDB88320 reflected, init 0xFFFFFFFF, final XOR 0xFFFFFFFF) —
///     the same value as ZIP / System.IO.Hashing.Crc32.
///     Uses the ARMv8 CRC32 instruction when the target supports it; otherwise a 16-entry
///     nibble table (4 bits at a time). Both paths produce identical bytes.
///     Note: the x86 SSE4.2 "crc32" instruction computes CRC-32C (Castagnoli), not this polynomial,
///     so x86 targets intentionally use the table path.
/// </summary>
public static class Crc32
{
    /// <summary>CRC type byte used on the wire for this algorithm (REQ-CRC-1).</summary>
    public const byte WireType = 1;

    private const uint Polynomial = 0xEDB88320;

    private static readonly uint[] Table = BuildTable();

    private static uint[] BuildTable()
    {
        // Nibble-at-a-time table: each entry is its index advanced by 4 bit-steps,
        // matching the two "(crc >> 4) ^ Table[...]" steps per byte in ComputeTable.
        var table = new uint[16];
        for (uint i = 0; i < 16; i++)
        {
            var crc = i;
            for (var bit = 0; bit < 4; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ Polynomial : crc >> 1;
            table[i] = crc;
        }

        return table;
    }

    public static uint Compute(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        crc = Crc32Arm.IsSupported ? ComputeHardware(data, crc) : ComputeTable(data, crc);
        return ~crc;
    }

    /// <summary>Software table path (also used as the reference in tests).</summary>
    internal static uint ComputeTable(ReadOnlySpan<byte> data, uint crc)
    {
        foreach (var b in data)
        {
            crc = (crc >> 4) ^ Table[(crc ^ b) & 0xF];
            crc = (crc >> 4) ^ Table[(crc ^ (b >> 4)) & 0xF];
        }

        return crc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ComputeHardware(ReadOnlySpan<byte> data, uint crc)
    {
        var offset = 0;
        if (Crc32Arm.Arm64.IsSupported)
            while (offset + 8 <= data.Length)
            {
                crc = Crc32Arm.Arm64.ComputeCrc32(crc, BinaryPrimitives.ReadUInt64LittleEndian(data[offset..]));
                offset += 8;
            }

        while (offset + 4 <= data.Length)
        {
            crc = Crc32Arm.ComputeCrc32(crc, BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]));
            offset += 4;
        }

        while (offset < data.Length)
        {
            crc = Crc32Arm.ComputeCrc32(crc, data[offset]);
            offset++;
        }

        return crc;
    }
}
