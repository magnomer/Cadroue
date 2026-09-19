using Cadroue.Core;

namespace Cadroue.UIVeneer.PBench;

public sealed partial class PFlow
{
    public event Action<TimeSpan>? PFlowCursorChange;

    public TimeSpan PFlowCursorRead() => LFlow.LFlowCursor;

    public void PFlowCursorUpdate(TimeSpan cursorTime)
    {
        if (!LFlow.LFlowCommandActive) return;
        PFlowCursorPropagate(cursorTime, false, false);
        PFlowKeyframeDefer();
    }

    public void PFlowCursorSeek(TimeSpan cursorTime)
    {
        if (!LFlow.LFlowCommandActive) return;
        PFlowKeyframeSuspend();
        PFlowCursorPropagate(cursorTime, true, false);
    }

    private void PFlowViewfinderSeek(TimeSpan cursorTime) => PFlowCursorSeek(cursorTime);

    private void PFlowMapSeek(TimeSpan cursorTime) => PFlowCursorSeek(cursorTime);

    private void PFlowCursorPropagate(TimeSpan cursorTime, bool pFlowViewerSeekRequest, bool lKeyframeRestartRequest)
    {
        LFlow.LFlowDirectionSet(null);
        LFlow.LFlowCursorSet(cursorTime);
        pViewfinder.PViewfinderCursorUpdate();
        pMap.PMapCursorUpdate();

        if (lKeyframeRestartRequest)
        {
            PFlowKeyframeRun();
        }

        if (pFlowViewerSeekRequest)
        {
            PFlowCursorChange?.Invoke(LFlow.LFlowCursor);
        }
    }

    public (TimeSpan, TimeSpan)? PFlowRangeRead() =>
        LFlow.LFlowSpool is not { } lSpool ? null : (lSpool.LSpoolRangeOrigin, lSpool.LSpoolRangeLimit);

    public void PFlowRangeSet(TimeSpan pFlowOrigin, TimeSpan pFlowLimit)
    {
        if (LFlow.LFlowSpool is not { } lSpool) return;
        lSpool.LSpoolRangeSet(pFlowOrigin, pFlowLimit);
        PFlowSpoolHandle();
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
        if (LFlow.LFlowSpool is not { } lSpool) return;
        pViewfinderLabelLeft.Text = PFlowTimeFormat(lSpool.LSpoolRangeOrigin);
        pViewfinderLabelRight.Text = PFlowTimeFormat(lSpool.LSpoolRangeLimit);
        PFlowKeyframeDefer();
    }

    private static string PFlowTimeFormat(TimeSpan displayTime) => displayTime.TotalHours >= 1
        ? $"{(int)displayTime.TotalHours}:{displayTime.Minutes:D2}:{displayTime.Seconds:D2}"
        : $"{displayTime.Minutes}:{displayTime.Seconds:D2}";
}
