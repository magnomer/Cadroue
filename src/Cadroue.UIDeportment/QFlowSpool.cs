namespace Cadroue.UIDeportment;

public sealed class QFlowSpool
{
    private readonly QFlow qFlow;
    private TimeSpan qFlowDragTime;

    public QFlowSpool(QFlow lOwner)
    {
        qFlow = lOwner;
    }

    public event Action? QFlowSpoolApplied;

    public TimeSpan QFlowDragTime => qFlowDragTime;

    public string QFlowLabelOrigin => QCursor.QCursorTimeFormat(qFlow.QFlowRange?.LSpoolRangeOrigin ?? TimeSpan.Zero);

    public string QFlowLabelLimit => QCursor.QCursorTimeFormat(qFlow.QFlowRange?.LSpoolRangeLimit ?? TimeSpan.Zero);

    public string QFlowLabelZero => QCursor.QCursorTimeFormat(TimeSpan.Zero);

    public string QFlowLabelDuration => QCursor.QCursorTimeFormat(qFlow.QFlowDuration);

    public void QFlowDragSet(TimeSpan lDragTime) => qFlowDragTime = lDragTime;

    public void QFlowRangeSet(TimeSpan lOrigin, TimeSpan lLimit)
    {
        if (qFlow.QFlowRange is not { } lSpool)
        {
            return;
        }

        lSpool.LSpoolRangeSet(lOrigin, lLimit);
        QFlowSpoolHandle();
    }

    public void QFlowSpoolHandle()
    {
        qFlow.QFlowKeyframe.QFlowKeyframeSuspend(true);
        QFlowSpoolUpdate();
    }

    public void QFlowSpoolUpdate()
    {
        QFlowSpoolApplied?.Invoke();
        if (qFlow.QFlowRange is null)
        {
            return;
        }

        qFlow.QFlowKeyframe.QFlowKeyframeDefer();
    }
}
