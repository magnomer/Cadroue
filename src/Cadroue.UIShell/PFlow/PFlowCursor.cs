using Cadroue.Core;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    private TimeSpan lCursor;

    public event Action<TimeSpan>? PFlowCursorChange;

    public TimeSpan PFlowCursorRead() => lCursor;

    public void PFlowCursorUpdate(TimeSpan cursorTime)
    {
        if (!pFlowCommandActive) return;
        PFlowCursorPropagate(cursorTime, false, false);
        PFlowKeyframeDefer();
    }

    public void PFlowCursorSeek(TimeSpan cursorTime)
    {
        if (!pFlowCommandActive) return;
        PFlowKeyframeSuspend();
        PFlowCursorPropagate(cursorTime, true, false);
    }

    private void PFlowViewfinderSeek(TimeSpan cursorTime) => PFlowCursorSeek(cursorTime);

    private void PFlowMapSeek(TimeSpan cursorTime) => PFlowCursorSeek(cursorTime);

    private void PFlowCursorPropagate(TimeSpan cursorTime, bool pFlowViewerSeekRequest, bool lKeyframeRestartRequest)
    {
        pFlowKeyframeDirection = null;
        lCursor = PFlowCursorClamp(cursorTime);
        pViewfinder.PViewfinderCursorUpdate(lCursor);
        pMap.PMapCursorUpdate(lCursor);

        if (lKeyframeRestartRequest)
        {
            PFlowKeyframeRun();
        }

        if (pFlowViewerSeekRequest)
        {
            PFlowCursorChange?.Invoke(lCursor);
        }
    }

    private void PFlowSpoolHandle()
    {
        PFlowKeyframeSuspend();
        PFlowSpoolUpdate();
    }

    private void PFlowSpoolUpdate()
    {
        pViewfinder.PViewfinderSpoolUpdate();
        pMap.PMapSpoolUpdate();
        if (lSpool is null) return;
        pViewfinderLabelLeft.Text = PFlowTimeFormat(lSpool.LSpoolRangeOrigin);
        pViewfinderLabelRight.Text = PFlowTimeFormat(lSpool.LSpoolRangeLimit);
        PFlowKeyframeDefer();
    }

    private TimeSpan PFlowCursorClamp(TimeSpan cursorTime)
    {
        if (lSpool is null || cursorTime < TimeSpan.Zero) return TimeSpan.Zero;
        return cursorTime > lSpool.LSpoolDuration ? lSpool.LSpoolDuration : cursorTime;
    }

    private static string PFlowTimeFormat(TimeSpan displayTime) => displayTime.TotalHours >= 1
        ? $"{(int)displayTime.TotalHours}:{displayTime.Minutes:D2}:{displayTime.Seconds:D2}"
        : $"{displayTime.Minutes}:{displayTime.Seconds:D2}";
}
