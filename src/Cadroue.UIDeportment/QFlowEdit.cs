namespace Cadroue.UIDeportment;

public sealed class QFlowEdit
{
    private readonly LFlow lFlow;
    private int? lFlowSectionIndex;
    private bool lFlowSectionEditable = true;
    private bool lFlowSegmentFired;

    public QFlowEdit(LFlow lOwner)
    {
        lFlow = lOwner;
    }

    public event Action<bool>? LFlowEditChanged;

    public int? LFlowSectionIndex => lFlowSectionIndex;

    public bool LFlowSectionEditable => lFlowSectionEditable;

    public bool LFlowSegmentFired => lFlowSegmentFired;

    public bool LFlowEditCheck() =>
        lFlow.LFlowSectionActive && lFlowSectionEditable;

    public void LFlowSectionSelect(int? lSectionIndex) => lFlowSectionIndex = lSectionIndex;

    public bool LFlowEditSet(bool lSectionEditable)
    {
        if (!lSectionEditable)
        {
            lFlow.LFlowName.LFlowNameHide();
        }

        if (lFlowSectionEditable == lSectionEditable)
        {
            return false;
        }

        lFlowSectionEditable = lSectionEditable;
        LFlowEditChanged?.Invoke(lSectionEditable);
        return true;
    }

    public void LFlowFiredSet(bool lSegmentFired) => lFlowSegmentFired = lSegmentFired;
}
