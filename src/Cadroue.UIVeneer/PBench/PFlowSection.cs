using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIVeneer.PBench;

public sealed partial class PFlow
{
    public void PFlowEditSet(bool pFlowSectionEdit)
    {
        if (!pFlowSectionEdit) PFlowNameClose();
        LFlow.LFlowEditSet(pFlowSectionEdit);
    }

    private void PFlowSegmentHandle(IReadOnlyList<LPiece> pFlowSections, int? pFlowActive)
    {
        LFlow.LFlowFiredSet(true);
        LFlow.LFlowSectionSelect(pFlowActive);
        pViewfinder.PViewfinderSectionsUpdate(pFlowSections);
        pMap.PMapSectionsUpdate(pFlowSections);
        PFlowSectionChange?.Invoke(pFlowSections, pFlowActive);
    }

    private void PFlowSectionAdd()
    {
        if (!LFlow.LFlowSectionEditable || !LFlow.LFlowSourceCheck()) return;
        LFlow.LFlowFiredSet(false);
        lSegment.LSegmentAdd(LFlow.LFlowCursor, LFlow.LFlowDuration, PFlowColorRead(), PFlowOverlapAllowed);
        if (LFlow.LFlowSegmentFired) PFlowSectionRecord("added", lSegment.LSegmentSelectionRead()!.Value);
    }

    private void PFlowStartSet()
    {
        if (!LFlow.LFlowSectionEditable || !LFlow.LFlowSourceCheck()) return;
        if (lSegment.LSegmentStartSet(LFlow.LFlowCursor, LFlow.LFlowDuration, PFlowColorRead(), PFlowOverlapAllowed)
            is not bool pFlowAdded) return;
        PFlowSectionRecord(pFlowAdded ? "added" : "start set", lSegment.LSegmentSelectionRead()!.Value);
    }

    private void PFlowSectionDivide()
    {
        if (!LFlow.LFlowSectionEditable) return;
        LFlow.LFlowFiredSet(false);
        lSegment.LSegmentDivide(LFlow.LFlowCursor, PFlowColorRead());
        if (!LFlow.LFlowSegmentFired) return;
        int pFlowFirst = lSegment.LSegmentSelectionRead()!.Value;
        PFlowSectionRecord($"split at {LFlow.LFlowCursor:hh\\:mm\\:ss\\.fff}, left half", pFlowFirst);
        PFlowSectionRecord("split, right half", pFlowFirst + 1);
    }

    private void PFlowEndSet()
    {
        if (!LFlow.LFlowSectionEditable || !LFlow.LFlowSourceCheck()) return;
        if (lSegment.LSegmentEndSet(LFlow.LFlowCursor, PFlowColorRead(), PFlowOverlapAllowed)
            is not bool pFlowAdded) return;
        PFlowSectionRecord(pFlowAdded ? "added" : "end set", lSegment.LSegmentSelectionRead()!.Value);
    }

    public void PFlowSectionDelete()
    {
        if (!LFlow.LFlowSectionEditable) return;
        IReadOnlyList<int> pFlowSelected = lSegment.LSegmentSelectedRead();
        int pFlowApproved = lSegment.LSegmentVersionRead();
        if (pFlowSelected.Count == 0) return;
        if (!PFlowDestructiveConfirm(
            LLocalization.LLocalizationTextRead("Flow.Section.DeleteConfirm"),
            LLocalization.LLocalizationTextRead("Terms.Delete"))) return;
        if (pFlowApproved != lSegment.LSegmentVersionRead())
        {
            LTraceLog.LTraceWarningRecord("Section delete skipped: sections changed while confirming");
            return;
        }

        foreach (int pFlowIndex in pFlowSelected)
        {
            PFlowSectionRecord("deleted", pFlowIndex);
        }

        lSegment.LSegmentDelete(pFlowSelected, pFlowApproved);
    }

    public void PFlowSectionClear()
    {
        if (!LFlow.LFlowSectionEditable) return;
        int pFlowCount = lSegment.LSegmentListRead().Count;
        int pFlowApproved = lSegment.LSegmentVersionRead();
        if (pFlowCount == 0) return;
        if (!PFlowDestructiveConfirm(
            LLocalization.LLocalizationTextRead("Flow.Section.ClearConfirm"),
            LLocalization.LLocalizationTextRead("Terms.Remove"))) return;
        if (!lSegment.LSegmentClear(pFlowApproved))
        {
            LTraceLog.LTraceWarningRecord("Section clear skipped: sections changed while confirming");
            return;
        }

        LTraceLog.LTraceInfoRecord($"Sections cleared: {pFlowCount} section(s) removed");
    }

    private string PFlowSourceFormat() =>
        string.IsNullOrWhiteSpace(LFlow.LFlowSourcePath)
            ? "(no media)"
            : System.IO.Path.GetFileName(LFlow.LFlowSourcePath);

    private void PFlowFaultHandle(LSegmentFault pFlowFault, int pFlowCount)
    {
        string pFlowSource = PFlowSourceFormat();
        LTraceLog.LTraceWarningRecord(pFlowFault switch
        {
            LSegmentFault.LSegmentFaultCeiling =>
                $"Section change refused in '{pFlowSource}': " +
                $"{pFlowCount} section(s) exceed the {LPiece.LPieceCeiling} limit",
            LSegmentFault.LSegmentFaultInvalid =>
                $"Section restore in '{pFlowSource}' dropped {pFlowCount} invalid saved section(s)",
            _ => $"Section save failed in '{pFlowSource}': {pFlowCount} section(s) kept in memory only"
        });
    }

    private void PFlowSectionRecord(string pFlowAction, int pFlowIndex)
    {
        IReadOnlyList<LPiece> pFlowSections = lSegment.LSegmentListRead();
        string pFlowSource = PFlowSourceFormat();

        if (pFlowIndex < 0 || pFlowIndex >= pFlowSections.Count)
        {
            LTraceLog.LTraceInfoRecord(
                $"Section {pFlowAction} in '{pFlowSource}': {pFlowSections.Count} section(s) remain");
            return;
        }

        LPiece pFlowSection = pFlowSections[pFlowIndex];
        string pFlowName = string.IsNullOrEmpty(pFlowSection.LPieceName)
            ? "unnamed"
            : $"'{pFlowSection.LPieceName}'";
        LTraceLog.LTraceInfoRecord(
            $"Section {pFlowAction} #{pFlowIndex + 1} of {pFlowSections.Count} in '{pFlowSource}': {pFlowName} " +
            $"{pFlowSection.LPieceOrigin:hh\\:mm\\:ss\\.fff}-{pFlowSection.LPieceEnd:hh\\:mm\\:ss\\.fff}");
    }

    public void PFlowSectionSelect(int pSectionIndex)
    {
        PFlowViewfinderSelect(pSectionIndex);
    }

    public void PFlowSelectToggle(int pSectionIndex)
    {
        lSegment.LSegmentSelectToggle(pSectionIndex);
    }

    public void PFlowRangeSelect(int pSectionIndex)
    {
        lSegment.LSegmentRangeSelect(pSectionIndex);
    }

    public IReadOnlyList<int> PFlowSelectedRead() => lSegment.LSegmentSelectedRead();

    public void PFlowSectionSeek(int pSectionIndex) => PFlowSectionSeek(pSectionIndex, false);

    public void PFlowSectionSeek(int pSectionIndex, bool pSectionEnd)
    {
        IReadOnlyList<LPiece> pFlowSections = lSegment.LSegmentListRead();
        if (!LFlow.LFlowCommandActive
            || LFlow.LFlowSpool is null
            || pSectionIndex < 0
            || pSectionIndex >= pFlowSections.Count)
        {
            return;
        }

        lSegment.LSegmentSelect(pSectionIndex);
        LPiece pSectionTarget = pFlowSections[pSectionIndex];
        PFlowCursorPropagate(pSectionEnd ? pSectionTarget.LPieceEnd : pSectionTarget.LPieceOrigin, true, true);
    }

    public void PFlowSectionToggle(int pSectionIndex)
    {
        if (!LFlow.LFlowSectionEditable) return;
        LFlow.LFlowFiredSet(false);
        lSegment.LSegmentToggle(pSectionIndex);
        if (!LFlow.LFlowSegmentFired) return;
        bool pFlowHidden = lSegment.LSegmentListRead()[pSectionIndex].LPieceHidden;
        PFlowSectionRecord(pFlowHidden ? "turned off" : "turned on", pSectionIndex);
    }

    public IReadOnlyList<LPiece> PFlowSectionsRead() => lSegment.LSegmentListRead();

    public IReadOnlyList<LSplitSectionDescription> PFlowSplitRead() =>
        lSegment.LSegmentListRead().Select(lSection => lSection.LPieceDescribe()).ToArray();

    public int? PFlowSelectionRead() => lSegment.LSegmentSelectionRead();

    public LSegment PFlowSegment => lSegment;

    public void PFlowSectionsSet(IReadOnlyList<LPiece> lSections, int? lSectionSelect)
    {
        if (!LFlow.LFlowSectionEditable || LFlow.LFlowSpool is null)
        {
            return;
        }

        lSegment.LSegmentBoundSet(lSections, lSectionSelect, LFlow.LFlowDuration);
    }

    public bool PFlowSectionMove(int pSectionSource, int pSectionTarget)
    {
        if (!LFlow.LFlowSectionEditable) return false;
        if (lSegment.LSegmentMove(pSectionSource, pSectionTarget) is not int pFlowInsert) return false;
        PFlowSectionRecord($"moved from #{pSectionSource + 1} to", pFlowInsert);
        return true;
    }

    public bool PFlowSectionSort()
    {
        if (!LFlow.LFlowSectionEditable) return false;
        LFlow.LFlowFiredSet(false);
        lSegment.LSegmentSort();
        if (!LFlow.LFlowSegmentFired) return false;
        LTraceLog.LTraceInfoRecord($"Sections sorted by name: {lSegment.LSegmentListRead().Count} section(s)");
        return true;
    }

    public void PFlowNameSet(int pSectionIndex, string pSectionName)
        => PFlowNameSet(pSectionIndex, pSectionName, null, null);

    public void PFlowNameSet(int pSectionIndex, string pSectionName, string? pSectionPrefix, string? pSectionSuffix)
    {
        if (!LFlow.LFlowSectionEditable) return;
        IReadOnlyList<LPiece> pFlowSections = lSegment.LSegmentListRead();
        if (pSectionIndex < 0 || pSectionIndex >= pFlowSections.Count) return;

        string pSectionWas = pFlowSections[pSectionIndex].LPieceName;
        LFlow.LFlowFiredSet(false);
        lSegment.LSegmentNameSet(pSectionIndex, pSectionName, pSectionPrefix, pSectionSuffix);
        if (LFlow.LFlowSegmentFired)
        {
            PFlowSectionRecord(
                string.IsNullOrEmpty(pSectionWas) ? "named" : $"renamed from '{pSectionWas}' to",
                pSectionIndex);
        }
    }

    private int PFlowColorRead() => lSegment.LSegmentListRead().Count % PSectionPalette.PSectionActiveCount;

    private void PFlowViewfinderSelect(int sectionIndex)
    {
        lSegment.LSegmentSelect(sectionIndex);
    }
}
