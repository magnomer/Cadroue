namespace Cadroue.UIDeportment;

public sealed class QFlowEdit
{
    private readonly QFlow qFlow;
    private int? qFlowSectionIndex;
    private bool qFlowSectionEditable = true;
    private bool qFlowSegmentFired;

    public QFlowEdit(QFlow lOwner)
    {
        qFlow = lOwner;
    }

    public event Action<bool>? QFlowEditChanged;

    public int? QFlowSectionIndex => qFlowSectionIndex;

    public bool QFlowSectionEditable => qFlowSectionEditable;

    public bool QFlowSegmentFired => qFlowSegmentFired;

    public bool QFlowEditCheck() =>
        qFlow.QFlowSectionActive && qFlowSectionEditable;

    public void QFlowSectionSelect(int? lSectionIndex) => qFlowSectionIndex = lSectionIndex;

    public bool QFlowEditSet(bool lSectionEditable)
    {
        if (!lSectionEditable)
        {
            qFlow.QFlowName.QFlowNameHide();
        }

        if (qFlowSectionEditable == lSectionEditable)
        {
            return false;
        }

        qFlowSectionEditable = lSectionEditable;
        QFlowEditChanged?.Invoke(lSectionEditable);
        return true;
    }

    public void QFlowFiredSet(bool lSegmentFired) => qFlowSegmentFired = lSegmentFired;
}
