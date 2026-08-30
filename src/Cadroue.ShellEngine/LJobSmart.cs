using System.IO;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    private (int, string) LJobLeadingRun(LEncodeStage pStage)
    {
        try
        {
            byte[] pBytes = File.ReadAllBytes(pStage.LEncodeStagePath);
            if (LBridge.LBridgeLeadingNormalize(pBytes))
            {
                File.WriteAllBytes(pStage.LEncodeStagePath, pBytes);
                LRunner.LRunnerRecord(
                    $"Smart encoding neutralized the copied middle's leading keyframe for '{lJobItem.LWorkOutputName}'");
            }

            return (0, string.Empty);
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
            LRunner.LRunnerRecord(
                $"Smart encoding could not adjust the copied middle for '{lJobItem.LWorkOutputName}'", pException);
            return (1, "copied middle could not be adjusted");
        }
    }

    private async Task<(int, string)> LJobSmartRun()
    {
        LBridgeStream? pSource = LScoutStream.LScoutStreamRead(lJobItem.LWorkSourcePath, lJobToken);
        if (pSource is null)
        {
            LRunner.LRunnerRecord(
                $"Smart encoding failed for '{lJobItem.LWorkOutputName}': the source stream could not be read");
            return (1, "source stream unreadable");
        }

        IReadOnlyList<LKeyframeEntry> pKeyframes = LScoutBridge.LScoutBridgeRead(
            lJobItem.LWorkSourcePath, lJobItem.LWorkOrigin, lJobItem.LWorkEnd, lJobToken);
        LWorkMedia? pMedia = lJobItem.LWorkSourceMedia
            ?? LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken);
        bool pOpenEnd = LBridge.LBridgeEndCheck(
            lJobItem.LWorkEnd,
            pMedia?.LWorkMediaDuration ?? TimeSpan.Zero,
            pMedia?.LWorkMediaFramerate ?? 0);
        LBridgePlan pPlan = LBridge.LBridgeRegionResolve(
            pKeyframes, lJobItem.LWorkOrigin, lJobItem.LWorkEnd, pOpenEnd);

        if (pPlan.LBridgeOutcome != LBridgeOutcome.LBridgeOutcomeSmart)
        {
            // STRICT SMART CONTRACT: this is the only full-encode path in a Smart
            // job. It is legal only because planning found no copyable middle.
            LRunner.LRunnerRecord(
                $"Smart encoding not usable for '{lJobItem.LWorkOutputName}': encoding the requested interval");
            return await LJobBatchRun(LEncode.LEncodeWholeBuild(lJobItem), 0, 1).ConfigureAwait(false);
        }

        IReadOnlyList<LEncodeStage> pStages = LEncode.LEncodeSmartBuild(lJobItem, pPlan, pSource);
        if (pStages.Count == 0)
        {
            return (1, $"smart encoding unsupported for source codec '{pSource.LBridgeCodec}'");
        }

        LRunner.LRunnerRecord(
            $"Smart encoding applied for '{lJobItem.LWorkOutputName}': {pStages.Count} stage(s)");
        return await LJobBatchRun(pStages, 0, pStages.Count).ConfigureAwait(false);
    }
}
