using System.Windows.Threading;
using Cadroue.Core;

using Cadroue.Application;

using Cadroue.Infrastructure;

namespace Cadroue.UIVeneer.PFlow;

public sealed partial class PFlow
{
    private readonly LKeyframeOrchestrator lKeyframeOrchestrator = new();
    private readonly DispatcherTimer lKeyframeRequestTimer;
    private readonly DispatcherTimer lKeyframeResumeTimer;

    private string? pFlowKeyframeStamp;
    private int? pFlowKeyframeDirection;

    private void PFlowKeyframeDefer()
    {
        if (!pFlowCommandActive
            || pFlowUnloaded
            || lSpool is null
            || string.IsNullOrWhiteSpace(lSourcePath))
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
        if (pFlowCommandActive && !pFlowUnloaded) lKeyframeResumeTimer.Start();
    }

    private static TimeSpan PFlowResumeRead()
        => TimeSpan.FromMilliseconds(LPreference.LPreferenceStateCurrent.LPreferenceKeyframeDelay);

    private void PFlowKeyframeRun()
    {
        lKeyframeRequestTimer.Stop();
        lKeyframeResumeTimer.Stop();
        if (pFlowCommandActive
            && !pFlowUnloaded
            && lSpool is not null
            && lMediaInfo is { } pFlowMediaInfo
            && !string.IsNullOrWhiteSpace(lSourcePath))
        {
            LTrace.LTraceRecord(
                LTraceKind.LTraceWork,
                $"Keyframe scan requested around {lCursor:hh\\:mm\\:ss\\.fff}",
                $"source {System.IO.Path.GetFileName(lSourcePath)}, duration {lSpool.LSpoolDuration:hh\\:mm\\:ss}\n"
                + $"window {LKeyframeView.LKeyframeRangeBefore:hh\\:mm\\:ss} before to "
                + $"{LKeyframeView.LKeyframeRangeAfter:hh\\:mm\\:ss} after the cursor");
            lKeyframeOrchestrator.LKeyframeStart(lSourcePath, pFlowMediaInfo, lCursor);
        }
    }

    private void PFlowTimerHandle(object? sender, EventArgs e) => PFlowKeyframeRun();

    private void PFlowResumeHandle(object? sender, EventArgs e) => PFlowKeyframeRun();

    private void PFlowNoticeHandle(LKeyframeNotice notice)
    {
        if (!pFlowCommandActive
            || pFlowUnloaded
            || Dispatcher.HasShutdownStarted
            || Dispatcher.HasShutdownFinished) return;
        Dispatcher.InvokeAsync(() =>
        {
            if (!pFlowUnloaded && notice.LKeyframeSerial == lKeyframeOrchestrator.LKeyframeCurrentSerial)
            {
                PFlowKeyframeRecord(notice);
                pViewfinder.PViewfinderKeyframesUpdate(notice.LKeyframeList, notice.LKeyframeRanges);
                pMap.PMapKeyframesUpdate(notice.LKeyframeRanges);
                if (pFlowKeyframeDirection is int direction)
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
        if (string.Equals(pFlowStamp, pFlowKeyframeStamp, StringComparison.Ordinal))
        {
            return;
        }

        pFlowKeyframeStamp = pFlowStamp;
        string pFlowSource = string.IsNullOrWhiteSpace(lSourcePath)
            ? "(no media)"
            : System.IO.Path.GetFileName(lSourcePath);
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
        if (lSpool is null || string.IsNullOrWhiteSpace(lSourcePath))
        {
            PFlowKeyframeDefer();
            return;
        }

        LKeyframeMoveResult result = direction switch
        {
            < 0 => lKeyframeOrchestrator.LKeyframePreviousMove(lCursor),
            > 0 => lKeyframeOrchestrator.LKeyframeNextMove(lCursor),
            _ => lKeyframeOrchestrator.LKeyframeNearestMove(lCursor)
        };
        if (result.LKeyframeFailed)
        {
            pFlowKeyframeDirection = null;
            LTraceLog.LTraceWarningRecord(
                "Keyframe navigation unavailable: the scan around the cursor failed repeatedly",
                $"source {System.IO.Path.GetFileName(lSourcePath)}, cursor {lCursor:hh\\:mm\\:ss\\.fff}");
            return;
        }

        if (!result.LKeyframeReady)
        {
            pFlowKeyframeDirection = direction;
            if (requestScan)
            {
                PFlowKeyframeRun();
            }
            return;
        }

        pFlowKeyframeDirection = null;
        if (result.LKeyframeTarget is not null)
        {
            PFlowCursorPropagate(result.LKeyframeTarget.Value, true, true);
        }
    }
}
