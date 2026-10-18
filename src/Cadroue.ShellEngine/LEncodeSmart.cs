using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LEncodeSmart
{
    public static async Task<IReadOnlyList<LEncodeStage>> LEncodeBridgeResolve(
        LWorkItem lWorkItem, IReadOnlyList<TimeSpan> lBridgeKeyframes)
    {
        LBridgePlan lBridgePlan = LBridge.LBridgeRegionResolve(
            lBridgeKeyframes, lWorkItem.LWorkOrigin, lWorkItem.LWorkEnd);
        LBridgeStream? lBridgeSource =
            await LScoutStream.LScoutStreamRead(lWorkItem.LWorkSourcePath).ConfigureAwait(false);
        LScoutAudioInterval? lAudioInterval =
            await LEncodeIntervalRead(lWorkItem, lBridgePlan).ConfigureAwait(false);
        return LEncodeSmartResolve(lWorkItem, lBridgePlan, lBridgeSource, lAudioInterval);
    }

    public static IReadOnlyList<LEncodeStage> LEncodeSmartResolve(
        LWorkItem lWorkItem,
        LBridgePlan lBridgePlan,
        LBridgeStream? lBridgeSource,
        LScoutAudioInterval? lAudioInterval = null)
    {
        if (LEncodeSmartCheck(lWorkItem))
        {
            LRunnerTrace.LRunnerRecord(
                $"Smart encoding deferred for '{lWorkItem.LWorkOutputName}': the item requires a full re-encode; " +
                $"encoding the requested interval directly");
            return LEncodeWholeBuild(lWorkItem, lBridgeSource);
        }

        LRunnerTrace.LRunnerRecord(lBridgePlan.LBridgeOutcome == LBridgeOutcome.LBridgeOutcomeSmart
            ? $"Smart encoding applied for '{lWorkItem.LWorkOutputName}': {LEncodeRegionFormat(lBridgePlan)}"
            : $"Smart encoding not usable for '{lWorkItem.LWorkOutputName}': encoding the requested interval");

        return LEncodeSmartBuild(lWorkItem, lBridgePlan, lBridgeSource, lAudioInterval: lAudioInterval);
    }

    public static async Task<LScoutAudioInterval?> LEncodeIntervalRead(
        LWorkItem lWorkItem, LBridgePlan lBridgePlan, CancellationToken lWorkToken = default)
    {
        LEncodingAudio lAudio = lWorkItem.LWorkOutput.LEncodingAudio;
        if (lBridgePlan.LBridgeOutcome != LBridgeOutcome.LBridgeOutcomeSmart
            || lBridgePlan.LBridgeMiddle is null
            || (lBridgePlan.LBridgeHead is null && lBridgePlan.LBridgeTail is null)
            || !LEncodeAudioCheck(lWorkItem)
            || !File.Exists(lWorkItem.LWorkSourcePath))
        {
            return null;
        }

        bool lAudioAllTracks = string.Equals(
            lAudio.LEncodingStream,
            "Include all audio tracks",
            StringComparison.OrdinalIgnoreCase);
        return await LScoutAudio.LScoutAudioResolve(
            lWorkItem.LWorkSourcePath,
            lWorkItem.LWorkOrigin,
            lWorkItem.LWorkEnd,
            lAudioAllTracks,
            lWorkToken).ConfigureAwait(false);
    }

    private static bool LEncodeAudioCheck(LWorkItem lWorkItem)
    {
        LEncodingAudio lAudio = lWorkItem.LWorkOutput.LEncodingAudio;
        bool lAudioExcluded =
            string.Equals(lAudio.LEncodingStream, "Exclude", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lAudio.LEncodingMode, "Exclude", StringComparison.OrdinalIgnoreCase);
        return !lAudioExcluded && !string.IsNullOrWhiteSpace(lWorkItem.LWorkSourceMedia?.LWorkAudioCodec);
    }

    private static string LEncodeRegionFormat(LBridgePlan lBridgePlan)
    {
        var lRegions = new List<string>();
        if (lBridgePlan.LBridgeHead is { } lHead)
        {
            lRegions.Add($"head bridge {LEncodeSpanFormat(lHead)}");
        }

        if (lBridgePlan.LBridgeMiddle is { } lMiddle)
        {
            lRegions.Add($"copied middle {LEncodeSpanFormat(lMiddle)}");
        }

        if (lBridgePlan.LBridgeTail is { } lTail)
        {
            lRegions.Add($"tail bridge {LEncodeSpanFormat(lTail)}");
        }

        return string.Join(", ", lRegions);
    }

    private static string LEncodeSpanFormat(LBridgeSpan lBridgeSpan) =>
        $"{LEncode.LEncodeTimeFormat(lBridgeSpan.LBridgeSpanOrigin)}-"
        + $"{LEncode.LEncodeTimeFormat(lBridgeSpan.LBridgeSpanEnd)}s";

    public static bool LEncodeSmartCheck(LWorkItem lWorkItem)
    {
        LEncoding lOutput = lWorkItem.LWorkOutput;
        LEncodingVideo lVideo = lOutput.LEncodingVideo;
        return !string.Equals(lVideo.LEncodingMode, "Smart", StringComparison.OrdinalIgnoreCase)
            || lWorkItem.LWorkKind != LWorkKind.LWorkKindSplit
            || LEncodeVideo.LEncodeVideoCheck(lWorkItem, lOutput)
            || !LEncode.LEncodeSourceCheck(lVideo.LEncodingFps);
    }

    internal static IReadOnlyList<LEncodeStage> LEncodeWholeBuild(
        LWorkItem lWorkItem,
        LBridgeStream? lBridgeSource = null)
    {
        string? lTimescale = LEncodeBridgeStage.LEncodeTimescaleRead(lWorkItem, lBridgeSource);
        int lPass = LEncodePlan.LEncodePassRead(lWorkItem, lWorkItem.LWorkOutput);
        var lStages = new List<LEncodeStage>();
        if (lPass > 0)
        {
            lStages.Add(LEncodePlan.LEncodePassBuild(
                lWorkItem, LEncodePlan.LEncodeArgumentBuild(lWorkItem, lTimescale, 1)));
        }

        lStages.Add(new LEncodeStage(
            LEncodePlan.LEncodeArgumentBuild(lWorkItem, lTimescale, lPass),
            LWorkStage.LWorkStageEncode,
            "Encoding",
            lWorkItem.LWorkOutputPath,
            false));
        return lStages;
    }

    public static IReadOnlyList<LEncodeStage> LEncodeSmartBuild(
        LWorkItem lWorkItem,
        LBridgePlan lBridgePlan,
        LBridgeStream? lBridgeSource,
        string? lIntermediateExtension = null,
        LScoutAudioInterval? lAudioInterval = null)
    {
        if (lBridgePlan.LBridgeOutcome != LBridgeOutcome.LBridgeOutcomeSmart
            || lBridgePlan.LBridgeMiddle is null)
        {
            return LEncodeWholeBuild(lWorkItem, lBridgeSource);
        }

        bool lAudioActive = LEncodeAudioCheck(lWorkItem);
        bool lVideoWholeCopyable = lBridgePlan.LBridgeHead is null && lBridgePlan.LBridgeTail is null;
        if (lVideoWholeCopyable)
        {
            return new[]
            {
                LEncodeBridgeStage.LEncodeDirectBuild(
                    lWorkItem, lBridgePlan.LBridgeMiddle, lAudioActive, lBridgeSource)
            };
        }

        TimeSpan lAudioOffset = TimeSpan.Zero;
        if (lAudioActive
            && File.Exists(lWorkItem.LWorkSourcePath))
        {
            if (lAudioInterval is null)
            {
                LRunnerTrace.LRunnerRecord(
                    $"Smart encoding audio timing could not be verified for '{lWorkItem.LWorkOutputName}': " +
                    $"preserving the audio stream with zero additional offset");
            }
            else if (!lAudioInterval.LScoutAudioPresent)
            {
                lAudioActive = false;
                LRunnerTrace.LRunnerRecord(
                    $"Smart encoding omitted audio for '{lWorkItem.LWorkOutputName}': " +
                    $"no audio packets overlap the requested interval");
            }
            else
            {
                lAudioOffset = lAudioInterval.LScoutAudioOffset;
            }
        }
        bool lBridgeReencode = lBridgePlan.LBridgeHead is not null || lBridgePlan.LBridgeTail is not null;
        string? lBridgeCodec = lBridgeSource?.LBridgeCodec ?? lWorkItem.LWorkSourceMedia?.LWorkMediaCodec;
        if (lBridgeReencode && LRepertoireVideo.LRepertoireEncoderResolve(lBridgeCodec) is null)
        {
            LRunnerTrace.LRunnerRecord(
                $"Smart encoding failed for '{lWorkItem.LWorkOutputName}': " +
                $"the source video codec '{lBridgeCodec ?? "unknown"}' has no matching encoder " +
                $"for the boundary re-encode; nothing was produced");
            return Array.Empty<LEncodeStage>();
        }

        string lResolvedExtension = LEncodeExtensionResolve(lIntermediateExtension);
        LEncodeSmartProduction lProduction = LEncodeBridge.LEncodeBridgeBuild(
            lWorkItem,
            lBridgePlan,
            lBridgeSource,
            lResolvedExtension);
        var lStages = new List<LEncodeStage>(lProduction.LEncodeStages);

        string? lAudioPath = null;
        if (lAudioActive)
        {
            lAudioPath = Path.Combine(
                LDepotEngine.LDepotBridgeSeam(),
                $"{lWorkItem.LWorkId:N}.audio{lResolvedExtension}");
            lStages.Add(LEncodeBridgeStage.LEncodeAudioBuild(lWorkItem, lAudioPath));
        }

        lStages.Add(LEncodeBridgeStage.LEncodeConcatBuild(
            lWorkItem,
            lProduction.LEncodeParts,
            lAudioPath,
            lAudioOffset,
            lBridgeSource));
        return lStages;
    }

    private static string LEncodeExtensionResolve(string? lIntermediateExtension)
    {
        string lExtension = lIntermediateExtension ?? ".mov";
        if (string.IsNullOrWhiteSpace(lExtension))
        {
            return string.Empty;
        }

        return lExtension.StartsWith(".", StringComparison.Ordinal)
            ? lExtension
            : "." + lExtension;
    }
}
