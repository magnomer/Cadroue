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

        LEncodeSmartProduction pProduction = LEncode.LEncodeBridgeBuild(lJobItem, pPlan, pSource);
        int pTotal = pProduction.LEncodeStages.Count + 1;

        (int pExit, string pError) = await LJobBatchRun(pProduction.LEncodeStages, 0, pTotal).ConfigureAwait(false);
        if (pExit != 0)
        {
            return (pExit, pError);
        }

        LRunner.LRunnerRecord(
            $"Smart encoding applied for '{lJobItem.LWorkOutputName}': joining {pProduction.LEncodeStages.Count} region(s)");
        LEncodeStage pFinal = LEncode.LEncodeConcatBuild(lJobItem, pProduction.LEncodeParts, pPlan.LBridgeInterval);
        return await LJobBatchRun(new[] { pFinal }, pProduction.LEncodeStages.Count, pTotal).ConfigureAwait(false);
    }
}
