namespace ShortP2P.LoRa.Jurisdiction;

/// <summary>
///     Standard Meshtastic-aligned region catalog (REQ-RF-4) with country → region presets (REQ-RF-5).
///     Versioned against the Meshtastic LoRa region table; refresh when upstream changes.
/// </summary>
public static class JurisdictionCatalog
{
    public const string UnsetCode = "UNSET";

    public static readonly JurisdictionProfile Unset = new(UnsetCode, 0, 0, 0, 0);

    public static readonly IReadOnlyList<JurisdictionProfile> Regions =
    [
        Unset,
        new("US", 902.0, 928.0, 100, 30),
        new("EU_433", 433.0, 434.0, 10, 10),
        new("EU_868", 869.4, 869.65, 10, 27),
        new("EU_866", 865.6, 867.6, 2.5, 27), // 10% only for router-equivalent roles (REQ-RF-4)
        new("EU_N_868", 869.4, 869.65, 10, 27),
        new("CN", 470.0, 510.0, 100, 19),
        new("JP", 920.5, 923.5, 100, 13),
        new("ANZ", 915.0, 928.0, 100, 30),
        new("ANZ_433", 433.05, 434.79, 100, 14),
        new("KR", 920.0, 923.0, 100, 23),
        new("TW", 920.0, 925.0, 100, 27),
        new("RU", 868.7, 869.2, 100, 20),
        new("IN", 865.0, 867.0, 100, 30),
        new("NZ_865", 864.0, 868.0, 100, 36),
        new("TH", 920.0, 925.0, 10, 27),
        new("UA_433", 433.0, 434.7, 10, 10),
        new("MY_433", 433.0, 435.0, 100, 20),
        new("MY_919", 919.0, 924.0, 100, 27),
        new("SG_923", 917.0, 925.0, 100, 20),
        new("KZ_433", 433.075, 434.775, 100, 10),
        new("KZ_863", 863.0, 868.0, 100, 30),
        new("BR_902", 902.0, 907.5, 100, 30),
        new("PH_433", 433.0, 434.7, 100, 10),
        new("PH_868", 868.0, 869.4, 100, 14),
        new("PH_915", 915.0, 918.0, 100, 24),
        new("NP_865", 865.0, 868.0, 100, 30),
        new("LORA_24", 2400.0, 2483.5, 100, 10)
    ];

    /// <summary>Country → allowed regions (REQ-RF-5; first entry is the preferred band).</summary>
    public static readonly IReadOnlyDictionary<string, string[]> CountryPresets =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["RU"] = ["RU"],
            ["US"] = ["US"],
            ["CA"] = ["US"],
            ["DE"] = ["EU_868", "EU_433"],
            ["FR"] = ["EU_868", "EU_433"],
            ["PL"] = ["EU_868", "EU_433"],
            ["GB"] = ["EU_868", "EU_433"],
            ["UA"] = ["EU_868", "UA_433"],
            ["AU"] = ["ANZ"],
            ["NZ"] = ["ANZ", "NZ_865"],
            ["BR"] = ["ANZ", "BR_902"],
            ["CN"] = ["CN"],
            ["JP"] = ["JP"],
            ["KZ"] = ["KZ_433", "KZ_863"],
            ["MY"] = ["MY_919", "MY_433"],
            ["PH"] = ["PH_915", "PH_868", "PH_433"],
            ["SG"] = ["SG_923"],
            ["ID"] = ["SG_923"],
            ["IN"] = ["IN"],
            ["TH"] = ["TH"],
            ["TW"] = ["TW"],
            ["KR"] = ["KR"],
            ["NP"] = ["NP_865"],
            ["ZA"] = ["EU_868", "EU_433"]
        };

    public static JurisdictionProfile? Find(string code)
    {
        return Regions.FirstOrDefault(r => string.Equals(r.Code, code, StringComparison.Ordinal));
    }

    public static JurisdictionProfile Require(string code)
    {
        return Find(code) ?? throw new ArgumentException($"Unknown jurisdiction region code: {code}", nameof(code));
    }
}
