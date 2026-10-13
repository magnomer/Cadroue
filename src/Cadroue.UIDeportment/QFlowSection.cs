using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QFlowSection
{
    private readonly QFlow qFlow;
    private readonly LSegment qSegment = new();

    public QFlowSection(QFlow lOwner)
    {
        qFlow = lOwner;
        qSegment.LSegmentNotice += QFlowSegmentHandle;
        qSegment.LSegmentFaultNotice += QFlowFaultHandle;
    }

    public event Action<IReadOnlyList<LPiece>, int?>? QFlowSectionChanged;

    public LSegment QFlowSegment => qSegment;

    public IReadOnlyList<LPiece> QFlowSectionsRead() => qSegment.LSegmentListRead();

    public int? QFlowSelectionRead() => qSegment.LSegmentSelectionRead();

    public IReadOnlyList<int> QFlowSelectedRead() => qSegment.LSegmentSelectedRead();

    public IReadOnlyList<LSplitSectionDescription> QFlowSplitRead() =>
        qSegment.LSegmentListRead().Select(lSection => lSection.LPieceDescribe()).ToArray();

    public bool QFlowEmptyCheck() =>
        qSegment.LSegmentListRead().Count == 0 && qSegment.LSegmentSelectionRead() is null;

    public void QFlowSectionAttach()
    {
        qSegment.LSegmentSourceSet(qFlow.QFlowSourcePath);
        qSegment.LSegmentReset();
        qFlow.QFlowEdit.QFlowSectionSelect(qSegment.LSegmentSelectionRead());
    }

    public void QFlowSectionLoad()
    {
        qSegment.LSegmentLoad(qFlow.QFlowDuration);
        QFlowSectionRaise();
    }

    public void QFlowSectionReset()
    {
        qSegment.LSegmentSourceSet(null);
        qSegment.LSegmentReset();
    }

    public void QFlowSectionRaise() =>
        QFlowSectionChanged?.Invoke(qSegment.LSegmentListRead(), qSegment.LSegmentSelectionRead());

    public void QFlowSectionAdd()
    {
        if (!qFlow.QFlowEdit.QFlowSectionEditable || !qFlow.QFlowSourceCheck())
        {
            return;
        }

        qFlow.QFlowEdit.QFlowFiredSet(false);
        qSegment.LSegmentAdd(qFlow.QFlowCursor, qFlow.QFlowDuration, QFlowColorRead(), QFlowOverlapRead());
        if (qFlow.QFlowEdit.QFlowSegmentFired)
        {
            QFlowSectionRecord("added", qSegment.LSegmentSelectionRead()!.Value);
        }
    }

    public void QFlowStartSet()
    {
        if (!qFlow.QFlowEdit.QFlowSectionEditable || !qFlow.QFlowSourceCheck())
        {
            return;
        }

        if (qSegment.LSegmentStartSet(qFlow.QFlowCursor, qFlow.QFlowDuration, QFlowColorRead(), QFlowOverlapRead())
            is not bool lAdded)
        {
            return;
        }

        QFlowSectionRecord(lAdded ? "added" : "start set", qSegment.LSegmentSelectionRead()!.Value);
    }

    public void QFlowEndSet()
    {
        if (!qFlow.QFlowEdit.QFlowSectionEditable || !qFlow.QFlowSourceCheck())
        {
            return;
        }

        if (qSegment.LSegmentEndSet(qFlow.QFlowCursor, QFlowColorRead(), QFlowOverlapRead()) is not bool lAdded)
        {
            return;
        }

        QFlowSectionRecord(lAdded ? "added" : "end set", qSegment.LSegmentSelectionRead()!.Value);
    }

    public void QFlowSectionDivide()
    {
        if (!qFlow.QFlowEdit.QFlowSectionEditable)
        {
            return;
        }

        qFlow.QFlowEdit.QFlowFiredSet(false);
        qSegment.LSegmentDivide(qFlow.QFlowCursor, QFlowColorRead());
        if (!qFlow.QFlowEdit.QFlowSegmentFired)
        {
            return;
        }

        int lFirst = qSegment.LSegmentSelectionRead()!.Value;
        QFlowSectionRecord($"split at {qFlow.QFlowCursor:hh\\:mm\\:ss\\.fff}, left half", lFirst);
        QFlowSectionRecord("split, right half", lFirst + 1);
    }

    public void QFlowSectionDelete()
    {
        if (!qFlow.QFlowEdit.QFlowSectionEditable)
        {
            return;
        }

        IReadOnlyList<int> lSelected = qSegment.LSegmentSelectedRead();
        int lApproved = qSegment.LSegmentVersionRead();
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

        if (lApproved != qSegment.LSegmentVersionRead())
        {
            LTraceLog.LTraceWarningRecord("Section delete skipped: sections changed while confirming");
            return;
        }

        foreach (int lIndex in lSelected)
        {
            QFlowSectionRecord("deleted", lIndex);
        }

        qSegment.LSegmentDelete(lSelected, lApproved);
    }

    public void QFlowSectionClear()
    {
        if (!qFlow.QFlowEdit.QFlowSectionEditable)
        {
            return;
        }

        int lCount = qSegment.LSegmentListRead().Count;
        int lApproved = qSegment.LSegmentVersionRead();
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

        if (!qSegment.LSegmentClear(lApproved))
        {
            LTraceLog.LTraceWarningRecord("Section clear skipped: sections changed while confirming");
            return;
        }

        LTraceLog.LTraceInfoRecord($"Sections cleared: {lCount} section(s) removed");
    }

    public void QFlowSectionSelect(int lIndex) => qSegment.LSegmentSelect(lIndex);

    public void QFlowSelectToggle(int lIndex) => qSegment.LSegmentSelectToggle(lIndex);

    public void QFlowRangeSelect(int lIndex) => qSegment.LSegmentRangeSelect(lIndex);

    public void QFlowSectionSeek(int lIndex, bool lEnd)
    {
        IReadOnlyList<LPiece> lSections = qSegment.LSegmentListRead();
        if (!qFlow.QFlowCommandActive || qFlow.QFlowRange is null || lIndex < 0 || lIndex >= lSections.Count)
        {
            return;
        }

        qSegment.LSegmentSelect(lIndex);
        LPiece lTarget = lSections[lIndex];
        qFlow.QFlowCursorPropagate(lEnd ? lTarget.LPieceEnd : lTarget.LPieceOrigin, true, true);
    }

    public void QFlowSectionToggle(int lIndex)
    {
        if (!qFlow.QFlowEdit.QFlowSectionEditable)
        {
            return;
        }

        qFlow.QFlowEdit.QFlowFiredSet(false);
        qSegment.LSegmentToggle(lIndex);
        if (!qFlow.QFlowEdit.QFlowSegmentFired)
        {
            return;
        }

        bool lHidden = qSegment.LSegmentListRead()[lIndex].LPieceHidden;
        QFlowSectionRecord(lHidden ? "turned off" : "turned on", lIndex);
    }

    public bool QFlowSectionMove(int lSource, int lTarget)
    {
        if (!qFlow.QFlowEdit.QFlowSectionEditable)
        {
            return false;
        }

        if (qSegment.LSegmentMove(lSource, lTarget) is not int lInsert)
        {
            return false;
        }

        QFlowSectionRecord($"moved from #{lSource + 1} to", lInsert);
        return true;
    }

    public bool QFlowSectionSort()
    {
        if (!qFlow.QFlowEdit.QFlowSectionEditable)
        {
            return false;
        }

        qFlow.QFlowEdit.QFlowFiredSet(false);
        qSegment.LSegmentSort();
        if (!qFlow.QFlowEdit.QFlowSegmentFired)
        {
            return false;
        }

        LTraceLog.LTraceInfoRecord($"Sections sorted by name: {qSegment.LSegmentListRead().Count} section(s)");
        return true;
    }

    public void QFlowNameSet(int lIndex, string lName, string? lPrefix, string? lSuffix)
    {
        if (!qFlow.QFlowEdit.QFlowSectionEditable)
        {
            return;
        }

        IReadOnlyList<LPiece> lSections = qSegment.LSegmentListRead();
        if (lIndex < 0 || lIndex >= lSections.Count)
        {
            return;
        }

        string lWas = lSections[lIndex].LPieceName;
        qFlow.QFlowEdit.QFlowFiredSet(false);
        qSegment.LSegmentNameSet(lIndex, lName, lPrefix, lSuffix);
        if (qFlow.QFlowEdit.QFlowSegmentFired)
        {
            QFlowSectionRecord(string.IsNullOrEmpty(lWas) ? "named" : $"renamed from '{lWas}' to", lIndex);
        }
    }

    public bool QFlowCombineApply(
        IReadOnlyList<LSweepSpan> lExcluded,
        IReadOnlyList<LSweepSpan> lKept,
        IReadOnlyList<LSweepBoundary> lBoundaries)
    {
        if (qFlow.QFlowUnloaded || qFlow.QFlowRange is not { } lSpool)
        {
            return false;
        }

        IReadOnlyList<LPiece> lSections = LSweep.LSweepCombineResolve(
            qSegment.LSegmentListRead(), lExcluded, lKept, lBoundaries, lSpool.LSpoolDuration, qFlow.QFlowPaletteCount);
        int? lSelect = lSections.Count > 0 ? 0 : null;
        return qSegment.LSegmentBoundSet(lSections, lSelect, lSpool.LSpoolDuration);
    }

    private void QFlowSegmentHandle(IReadOnlyList<LPiece> lSections, int? lActive)
    {
        qFlow.QFlowEdit.QFlowFiredSet(true);
        qFlow.QFlowEdit.QFlowSectionSelect(lActive);
        QFlowSectionChanged?.Invoke(lSections, lActive);
    }

    private void QFlowFaultHandle(LSegmentFault lFault, int lCount)
    {
        string lSource = qFlow.QFlowSourceFormat();
        LTraceLog.LTraceWarningRecord(lFault switch
        {
            LSegmentFault.LSegmentFaultCeiling =>
                $"Section change refused in '{lSource}': {lCount} section(s) exceed the {LPiece.LPieceCeiling} limit",
            LSegmentFault.LSegmentFaultInvalid =>
                $"Section restore in '{lSource}' dropped {lCount} invalid saved section(s)",
            _ => $"Section save failed in '{lSource}': {lCount} section(s) kept in memory only"
        });
    }

    private void QFlowSectionRecord(string lAction, int lIndex)
    {
        IReadOnlyList<LPiece> lSections = qSegment.LSegmentListRead();
        string lSource = qFlow.QFlowSourceFormat();
        if (lIndex < 0 || lIndex >= lSections.Count)
        {
            LTraceLog.LTraceInfoRecord($"Section {lAction} in '{lSource}': {lSections.Count} section(s) remain");
            return;
        }

        LPiece lSection = lSections[lIndex];
        string lName = string.IsNullOrEmpty(lSection.LPieceName) ? "unnamed" : $"'{lSection.LPieceName}'";
        LTraceLog.LTraceInfoRecord(
            $"Section {lAction} #{lIndex + 1} of {lSections.Count} in '{lSource}': {lName} "
            + $"{lSection.LPieceOrigin:hh\\:mm\\:ss\\.fff}-{lSection.LPieceEnd:hh\\:mm\\:ss\\.fff}");
    }

    private int QFlowColorRead() => qSegment.LSegmentListRead().Count % qFlow.QFlowPaletteCount;

    private static bool QFlowOverlapRead() => LPreference.LPreferenceStateCurrent.LPreferenceOverlapAllowed;
}
