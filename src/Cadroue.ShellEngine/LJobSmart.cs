using Cadroue.Application;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    private async Task<(int, string)> LJobSmartRun()
    {
        IReadOnlyList<TimeSpan> pKeyframes = LScout.LScoutBridgeRead(
            lJobItem.LWorkSourcePath, lJobItem.LWorkOrigin, lJobItem.LWorkEnd, lJobToken);
        LBridgePlan pPlan = LBridge.LBridgeRegionResolve(pKeyframes, lJobItem.LWorkOrigin, lJobItem.LWorkEnd);

        if (pPlan.LBridgeOutcome != LBridgeOutcome.LBridgeOutcomeSmart)
        {
            LRunner.LRunnerRecord(
                $"Smart encoding not usable for '{lJobItem.LWorkOutputName}': encoding the requested interval");
            return await LJobBatchRun(LEncode.LEncodeWholeBuild(lJobItem), 0, 1).ConfigureAwait(false);
        }

        LEncodeSmartProduction pProduction = LEncode.LEncodeBridgeBuild(lJobItem, pPlan);
        int pTotal = pProduction.LEncodeStages.Count + 1;

        (int pExit, string pError) = await LJobBatchRun(pProduction.LEncodeStages, 0, pTotal).ConfigureAwait(false);
        if (pExit != 0)
        {
            return (pExit, pError);
        }

        LBridgeCompatibility pCompatibility = LJobBridgeValidate(
            pProduction.LEncodeProbeTarget, pProduction.LEncodeMiddlePath);

        LEncodeStage pFinal;
        if (pCompatibility.LBridgeCompatible)
        {
            LRunner.LRunnerRecord(
                $"Smart encoding applied for '{lJobItem.LWorkOutputName}': joining {pProduction.LEncodeStages.Count} region(s)");
            pFinal = LEncode.LEncodeConcatBuild(lJobItem, pProduction.LEncodeParts, pPlan.LBridgeInterval);
            return await LJobBatchRun(new[] { pFinal }, pProduction.LEncodeStages.Count, pTotal).ConfigureAwait(false);
        }

        LRunner.LRunnerRecord(
            $"Smart encoding fallback for '{lJobItem.LWorkOutputName}': the bridge cannot join the copied continuation ({pCompatibility.LBridgeReason}); encoding the requested interval");
        return await LJobBatchRun(LEncode.LEncodeWholeBuild(lJobItem), pProduction.LEncodeStages.Count, pTotal).ConfigureAwait(false);
    }

    private LBridgeCompatibility LJobBridgeValidate(string? pProbeTarget, string pReferencePath)
    {
        if (string.IsNullOrEmpty(pProbeTarget))
        {
            return new LBridgeCompatibility(true, LBridgeReason.LBridgeReasonCompatible);
        }

        LBridgeStream? pGenerated = LScout.LScoutStreamRead(pProbeTarget, lJobToken);
        LBridgeStream? pReference = LScout.LScoutStreamRead(pReferencePath, lJobToken);
        if (pGenerated is null || pReference is null)
        {
            LRunner.LRunnerRecord(
                $"Smart encoding could not verify the bridge for '{lJobItem.LWorkOutputName}'; encoding the requested interval");
            return new LBridgeCompatibility(false, LBridgeReason.LBridgeReasonUnverified);
        }

        return LBridge.LBridgeValidate(pGenerated, pReference);
    }
}
