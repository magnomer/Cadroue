namespace Cadroue.UIDeportment;

public sealed class QFlowSpool
{
    private readonly LFlow lFlow;
    private TimeSpan lFlowDragTime;

    public QFlowSpool(LFlow lOwner)
    {
        lFlow = lOwner;
    }

    public event Action? LFlowSpoolApplied;

    public TimeSpan LFlowDragTime => lFlowDragTime;

    public string LFlowLabelOrigin => LCursor.LCursorTimeFormat(lFlow.LFlowSpool?.LSpoolRangeOrigin ?? TimeSpan.Zero);

    public string LFlowLabelLimit => LCursor.LCursorTimeFormat(lFlow.LFlowSpool?.LSpoolRangeLimit ?? TimeSpan.Zero);

    public string LFlowLabelZero => LCursor.LCursorTimeFormat(TimeSpan.Zero);

    public string LFlowLabelDuration => LCursor.LCursorTimeFormat(lFlow.LFlowDuration);

    public void LFlowDragSet(TimeSpan lDragTime) => lFlowDragTime = lDragTime;

    public void LFlowRangeSet(TimeSpan lOrigin, TimeSpan lLimit)
    {
        if (lFlow.LFlowSpool is not { } lSpool)
        {
            return;
        }

        lSpool.LSpoolRangeSet(lOrigin, lLimit);
        LFlowSpoolHandle();
    }

    public void LFlowSpoolHandle()
    {
        lFlow.LFlowKeyframe.LFlowKeyframeSuspend(true);
        LFlowSpoolUpdate();
    }

    public void LFlowSpoolUpdate()
    {
        LFlowSpoolApplied?.Invoke();
        if (lFlow.LFlowSpool is null)
        {
            return;
        }

        lFlow.LFlowKeyframe.LFlowKeyframeDefer();
    }
}
