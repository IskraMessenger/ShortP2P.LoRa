namespace ShortP2P.LoRa.Directory;

/// <summary>Who owns a MeshId (REQ-DIR-2).</summary>
public enum MeshSubject : byte
{
    Self = 1,
    Peer = 2,
    Server = 3
}
