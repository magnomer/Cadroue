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
            // STRICT SMART CONTRACT: full encoding is allowed only when planning
            // found no copyable middle. Once a middle exists, later uncertainty
            // must never silently replace Smart with a full re-encode.
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
            // A keyframe-to-keyframe Smart interval is ordinary stream copy. Keep the
            // streams in one input timeline; splitting them through independent MKV
            // intermediates can preserve different timestamp origins that only become
            // visible when the result is decoded by a later Edit/Convert operation.
            return new[] { LEncodeDirectCopyBuild(lWorkItem, lBridgePlan.LBridgeMiddle, lAudioActive, lBridgeSource) };
        }

        TimeSpan lAudioOffset = TimeSpan.Zero;
        if (lAudioActive
            && File.Exists(lWorkItem.LWorkSourcePath))
        {
            bool lAudioAllTracks = string.Equals(
                lAudio.LEncodingStream,
                "Include all audio tracks",
                StringComparison.OrdinalIgnoreCase);
            LScoutAudioInterval? lAudioInterval = LScout.LScoutAudioResolve(
                lWorkItem.LWorkSourcePath,
                lWorkItem.LWorkOrigin,
                lWorkItem.LWorkEnd,
                lAudioAllTracks);
            if (lAudioInterval is null)
            {
                LRunner.LRunnerRecord(
                    $"Smart encoding audio timing could not be verified for '{lWorkItem.LWorkOutputName}': preserving the audio stream with zero additional offset");
            }
            else if (!lAudioInterval.LScoutAudioPresent)
            {
                lAudioActive = false;
                LRunner.LRunnerRecord(
                    $"Smart encoding omitted audio for '{lWorkItem.LWorkOutputName}': no audio packets overlap the requested interval");
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
                $"Smart encoding failed for '{lWorkItem.LWorkOutputName}': the source video codec '{lBridgeCodec ?? "unknown"}' has no matching encoder for the boundary re-encode; nothing was produced");
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
            lConcatParts.Add(lHeadPath);
        }

        string lMiddlePath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.middle{lBridgeExtension}");
        lStages.Add(LEncodeMiddleBuild(lWorkItem, lBridgePlan.LBridgeMiddle!, lMiddlePath, lBridgeSource));
        lConcatParts.Add(lMiddlePath);

        string lLeadingCodec = (lBridgeSource?.LBridgeCodec ?? lWorkItem.LWorkSourceMedia?.LWorkMediaCodec ?? string.Empty)
            .ToLowerInvariant();
        if (lBridgePlan.LBridgeHead is not null && lLeadingCodec is "hevc" or "h265")
        {
            // The copied middle follows the head, so its open-GOP first keyframe must
            // be neutralized before the join (see LBridgeLeadingNormalize). A head-less
            // plan starts on the middle, where a decoder discards leading pictures itself.
            lStages.Add(new LEncodeStage(
                string.Empty, LWorkStage.LWorkStageAdjust, "Normalizing splice", lMiddlePath, true));
        }

        if (lBridgePlan.LBridgeTail is { } lBridgeTail)
        {
            string lTailPath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.tail{lBridgeExtension}");
            lStages.Add(LEncodeSpanBuild(lWorkItem, lBridgeTail, lTailPath, "Encoding tail bridge", lBridgeSource));
            lConcatParts.Add(lTailPath);
        }

        return new LEncodeSmartProduction(lStages, lConcatParts);
    }

    private static string LEncodeExtensionResolve(string? lIntermediateExtension)
    {
        // Bridge pieces default to an ISO-BMFF container: it preserves the copied
        // middle's source timestamps and its mdat carries plain length-prefixed NAL
        // units, so the leading-keyframe neutralization is a direct byte rewrite.
        string lExtension = lIntermediateExtension ?? ".mov";
        if (string.IsNullOrWhiteSpace(lExtension))
        {
            return string.Empty;
        }

        return lExtension.StartsWith(".", StringComparison.Ordinal)
            ? lExtension
            : "." + lExtension;
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
        LEncodeTimescaleAppend(lArguments, lWorkItem, lBridgeSource, lBridgePath);
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lBridgePath)}");
        return new LEncodeStage(lArguments.ToString(), LWorkStage.LWorkStageEncode, lBridgeLabel, lBridgePath, true);
    }

    private static LEncodeStage LEncodeMiddleBuild(
        LWorkItem lWorkItem,
        LBridgeSpan lBridgeSpan,
        string lBridgePath,
        LBridgeStream? lBridgeSource)
    {
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        TimeSpan lCopyOrigin = lBridgeSpan.LBridgeSpanOrigin;
        TimeSpan lCopyDuration = lBridgeSpan.LBridgeSpanEnd - lBridgeSpan.LBridgeSpanOrigin;
        if (lBridgeSpan.LBridgeDecodeEnd is TimeSpan lDecodeEnd)
        {
            // A copied GOP must stop before the following keyframe's DTS. Stopping at
            // its PTS also copies that keyframe and its reordered frames into the
            // encoded tail, producing duplicate preroll and an inflated timeline.
            TimeSpan lDecodeDuration = lDecodeEnd - lCopyOrigin;
            if (lDecodeDuration > TimeSpan.Zero)
            {
                lCopyDuration = lDecodeDuration;
            }
        }

        lArguments.Append(CultureInfo.InvariantCulture, $" -ss {LEncodeTimeFormat(lCopyOrigin)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -t {LEncodeTimeFormat(lCopyDuration)}");
        // The keyframe timestamps retain probe precision, so packets before the
        // selected presentation boundary belong to the preceding GOP. Keeping
        // them can lengthen container timelines by a complete GOP.
        lArguments.Append(" -copypriorss 0 -c:v copy -an");
        LEncodeTimescaleAppend(lArguments, lWorkItem, lBridgeSource, lBridgePath);
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lBridgePath)}");
        return new LEncodeStage(lArguments.ToString(), LWorkStage.LWorkStageEncode, "Copying middle", lBridgePath, true);
    }

    private static LEncodeStage LEncodeDirectCopyBuild(
        LWorkItem lWorkItem,
        LBridgeSpan lCopySpan,
        bool lAudioActive,
        LBridgeStream? lBridgeSource)
    {
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -ss {LEncodeTimeFormat(lCopySpan.LBridgeSpanOrigin)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lArguments.Append(CultureInfo.InvariantCulture,
            $" -t {LEncodeTimeFormat(lCopySpan.LBridgeSpanEnd - lCopySpan.LBridgeSpanOrigin)}");
        lArguments.Append(" -map 0:v:0 -c:v copy -avoid_negative_ts make_zero");

        if (lAudioActive)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -map {LEncodeMapRead(lWorkItem)}");
            if (string.Equals(
                lWorkItem.LWorkOutput.LEncodingAudio.LEncodingMode,
                "Copy",
                StringComparison.OrdinalIgnoreCase))
            {
                lArguments.Append(" -c:a copy");
            }
            else
            {
                LEncodeAudio.LEncodeMuxAppend(lArguments, lWorkItem.LWorkOutput);
            }
        }
        else
        {
            lArguments.Append(" -an");
        }

        LEncodeTimescaleAppend(lArguments, lWorkItem, lBridgeSource);
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lWorkItem.LWorkOutputPath)}");
        return new LEncodeStage(
            lArguments.ToString(),
            LWorkStage.LWorkStageEncode,
            "Copying",
            lWorkItem.LWorkOutputPath,
            false);
    }

    private static LEncodeStage LEncodeAudioBuild(LWorkItem lWorkItem, string lAudioPath)
    {
        LEncoding lOutput = lWorkItem.LWorkOutput;
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -ss {LEncodeTimeFormat(lWorkItem.LWorkOrigin)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lArguments.Append(" -ss 0");
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
        LWorkItem lWorkItem,
        IReadOnlyList<string> lBridgeParts,
        string? lAudioPath,
        TimeSpan lAudioOffset = default,
        LBridgeStream? lBridgeSource = null)
    {
        string lJoinPath = LEncodeJoinSave(lWorkItem, lBridgeParts);
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -f concat -safe 0 -i {LEncodeFormat(lJoinPath)}");

        if (lAudioPath is null)
        {
            lArguments.Append(" -map 0:v:0 -c copy -an");
            LEncodeTimescaleAppend(lArguments, lWorkItem, lBridgeSource);
            lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lWorkItem.LWorkOutputPath)}");
            return new LEncodeStage(lArguments.ToString(), LWorkStage.LWorkStageMux, "Joining bridges", lWorkItem.LWorkOutputPath, false);
        }

        if (lAudioOffset > TimeSpan.Zero)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -itsoffset {LEncodeTimeFormat(lAudioOffset)}");
        }

        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lAudioPath)}");
        lArguments.Append(" -map 0:v:0 -map 1:a -c copy");
        LEncodeTimescaleAppend(lArguments, lWorkItem, lBridgeSource);
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lWorkItem.LWorkOutputPath)}");
        return new LEncodeStage(lArguments.ToString(), LWorkStage.LWorkStageMux, "Joining bridges", lWorkItem.LWorkOutputPath, false);
    }

    private static void LEncodeTimescaleAppend(
        StringBuilder lArguments,
        LWorkItem lWorkItem,
        LBridgeStream? lBridgeSource,
        string? lTargetPath = null)
    {
        string lTimescale = LEncodeTimescaleRead(lWorkItem, lBridgeSource, lTargetPath);
        if (lTimescale.Length > 0)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" {lTimescale}");
        }
    }

    private static string LEncodeTimescaleRead(
        LWorkItem lWorkItem,
        LBridgeStream? lBridgeSource,
        string? lTargetPath = null)
    {
        string lExtension = Path.GetExtension(lTargetPath ?? lWorkItem.LWorkOutputPath);
        bool lMovFamily = (lTargetPath is null
                && (string.Equals(lWorkItem.LWorkOutput.LEncodingContainer, "MP4", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(lWorkItem.LWorkOutput.LEncodingContainer, "MOV", StringComparison.OrdinalIgnoreCase)))
            || string.Equals(lExtension, ".mp4", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lExtension, ".m4v", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lExtension, ".mov", StringComparison.OrdinalIgnoreCase);
        if (!lMovFamily || string.IsNullOrWhiteSpace(lBridgeSource?.LBridgeTimeBase))
        {
            return string.Empty;
        }

        string[] lTimeBase = lBridgeSource.LBridgeTimeBase.Split('/');
        if (lTimeBase.Length != 2
            || !long.TryParse(lTimeBase[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long lNumerator)
            || !long.TryParse(lTimeBase[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long lDenominator)
            || lNumerator != 1
            || lDenominator <= 0
            || lDenominator > int.MaxValue)
        {
            return string.Empty;
        }

        // Smart may join independently muxed video pieces. Without an explicit MOV/MP4
        // track timescale, the final remux can choose a different unit from a neighboring
        // Smart section that took the full-encode or direct-copy route. Such files are
        // individually valid but concat later interprets their packet timestamps using
        // one time base, shortening or lengthening video and corrupting the joined
        // audio/video presentation timeline.
        return $"-video_track_timescale {lDenominator.ToString(CultureInfo.InvariantCulture)}";
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
