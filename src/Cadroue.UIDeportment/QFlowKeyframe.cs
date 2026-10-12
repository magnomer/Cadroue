using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QFlowKeyframe
{
    private readonly QFlow qFlow;
    private readonly CUsher qFlowUsher;
    private readonly LKeyframeOrchestrator qKeyframeOrchestrator = new();
    private bool qFlowResumePending;
    private IReadOnlyList<LKeyframeEntry> qFlowEntries = [];
    private IReadOnlyList<LKeyframeScanRange> qFlowRanges = [];

    public QFlowKeyframe(QFlow lOwner, CUsher lUsher)
    {
        qFlow = lOwner;
        qFlowUsher = lUsher;
        qKeyframeOrchestrator.LKeyframeNoticeReady += QFlowKeyframeHandle;
    }

    public event Action? QFlowTimerDeferred;
    public event Action<TimeSpan>? QFlowTimerResumed;
    public event Action? QFlowTimerStopped;
    public event Action<LKeyframeNotice>? QFlowKeyframeReady;
    public event Action<IReadOnlyList<LKeyframeEntry>, IReadOnlyList<LKeyframeScanRange>>? QFlowKeyframeChanged;

    public bool QFlowResumePending => qFlowResumePending;

    public IReadOnlyList<LKeyframeEntry> QFlowKeyframeEntries => qFlowEntries;

    public IReadOnlyList<LKeyframeScanRange> QFlowKeyframeRanges => qFlowRanges;

    public static TimeSpan QFlowRequestDelay => TimeSpan.FromMilliseconds(250);

    public static TimeSpan QFlowResumeDelay =>
        TimeSpan.FromMilliseconds(LPreference.LPreferenceStateCurrent.LPreferenceKeyframeDelay);

    public void QFlowKeyframeDefer()
    {
        if (!qFlow.QFlowCommandActive || qFlow.QFlowUnloaded || !qFlow.QFlowSourceCheck())
        {
            QFlowTimerStopped?.Invoke();
            return;
        }

        if (qFlowResumePending)
        {
            return;
        }

        QFlowTimerDeferred?.Invoke();
    }

    public void QFlowKeyframeSuspend(bool lResume)
    {
        QFlowKeyframeReset();
        qKeyframeOrchestrator.LKeyframeSuspend();
        if (lResume && qFlow.QFlowCommandActive && !qFlow.QFlowUnloaded)
        {
            qFlowResumePending = true;
            QFlowTimerResumed?.Invoke(QFlowResumeDelay);
        }
    }

    public void QFlowKeyframeReset()
    {
        qFlowResumePending = false;
        QFlowTimerStopped?.Invoke();
    }

    public void QFlowKeyframeRun()
    {
        QFlowKeyframeReset();
        if (!qFlow.QFlowScanCheck())
        {
            return;
        }

        string lSourcePath = qFlow.QFlowSourcePath!;
        LTrace.LTraceRecord(
            LTraceKind.LTraceWork,
            $"Keyframe scan requested around {qFlow.QFlowCursor:hh\\:mm\\:ss\\.fff}",
            $"source {qFlowUsher.CUsherNameRead(lSourcePath)}, duration {qFlow.QFlowDuration:hh\\:mm\\:ss}\n"
            + $"window {LKeyframeView.LKeyframeRangeBefore:hh\\:mm\\:ss} before to "
            + $"{LKeyframeView.LKeyframeRangeAfter:hh\\:mm\\:ss} after the cursor");
        qKeyframeOrchestrator.LKeyframeStart(lSourcePath, qFlow.QFlowMediaInfo!, qFlow.QFlowCursor);
    }

    public void QFlowKeyframeTick() => QFlowKeyframeRun();

    public void QFlowKeyframeSync(TimeSpan lCursor) => qKeyframeOrchestrator.LKeyframeSync(lCursor);

    public void QFlowKeyframeClear()
    {
        qFlowEntries = [];
        qFlowRanges = [];
    }

    public void QFlowKeyframeClose()
    {
        QFlowKeyframeReset();
        qKeyframeOrchestrator.LKeyframeNoticeReady -= QFlowKeyframeHandle;
        qKeyframeOrchestrator.Dispose();
    }

    private void QFlowKeyframeHandle(LKeyframeNotice lNotice)
    {
        if (!qFlow.QFlowCommandActive || qFlow.QFlowUnloaded)
        {
            return;
        }

        QFlowKeyframeReady?.Invoke(lNotice);
    }

    public void QFlowKeyframeApply(LKeyframeNotice lNotice)
    {
        if (qFlow.QFlowUnloaded || lNotice.LKeyframeSerial != qKeyframeOrchestrator.LKeyframeCurrentSerial)
        {
            return;
        }

        QFlowKeyframeRecord(lNotice);
        qFlowEntries = lNotice.LKeyframeList;
        qFlowRanges = lNotice.LKeyframeRanges;
        QFlowKeyframeChanged?.Invoke(lNotice.LKeyframeList, lNotice.LKeyframeRanges);
        if (qFlow.QFlowKeyframeDirection is int lDirection)
        {
            QFlowKeyframeMove(lDirection, false);
        }
    }

    private void QFlowKeyframeRecord(LKeyframeNotice lNotice)
    {
        double lScanned = lNotice.LKeyframeRanges.Sum(
            lRange => (lRange.LKeyframeRangeLimit - lRange.LKeyframeRangeOrigin).TotalSeconds);
        string lStamp =
            $"{lNotice.LKeyframeKind}/{lNotice.LKeyframeList.Count}/{lNotice.LKeyframeRanges.Count}/{lScanned:0.###}";
        if (!qFlow.QFlowStamp.QFlowStampSet(lStamp))
        {
            return;
        }

        string lSource = qFlow.QFlowSourceFormat();
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

    public void QFlowKeyframeMove(int lDirection) => QFlowKeyframeMove(lDirection, true);

    private void QFlowKeyframeMove(int lDirection, bool lRequestScan)
    {
        if (!qFlow.QFlowSourceCheck())
        {
            QFlowKeyframeDefer();
            return;
        }

        TimeSpan lCursor = qFlow.QFlowCursor;
        LKeyframeMoveResult lResult = lDirection switch
        {
            < 0 => qKeyframeOrchestrator.LKeyframePreviousMove(lCursor),
            > 0 => qKeyframeOrchestrator.LKeyframeNextMove(lCursor),
            _ => qKeyframeOrchestrator.LKeyframeNearestMove(lCursor)
        };
        if (lResult.LKeyframeFailed)
        {
            qFlow.QFlowDirectionSet(null);
            LTraceLog.LTraceWarningRecord(
                "Keyframe navigation unavailable: the scan around the cursor failed repeatedly",
                $"source {qFlow.QFlowSourceFormat()}, cursor {lCursor:hh\\:mm\\:ss\\.fff}");
            return;
        }

        if (!lResult.LKeyframeReady)
        {
            qFlow.QFlowDirectionSet(lDirection);
            if (lRequestScan)
            {
                QFlowKeyframeRun();
            }

            return;
        }

        qFlow.QFlowDirectionSet(null);
        if (lResult.LKeyframeTarget is { } lTarget)
        {
            qFlow.QFlowCursorPropagate(lTarget, true, true);
        }
    }
}
