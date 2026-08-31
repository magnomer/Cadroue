namespace Cadroue.Core;

public sealed record LCapabilityQuality(
    string LCapabilityQualityLabel,
    string LCapabilityQualityOption,
    string LCapabilityQualityDefault,
    double? LCapabilityQualityMinimum = null,
    double? LCapabilityQualityMaximum = null,
    bool LCapabilityQualityAscending = false,
    double? LCapabilityQualityStride = null)
{
    public bool LCapabilityQualityBitrate => LCapabilityQualityOption is "-b:v" or "-b:a";

    public string LCapabilityQualityRange =>
        LCapabilityQualityMinimum is null || LCapabilityQualityMaximum is null || LCapabilityQualityBitrate
            ? string.Empty
            : $"{LCapabilityQualityMinimum:0.##}-{LCapabilityQualityMaximum:0.##}, "
              + (LCapabilityQualityAscending ? "higher is better" : "lower is better");

    public double LCapabilityQualityStep
    {
        get
        {
            if (LCapabilityQualityStride is double lStep)
            {
                return lStep;
            }

            double lMinimum = LCapabilityQualityMinimum ?? 0;
            double lMaximum = LCapabilityQualityMaximum ?? 0;
            return lMinimum == Math.Floor(lMinimum) && lMaximum == Math.Floor(lMaximum) ? 1 : 0.1;
        }
    }
}

public sealed record LCapabilityMode(
    string LCapabilityModeLabel,
    LCapabilityQuality? LCapabilityModeQuality = null);

public sealed record LCapabilityChoice(string LCapabilityChoiceValue, string LCapabilityChoiceLabel)
{
    public static implicit operator LCapabilityChoice(string lValue) => new(lValue, lValue);
}

public sealed record LCapabilitySpeed(
    string LCapabilitySpeedLabel,
    string LCapabilitySpeedOption,
    string LCapabilitySpeedDefault,
    IReadOnlyList<LCapabilityChoice> LCapabilitySpeedValues);

public sealed record LCapabilityExtra(
    string LCapabilityExtraLabel,
    string LCapabilityExtraOption,
    string LCapabilityExtraDefault,
    IReadOnlyList<LCapabilityChoice> LCapabilityExtraValues);

public sealed record LCapabilityCodec(
    string LCapabilityEncoder,
    IReadOnlyList<LCapabilityMode> LCapabilityModes,
    LCapabilitySpeed? LCapabilitySpeed = null,
    IReadOnlyList<LCapabilityExtra>? LCapabilityExtras = null,
    string LCapabilityNotice = "")
{
    public IReadOnlyList<LCapabilityExtra> LCapabilityExtraList =>
        LCapabilityExtras ?? [];

    public string[] LCapabilityModeLabels =>
        LCapabilityModes.Select(lMode => lMode.LCapabilityModeLabel).ToArray();

    public LCapabilityMode LCapabilityModeFind(string? lModeLabel) =>
        LCapabilityModes.FirstOrDefault(lMode =>
            string.Equals(lMode.LCapabilityModeLabel, lModeLabel, StringComparison.Ordinal))
        ?? LCapabilityModes[0];
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
