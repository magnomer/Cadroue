using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.MigrationInterface;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    public static Func<string, IReadOnlyList<Cadroue.Core.LSidecarSectionRecord>>? PFlowSidecarSource { get; set; }

    private bool pFlowSectionEditable = true;

    public void PFlowEditSet(bool pFlowSectionEdit) =>
        pFlowSectionEditable = pFlowSectionEdit;

    private void PFlowSegmentHandle(IReadOnlyList<LPiece> pFlowSections, int? pFlowActive)
    {
        pFlowSegmentFired = true;
        pViewfinder.PViewfinderSectionsUpdate(pFlowSections, pFlowActive);
        pMap.PMapSectionsUpdate(pFlowSections, pFlowActive);
        PFlowSectionChange?.Invoke(pFlowSections, pFlowActive);
        PFlowSidecarSave();
    }

    private void PFlowSectionAdd()
    {
        if (!pFlowSectionEditable) return;
        if (lSpool is null || string.IsNullOrWhiteSpace(lSourcePath)) return;
        pFlowSegmentFired = false;
        lSegment.LSegmentAdd(lCursor, lSpool.LSpoolDuration, PFlowColorRead(), PFlowOverlapAllowed);
        if (pFlowSegmentFired) PFlowSectionRecord("added", lSegment.LSegmentSelectionRead()!.Value);
    }

    private void PFlowStartSet()
    {
        if (!pFlowSectionEditable) return;
        if (lSpool is null || string.IsNullOrWhiteSpace(lSourcePath)) return;
        int? pFlowActive = lSegment.LSegmentSelectionRead();
        IReadOnlyList<LPiece> pFlowSections = lSegment.LSegmentListRead();
        bool pFlowAdded = pFlowActive is null || pFlowSections[pFlowActive.Value].LPieceEnd < lCursor;
        pFlowSegmentFired = false;
        lSegment.LSegmentStartSet(lCursor, lSpool.LSpoolDuration, PFlowColorRead(), PFlowOverlapAllowed);
        if (pFlowSegmentFired) PFlowSectionRecord(pFlowAdded ? "added" : "start set", lSegment.LSegmentSelectionRead()!.Value);
    }

    private void PFlowSectionDivide()
    {
        if (!pFlowSectionEditable) return;
        pFlowSegmentFired = false;
        lSegment.LSegmentDivide(lCursor, PFlowColorRead());
        if (!pFlowSegmentFired) return;
        int pFlowFirst = lSegment.LSegmentSelectionRead()!.Value;
        PFlowSectionRecord($"split at {lCursor:hh\\:mm\\:ss\\.fff}, left half", pFlowFirst);
        PFlowSectionRecord("split, right half", pFlowFirst + 1);
    }

    private void PFlowEndSet()
    {
        if (!pFlowSectionEditable) return;
        if (lSpool is null || string.IsNullOrWhiteSpace(lSourcePath)) return;
        bool pFlowAdded = lSegment.LSegmentSelectionRead() is null;
        pFlowSegmentFired = false;
        lSegment.LSegmentEndSet(lCursor, PFlowColorRead(), PFlowOverlapAllowed);
        if (pFlowSegmentFired) PFlowSectionRecord(pFlowAdded ? "added" : "end set", lSegment.LSegmentSelectionRead()!.Value);
    }

    public void PFlowSectionDelete()
    {
        if (!pFlowSectionEditable) return;
        if (lSegment.LSegmentSelectionRead() is not int pFlowIndex) return;
        if (!PFlowDestructiveConfirm(LLocalization.LLocalizationTextRead("Flow.Section.DeleteConfirm"))) return;
        PFlowSectionRecord("deleted", pFlowIndex);
        lSegment.LSegmentDelete();
    }

    private void PFlowSectionRecord(string pFlowAction, int pFlowIndex)
    {
        IReadOnlyList<LPiece> pFlowSections = lSegment.LSegmentListRead();
        string pFlowSource = string.IsNullOrWhiteSpace(lSourcePath)
            ? "(no media)"
            : System.IO.Path.GetFileName(lSourcePath);

        if (pFlowIndex < 0 || pFlowIndex >= pFlowSections.Count)
        {
            LTraceLog.LTraceInfoRecord($"Section {pFlowAction} in '{pFlowSource}': {pFlowSections.Count} section(s) remain");
            return;
        }

        LPiece pFlowSection = pFlowSections[pFlowIndex];
        string pFlowName = string.IsNullOrEmpty(pFlowSection.LPieceName)
            ? "unnamed"
            : $"'{pFlowSection.LPieceName}'";
        LTraceLog.LTraceInfoRecord(
            $"Section {pFlowAction} #{pFlowIndex + 1} of {pFlowSections.Count} in '{pFlowSource}': {pFlowName} " +
            $"{pFlowSection.LPieceStart:hh\\:mm\\:ss\\.fff}-{pFlowSection.LPieceEnd:hh\\:mm\\:ss\\.fff}");
    }

    private void PFlowSidecarSave()
    {
        if (!pFlowSectionEditable || pFlowSidecarRestoring || lSourcePath is null)
        {
            return;
        }

        try
        {
            lKeyframeOrchestrator.LKeyframeSidecarSave();
            LTraceLog.LTraceInfoRecord(
                $"Sidecar written for '{System.IO.Path.GetFileName(lSourcePath)}': " +
                $"{lSegment.LSegmentListRead().Count} section(s)");
        }
        catch (Exception pFlowException)
        {
            LTraceLog.LTraceErrorRecord(
                $"Sidecar could not be written for '{System.IO.Path.GetFileName(lSourcePath)}'",
                pFlowException);
        }
    }

    public void PFlowSectionSelect(int pSectionIndex)
    {
        PFlowViewfinderSelect(pSectionIndex);
    }

    public void PFlowSectionSeek(int pSectionIndex)
    {
        IReadOnlyList<LPiece> pFlowSections = lSegment.LSegmentListRead();
        if (!pFlowCommandActive
            || lSpool is null
            || pSectionIndex < 0
            || pSectionIndex >= pFlowSections.Count)
        {
            return;
        }

        lSegment.LSegmentSelect(pSectionIndex);
        PFlowCursorPropagate(pFlowSections[pSectionIndex].LPieceStart, true, true);
    }

    public void PFlowSectionToggle(int pSectionIndex)
    {
        if (!pFlowSectionEditable) return;
        pFlowSegmentFired = false;
        lSegment.LSegmentToggle(pSectionIndex);
        if (!pFlowSegmentFired) return;
        bool pFlowHidden = lSegment.LSegmentListRead()[pSectionIndex].LPieceHidden;
        PFlowSectionRecord(pFlowHidden ? "turned off" : "turned on", pSectionIndex);
    }

    public IReadOnlyList<LPiece> PFlowSectionsRead() => lSegment.LSegmentListRead();

    public IReadOnlyList<LSplitSectionDescription> PFlowSplitRead() =>
        lSegment.LSegmentListRead()
            .Select(lSection => new LSplitSectionDescription(
                lSection.LPieceStart,
                lSection.LPieceEnd,
                lSection.LPieceName,
                lSection.LPiecePrefix,
                lSection.LPieceSuffix,
                lSection.LPieceHidden))
            .ToArray();

    internal IReadOnlyList<Cadroue.Core.LSidecarSectionRecord> PFlowSidecarRead() =>
        lSegment.LSegmentListRead()
            .Select(lSection => new Cadroue.Core.LSidecarSectionRecord
            {
                LSidecarStartMilliseconds = (long)lSection.LPieceStart.TotalMilliseconds,
                LSidecarEndMilliseconds = (long)lSection.LPieceEnd.TotalMilliseconds,
                LSidecarColorIndex = lSection.LPieceColorIndex,
                LSidecarName = lSection.LPieceName,
                LSidecarPrefix = lSection.LPiecePrefix,
                LSidecarSuffix = lSection.LPieceSuffix,
                LSidecarHidden = lSection.LPieceHidden
            })
            .ToArray();

    internal void PFlowSidecarApply(IReadOnlyList<Cadroue.Core.LSidecarSectionRecord> lSidecarSections)
    {
        if (lSpool is null || lSegment.LSegmentListRead().Count > 0 || lSidecarSections.Count == 0)
        {
            return;
        }

        LTraceLog.LTraceInfoRecord(
            $"Sidecar restored for '{System.IO.Path.GetFileName(lSourcePath)}': " +
            $"{lSidecarSections.Count} section(s)");
        pFlowSidecarRestoring = true;
        try
        {
            PFlowSectionsSet(
                lSidecarSections
                    .Select(lSection => new LPiece(
                        TimeSpan.FromMilliseconds(lSection.LSidecarStartMilliseconds),
                        TimeSpan.FromMilliseconds(lSection.LSidecarEndMilliseconds),
                        lSection.LSidecarColorIndex,
                        lSection.LSidecarName)
                    {
                        LPiecePrefix = lSection.LSidecarPrefix ?? string.Empty,
                        LPieceSuffix = lSection.LSidecarSuffix ?? string.Empty,
                        LPieceHidden = lSection.LSidecarHidden
                    })
                    .ToArray(),
                null);
        }
        finally
        {
            pFlowSidecarRestoring = false;
        }
    }

    public int? PFlowSelectionRead() => lSegment.LSegmentSelectionRead();

    public void PFlowSectionsSet(IReadOnlyList<LPiece> lSections, int? lSectionSelect)
    {
        if ((!pFlowSectionEditable && !pFlowSidecarRestoring) || lSpool is null)
        {
            return;
        }

        List<LPiece> pFlowValid = new();
        foreach (LPiece lSection in lSections)
        {
            if (lSection.LPieceEnd <= lSpool.LSpoolDuration && lSection.LPieceStart < lSection.LPieceEnd)
            {
                pFlowValid.Add(lSection);
            }
        }

        lSegment.LSegmentSet(pFlowValid, lSectionSelect);
    }

    public bool PFlowSectionMove(int pSectionSource, int pSectionTarget)
    {
        if (!pFlowSectionEditable) return false;
        int pFlowCount = lSegment.LSegmentListRead().Count;
        if (pSectionSource < 0 || pSectionSource >= pFlowCount)
        {
            return false;
        }

        int pFlowInsert = Math.Clamp(
            pSectionSource < pSectionTarget ? pSectionTarget - 1 : pSectionTarget,
            0,
            pFlowCount - 1);
        if (pFlowInsert == pSectionSource)
        {
            return false;
        }

        pFlowSegmentFired = false;
        lSegment.LSegmentMove(pSectionSource, pSectionTarget);
        if (!pFlowSegmentFired) return false;
        PFlowSectionRecord($"moved from #{pSectionSource + 1} to", pFlowInsert);
        return true;
    }

    public bool PFlowSectionSort()
    {
        if (!pFlowSectionEditable) return false;
        pFlowSegmentFired = false;
        lSegment.LSegmentSort();
        if (!pFlowSegmentFired) return false;
        LTraceLog.LTraceInfoRecord($"Sections sorted by name: {lSegment.LSegmentListRead().Count} section(s)");
        return true;
    }

    public void PFlowNameSet(int pSectionIndex, string pSectionName)
        => PFlowNameSet(pSectionIndex, pSectionName, null, null);

    public void PFlowNameSet(int pSectionIndex, string pSectionName, string? pSectionPrefix, string? pSectionSuffix)
    {
        if (!pFlowSectionEditable) return;
        IReadOnlyList<LPiece> pFlowSections = lSegment.LSegmentListRead();
        if (pSectionIndex < 0 || pSectionIndex >= pFlowSections.Count) return;

        string pSectionWas = pFlowSections[pSectionIndex].LPieceName;
        pFlowSegmentFired = false;
        lSegment.LSegmentNameSet(pSectionIndex, pSectionName, pSectionPrefix, pSectionSuffix);
        if (pFlowSegmentFired)
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
