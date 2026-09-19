using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LFlowSection
{
    private readonly LFlow lFlow;
    private readonly LSegment lSegment = new();

    public LFlowSection(LFlow lOwner)
    {
        lFlow = lOwner;
        lSegment.LSegmentNotice += LFlowSegmentHandle;
        lSegment.LSegmentFaultNotice += LFlowFaultHandle;
    }

    public event Action<IReadOnlyList<LPiece>, int?>? LFlowSectionChange;

    public LSegment LFlowSegment => lSegment;

    public IReadOnlyList<LPiece> LFlowSectionsRead() => lSegment.LSegmentListRead();

    public int? LFlowSelectionRead() => lSegment.LSegmentSelectionRead();

    public IReadOnlyList<int> LFlowSelectedRead() => lSegment.LSegmentSelectedRead();

    public IReadOnlyList<LSplitSectionDescription> LFlowSplitRead() =>
        lSegment.LSegmentListRead().Select(lSection => lSection.LPieceDescribe()).ToArray();

    public bool LFlowEmptyCheck() =>
        lSegment.LSegmentListRead().Count == 0 && lSegment.LSegmentSelectionRead() is null;

    public void LFlowSectionAttach()
    {
        lSegment.LSegmentSourceSet(lFlow.LFlowSourcePath);
        lSegment.LSegmentReset();
        lFlow.LFlowSectionSelect(lSegment.LSegmentSelectionRead());
    }

    public void LFlowSectionLoad()
    {
        lSegment.LSegmentLoad(lFlow.LFlowDuration);
        LFlowSectionRaise();
    }

    public void LFlowSectionReset()
    {
        lSegment.LSegmentSourceSet(null);
        lSegment.LSegmentReset();
    }

    public void LFlowSectionRaise() =>
        LFlowSectionChange?.Invoke(lSegment.LSegmentListRead(), lSegment.LSegmentSelectionRead());

    public void LFlowSectionAdd()
    {
        if (!lFlow.LFlowSectionEditable || !lFlow.LFlowSourceCheck())
        {
            return;
        }

        lFlow.LFlowFiredSet(false);
        lSegment.LSegmentAdd(lFlow.LFlowCursor, lFlow.LFlowDuration, LFlowColorRead(), LFlowOverlapRead());
        if (lFlow.LFlowSegmentFired)
        {
            LFlowSectionRecord("added", lSegment.LSegmentSelectionRead()!.Value);
        }
    }

    public void LFlowStartSet()
    {
        if (!lFlow.LFlowSectionEditable || !lFlow.LFlowSourceCheck())
        {
            return;
        }

        if (lSegment.LSegmentStartSet(lFlow.LFlowCursor, lFlow.LFlowDuration, LFlowColorRead(), LFlowOverlapRead())
            is not bool lAdded)
        {
            return;
        }

        LFlowSectionRecord(lAdded ? "added" : "start set", lSegment.LSegmentSelectionRead()!.Value);
    }

    public void LFlowEndSet()
    {
        if (!lFlow.LFlowSectionEditable || !lFlow.LFlowSourceCheck())
        {
            return;
        }

        if (lSegment.LSegmentEndSet(lFlow.LFlowCursor, LFlowColorRead(), LFlowOverlapRead()) is not bool lAdded)
        {
            return;
        }

        LFlowSectionRecord(lAdded ? "added" : "end set", lSegment.LSegmentSelectionRead()!.Value);
    }

    public void LFlowSectionDivide()
    {
        if (!lFlow.LFlowSectionEditable)
        {
            return;
        }

        lFlow.LFlowFiredSet(false);
        lSegment.LSegmentDivide(lFlow.LFlowCursor, LFlowColorRead());
        if (!lFlow.LFlowSegmentFired)
        {
            return;
        }

        int lFirst = lSegment.LSegmentSelectionRead()!.Value;
        LFlowSectionRecord($"split at {lFlow.LFlowCursor:hh\\:mm\\:ss\\.fff}, left half", lFirst);
        LFlowSectionRecord("split, right half", lFirst + 1);
    }

    public void LFlowSectionDelete()
    {
        if (!lFlow.LFlowSectionEditable)
        {
            return;
        }

        IReadOnlyList<int> lSelected = lSegment.LSegmentSelectedRead();
        int lApproved = lSegment.LSegmentVersionRead();
        if (lSelected.Count == 0)
        {
            return;
        }

        LAskNotice.LAskPublish(
            LFlowAskCreate("Flow.Section.DeleteConfirm", "Terms.Delete"),
            lAnswer => LFlowDeleteRun(lAnswer, lSelected, lApproved));
    }

    private void LFlowDeleteRun(bool lAnswer, IReadOnlyList<int> lSelected, int lApproved)
    {
        if (!lAnswer)
        {
            return;
        }

        if (lApproved != lSegment.LSegmentVersionRead())
        {
            LTraceLog.LTraceWarningRecord("Section delete skipped: sections changed while confirming");
            return;
        }

        foreach (int lIndex in lSelected)
        {
            LFlowSectionRecord("deleted", lIndex);
        }

        lSegment.LSegmentDelete(lSelected, lApproved);
    }

    public void LFlowSectionClear()
    {
        if (!lFlow.LFlowSectionEditable)
        {
            return;
        }

        int lCount = lSegment.LSegmentListRead().Count;
        int lApproved = lSegment.LSegmentVersionRead();
        if (lCount == 0)
        {
            return;
        }

        LAskNotice.LAskPublish(
            LFlowAskCreate("Flow.Section.ClearConfirm", "Terms.Remove"),
            lAnswer => LFlowClearRun(lAnswer, lCount, lApproved));
    }

    private void LFlowClearRun(bool lAnswer, int lCount, int lApproved)
    {
        if (!lAnswer)
        {
            return;
        }

        if (!lSegment.LSegmentClear(lApproved))
        {
            LTraceLog.LTraceWarningRecord("Section clear skipped: sections changed while confirming");
            return;
        }

        LTraceLog.LTraceInfoRecord($"Sections cleared: {lCount} section(s) removed");
    }

    public void LFlowSectionSelect(int lIndex) => lSegment.LSegmentSelect(lIndex);

    public void LFlowSelectToggle(int lIndex) => lSegment.LSegmentSelectToggle(lIndex);

    public void LFlowRangeSelect(int lIndex) => lSegment.LSegmentRangeSelect(lIndex);

    public void LFlowSectionSeek(int lIndex, bool lEnd)
    {
        IReadOnlyList<LPiece> lSections = lSegment.LSegmentListRead();
        if (!lFlow.LFlowCommandActive || lFlow.LFlowSpool is null || lIndex < 0 || lIndex >= lSections.Count)
        {
            return;
        }

        lSegment.LSegmentSelect(lIndex);
        LPiece lTarget = lSections[lIndex];
        lFlow.LFlowCursorPropagate(lEnd ? lTarget.LPieceEnd : lTarget.LPieceOrigin, true, true);
    }

    public void LFlowSectionToggle(int lIndex)
    {
        if (!lFlow.LFlowSectionEditable)
        {
            return;
        }

        lFlow.LFlowFiredSet(false);
        lSegment.LSegmentToggle(lIndex);
        if (!lFlow.LFlowSegmentFired)
        {
            return;
        }

        bool lHidden = lSegment.LSegmentListRead()[lIndex].LPieceHidden;
        LFlowSectionRecord(lHidden ? "turned off" : "turned on", lIndex);
    }

    public bool LFlowSectionMove(int lSource, int lTarget)
    {
        if (!lFlow.LFlowSectionEditable)
        {
            return false;
        }

        if (lSegment.LSegmentMove(lSource, lTarget) is not int lInsert)
        {
            return false;
        }

        LFlowSectionRecord($"moved from #{lSource + 1} to", lInsert);
        return true;
    }

    public bool LFlowSectionSort()
    {
        if (!lFlow.LFlowSectionEditable)
        {
            return false;
        }

        lFlow.LFlowFiredSet(false);
        lSegment.LSegmentSort();
        if (!lFlow.LFlowSegmentFired)
        {
            return false;
        }

        LTraceLog.LTraceInfoRecord($"Sections sorted by name: {lSegment.LSegmentListRead().Count} section(s)");
        return true;
    }

    public void LFlowNameSet(int lIndex, string lName, string? lPrefix, string? lSuffix)
    {
        if (!lFlow.LFlowSectionEditable)
        {
            return;
        }

        IReadOnlyList<LPiece> lSections = lSegment.LSegmentListRead();
        if (lIndex < 0 || lIndex >= lSections.Count)
        {
            return;
        }

        string lWas = lSections[lIndex].LPieceName;
        lFlow.LFlowFiredSet(false);
        lSegment.LSegmentNameSet(lIndex, lName, lPrefix, lSuffix);
        if (lFlow.LFlowSegmentFired)
        {
            LFlowSectionRecord(string.IsNullOrEmpty(lWas) ? "named" : $"renamed from '{lWas}' to", lIndex);
        }
    }

    public bool LFlowCombineApply(
        IReadOnlyList<LSweepSpan> lExcluded,
        IReadOnlyList<LSweepSpan> lKept,
        IReadOnlyList<LSweepBoundary> lBoundaries)
    {
        if (lFlow.LFlowUnloaded || lFlow.LFlowSpool is not { } lSpool)
        {
            return false;
        }

        IReadOnlyList<LPiece> lSections = LSweep.LSweepCombineResolve(
            lSegment.LSegmentListRead(), lExcluded, lKept, lBoundaries, lSpool.LSpoolDuration, lFlow.LFlowPaletteCount);
        int? lSelect = lSections.Count > 0 ? 0 : null;
        return lSegment.LSegmentBoundSet(lSections, lSelect, lSpool.LSpoolDuration);
    }

    private void LFlowSegmentHandle(IReadOnlyList<LPiece> lSections, int? lActive)
    {
        lFlow.LFlowFiredSet(true);
        lFlow.LFlowSectionSelect(lActive);
        LFlowSectionChange?.Invoke(lSections, lActive);
    }

    private void LFlowFaultHandle(LSegmentFault lFault, int lCount)
    {
        string lSource = lFlow.LFlowSourceFormat();
        LTraceLog.LTraceWarningRecord(lFault switch
        {
            LSegmentFault.LSegmentFaultCeiling =>
                $"Section change refused in '{lSource}': {lCount} section(s) exceed the {LPiece.LPieceCeiling} limit",
            LSegmentFault.LSegmentFaultInvalid =>
                $"Section restore in '{lSource}' dropped {lCount} invalid saved section(s)",
            _ => $"Section save failed in '{lSource}': {lCount} section(s) kept in memory only"
        });
    }

    private void LFlowSectionRecord(string lAction, int lIndex)
    {
        IReadOnlyList<LPiece> lSections = lSegment.LSegmentListRead();
        string lSource = lFlow.LFlowSourceFormat();
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

    private int LFlowColorRead() => lSegment.LSegmentListRead().Count % lFlow.LFlowPaletteCount;

    private static bool LFlowOverlapRead() => LPreference.LPreferenceStateCurrent.LPreferenceOverlapAllowed;

    private static LAsk? LFlowAskCreate(string lQuestionKey, string lActionKey) =>
        LPreference.LPreferenceStateCurrent.LPreferenceConfirmDestructive
            ? new LAsk(
                LLocalization.LLocalizationTextRead(lQuestionKey),
                LLocalization.LLocalizationTextRead(lActionKey))
            {
                LAskTitle = LLocalization.LLocalizationTextRead("Flow.Confirm.Title")
            }
            : null;
}
