namespace Cadroue.Core;

/// <summary>
/// One selectable quality scalar. The option name, range and direction differ per
/// encoder: there is no project-wide "CRF". Higher-is-better is the exception
/// (Media Foundation, WebP, Theora) and must be stated, not assumed.
/// </summary>
public sealed record LCapabilityQuality(
    string CapabilityQualityLabel,
    string CapabilityQualityOption,
    string CapabilityQualityDefault,
    double? CapabilityQualityMinimum = null,
    double? CapabilityQualityMaximum = null,
    bool CapabilityQualityHigherBetter = false)
{
    public string CapabilityQualityRange => CapabilityQualityMinimum is null || CapabilityQualityMaximum is null
        ? string.Empty
        : $"{CapabilityQualityMinimum:0.##}-{CapabilityQualityMaximum:0.##}, "
          + (CapabilityQualityHigherBetter ? "higher is better" : "lower is better");
}

/// <summary>One rate-control mode and the quality scalar it exposes.</summary>
public sealed record LCapabilityMode(
    string CapabilityModeLabel,
    LCapabilityQuality? CapabilityModeQuality = null);

/// <summary>
/// Speed/effort control. Named for some encoders (x264 words, NVENC p1-p7),
/// numeric for others (SVT-AV1, libaom cpu-used), absent for many.
/// </summary>
public sealed record LCapabilitySpeed(
    string CapabilitySpeedLabel,
    string CapabilitySpeedOption,
    string CapabilitySpeedDefault,
    IReadOnlyList<string> CapabilitySpeedValues);

/// <summary>An encoder-specific extra row (profile, tune, usage, lossless flag).</summary>
public sealed record LCapabilityExtra(
    string CapabilityExtraLabel,
    string CapabilityExtraOption,
    string CapabilityExtraDefault,
    IReadOnlyList<string> CapabilityExtraValues);

/// <summary>The full rate-control shape of one FFmpeg video encoder.</summary>
public sealed record LCapabilityCodec(
    string CapabilityEncoder,
    IReadOnlyList<LCapabilityMode> CapabilityModes,
    LCapabilitySpeed? CapabilitySpeed = null,
    IReadOnlyList<LCapabilityExtra>? CapabilityExtras = null,
    string CapabilityNotice = "")
{
    public IReadOnlyList<LCapabilityExtra> CapabilityExtraList =>
        CapabilityExtras ?? [];

    public string[] CapabilityModeLabels =>
        CapabilityModes.Select(lMode => lMode.CapabilityModeLabel).ToArray();

    public LCapabilityMode CapabilityModeFind(string? lModeLabel) =>
        CapabilityModes.FirstOrDefault(lMode =>
            string.Equals(lMode.CapabilityModeLabel, lModeLabel, StringComparison.Ordinal))
        ?? CapabilityModes[0];
}

public static class LCapability
{
    /// <summary>
    /// Resolve the capability shape for an FFmpeg encoder name. Unknown encoders fall
    /// back to the generic qscale shape rather than pretending to support CRF.
    /// </summary>
    public static LCapabilityCodec LCapabilityRead(string? lEncoder)
    {
        if (!string.IsNullOrWhiteSpace(lEncoder)
            && LCapabilityTable.LCapabilityMap.TryGetValue(lEncoder, out LCapabilityCodec? lCodec))
        {
            return lCodec;
        }

        return LCapabilityTable.LCapabilityFallback;
    }

    /// <summary>
    /// Pull the FFmpeg encoder name out of a dialog list entry. Those entries read
    /// "Family, Implementation / ffmpegname", so the name is the last slash-separated
    /// part (e.g. "H.265, x265 / libx265" gives "libx265").
    /// </summary>
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
