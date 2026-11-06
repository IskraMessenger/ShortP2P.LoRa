namespace ShortP2P.LoRa.Chunking;

/// <summary>One parsed chunk of a split datagram (REQ-CHUNK-1).</summary>
public readonly record struct MeshChunk(
    string Id,
    ushort Number,
    ushort Total,
    byte CrcType,
    uint Crc,
    byte[] Data);
