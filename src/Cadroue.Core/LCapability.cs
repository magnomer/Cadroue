namespace Cadroue.Core;

public sealed record LCapabilityQuality(
    string CapabilityQualityLabel,
    string CapabilityQualityOption,
    string CapabilityQualityDefault,
    double? CapabilityQualityMinimum = null,
    double? CapabilityQualityMaximum = null,
    bool CapabilityQualityHigherBetter = false)
{
    public string LCapabilityQualityRange => CapabilityQualityMinimum is null || CapabilityQualityMaximum is null
        ? string.Empty
        : $"{CapabilityQualityMinimum:0.##}-{CapabilityQualityMaximum:0.##}, "
          + (CapabilityQualityHigherBetter ? "higher is better" : "lower is better");
}

public sealed record LCapabilityMode(
    string CapabilityModeLabel,
    LCapabilityQuality? CapabilityModeQuality = null);

public sealed record LCapabilitySpeed(
    string CapabilitySpeedLabel,
    string CapabilitySpeedOption,
    string CapabilitySpeedDefault,
    IReadOnlyList<string> CapabilitySpeedValues);

public sealed record LCapabilityExtra(
    string CapabilityExtraLabel,
    string CapabilityExtraOption,
    string CapabilityExtraDefault,
    IReadOnlyList<string> CapabilityExtraValues);

public sealed record LCapabilityCodec(
    string CapabilityEncoder,
    IReadOnlyList<LCapabilityMode> CapabilityModes,
    LCapabilitySpeed? CapabilitySpeed = null,
    IReadOnlyList<LCapabilityExtra>? CapabilityExtras = null,
    string CapabilityNotice = "")
{
    public IReadOnlyList<LCapabilityExtra> LCapabilityExtraList =>
        CapabilityExtras ?? [];

    public string[] LCapabilityModeLabels =>
        CapabilityModes.Select(lMode => lMode.CapabilityModeLabel).ToArray();

    public LCapabilityMode LCapabilityModeFind(string? lModeLabel) =>
        CapabilityModes.FirstOrDefault(lMode =>
            string.Equals(lMode.CapabilityModeLabel, lModeLabel, StringComparison.Ordinal))
        ?? CapabilityModes[0];
}

public static class LCapability
{
    public static LCapabilityCodec LCapabilityRead(string? lEncoder)
    {
        if (!string.IsNullOrWhiteSpace(lEncoder)
            && LCapabilityTable.LCapabilityMap.TryGetValue(lEncoder, out LCapabilityCodec? lCodec))
        {
            return lCodec;
        }

        return LCapabilityTable.LCapabilityFallback;
    }

    public static string LCapabilityNameRead(string? lEncoderText)
    {
        if (string.IsNullOrWhiteSpace(lEncoderText))
        {
            return string.Empty;
        }

        int lSlashIndex = lEncoderText.LastIndexOf('/');
        string lName = lSlashIndex < 0 ? lEncoderText : lEncoderText[(lSlashIndex + 1)..];
        return lName.Trim();
    }
}
