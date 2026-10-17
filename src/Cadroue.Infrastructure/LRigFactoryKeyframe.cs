using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRigFactoryKeyframe
{
    // One flow's keyframe scan over its own orchestrator, with the trace kinds the scan records.
    public static LRigKeyframe LRigKeyframeCreate()
    {
        LKeyframeOrchestrator lOrchestrator = new();
        return new LRigKeyframe(
            lOrchestrator.LKeyframeStart,
            lOrchestrator.LKeyframeSuspend,
            lOrchestrator.LKeyframeSync,
            () => lOrchestrator.LKeyframeCurrentSerial,
            lCursor => LRigKeyframeRead(lOrchestrator.LKeyframePreviousMove(lCursor)),
            lCursor => LRigKeyframeRead(lOrchestrator.LKeyframeNearestMove(lCursor)),
            lCursor => LRigKeyframeRead(lOrchestrator.LKeyframeNextMove(lCursor)),
            lHandler => lOrchestrator.LKeyframeNoticeReady += lHandler,
            lHandler =>
            {
                lOrchestrator.LKeyframeNoticeReady -= lHandler;
                lOrchestrator.Dispose();
            },
            (lSummary, lDetail) => LTrace.LTraceRecord(LTraceKind.LTraceWork, lSummary, lDetail),
            (lMessage, lDetail) => LTraceLog.LTraceWarningRecord(lMessage, lDetail));
    }

    private static (bool, TimeSpan?, bool) LRigKeyframeRead(LKeyframeMoveResult lMove) =>
        new(lMove.LKeyframeReady, lMove.LKeyframeTarget, lMove.LKeyframeFailed);
}
