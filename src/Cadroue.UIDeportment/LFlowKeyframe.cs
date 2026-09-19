using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class LFlowKeyframe
{
    private readonly LFlow lFlow;
    private readonly LKeyframeOrchestrator lKeyframeOrchestrator = new();
    private bool lFlowResumePending;

    public LFlowKeyframe(LFlow lOwner)
    {
        lFlow = lOwner;
        lKeyframeOrchestrator.LKeyframeNoticeReady += LFlowKeyframeHandle;
    }

    public event Action? LFlowTimerDefer;
    public event Action<TimeSpan>? LFlowTimerResume;
    public event Action? LFlowTimerReset;
    public event Action<LKeyframeNotice>? LFlowKeyframeReady;
    public event Action<IReadOnlyList<LKeyframeEntry>, IReadOnlyList<LKeyframeScanRange>>? LFlowKeyframeChange;

    public bool LFlowResumePending => lFlowResumePending;

    public static TimeSpan LFlowRequestDelay => TimeSpan.FromMilliseconds(250);

    public static TimeSpan LFlowResumeDelay =>
        TimeSpan.FromMilliseconds(LPreference.LPreferenceStateCurrent.LPreferenceKeyframeDelay);

    public void LFlowKeyframeDefer()
    {
        if (!lFlow.LFlowCommandActive || lFlow.LFlowUnloaded || !lFlow.LFlowSourceCheck())
        {
            LFlowTimerReset?.Invoke();
            return;
        }

        if (lFlowResumePending)
        {
            return;
        }

        LFlowTimerDefer?.Invoke();
    }

    public void LFlowKeyframeSuspend(bool lResume)
    {
        LFlowKeyframeReset();
        lKeyframeOrchestrator.LKeyframeSuspend();
        if (lResume && lFlow.LFlowCommandActive && !lFlow.LFlowUnloaded)
        {
            lFlowResumePending = true;
            LFlowTimerResume?.Invoke(LFlowResumeDelay);
        }
    }

    public void LFlowKeyframeReset()
    {
        lFlowResumePending = false;
        LFlowTimerReset?.Invoke();
    }

    public void LFlowKeyframeRun()
    {
        LFlowKeyframeReset();
        if (!lFlow.LFlowScanCheck())
        {
            return;
        }

        string lSourcePath = lFlow.LFlowSourcePath!;
        LTrace.LTraceRecord(
            LTraceKind.LTraceWork,
            $"Keyframe scan requested around {lFlow.LFlowCursor:hh\\:mm\\:ss\\.fff}",
            $"source {LUsher.LUsherNameRead(lSourcePath)}, duration {lFlow.LFlowDuration:hh\\:mm\\:ss}\n"
            + $"window {LKeyframeView.LKeyframeRangeBefore:hh\\:mm\\:ss} before to "
            + $"{LKeyframeView.LKeyframeRangeAfter:hh\\:mm\\:ss} after the cursor");
        lKeyframeOrchestrator.LKeyframeStart(lSourcePath, lFlow.LFlowMediaInfo!, lFlow.LFlowCursor);
    }

    public void LFlowKeyframeTick() => LFlowKeyframeRun();

    public void LFlowKeyframeClose()
    {
        LFlowKeyframeReset();
        lKeyframeOrchestrator.LKeyframeNoticeReady -= LFlowKeyframeHandle;
        lKeyframeOrchestrator.Dispose();
    }

    private void LFlowKeyframeHandle(LKeyframeNotice lNotice)
    {
        if (!lFlow.LFlowCommandActive || lFlow.LFlowUnloaded)
        {
            return;
        }

        LFlowKeyframeReady?.Invoke(lNotice);
    }

    public void LFlowKeyframeApply(LKeyframeNotice lNotice)
    {
        if (lFlow.LFlowUnloaded || lNotice.LKeyframeSerial != lKeyframeOrchestrator.LKeyframeCurrentSerial)
        {
            return;
        }

        LFlowKeyframeRecord(lNotice);
        LFlowKeyframeChange?.Invoke(lNotice.LKeyframeList, lNotice.LKeyframeRanges);
        if (lFlow.LFlowKeyframeDirection is int lDirection)
        {
            LFlowKeyframeMove(lDirection, false);
        }
    }

    private void LFlowKeyframeRecord(LKeyframeNotice lNotice)
    {
        double lScanned = lNotice.LKeyframeRanges.Sum(
            lRange => (lRange.LKeyframeRangeLimit - lRange.LKeyframeRangeOrigin).TotalSeconds);
        string lStamp =
            $"{lNotice.LKeyframeKind}/{lNotice.LKeyframeList.Count}/{lNotice.LKeyframeRanges.Count}/{lScanned:0.###}";
        if (!lFlow.LFlowStampSet(lStamp))
        {
            return;
        }

        string lSource = lFlow.LFlowSourceFormat();
        LTraceLog.LTraceInfoRecord(lNotice.LKeyframeKind switch
        {
            LKeyframeKind.LKeyframeKindIntra =>
                $"Keyframe scan '{lSource}': every frame is a keyframe, no scan needed",
            LKeyframeKind.LKeyframeKindNone =>
                $"Keyframe scan '{lSource}': no video stream, no scan needed",
            _ => $"Keyframe scan '{lSource}': {lNotice.LKeyframeList.Count} keyframe(s) known, "
                + $"{TimeSpan.FromSeconds(lScanned):hh\\:mm\\:ss} scanned across "
                + $"{lNotice.LKeyframeRanges.Count} range(s)"
        });
    }

    public void LFlowKeyframeMove(int lDirection) => LFlowKeyframeMove(lDirection, true);

    private void LFlowKeyframeMove(int lDirection, bool lRequestScan)
    {
        if (!lFlow.LFlowSourceCheck())
        {
            LFlowKeyframeDefer();
            return;
        }

        TimeSpan lCursor = lFlow.LFlowCursor;
        LKeyframeMoveResult lResult = lDirection switch
        {
            < 0 => lKeyframeOrchestrator.LKeyframePreviousMove(lCursor),
            > 0 => lKeyframeOrchestrator.LKeyframeNextMove(lCursor),
            _ => lKeyframeOrchestrator.LKeyframeNearestMove(lCursor)
        };
        if (lResult.LKeyframeFailed)
        {
            lFlow.LFlowDirectionSet(null);
            LTraceLog.LTraceWarningRecord(
                "Keyframe navigation unavailable: the scan around the cursor failed repeatedly",
                $"source {lFlow.LFlowSourceFormat()}, cursor {lCursor:hh\\:mm\\:ss\\.fff}");
            return;
        }

        if (!lResult.LKeyframeReady)
        {
            lFlow.LFlowDirectionSet(lDirection);
            if (lRequestScan)
            {
                LFlowKeyframeRun();
            }

            return;
        }

        lFlow.LFlowDirectionSet(null);
        if (lResult.LKeyframeTarget is { } lTarget)
        {
            lFlow.LFlowCursorPropagate(lTarget, true, true);
        }
    }
}
