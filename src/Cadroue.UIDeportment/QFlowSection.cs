using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

// The sections' wiring: hands each section input to the section gate with the editable switch and relays its change.
// The store and its records pass through here only for the readers not yet moved to a gate.
public sealed class QFlowSection
{
    private readonly QFlow qFlow;
    private readonly CSection qFlowSectionGate;

    public QFlowSection(QFlow lOwner, CSection lGate)
    {
        qFlow = lOwner;
        qFlowSectionGate = lGate;
        qFlowSectionGate.CSectionChanged += QFlowSectionHandle;
    }

    public event Action<IReadOnlyList<LPiece>, int?>? QFlowSectionChanged;

    public LSegment QFlowSegment => qFlowSectionGate.CSectionSegment;

    public IReadOnlyList<LPiece> QFlowSectionsRead() => qFlowSectionGate.CSectionSegment.LSegmentListRead();

    public int? QFlowSelectionRead() => qFlowSectionGate.CSectionSelection;

    public IReadOnlyList<int> QFlowSelectedRead() => qFlowSectionGate.CSectionSelected;

    public IReadOnlyList<LSplitSectionDescription> QFlowSplitRead() =>
        qFlowSectionGate.CSectionSegment.LSegmentListRead().Select(lSection => lSection.LPieceDescribe()).ToArray();

    public bool QFlowEmptyCheck() => qFlowSectionGate.CSectionEmpty;

    // The flow's attach has reset the sections; the edit follows their selection.
    public void QFlowSectionAttach() => qFlow.QFlowEdit.QFlowSectionSelect(qFlowSectionGate.CSectionSelection);

    public void QFlowSectionLoad()
    {
        qFlowSectionGate.CSectionLoad();
        QFlowSectionRaise();
    }

    public void QFlowSectionReset() => qFlowSectionGate.CSectionReset();

    public void QFlowSectionRaise() =>
        QFlowSectionChanged?.Invoke(
            qFlowSectionGate.CSectionSegment.LSegmentListRead(), qFlowSectionGate.CSectionSelection);

    public void QFlowSectionAdd() => qFlowSectionGate.CSectionAdd(qFlow.QFlowEdit.QFlowSectionEditable);

    public void QFlowStartSet() => qFlowSectionGate.CSectionStartSet(qFlow.QFlowEdit.QFlowSectionEditable);

    public void QFlowEndSet() => qFlowSectionGate.CSectionEndSet(qFlow.QFlowEdit.QFlowSectionEditable);

    public void QFlowSectionDivide() => qFlowSectionGate.CSectionDivide(qFlow.QFlowEdit.QFlowSectionEditable);

    // Asks through QAskNotice, so it waits for the ask port; the confirmed run is the gate's.
    public void QFlowSectionDelete()
    {
        if (!qFlow.QFlowEdit.QFlowSectionEditable)
        {
            return;
        }

        IReadOnlyList<int> lSelected = qFlowSectionGate.CSectionSelected;
        int lApproved = qFlowSectionGate.CSectionVersion;
        if (lSelected.Count == 0)
        {
            return;
        }

        QAskNotice.QAskPublish(
            QAskNotice.QAskCreate("Flow.Section.DeleteConfirm", "Terms.Delete", "Flow.Confirm.Title"),
            lAnswer => QFlowDeleteRun(lAnswer, lSelected, lApproved));
    }

    private void QFlowDeleteRun(bool lAnswer, IReadOnlyList<int> lSelected, int lApproved)
    {
        if (!lAnswer)
        {
            return;
        }

        qFlowSectionGate.CSectionDelete(lSelected, lApproved);
    }

    // Asks through QAskNotice, so it waits for the ask port; the confirmed run is the gate's.
    public void QFlowSectionClear()
    {
        if (!qFlow.QFlowEdit.QFlowSectionEditable)
        {
            return;
        }

        int lCount = qFlowSectionGate.CSectionCount;
        int lApproved = qFlowSectionGate.CSectionVersion;
        if (lCount == 0)
        {
            return;
        }

        QAskNotice.QAskPublish(
            QAskNotice.QAskCreate("Flow.Section.ClearConfirm", "Terms.Remove", "Flow.Confirm.Title"),
            lAnswer => QFlowClearRun(lAnswer, lCount, lApproved));
    }

    private void QFlowClearRun(bool lAnswer, int lCount, int lApproved)
    {
        if (!lAnswer)
        {
            return;
        }

        qFlowSectionGate.CSectionClear(lCount, lApproved);
    }

    public void QFlowSectionSelect(int lIndex) => qFlowSectionGate.CSectionSelect(lIndex);

    public void QFlowSelectToggle(int lIndex) => qFlowSectionGate.CSectionSelectToggle(lIndex);

    public void QFlowRangeSelect(int lIndex) => qFlowSectionGate.CSectionRangeSelect(lIndex);

    public void QFlowSectionSeek(int lIndex, bool lEnd)
    {
        if (qFlowSectionGate.CSectionSeek(lIndex, lEnd) is not { } lTarget)
        {
            return;
        }

        qFlow.QFlowCursorPropagate(lTarget, true, true);
    }

    public void QFlowSectionToggle(int lIndex) =>
        qFlowSectionGate.CSectionToggle(qFlow.QFlowEdit.QFlowSectionEditable, lIndex);

    public bool QFlowSectionMove(int lSource, int lTarget) =>
        qFlowSectionGate.CSectionMove(qFlow.QFlowEdit.QFlowSectionEditable, lSource, lTarget);

    public bool QFlowSectionSort() => qFlowSectionGate.CSectionSort(qFlow.QFlowEdit.QFlowSectionEditable);

    public void QFlowNameSet(int lIndex, string lName, string? lPrefix, string? lSuffix) =>
        qFlowSectionGate.CSectionNameSet(qFlow.QFlowEdit.QFlowSectionEditable, lIndex, lName, lPrefix, lSuffix);

    public bool QFlowCombineApply(
        IReadOnlyList<LSweepSpan> lExcluded,
        IReadOnlyList<LSweepSpan> lKept,
        IReadOnlyList<LSweepBoundary> lBoundaries) =>
        qFlowSectionGate.CSectionCombineApply(lExcluded, lKept, lBoundaries);

    private void QFlowSectionHandle()
    {
        qFlow.QFlowEdit.QFlowSectionSelect(qFlowSectionGate.CSectionSelection);
        QFlowSectionRaise();
    }
}
