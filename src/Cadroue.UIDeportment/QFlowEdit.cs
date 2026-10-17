namespace Cadroue.UIDeportment;

public sealed class QFlowEdit
{
    private readonly QFlow qFlow;
    private int? qFlowSectionIndex;
    private bool qFlowSegmentFired;

    public QFlowEdit(QFlow lOwner)
    {
        qFlow = lOwner;
    }

    public event Action<bool>? QFlowEditChanged;

    public int? QFlowSectionIndex => qFlowSectionIndex;

    // The switch lives on the sections gate, its single home.
    public bool QFlowSectionEditable => qFlow.QFlowSection.QFlowSectionGate.CSectionEditable;

    public bool QFlowSegmentFired => qFlowSegmentFired;

    public bool QFlowEditCheck() =>
        qFlow.QFlowSectionActive && QFlowSectionEditable;

    public void QFlowSectionSelect(int? lSectionIndex) => qFlowSectionIndex = lSectionIndex;

    // The gate turns the switch, hides the name editor when it turns off and tells each turn.
    public bool QFlowEditSet(bool lSectionEditable) =>
        qFlow.QFlowSection.QFlowSectionGate.CSectionEditableSet(lSectionEditable);

    // The gate's turn, relayed to the panels.
    public void QFlowEditRaise(bool lSectionEditable) => QFlowEditChanged?.Invoke(lSectionEditable);

    public void QFlowFiredSet(bool lSegmentFired) => qFlowSegmentFired = lSegmentFired;
}
