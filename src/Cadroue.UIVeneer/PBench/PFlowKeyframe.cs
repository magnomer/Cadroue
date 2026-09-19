using System.Windows.Threading;
using Cadroue.Core;

using Cadroue.Application;

using Cadroue.Infrastructure;

namespace Cadroue.UIVeneer.PBench;

public sealed partial class PFlow
{
    private readonly LKeyframeOrchestrator lKeyframeOrchestrator = new();
    private readonly DispatcherTimer lKeyframeRequestTimer;
    private readonly DispatcherTimer lKeyframeResumeTimer;

    private void PFlowKeyframeDefer()
    {
        if (!LFlow.LFlowCommandActive || LFlow.LFlowUnloaded || !LFlow.LFlowSourceCheck())
        {
            lKeyframeRequestTimer.Stop();
            return;
        }
        if (lKeyframeResumeTimer.IsEnabled) return;
        lKeyframeRequestTimer.Stop();
        lKeyframeRequestTimer.Start();
    }

    private void PFlowKeyframeSuspend()
    {
        lKeyframeRequestTimer.Stop();
        lKeyframeResumeTimer.Stop();
        lKeyframeOrchestrator.LKeyframeSuspend();
        lKeyframeResumeTimer.Interval = PFlowResumeRead();
        if (LFlow.LFlowCommandActive && !LFlow.LFlowUnloaded) lKeyframeResumeTimer.Start();
    }

    private static TimeSpan PFlowResumeRead()
        => TimeSpan.FromMilliseconds(LPreference.LPreferenceStateCurrent.LPreferenceKeyframeDelay);

    private void PFlowKeyframeRun()
    {
        lKeyframeRequestTimer.Stop();
        lKeyframeResumeTimer.Stop();
        if (LFlow.LFlowScanCheck())
        {
            string lSourcePath = LFlow.LFlowSourcePath!;
            LTrace.LTraceRecord(
                LTraceKind.LTraceWork,
                $"Keyframe scan requested around {LFlow.LFlowCursor:hh\\:mm\\:ss\\.fff}",
                $"source {System.IO.Path.GetFileName(lSourcePath)}, duration {LFlow.LFlowDuration:hh\\:mm\\:ss}\n"
                + $"window {LKeyframeView.LKeyframeRangeBefore:hh\\:mm\\:ss} before to "
                + $"{LKeyframeView.LKeyframeRangeAfter:hh\\:mm\\:ss} after the cursor");
            lKeyframeOrchestrator.LKeyframeStart(lSourcePath, LFlow.LFlowMediaInfo!, LFlow.LFlowCursor);
        }
    }

    private void PFlowTimerHandle(object? sender, EventArgs e) => PFlowKeyframeRun();

    private void PFlowResumeHandle(object? sender, EventArgs e) => PFlowKeyframeRun();

    private void PFlowNoticeHandle(LKeyframeNotice notice)
    {
        if (!LFlow.LFlowCommandActive
            || LFlow.LFlowUnloaded
            || Dispatcher.HasShutdownStarted
            || Dispatcher.HasShutdownFinished) return;
        Dispatcher.InvokeAsync(() =>
        {
            if (!LFlow.LFlowUnloaded && notice.LKeyframeSerial == lKeyframeOrchestrator.LKeyframeCurrentSerial)
            {
                PFlowKeyframeRecord(notice);
                pViewfinder.PViewfinderKeyframesUpdate(notice.LKeyframeList, notice.LKeyframeRanges);
                pMap.PMapKeyframesUpdate(notice.LKeyframeRanges);
                if (LFlow.LFlowKeyframeDirection is int direction)
                {
                    PFlowKeyframeMove(direction, false);
                }
            }
        }, DispatcherPriority.Background);
    }

    private void PFlowKeyframeRecord(LKeyframeNotice notice)
    {
        double pFlowScanned = notice.LKeyframeRanges.Sum(
            pRange => (pRange.LKeyframeRangeLimit - pRange.LKeyframeRangeOrigin).TotalSeconds);
        string pFlowStamp =
            $"{notice.LKeyframeKind}/{notice.LKeyframeList.Count}/{notice.LKeyframeRanges.Count}/{pFlowScanned:0.###}";
        if (!LFlow.LFlowStampSet(pFlowStamp))
        {
            return;
        }

        string pFlowSource = PFlowSourceFormat();
        LTraceLog.LTraceInfoRecord(notice.LKeyframeKind switch
        {
            LKeyframeKind.LKeyframeKindIntra =>
                $"Keyframe scan '{pFlowSource}': every frame is a keyframe, no scan needed",
            LKeyframeKind.LKeyframeKindNone =>
                $"Keyframe scan '{pFlowSource}': no video stream, no scan needed",
            _ => $"Keyframe scan '{pFlowSource}': {notice.LKeyframeList.Count} keyframe(s) known, "
                + $"{TimeSpan.FromSeconds(pFlowScanned):hh\\:mm\\:ss} scanned across "
                + $"{notice.LKeyframeRanges.Count} range(s)"
        });
    }

    private void PFlowKeyframeMove(int direction, bool requestScan = true)
    {
        if (!LFlow.LFlowSourceCheck())
        {
            PFlowKeyframeDefer();
            return;
        }

        TimeSpan lCursor = LFlow.LFlowCursor;
        LKeyframeMoveResult result = direction switch
        {
            < 0 => lKeyframeOrchestrator.LKeyframePreviousMove(lCursor),
            > 0 => lKeyframeOrchestrator.LKeyframeNextMove(lCursor),
            _ => lKeyframeOrchestrator.LKeyframeNearestMove(lCursor)
        };
        if (result.LKeyframeFailed)
        {
            LFlow.LFlowDirectionSet(null);
            LTraceLog.LTraceWarningRecord(
                "Keyframe navigation unavailable: the scan around the cursor failed repeatedly",
                $"source {PFlowSourceFormat()}, cursor {lCursor:hh\\:mm\\:ss\\.fff}");
            return;
        }

        if (!result.LKeyframeReady)
        {
            LFlow.LFlowDirectionSet(direction);
            if (requestScan)
            {
                PFlowKeyframeRun();
            }
            return;
        }

        LFlow.LFlowDirectionSet(null);
        if (result.LKeyframeTarget is not null)
        {
            PFlowCursorPropagate(result.LKeyframeTarget.Value, true, true);
        }
    }
}
