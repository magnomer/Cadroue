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
    private static readonly TimeSpan LEncodeCopyBias = TimeSpan.FromMilliseconds(2);

    public static IReadOnlyList<LEncodeStage> LEncodeBridgeResolve(
        LWorkItem lWorkItem, IReadOnlyList<TimeSpan> lBridgeKeyframes)
    {
        LBridgePlan lBridgePlan = LBridge.LBridgeRegionResolve(
            lBridgeKeyframes, lWorkItem.LWorkOrigin, lWorkItem.LWorkEnd);
        LBridgeStream? lBridgeSource = LScout.LScoutStreamRead(lWorkItem.LWorkSourcePath);
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
                $"Smart encoding deferred for '{lWorkItem.LWorkOutputName}': the item requires a full re-encode; encoding the requested interval directly");
            return LEncodeWholeBuild(lWorkItem);
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

    internal static IReadOnlyList<LEncodeStage> LEncodeWholeBuild(LWorkItem lWorkItem) =>
        new[]
        {
            new LEncodeStage(LEncodeArgumentBuild(lWorkItem), LWorkStage.LWorkStageEncode, "Encoding", lWorkItem.LWorkOutputPath, false)
        };

    public static IReadOnlyList<LEncodeStage> LEncodeSmartBuild(
        LWorkItem lWorkItem, LBridgePlan lBridgePlan, LBridgeStream? lBridgeSource)
    {
        if (lBridgePlan.LBridgeOutcome != LBridgeOutcome.LBridgeOutcomeSmart
            || lBridgePlan.LBridgeMiddle is null)
        {
            return LEncodeWholeBuild(lWorkItem);
        }

        LEncodingAudio lAudio = lWorkItem.LWorkOutput.LEncodingAudio;
        bool lAudioExcluded =
            string.Equals(lAudio.LEncodingStream, "Exclude", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lAudio.LEncodingMode, "Exclude", StringComparison.OrdinalIgnoreCase);
        bool lAudioPresent = !string.IsNullOrWhiteSpace(lWorkItem.LWorkSourceMedia?.LWorkAudioCodec);
        bool lAudioActive = !lAudioExcluded && lAudioPresent;
        bool lAudioCopy = string.Equals(lAudio.LEncodingMode, "Copy", StringComparison.OrdinalIgnoreCase);
        bool lVideoWholeCopyable = lBridgePlan.LBridgeHead is null && lBridgePlan.LBridgeTail is null;

        if (lVideoWholeCopyable && (!lAudioActive || lAudioCopy))
        {
            return new[] { LEncodeDirectBuild(lWorkItem, lAudioActive) };
        }

        bool lBridgeReencode = lBridgePlan.LBridgeHead is not null || lBridgePlan.LBridgeTail is not null;
        string? lBridgeCodec = lBridgeSource?.LBridgeCodec ?? lWorkItem.LWorkSourceMedia?.LWorkMediaCodec;
        if (lBridgeReencode && LRepertoireCatalog.LRepertoireEncoderResolve(lBridgeCodec) is null)
        {
            LRunner.LRunnerRecord(
                $"Smart encoding failed for '{lWorkItem.LWorkOutputName}': the source video codec '{lBridgeCodec ?? "unknown"}' has no matching encoder for the boundary re-encode; nothing was produced");
            return Array.Empty<LEncodeStage>();
        }

        LEncodeSmartProduction lProduction = LEncodeBridgeBuild(lWorkItem, lBridgePlan, lBridgeSource);
        var lStages = new List<LEncodeStage>(lProduction.LEncodeStages);

        string? lAudioPath = null;
        if (lAudioActive)
        {
            lAudioPath = Path.Combine(LDepot.LDepotBridgeRead(), $"{lWorkItem.LWorkId:N}.audio.mkv");
            lStages.Add(LEncodeAudioBuild(lWorkItem, lAudioPath));
        }

        lStages.Add(LEncodeConcatBuild(lWorkItem, lProduction.LEncodeParts, lAudioPath));
        return lStages;
    }

    internal static LEncodeSmartProduction LEncodeBridgeBuild(
        LWorkItem lWorkItem, LBridgePlan lBridgePlan, LBridgeStream? lBridgeSource)
    {
        string lBridgeFolder = LDepot.LDepotBridgeRead();
        const string lBridgeExtension = ".mkv";
        var lStages = new List<LEncodeStage>();
        var lConcatParts = new List<string>();

        if (lBridgePlan.LBridgeHead is { } lBridgeHead)
        {
            string lHeadPath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.head{lBridgeExtension}");
            lStages.Add(LEncodeSpanBuild(lWorkItem, lBridgeHead, lHeadPath, "Encoding head bridge", lBridgeSource));
            lConcatParts.Add(lHeadPath);
        }

        string lMiddlePath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.middle{lBridgeExtension}");
        lStages.Add(LEncodeMiddleBuild(lWorkItem, lBridgePlan.LBridgeMiddle!, lMiddlePath));
        lConcatParts.Add(lMiddlePath);

        if (lBridgePlan.LBridgeTail is { } lBridgeTail)
        {
            string lTailPath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.tail{lBridgeExtension}");
            lStages.Add(LEncodeSpanBuild(lWorkItem, lBridgeTail, lTailPath, "Encoding tail bridge", lBridgeSource));
            lConcatParts.Add(lTailPath);
        }

        return new LEncodeSmartProduction(lStages, lConcatParts);
    }

    private static LEncodeStage LEncodeSpanBuild(
        LWorkItem lWorkItem, LBridgeSpan lBridgeSpan, string lBridgePath, string lBridgeLabel, LBridgeStream? lBridgeSource)
    {
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -ss {LEncodeTimeFormat(lBridgeSpan.LBridgeSpanOrigin)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -t {LEncodeTimeFormat(lBridgeSpan.LBridgeSpanEnd - lBridgeSpan.LBridgeSpanOrigin)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeMatchResolve(lWorkItem, lBridgeSource)}");
        lArguments.Append(" -an");
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lBridgePath)}");
        return new LEncodeStage(lArguments.ToString(), LWorkStage.LWorkStageEncode, lBridgeLabel, lBridgePath, true);
    }

    private static LEncodeStage LEncodeMiddleBuild(LWorkItem lWorkItem, LBridgeSpan lBridgeSpan, string lBridgePath)
    {
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -ss {LEncodeTimeFormat(lBridgeSpan.LBridgeSpanOrigin + LEncodeCopyBias)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -t {LEncodeTimeFormat(lBridgeSpan.LBridgeSpanEnd - lBridgeSpan.LBridgeSpanOrigin)}");
        lArguments.Append(" -c:v copy -an");
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lBridgePath)}");
        return new LEncodeStage(lArguments.ToString(), LWorkStage.LWorkStageEncode, "Copying middle", lBridgePath, true);
    }

    private static LEncodeStage LEncodeDirectBuild(LWorkItem lWorkItem, bool lAudioActive)
    {
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -ss {LEncodeTimeFormat(lWorkItem.LWorkOrigin + LEncodeCopyBias)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -t {LEncodeTimeFormat(lWorkItem.LWorkDuration)}");
        lArguments.Append(" -map 0:v:0");
        if (lAudioActive)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -map {LEncodeMapRead(lWorkItem)} -c copy");
        }
        else
        {
            lArguments.Append(" -c copy -an");
        }

        lArguments.Append(" -avoid_negative_ts make_zero");
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lWorkItem.LWorkOutputPath)}");
        return new LEncodeStage(lArguments.ToString(), LWorkStage.LWorkStageEncode, "Copying", lWorkItem.LWorkOutputPath, false);
    }

    private static LEncodeStage LEncodeAudioBuild(LWorkItem lWorkItem, string lAudioPath)
    {
        LEncoding lOutput = lWorkItem.LWorkOutput;
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -ss {LEncodeTimeFormat(lWorkItem.LWorkOrigin)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -t {LEncodeTimeFormat(lWorkItem.LWorkDuration)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -vn -map {LEncodeMapRead(lWorkItem)}");

        string lLabel;
        if (string.Equals(lOutput.LEncodingAudio.LEncodingMode, "Copy", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -c:a copy");
            lLabel = "Copying audio";
        }
        else
        {
            LEncodeAudio.LEncodeMuxAppend(lArguments, lOutput);
            lLabel = "Encoding audio";
        }

        lArguments.Append(" -avoid_negative_ts make_zero");
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lAudioPath)}");
        return new LEncodeStage(lArguments.ToString(), LWorkStage.LWorkStageEncode, lLabel, lAudioPath, true);
    }

    private static string LEncodeMapRead(LWorkItem lWorkItem) =>
        string.Equals(
            lWorkItem.LWorkOutput.LEncodingAudio.LEncodingStream,
            "Include all audio tracks",
            StringComparison.OrdinalIgnoreCase)
            ? "0:a"
            : "0:a:0";

    internal static LEncodeStage LEncodeConcatBuild(
        LWorkItem lWorkItem, IReadOnlyList<string> lBridgeParts, string? lAudioPath)
    {
        string lJoinPath = LEncodeJoinSave(lWorkItem, lBridgeParts);
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -f concat -safe 0 -i {LEncodeFormat(lJoinPath)}");

        if (lAudioPath is null)
        {
            lArguments.Append(" -map 0:v:0 -c copy -an -avoid_negative_ts make_zero");
            lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lWorkItem.LWorkOutputPath)}");
            return new LEncodeStage(lArguments.ToString(), LWorkStage.LWorkStageMux, "Joining bridges", lWorkItem.LWorkOutputPath, false);
        }

        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lAudioPath)}");
        lArguments.Append(" -map 0:v:0 -map 1:a -c copy -avoid_negative_ts make_zero");
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

    private static string LEncodeMatchResolve(LWorkItem lWorkItem, LBridgeStream? lBridgeSource)
    {
        string lCodec = (lBridgeSource?.LBridgeCodec ?? lWorkItem.LWorkSourceMedia?.LWorkMediaCodec ?? string.Empty)
            .ToLowerInvariant();
        string lEncoder = LRepertoireCatalog.LRepertoireEncoderResolve(lCodec)
            ?? throw new InvalidOperationException(
                $"no smart-encoding boundary encoder maps to source codec '{lCodec}'");

        var lArguments = new StringBuilder();
        lArguments.Append(CultureInfo.InvariantCulture, $"-c:v {lEncoder}");

        if (LEncodeProfileResolve(lEncoder, lBridgeSource?.LBridgeProfile) is { } lProfile)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -profile:v {lProfile}");
        }

        string lPixel = lBridgeSource?.LBridgePixel ?? string.Empty;
        if (LEncodeValueCheck(lPixel))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -pix_fmt {lPixel}");
        }

        long lBitrate = lBridgeSource?.LBridgeBitrate ?? 0;
        if (lBitrate > 0)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -b:v {lBitrate}");
        }
        else
        {
            string lQuality = LEncodeQualityResolve(lEncoder);
            if (lQuality.Length > 0)
            {
                lArguments.Append(CultureInfo.InvariantCulture, $" {lQuality}");
            }
        }

        LEncodeColorAppend(lArguments, "-colorspace", lBridgeSource?.LBridgeColorSpace);
        LEncodeColorAppend(lArguments, "-color_primaries", lBridgeSource?.LBridgeColorPrimaries);
        LEncodeColorAppend(lArguments, "-color_trc", lBridgeSource?.LBridgeColorTransfer);
        LEncodeColorAppend(lArguments, "-color_range", lBridgeSource?.LBridgeColorRange);

        lArguments.Append(" -fps_mode passthrough");

        return lArguments.ToString();
    }

    private static string? LEncodeProfileResolve(string lEncoder, string? lProfile)
    {
        if (!LEncodeValueCheck(lProfile ?? string.Empty))
        {
            return null;
        }

        string lNormalized = lProfile!.ToLowerInvariant();
        return lEncoder switch
        {
            "libx264" => lNormalized switch
            {
                "baseline" or "constrained baseline" => "baseline",
                "main" => "main",
                "high" => "high",
                "high 10" => "high10",
                "high 4:2:2" => "high422",
                "high 4:4:4 predictive" or "high 4:4:4" => "high444",
                _ => null
            },
            "libx265" => lNormalized switch
            {
                "main" => "main",
                "main 10" => "main10",
                _ => null
            },
            _ => null
        };
    }

    private static string LEncodeQualityResolve(string lEncoder) => lEncoder switch
    {
        "libx265" => "-crf 18",
        "libvpx-vp9" => "-b:v 0 -crf 24",
        "libaom-av1" => "-crf 24",
        "ffv1" => string.Empty,
        _ => "-crf 18"
    };

    private static void LEncodeColorAppend(StringBuilder lArguments, string lFlag, string? lValue)
    {
        if (LEncodeValueCheck(lValue ?? string.Empty))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" {lFlag} {lValue}");
        }
    }

    private static bool LEncodeValueCheck(string lValue) =>
        !string.IsNullOrWhiteSpace(lValue)
        && !string.Equals(lValue, "unknown", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(lValue, "N/A", StringComparison.OrdinalIgnoreCase);
}
