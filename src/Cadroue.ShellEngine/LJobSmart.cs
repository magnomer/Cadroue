using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed class LJobSmart
{
    private readonly LRunner lJobOwner;
    private readonly LWorkItem lJobItem;
    private readonly CancellationToken lJobToken;
    private readonly LJobStage lJobStage;

    internal LJobSmart(
        LRunner lJobRunner, LWorkItem lJobWorkItem, CancellationToken lJobStopToken, LJobStage lJobWorkStage)
    {
        lJobOwner = lJobRunner;
        lJobItem = lJobWorkItem;
        lJobToken = lJobStopToken;
        lJobStage = lJobWorkStage;
    }

    internal async Task<(int, string)> LJobSmartRun()
    {
        LBridgeStream? pSource =
            await LScoutStream.LScoutStreamRead(lJobItem.LWorkSourcePath, lJobToken).ConfigureAwait(false);
        if (pSource is null)
        {
            LRunnerTrace.LRunnerRecord(
                $"Smart encoding failed for '{lJobItem.LWorkOutputName}': the source stream could not be read");
            return (1, "source stream unreadable");
        }

        IReadOnlyList<LKeyframeEntry> pKeyframes = await LScoutBridge.LScoutBridgeRead(
            lJobItem.LWorkSourcePath, lJobItem.LWorkOrigin, lJobItem.LWorkEnd, lJobToken, pSource)
            .ConfigureAwait(false);
        LWorkMedia? pMedia = lJobItem.LWorkSourceMedia
            ?? await LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken, lJobOwner.LRunnerLevel)
                .ConfigureAwait(false);
        bool pOpenEnd = LBridge.LBridgeEndCheck(
            lJobItem.LWorkEnd,
            pMedia?.LWorkMediaDuration ?? TimeSpan.Zero,
            pMedia?.LWorkMediaFramerate ?? 0);
        LBridgePlan pPlan = LBridge.LBridgeRegionResolve(
            pKeyframes, lJobItem.LWorkOrigin, lJobItem.LWorkEnd, pOpenEnd);

        if (pPlan.LBridgeOutcome != LBridgeOutcome.LBridgeOutcomeSmart)
        {
            LRunnerTrace.LRunnerRecord(
                $"Smart encoding not usable for '{lJobItem.LWorkOutputName}': encoding the requested interval");
            return await lJobStage.LJobBatchRun(LEncodeSmart.LEncodeWholeBuild(lJobItem), 0, 1).ConfigureAwait(false);
        }

        LScoutAudioInterval? pAudio =
            await LEncodeSmart.LEncodeIntervalRead(lJobItem, pPlan, lJobToken).ConfigureAwait(false);
        IReadOnlyList<LEncodeStage> pStages =
            LEncodeSmart.LEncodeSmartBuild(lJobItem, pPlan, pSource, lAudioInterval: pAudio);
        if (pStages.Count == 0)
        {
            return (1, $"smart encoding unsupported for source codec '{pSource.LBridgeCodec}'");
        }

        LRunnerTrace.LRunnerRecord(
            $"Smart encoding applied for '{lJobItem.LWorkOutputName}': {pStages.Count} stage(s)");
        return await lJobStage.LJobBatchRun(pStages, 0, pStages.Count).ConfigureAwait(false);
    }
}
