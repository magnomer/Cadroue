using System.Globalization;
using System.IO;
using System.Text;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LEncode
{
    public static IReadOnlyList<LEncodeStage> LEncodeSmartBuild(LWorkItem lWorkItem, LBridgePlan lBridgePlan)
    {
        if (lBridgePlan.LBridgeOutcome != LBridgeOutcome.LBridgeOutcomeSmart
            || lBridgePlan.LBridgeMiddle is not { } lBridgeMiddle)
        {
            return new[]
            {
                new LEncodeStage(LEncodeArgumentBuild(lWorkItem), LWorkStage.LWorkStageEncode, "Encoding", lWorkItem.LWorkOutputPath, false)
            };
        }

        string lBridgeFolder = LDepot.LDepotBridgeRead();
        string lBridgeExtension = LEncodeExtensionResolve(lWorkItem.LWorkOutputPath);
        var lStages = new List<LEncodeStage>();
        var lConcatParts = new List<string>();

        if (lBridgePlan.LBridgeHead is { } lBridgeHead)
        {
            string lHeadPath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.head{lBridgeExtension}");
            lStages.Add(LEncodeBridgeBuild(lWorkItem, lBridgeHead, lHeadPath, "Encoding head bridge"));
            lConcatParts.Add(lHeadPath);
        }

        string lMiddlePath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.middle{lBridgeExtension}");
        lStages.Add(LEncodeMiddleBuild(lWorkItem, lBridgeMiddle, lMiddlePath));
        lConcatParts.Add(lMiddlePath);

        if (lBridgePlan.LBridgeTail is { } lBridgeTail)
        {
            string lTailPath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.tail{lBridgeExtension}");
            lStages.Add(LEncodeBridgeBuild(lWorkItem, lBridgeTail, lTailPath, "Encoding tail bridge"));
            lConcatParts.Add(lTailPath);
        }

        lStages.Add(LEncodeConcatBuild(lWorkItem, lConcatParts));
        return lStages;
    }

    private static LEncodeStage LEncodeBridgeBuild(
        LWorkItem lWorkItem, LBridgeSpan lBridgeSpan, string lBridgePath, string lBridgeLabel)
    {
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -ss {LEncodeTimeFormat(lBridgeSpan.LBridgeSpanOrigin)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" -t {LEncodeTimeFormat(lBridgeSpan.LBridgeSpanEnd - lBridgeSpan.LBridgeSpanOrigin)}");
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeLosslessResolve(lWorkItem)}");
        lArguments.Append(" -c:a copy");
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
        lArguments.Append(" -c copy");
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lBridgePath)}");
        return new LEncodeStage(lArguments.ToString(), LWorkStage.LWorkStageEncode, "Copying middle", lBridgePath, true);
    }

    private static LEncodeStage LEncodeConcatBuild(LWorkItem lWorkItem, IReadOnlyList<string> lBridgeParts)
    {
        string lJoinPath = LEncodeJoinSave(lWorkItem, lBridgeParts);
        var lArguments = new StringBuilder();
        LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -f concat -safe 0 -i {LEncodeFormat(lJoinPath)}");
        lArguments.Append(" -c copy");
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

    private static string LEncodeExtensionResolve(string lOutputPath)
    {
        string lExtension = Path.GetExtension(lOutputPath);
        return string.IsNullOrEmpty(lExtension) ? ".mkv" : lExtension;
    }
}
