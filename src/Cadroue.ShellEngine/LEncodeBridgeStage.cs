using System.Globalization;
using System.IO;
using System.Text;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LEncode
{
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

    private static LEncodeStage LEncodeDirectBuild(
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
}
