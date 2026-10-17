namespace Cadroue.Core;

public static class LDetectorRow
{
    private sealed record LDetectorEntry(
        int LDetectorEntryKind,
        bool LDetectorEntryEnabled,
        double LDetectorEntryThreshold,
        double LDetectorEntryMinimum,
        double LDetectorEntryWindow,
        int LDetectorEntryType,
        double LDetectorEntryHue,
        double LDetectorEntrySaturation,
        double LDetectorEntryBrightness,
        double LDetectorEntryTolerance,
        double LDetectorEntryCoverage,
        string LDetectorEntryPreset);

    public static LSidecarSplitEntry LDetectorSidecarFormat(LDetectorBundle lSet) => new()
    {
        LSidecarSplitDetectors = LDetectorEntriesFormat(lSet).Select(lEntry => new LSidecarDetectorEntry
        {
            LSidecarDetectorKind = lEntry.LDetectorEntryKind,
            LSidecarDetectorEnabled = lEntry.LDetectorEntryEnabled,
            LSidecarDetectorThreshold = lEntry.LDetectorEntryThreshold,
            LSidecarDetectorMinimum = lEntry.LDetectorEntryMinimum,
            LSidecarDetectorWindow = lEntry.LDetectorEntryWindow,
            LSidecarDetectorType = lEntry.LDetectorEntryType,
            LSidecarDetectorHue = lEntry.LDetectorEntryHue,
            LSidecarDetectorSaturation = lEntry.LDetectorEntrySaturation,
            LSidecarDetectorBrightness = lEntry.LDetectorEntryBrightness,
            LSidecarDetectorTolerance = lEntry.LDetectorEntryTolerance,
            LSidecarDetectorCoverage = lEntry.LDetectorEntryCoverage,
            LSidecarDetectorPreset = lEntry.LDetectorEntryPreset
        }).ToList()
    };

    public static LDetectorBundle LDetectorSidecarParse(LSidecarSplitEntry lRecord) =>
        LDetectorEntriesParse(lRecord.LSidecarSplitDetectors.Select(lDetector => new LDetectorEntry(
            lDetector.LSidecarDetectorKind,
            lDetector.LSidecarDetectorEnabled,
            lDetector.LSidecarDetectorThreshold,
            lDetector.LSidecarDetectorMinimum,
            lDetector.LSidecarDetectorWindow,
            lDetector.LSidecarDetectorType,
            lDetector.LSidecarDetectorHue,
            lDetector.LSidecarDetectorSaturation,
            lDetector.LSidecarDetectorBrightness,
            lDetector.LSidecarDetectorTolerance,
            lDetector.LSidecarDetectorCoverage,
            lDetector.LSidecarDetectorPreset)));

    public static List<LSceneDetector> LDetectorSceneFormat(LDetectorBundle lSet) =>
        LDetectorEntriesFormat(lSet).Select(lEntry => new LSceneDetector
        {
            LSceneDetectorKind = lEntry.LDetectorEntryKind,
            LSceneDetectorEnabled = lEntry.LDetectorEntryEnabled,
            LSceneDetectorThreshold = lEntry.LDetectorEntryThreshold,
            LSceneDetectorMinimum = lEntry.LDetectorEntryMinimum,
            LSceneDetectorWindow = lEntry.LDetectorEntryWindow,
            LSceneDetectorType = lEntry.LDetectorEntryType,
            LSceneDetectorHue = lEntry.LDetectorEntryHue,
            LSceneDetectorSaturation = lEntry.LDetectorEntrySaturation,
            LSceneDetectorBrightness = lEntry.LDetectorEntryBrightness,
            LSceneDetectorTolerance = lEntry.LDetectorEntryTolerance,
            LSceneDetectorCoverage = lEntry.LDetectorEntryCoverage,
            LSceneDetectorPreset = lEntry.LDetectorEntryPreset
        }).ToList();

    public static LDetectorBundle LDetectorSceneParse(IReadOnlyList<LSceneDetector> lDetectors) =>
        LDetectorEntriesParse(lDetectors.Select(lDetector => new LDetectorEntry(
            lDetector.LSceneDetectorKind,
            lDetector.LSceneDetectorEnabled,
            lDetector.LSceneDetectorThreshold,
            lDetector.LSceneDetectorMinimum,
            lDetector.LSceneDetectorWindow,
            lDetector.LSceneDetectorType,
            lDetector.LSceneDetectorHue,
            lDetector.LSceneDetectorSaturation,
            lDetector.LSceneDetectorBrightness,
            lDetector.LSceneDetectorTolerance,
            lDetector.LSceneDetectorCoverage,
            lDetector.LSceneDetectorPreset)));

    private static IEnumerable<LDetectorEntry> LDetectorEntriesFormat(LDetectorBundle lSet)
    {
        if (lSet.LDetectorSetBlank is { } lBlank)
        {
            yield return new LDetectorEntry(
                (int)LDetectorKind.LDetectorKindBlank,
                lBlank.LDetectorBlankEnabled,
                0,
                lBlank.LDetectorBlankMinimum,
                LDetector.LDetectorWindowRead(LDetectorKind.LDetectorKindLuminance).LDetectorBoundDefault,
                (int)lBlank.LDetectorBlankType,
                lBlank.LDetectorBlankHue,
                lBlank.LDetectorBlankSaturation,
                lBlank.LDetectorBlankBrightness,
                lBlank.LDetectorBlankTolerance,
                lBlank.LDetectorBlankCoverage,
                LDetectorCatalog.LDetectorTokenDefault);
        }

        foreach (LDetectorStep lStep in lSet.LDetectorSetSteps)
        {
            yield return new LDetectorEntry(
                (int)lStep.LDetectorStepKind,
                lStep.LDetectorStepEnabled,
                lStep.LDetectorStepThreshold,
                lStep.LDetectorStepMinimum,
                lStep.LDetectorStepWindow,
                LDetectorModeFormat(lSet, lStep.LDetectorStepKind),
                0,
                0,
                LDetectorBlank.LDetectorBlankValue,
                LDetector.LDetectorToleranceRead().LDetectorBoundDefault,
                LDetector.LDetectorCoverageRead().LDetectorBoundDefault,
                lSet.LDetectorSetPresets.GetValueOrDefault(lStep.LDetectorStepKind, string.Empty));
        }
    }

    private static int LDetectorModeFormat(LDetectorBundle lSet, LDetectorKind lKind) => lKind switch
    {
        LDetectorKind.LDetectorKindStill => (int?)lSet.LDetectorSetStill ?? 0,
        LDetectorKind.LDetectorKindLuminance => (int?)lSet.LDetectorSetLuminance ?? 0,
        LDetectorKind.LDetectorKindVolume => (int?)lSet.LDetectorSetMetric ?? 0,
        _ => 0
    };

    private static LDetectorBundle LDetectorEntriesParse(IEnumerable<LDetectorEntry> lEntries)
    {
        var lSteps = new List<LDetectorStep>();
        var lPresets = new Dictionary<LDetectorKind, string>();
        LDetectorBlank? lBlank = null;
        LDetectorStillMode? lStill = null;
        LDetectorLuminanceMode? lLuminance = null;
        LDetectorMetricMode? lMetric = null;
        foreach (LDetectorEntry lEntry in lEntries)
        {
            if (!Enum.IsDefined(typeof(LDetectorKind), lEntry.LDetectorEntryKind))
            {
                continue;
            }

            var lKind = (LDetectorKind)lEntry.LDetectorEntryKind;
            if (lKind == LDetectorKind.LDetectorKindBlank)
            {
                lBlank = new LDetectorBlank(
                    lEntry.LDetectorEntryEnabled,
                    LDetectorEnumParse(lEntry.LDetectorEntryType, LDetectorType.LDetectorTypeBlack),
                    lEntry.LDetectorEntryHue,
                    lEntry.LDetectorEntrySaturation,
                    lEntry.LDetectorEntryBrightness,
                    lEntry.LDetectorEntryTolerance,
                    lEntry.LDetectorEntryCoverage,
                    lEntry.LDetectorEntryMinimum);
                continue;
            }

            lSteps.Add(new LDetectorStep(
                lKind,
                lEntry.LDetectorEntryEnabled,
                lEntry.LDetectorEntryThreshold,
                lEntry.LDetectorEntryMinimum,
                lEntry.LDetectorEntryWindow));
            lPresets[lKind] = lEntry.LDetectorEntryPreset;
            switch (lKind)
            {
                case LDetectorKind.LDetectorKindStill:
                    lStill = LDetectorEnumParse(lEntry.LDetectorEntryType, LDetectorStillMode.LDetectorStillDiscard);
                    break;
                case LDetectorKind.LDetectorKindLuminance:
                    lLuminance = LDetectorEnumParse(
                        lEntry.LDetectorEntryType, LDetectorLuminanceMode.LDetectorLuminanceNormal);
                    break;
                case LDetectorKind.LDetectorKindVolume:
                    lMetric = LDetectorEnumParse(lEntry.LDetectorEntryType, LDetectorMetricMode.LDetectorMetricLufs);
                    break;
            }
        }

        return new LDetectorBundle(lSteps, lBlank, lStill, lLuminance, lMetric, lPresets);
    }

    private static LDetectorMode LDetectorEnumParse<LDetectorMode>(int lValue, LDetectorMode lFallback)
        where LDetectorMode : struct, Enum =>
        Enum.IsDefined(typeof(LDetectorMode), lValue) ? (LDetectorMode)(object)lValue : lFallback;
}
