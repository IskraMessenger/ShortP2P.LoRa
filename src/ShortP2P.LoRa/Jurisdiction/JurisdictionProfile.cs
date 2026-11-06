namespace ShortP2P.LoRa.Jurisdiction;

/// <summary>
///     One jurisdiction / region profile: frequency band, duty-cycle cap, TX power cap (REQ-RF-4).
///     Values are the firmware ceiling Meshtastic documents, not legal advice.
/// </summary>
public sealed record JurisdictionProfile(
    string Code,
    double FrequencyMinMHz,
    double FrequencyMaxMHz,
    double DutyCycleMaxPercent,
    int PowerMaxDbm)
{
    /// <summary>UNSET means no TX until a region is chosen (REQ-RF-2).</summary>
    public bool IsUnset => string.Equals(Code, JurisdictionCatalog.UnsetCode, StringComparison.Ordinal);
}
