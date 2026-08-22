using System.Globalization;
using System.IO;
using System.Text;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

internal sealed record LEncodeSmartProduction(
    IReadOnlyList<LEncodeStage> LEncodeStages,
    IReadOnlyList<string> LEncodeParts,
    string? LEncodeProbeTarget,
    string LEncodeMiddlePath);

public static partial class LEncode
{
    public static IReadOnlyList<LEncodeStage> LEncodeBridgeResolve(
        LWorkItem lWorkItem, IReadOnlyList<TimeSpan> lBridgeKeyframes)
    {
        LBridgePlan lBridgePlan = LBridge.LBridgeRegionResolve(
            lBridgeKeyframes, lWorkItem.LWorkOrigin, lWorkItem.LWorkEnd);
        return LEncodeSmartResolve(
            lWorkItem, lBridgePlan, new LBridgeCompatibility(true, LBridgeReason.LBridgeReasonCompatible));
    }

    public static IReadOnlyList<LEncodeStage> LEncodeSmartResolve(
        LWorkItem lWorkItem,
        LBridgePlan lBridgePlan,
        LBridgeCompatibility lBridgeCompatibility)
    {
        if (LEncodeSmartCheck(lWorkItem))
        {
            LRunner.LRunnerRecord(
                $"Smart encoding deferred for '{lWorkItem.LWorkOutputName}': the item requires a full re-encode; encoding the requested interval directly");
            return LEncodeWholeBuild(lWorkItem);
        }

        if (lBridgePlan.LBridgeOutcome == LBridgeOutcome.LBridgeOutcomeSmart
            && !lBridgeCompatibility.LBridgeCompatible)
        {
            LRunner.LRunnerRecord(
                $"Smart encoding fallback for '{lWorkItem.LWorkOutputName}': the bridge cannot join the copied continuation ({lBridgeCompatibility.LBridgeReason}); encoding the requested interval");
            return LEncodeWholeBuild(lWorkItem);
        }

        LRunner.LRunnerRecord(lBridgePlan.LBridgeOutcome == LBridgeOutcome.LBridgeOutcomeSmart
            ? $"Smart encoding applied for '{lWorkItem.LWorkOutputName}': {LEncodeRegionFormat(lBridgePlan)}"
            : $"Smart encoding not usable for '{lWorkItem.LWorkOutputName}': encoding the requested interval");

        return LEncodeSmartBuild(lWorkItem, lBridgePlan);
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
            || LEncodeVideo.LEncodeVideoCheck(lWorkItem, lOutput)
            || !LEncodeSourceCheck(lVideo.LEncodingFps);
    }

    internal static IReadOnlyList<LEncodeStage> LEncodeWholeBuild(LWorkItem lWorkItem) =>
        new[]
        {
            new LEncodeStage(LEncodeArgumentBuild(lWorkItem), LWorkStage.LWorkStageEncode, "Encoding", lWorkItem.LWorkOutputPath, false)
        };

    public static IReadOnlyList<LEncodeStage> LEncodeSmartBuild(LWorkItem lWorkItem, LBridgePlan lBridgePlan)
    {
        if (lBridgePlan.LBridgeOutcome != LBridgeOutcome.LBridgeOutcomeSmart
            || lBridgePlan.LBridgeMiddle is null)
        {
            return LEncodeWholeBuild(lWorkItem);
        }

        LEncodeSmartProduction lProduction = LEncodeBridgeBuild(lWorkItem, lBridgePlan);
        var lStages = new List<LEncodeStage>(lProduction.LEncodeStages)
        {
            LEncodeConcatBuild(lWorkItem, lProduction.LEncodeParts, lBridgePlan.LBridgeInterval)
        };
        return lStages;
    }

    internal static LEncodeSmartProduction LEncodeBridgeBuild(LWorkItem lWorkItem, LBridgePlan lBridgePlan)
    {
        string lBridgeFolder = LDepot.LDepotBridgeRead();
        const string lBridgeExtension = ".mkv";
        var lStages = new List<LEncodeStage>();
        var lConcatParts = new List<string>();
        string? lProbeTarget = null;

        if (lBridgePlan.LBridgeHead is { } lBridgeHead)
        {
            string lHeadPath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.head{lBridgeExtension}");
            lStages.Add(LEncodeSpanBuild(lWorkItem, lBridgeHead, lHeadPath, "Encoding head bridge"));
            lConcatParts.Add(lHeadPath);
            lProbeTarget = lHeadPath;
        }

        string lMiddlePath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.middle{lBridgeExtension}");
        lStages.Add(LEncodeMiddleBuild(lWorkItem, lBridgePlan.LBridgeMiddle!, lMiddlePath));
        lConcatParts.Add(lMiddlePath);

        if (lBridgePlan.LBridgeTail is { } lBridgeTail)
        {
            string lTailPath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.tail{lBridgeExtension}");
            lStages.Add(LEncodeSpanBuild(lWorkItem, lBridgeTail, lTailPath, "Encoding tail bridge"));
            lConcatParts.Add(lTailPath);
            lProbeTarget ??= lTailPath;
        }

        return new LEncodeSmartProduction(lStages, lConcatParts, lProbeTarget, lMiddlePath);
    }

    private static LEncodeStage LEncodeSpanBuild(
        LWorkItem lWorkItem, LBridgeSpan lBridgeSpan, string lBridgePath, string lBridgeLabel)
    {
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -ss {LEncodeTimeFormat(lBridgeSpan.LBridgeSpanOrigin)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -t {LEncodeTimeFormat(lBridgeSpan.LBridgeSpanEnd - lBridgeSpan.LBridgeSpanOrigin)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeLosslessResolve(lWorkItem)}");
        lArguments.Append(" -an");
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lBridgePath)}");
        return new LEncodeStage(lArguments.ToString(), LWorkStage.LWorkStageEncode, lBridgeLabel, lBridgePath, true);
    }

    private static LEncodeStage LEncodeMiddleBuild(LWorkItem lWorkItem, LBridgeSpan lBridgeSpan, string lBridgePath)
    {
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -ss {LEncodeTimeFormat(lBridgeSpan.LBridgeSpanOrigin)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -t {LEncodeTimeFormat(lBridgeSpan.LBridgeSpanEnd - lBridgeSpan.LBridgeSpanOrigin)}");
        lArguments.Append(" -c:v copy -an");
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lBridgePath)}");
        return new LEncodeStage(lArguments.ToString(), LWorkStage.LWorkStageEncode, "Copying middle", lBridgePath, true);
    }

    internal static LEncodeStage LEncodeConcatBuild(
        LWorkItem lWorkItem, IReadOnlyList<string> lBridgeParts, LBridgeSpan lBridgeInterval)
    {
        LEncoding lOutput = lWorkItem.LWorkOutput;
        string lJoinPath = LEncodeJoinSave(lWorkItem, lBridgeParts);
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -f concat -safe 0 -i {LEncodeFormat(lJoinPath)}");

        bool lAudioExcluded =
            string.Equals(lOutput.LEncodingAudio.LEncodingStream, "Exclude", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lOutput.LEncodingAudio.LEncodingMode, "Exclude", StringComparison.OrdinalIgnoreCase);
        bool lAudioPresent = !string.IsNullOrWhiteSpace(lWorkItem.LWorkSourceMedia?.LWorkAudioCodec);

        if (lAudioExcluded || !lAudioPresent)
        {
            lArguments.Append(" -map 0:v:0 -c copy -an");
            lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lWorkItem.LWorkOutputPath)}");
            return new LEncodeStage(lArguments.ToString(), LWorkStage.LWorkStageMux, "Joining bridges", lWorkItem.LWorkOutputPath, false);
        }

        lArguments.Append(CultureInfo.InvariantCulture, $" -ss {LEncodeTimeFormat(lBridgeInterval.LBridgeSpanOrigin)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lArguments.Append(" -map 0:v:0 -map 1:a:0");
        lArguments.Append(CultureInfo.InvariantCulture, $" -t {LEncodeTimeFormat(lBridgeInterval.LBridgeSpanEnd - lBridgeInterval.LBridgeSpanOrigin)}");
        lArguments.Append(" -c:v copy");

        if (string.Equals(lOutput.LEncodingAudio.LEncodingMode, "Copy", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -c:a copy");
        }
        else
        {
            LEncodeAudio.LEncodeMuxAppend(lArguments, lOutput);
        }

        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lWorkItem.LWorkOutputPath)}");
        return new LEncodeStage(lArguments.ToString(), LWorkStage.LWorkStageMux, "Joining bridges", lWorkItem.LWorkOutputPath, false);
    }

    private static string LEncodeJoinSave(LWorkItem lWorkItem, IReadOnlyList<string> lBridgeParts)
    {
        string lJoinPath = Path.Combine(LDepot.LDepotBridgeRead(), $"{lWorkItem.LWorkId:N}.concat.txt");
        var lJoinList = new StringBuilder();
        foreach (string lPart in lBridgeParts)
        {
            string lEscaped = lPart.Replace("\\", "/", StringComparison.Ordinal).Replace("'", "'\\''", StringComparison.Ordinal);
            lJoinList.Append(CultureInfo.InvariantCulture, $"file '{lEscaped}'\n");
        }

        File.WriteAllText(lJoinPath, lJoinList.ToString());
        return lJoinPath;
    }

    internal static void LEncodeBridgeClear(Guid lWorkId)
    {
        string lJoinPath = Path.Combine(LDepot.LDepotBridgeRead(), $"{lWorkId:N}.concat.txt");
        try
        {
            if (File.Exists(lJoinPath))
            {
                File.Delete(lJoinPath);
            }
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string LEncodeLosslessResolve(LWorkItem lWorkItem)
    {
        string lCodec = lWorkItem.LWorkSourceMedia?.LWorkMediaCodec ?? string.Empty;
        return lCodec.ToLowerInvariant() switch
        {
            "hevc" or "h265" => "-c:v libx265 -x265-params lossless=1",
            "vp9" => "-c:v libvpx-vp9 -lossless 1",
            "av1" => "-c:v libaom-av1 -aom-params lossless=1",
            "ffv1" => "-c:v ffv1",
            _ => "-c:v libx264 -qp 0"
        };
    }
}
