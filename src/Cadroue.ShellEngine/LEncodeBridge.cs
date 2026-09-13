using System.Globalization;
using System.IO;
using System.Text;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

internal sealed record LEncodeSmartProduction(
    IReadOnlyList<LEncodeStage> LEncodeStages,
    IReadOnlyList<string> LEncodeParts);

public static partial class LEncode
{
    public static IReadOnlyList<LEncodeStage> LEncodeBridgeResolve(
        LWorkItem lWorkItem, IReadOnlyList<TimeSpan> lBridgeKeyframes)
    {
        LBridgePlan lBridgePlan = LBridge.LBridgeRegionResolve(
            lBridgeKeyframes, lWorkItem.LWorkOrigin, lWorkItem.LWorkEnd);
        LBridgeStream? lBridgeSource = LScoutStream.LScoutStreamRead(lWorkItem.LWorkSourcePath);
        return LEncodeSmartResolve(lWorkItem, lBridgePlan, lBridgeSource);
    }

    public static IReadOnlyList<LEncodeStage> LEncodeSmartResolve(
        LWorkItem lWorkItem,
        LBridgePlan lBridgePlan,
        LBridgeStream? lBridgeSource)
    {
        if (LEncodeSmartCheck(lWorkItem))
        {
            LRunner.LRunnerRecord(
                $"Smart encoding deferred for '{lWorkItem.LWorkOutputName}': the item requires a full re-encode; " +
                $"encoding the requested interval directly");
            return LEncodeWholeBuild(lWorkItem, lBridgeSource);
        }

        LRunner.LRunnerRecord(lBridgePlan.LBridgeOutcome == LBridgeOutcome.LBridgeOutcomeSmart
            ? $"Smart encoding applied for '{lWorkItem.LWorkOutputName}': {LEncodeRegionFormat(lBridgePlan)}"
            : $"Smart encoding not usable for '{lWorkItem.LWorkOutputName}': encoding the requested interval");

        return LEncodeSmartBuild(lWorkItem, lBridgePlan, lBridgeSource);
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
        $"{LEncodeTimeFormat(lBridgeSpan.LBridgeSpanOrigin)}-{LEncodeTimeFormat(lBridgeSpan.LBridgeSpanEnd)}s";

    public static bool LEncodeSmartCheck(LWorkItem lWorkItem)
    {
        LEncoding lOutput = lWorkItem.LWorkOutput;
        LEncodingVideo lVideo = lOutput.LEncodingVideo;
        return !string.Equals(lVideo.LEncodingMode, "Smart", StringComparison.OrdinalIgnoreCase)
            || lWorkItem.LWorkKind != LWorkKind.LWorkKindSplit
            || LEncodeVideo.LEncodeVideoCheck(lWorkItem, lOutput)
            || !LEncodeSourceCheck(lVideo.LEncodingFps);
    }

    internal static IReadOnlyList<LEncodeStage> LEncodeWholeBuild(
        LWorkItem lWorkItem,
        LBridgeStream? lBridgeSource = null) =>
        new[]
        {
            new LEncodeStage(
                LEncodeArgumentBuild(lWorkItem, LEncodeTimescaleRead(lWorkItem, lBridgeSource)),
                LWorkStage.LWorkStageEncode,
                "Encoding",
                lWorkItem.LWorkOutputPath,
                false)
        };

    public static IReadOnlyList<LEncodeStage> LEncodeSmartBuild(
        LWorkItem lWorkItem,
        LBridgePlan lBridgePlan,
        LBridgeStream? lBridgeSource,
        string? lIntermediateExtension = null)
    {
        if (lBridgePlan.LBridgeOutcome != LBridgeOutcome.LBridgeOutcomeSmart
            || lBridgePlan.LBridgeMiddle is null)
        {
            return LEncodeWholeBuild(lWorkItem, lBridgeSource);
        }

        LEncodingAudio lAudio = lWorkItem.LWorkOutput.LEncodingAudio;
        bool lAudioExcluded =
            string.Equals(lAudio.LEncodingStream, "Exclude", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lAudio.LEncodingMode, "Exclude", StringComparison.OrdinalIgnoreCase);
        bool lAudioPresent = !string.IsNullOrWhiteSpace(lWorkItem.LWorkSourceMedia?.LWorkAudioCodec);
        bool lAudioActive = !lAudioExcluded && lAudioPresent;
        bool lVideoWholeCopyable = lBridgePlan.LBridgeHead is null && lBridgePlan.LBridgeTail is null;
        if (lVideoWholeCopyable)
        {
            return new[] { LEncodeDirectBuild(lWorkItem, lBridgePlan.LBridgeMiddle, lAudioActive, lBridgeSource) };
        }

        TimeSpan lAudioOffset = TimeSpan.Zero;
        if (lAudioActive
            && File.Exists(lWorkItem.LWorkSourcePath))
        {
            bool lAudioAllTracks = string.Equals(
                lAudio.LEncodingStream,
                "Include all audio tracks",
                StringComparison.OrdinalIgnoreCase);
            LScoutAudioInterval? lAudioInterval = LScoutAudio.LScoutAudioResolve(
                lWorkItem.LWorkSourcePath,
                lWorkItem.LWorkOrigin,
                lWorkItem.LWorkEnd,
                lAudioAllTracks);
            if (lAudioInterval is null)
            {
                LRunner.LRunnerRecord(
                    $"Smart encoding audio timing could not be verified for '{lWorkItem.LWorkOutputName}': " +
                    $"preserving the audio stream with zero additional offset");
            }
            else if (!lAudioInterval.LScoutAudioPresent)
            {
                lAudioActive = false;
                LRunner.LRunnerRecord(
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
        if (lBridgeReencode && LRepertoireCatalog.LRepertoireEncoderResolve(lBridgeCodec) is null)
        {
            LRunner.LRunnerRecord(
                $"Smart encoding failed for '{lWorkItem.LWorkOutputName}': " +
                $"the source video codec '{lBridgeCodec ?? "unknown"}' has no matching encoder " +
                $"for the boundary re-encode; nothing was produced");
            return Array.Empty<LEncodeStage>();
        }

        string lResolvedExtension = LEncodeExtensionResolve(lIntermediateExtension);
        LEncodeSmartProduction lProduction = LEncodeBridgeBuild(
            lWorkItem,
            lBridgePlan,
            lBridgeSource,
            lResolvedExtension);
        var lStages = new List<LEncodeStage>(lProduction.LEncodeStages);

        string? lAudioPath = null;
        if (lAudioActive)
        {
            lAudioPath = Path.Combine(
                LDepot.LDepotBridgeRead(),
                $"{lWorkItem.LWorkId:N}.audio{lResolvedExtension}");
            lStages.Add(LEncodeAudioBuild(lWorkItem, lAudioPath));
        }

        lStages.Add(LEncodeConcatBuild(
            lWorkItem,
            lProduction.LEncodeParts,
            lAudioPath,
            lAudioOffset,
            lBridgeSource));
        return lStages;
    }

    internal static LEncodeSmartProduction LEncodeBridgeBuild(
        LWorkItem lWorkItem,
        LBridgePlan lBridgePlan,
        LBridgeStream? lBridgeSource,
        string lBridgeExtension)
    {
        string lBridgeFolder = LDepot.LDepotBridgeRead();
        var lStages = new List<LEncodeStage>();
        var lConcatParts = new List<string>();

        if (lBridgePlan.LBridgeHead is { } lBridgeHead)
        {
            string lHeadPath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.head{lBridgeExtension}");
            lStages.Add(LEncodeSpanBuild(lWorkItem, lBridgeHead, lHeadPath, "Encoding head bridge", lBridgeSource));
            lConcatParts.Add(LEncodePieceBuild(lStages, lHeadPath));
        }

        string lMiddlePath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.middle{lBridgeExtension}");
        lStages.Add(LEncodeMiddleBuild(lWorkItem, lBridgePlan.LBridgeMiddle!, lMiddlePath, lBridgeSource));

        string lLeadingCodec =
            (lBridgeSource?.LBridgeCodec ?? lWorkItem.LWorkSourceMedia?.LWorkMediaCodec ?? string.Empty)
                .ToLowerInvariant();
        if (lBridgePlan.LBridgeHead is not null && lLeadingCodec is "hevc" or "h265")
        {
            lStages.Add(new LEncodeStage(
                string.Empty, LWorkStage.LWorkStageSplice, "Normalizing splice", lMiddlePath, true));
        }

        lConcatParts.Add(LEncodePieceBuild(lStages, lMiddlePath));

        if (lBridgePlan.LBridgeTail is { } lBridgeTail)
        {
            string lTailPath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.tail{lBridgeExtension}");
            lStages.Add(LEncodeSpanBuild(lWorkItem, lBridgeTail, lTailPath, "Encoding tail bridge", lBridgeSource));
            lConcatParts.Add(LEncodePieceBuild(lStages, lTailPath));
        }

        return new LEncodeSmartProduction(lStages, lConcatParts);
    }

    private static string LEncodePieceBuild(List<LEncodeStage> lStages, string lPartPath)
    {
        string lPiecePath = Path.ChangeExtension(lPartPath, ".ts");
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lPartPath)}");
        lArguments.Append(" -map 0:v:0 -c copy -f mpegts");
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lPiecePath)}");
        lStages.Add(new LEncodeStage(
            lArguments.ToString(), LWorkStage.LWorkStageExtract, "Preparing bridge piece", lPiecePath, true));
        return lPiecePath;
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
