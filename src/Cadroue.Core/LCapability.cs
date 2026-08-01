namespace Cadroue.Core;

public sealed record LCapabilityQuality(
    string CapabilityQualityLabel,
    string CapabilityQualityOption,
    string CapabilityQualityDefault,
    double? CapabilityQualityMinimum = null,
    double? CapabilityQualityMaximum = null,
    bool CapabilityQualityHigherBetter = false,
    double? CapabilityQualityStep = null)
{
    public bool LCapabilityQualityBitrate => CapabilityQualityOption is "-b:v" or "-b:a";

    public string LCapabilityQualityRange =>
        CapabilityQualityMinimum is null || CapabilityQualityMaximum is null || LCapabilityQualityBitrate
            ? string.Empty
            : $"{CapabilityQualityMinimum:0.##}-{CapabilityQualityMaximum:0.##}, "
              + (CapabilityQualityHigherBetter ? "higher is better" : "lower is better");

    public double LCapabilityQualityStep
    {
        get
        {
            if (CapabilityQualityStep is double lStep)
            {
                return lStep;
            }

            double lMinimum = CapabilityQualityMinimum ?? 0;
            double lMaximum = CapabilityQualityMaximum ?? 0;
            return lMinimum == Math.Floor(lMinimum) && lMaximum == Math.Floor(lMaximum) ? 1 : 0.1;
        }
    }
}

public sealed record LCapabilityMode(
    string CapabilityModeLabel,
    LCapabilityQuality? CapabilityModeQuality = null);

public sealed record LCapabilityChoice(string CapabilityChoiceValue, string CapabilityChoiceLabel)
{
    public static implicit operator LCapabilityChoice(string lValue) => new(lValue, lValue);
}

public sealed record LCapabilitySpeed(
    string CapabilitySpeedLabel,
    string CapabilitySpeedOption,
    string CapabilitySpeedDefault,
    IReadOnlyList<LCapabilityChoice> CapabilitySpeedValues);

public sealed record LCapabilityExtra(
    string CapabilityExtraLabel,
    string CapabilityExtraOption,
    string CapabilityExtraDefault,
    IReadOnlyList<LCapabilityChoice> CapabilityExtraValues);

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

    public static LCapabilityCodec LCapabilityAudioRead(string? lEncoder)
    {
        if (string.IsNullOrWhiteSpace(lEncoder))
        {
            return LCapabilityTable.LCapabilityAudioFallback;
        }

        if (LCapabilityTable.LCapabilityAudioMap.TryGetValue(lEncoder, out LCapabilityCodec? lCodec))
        {
            return lCodec;
        }

        if (lEncoder.StartsWith("pcm_", StringComparison.OrdinalIgnoreCase))
        {
            return LCapabilityTable.LCapabilityAudioUncompressed;
        }

        return LCapabilityTable.LCapabilityAudioFallback;
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
