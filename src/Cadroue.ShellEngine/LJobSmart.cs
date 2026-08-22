using Cadroue.Application;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    private async Task<(int, string)> LJobSmartRun()
    {
        LBridgeStream? pSource = LScout.LScoutStreamRead(lJobItem.LWorkSourcePath, lJobToken);
        if (pSource is null)
        {
            LRunner.LRunnerRecord(
                $"Smart encoding failed for '{lJobItem.LWorkOutputName}': the source stream could not be read");
            return (1, "source stream unreadable");
        }

        IReadOnlyList<TimeSpan> pKeyframes = LScout.LScoutBridgeRead(
            lJobItem.LWorkSourcePath, lJobItem.LWorkOrigin, lJobItem.LWorkEnd, lJobToken);
        LBridgePlan pPlan = LBridge.LBridgeRegionResolve(pKeyframes, lJobItem.LWorkOrigin, lJobItem.LWorkEnd);

        if (pPlan.LBridgeOutcome != LBridgeOutcome.LBridgeOutcomeSmart)
        {
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
