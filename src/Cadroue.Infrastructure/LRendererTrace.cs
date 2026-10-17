namespace Cadroue.Infrastructure;

public sealed record LRendererFacts(
    string LRendererProcessorSetting,
    string LRendererAcceleration,
    bool LRendererFilterSynced)
{
    public string? LRendererProcessorActive { get; init; }
    public bool? LRendererAccelerated { get; init; }
    public string? LRendererAdapter { get; init; }
    public string? LRendererPixel { get; init; }
    public string? LRendererColorRange { get; init; }
    public string? LRendererColorSpace { get; init; }
    public double? LRendererContrast { get; init; }
}

public static class LRendererTrace
{
    public static void LRendererFactsRecord(string lReason, LRendererFacts lFacts, double? lMilliseconds = null) =>
        LRendererFactsRecord(lReason, lFacts, null, lMilliseconds);

    public static void LRendererSetupRecord(
        LRendererFacts lFacts,
        double lOutputFps,
        int lDecoderThreads,
        int lVideoFrames,
        bool lScreenClear,
        bool lSwsForced,
        double lMilliseconds) =>
        LRendererFactsRecord(
            "Player created",
            lFacts,
            string.Join(
                ", ",
                $"max output {lOutputFps}fps",
                $"decoder threads {lDecoderThreads}",
                $"max video frames {lVideoFrames}",
                $"clear screen {lScreenClear}",
                $"sws forced {lSwsForced}"),
            lMilliseconds);

    public static void LRendererProcessorRecord(string lProcessorWas, string lReason)
    {
        if (string.Equals(lProcessorWas, LRenderer.LRendererFlyleafToken, StringComparison.Ordinal))
        {
            return;
        }

        LTrace.LTraceRecord(
            LTraceKind.LTraceUi,
            $"Video processor forced from {lProcessorWas} to Flyleaf",
            "FLVP runs custom pixel shaders per frame; D3D11VP would use the driver's video processor\n"
            + $"caused by: {lReason}");
    }

    private static void LRendererFactsRecord(
        string lReason, LRendererFacts lFacts, string? lConfig, double? lMilliseconds)
    {
        if (!LTrace.LTraceCheck(LTraceKind.LTraceUi))
        {
            return;
        }

        LTrace.LTraceRecord(LTraceKind.LTraceUi, lReason, LRendererFactsFormat(lFacts, lConfig), lMilliseconds);
    }

    private static string LRendererFactsFormat(LRendererFacts lFacts, string? lConfig)
    {
        string lVerdict = lFacts.LRendererProcessorActive is null
            ? "renderer not ready"
            : lFacts.LRendererContrast is null
                ? "no contrast filter reached the engine"
                : $"contrast {lFacts.LRendererContrast} reached the engine";
        string lDecode = lFacts.LRendererAccelerated switch
        {
            null => "decoder not ready",
            true => "HARDWARE (D3D11VA)",
            false => "SOFTWARE, hardware decode did not engage"
        };
        return $"verdict {lVerdict}\n"
            + $"acceleration requested {lFacts.LRendererAcceleration}, "
            + $"processor requested {lFacts.LRendererProcessorSetting}, "
            + $"processor in use {lFacts.LRendererProcessorActive ?? "none"}\n"
            + $"decode path {lDecode}, adapter {lFacts.LRendererAdapter ?? "none"}\n"
            + $"pixel format {lFacts.LRendererPixel ?? "unknown"}, "
            + $"color range {lFacts.LRendererColorRange ?? "unknown"}, "
            + $"color space {lFacts.LRendererColorSpace ?? "unknown"}\n"
            + $"sync vp filters {lFacts.LRendererFilterSynced}"
            + (lConfig is null ? string.Empty : $"\n{lConfig}");
    }
}
