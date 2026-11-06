namespace ShortP2P.LoRa;

/// <summary>
///     Transport kind identifiers contributed by the LoRa / Meshtastic library.
///     Values must not collide with well-known kinds in ShortP2P.Transport.Abstractions.TransportKind.
/// </summary>
public static class TransportKind
{
    /// <summary>Meshtastic mesh transport.</summary>
    public const byte Meshtastic = 5;
}
