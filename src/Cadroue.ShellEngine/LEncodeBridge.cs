using System.Globalization;
using System.Text;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed record LEncodeSmartProduction(
    IReadOnlyList<LEncodeStage> LEncodeStages,
    IReadOnlyList<string> LEncodeParts);

internal static class LEncodeBridge
{
    internal static LEncodeSmartProduction LEncodeBridgeBuild(
        LWorkItem lWorkItem,
        LBridgePlan lBridgePlan,
        LBridgeStream? lBridgeSource,
        string lBridgeExtension)
    {
        string lBridgeFolder = LDepotEngine.LDepotBridgeSeam();
        var lStages = new List<LEncodeStage>();
        var lConcatParts = new List<string>();

        if (lBridgePlan.LBridgeHead is { } lBridgeHead)
        {
            string lHeadPath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.head{lBridgeExtension}");
            lStages.Add(LEncodeBridgeStage.LEncodeSpanBuild(
                lWorkItem, lBridgeHead, lHeadPath, "Encoding head bridge", lBridgeSource));
            lConcatParts.Add(LEncodePieceBuild(lStages, lHeadPath));
        }

        string lMiddlePath = Path.Combine(lBridgeFolder, $"{lWorkItem.LWorkId:N}.middle{lBridgeExtension}");
        lStages.Add(LEncodeBridgeStage.LEncodeMiddleBuild(
            lWorkItem, lBridgePlan.LBridgeMiddle!, lMiddlePath, lBridgeSource));

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
            lStages.Add(LEncodeBridgeStage.LEncodeSpanBuild(
                lWorkItem, lBridgeTail, lTailPath, "Encoding tail bridge", lBridgeSource));
            lConcatParts.Add(LEncodePieceBuild(lStages, lTailPath));
        }

        return new LEncodeSmartProduction(lStages, lConcatParts);
    }

    private static string LEncodePieceBuild(List<LEncodeStage> lStages, string lPartPath)
    {
        string lPiecePath = Path.ChangeExtension(lPartPath, ".ts");
        var lArguments = new StringBuilder();
        LEncode.LEncodeHeaderAppend(lArguments);
        lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncode.LEncodeFormat(lPartPath)}");
        lArguments.Append(" -map 0:v:0 -c copy -f mpegts");
        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncode.LEncodeFormat(lPiecePath)}");
        lStages.Add(new LEncodeStage(
            lArguments.ToString(), LWorkStage.LWorkStageExtract, "Preparing bridge piece", lPiecePath, true));
        return lPiecePath;
    }
}
