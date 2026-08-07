using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PPanels;

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

    private void PFlowSectionApply(List<LPiece> pFlowSections, int? pFlowActive)
    {
        lSectionList.Clear();
        lSectionList.AddRange(pFlowSections);
        lSectionIndexActive = pFlowActive;
    }

    private void PFlowSectionAdd()
    {
        if (!pFlowSectionEditable) return;
        if (lSpool is null || string.IsNullOrWhiteSpace(lSourcePath)) return;
        if (LPiece.LPieceAdd(lSectionList, lCursor, lSpool.LSpoolDuration, PFlowColorRead(), PFlowOverlapAllowed)
            is not { } pFlowPlan) return;
        PFlowSectionApply(pFlowPlan.Sections, pFlowPlan.Active);
        PFlowSectionRecord("added", lSectionIndexActive!.Value);
        PFlowSectionUpdate();
    }

    private void PFlowStartSet()
    {
        if (!pFlowSectionEditable) return;
        if (lSpool is null || string.IsNullOrWhiteSpace(lSourcePath)) return;
        bool pFlowAdded = lSectionIndexActive is null
            || lSectionList[lSectionIndexActive.Value].LPieceEnd < lCursor;
        if (LPiece.LPieceStartSet(lSectionList, lSectionIndexActive, lCursor, lSpool.LSpoolDuration, PFlowColorRead(), PFlowOverlapAllowed)
            is not { } pFlowPlan) return;
        PFlowSectionApply(pFlowPlan.Sections, pFlowPlan.Active);
        PFlowSectionRecord(pFlowAdded ? "added" : "start set", lSectionIndexActive!.Value);
        PFlowSectionUpdate();
    }

    private void PFlowSectionDivide()
    {
        if (!pFlowSectionEditable) return;
        if (LPiece.LPieceDivide(lSectionList, lSectionIndexActive, lCursor, PFlowColorRead())
            is not { } pFlowPlan) return;
        PFlowSectionApply(pFlowPlan.Sections, pFlowPlan.First);
        PFlowSectionRecord($"split at {lCursor:hh\\:mm\\:ss\\.fff}, left half", pFlowPlan.First);
        PFlowSectionRecord("split, right half", pFlowPlan.Second);
        PFlowSectionUpdate();
    }

    private void PFlowEndSet()
    {
        if (!pFlowSectionEditable) return;
        if (lSpool is null || string.IsNullOrWhiteSpace(lSourcePath)) return;
        bool pFlowAdded = lSectionIndexActive is null;
        if (LPiece.LPieceEndSet(lSectionList, lSectionIndexActive, lCursor, PFlowColorRead(), PFlowOverlapAllowed)
            is not { } pFlowPlan) return;
        PFlowSectionApply(pFlowPlan.Sections, pFlowPlan.Active);
        PFlowSectionRecord(pFlowAdded ? "added" : "end set", lSectionIndexActive!.Value);
        PFlowSectionUpdate();
    }

    public void PFlowSectionDelete()
    {
        if (!pFlowSectionEditable) return;
        if (lSectionIndexActive is null) return;
        if (!PFlowDestructiveConfirm(LLocalization.LLocalizationTextRead("Flow.Section.DeleteConfirm"))) return;
        int index = lSectionIndexActive.Value;
        PFlowSectionRecord("deleted", index);
        lSectionList.RemoveAt(index);
        lSectionIndexActive = lSectionList.Count == 0 ? null : Math.Min(index, lSectionList.Count - 1);
        PFlowSectionUpdate();
    }

    private void PFlowSectionRecord(string pFlowAction, int pFlowIndex)
    {
        string pFlowSource = string.IsNullOrWhiteSpace(lSourcePath)
            ? "(no media)"
            : System.IO.Path.GetFileName(lSourcePath);

        if (pFlowIndex < 0 || pFlowIndex >= lSectionList.Count)
        {
            LTraceLog.LTraceInfoRecord($"Section {pFlowAction} in '{pFlowSource}': {lSectionList.Count} section(s) remain");
            return;
        }

        LPiece pFlowSection = lSectionList[pFlowIndex];
        string pFlowName = string.IsNullOrEmpty(pFlowSection.LPieceName)
            ? "unnamed"
            : $"'{pFlowSection.LPieceName}'";
        LTraceLog.LTraceInfoRecord(
            $"Section {pFlowAction} #{pFlowIndex + 1} of {lSectionList.Count} in '{pFlowSource}': {pFlowName} " +
            $"{pFlowSection.LPieceStart:hh\\:mm\\:ss\\.fff}-{pFlowSection.LPieceEnd:hh\\:mm\\:ss\\.fff}");
    }

    private void PFlowSectionUpdate()
    {
        pViewfinder.PViewfinderSectionsUpdate(lSectionList, lSectionIndexActive);
        pMap.PMapSectionsUpdate(lSectionList, lSectionIndexActive);
        PFlowSectionChange?.Invoke(lSectionList.AsReadOnly(), lSectionIndexActive);
        PFlowSidecarSave();
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
                $"{lSectionList.Count} section(s)");
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
        if (!pFlowCommandActive
            || lSpool is null
            || pSectionIndex < 0
            || pSectionIndex >= lSectionList.Count)
        {
            return;
        }

        lSectionIndexActive = pSectionIndex;
        PFlowSectionUpdate();
        PFlowCursorPropagate(lSectionList[pSectionIndex].LPieceStart, true, true);
    }

    public void PFlowSectionToggle(int pSectionIndex)
    {
        if (!pFlowSectionEditable || pSectionIndex < 0 || pSectionIndex >= lSectionList.Count)
        {
            return;
        }

        LPiece pSectionEntry = lSectionList[pSectionIndex];
        lSectionList[pSectionIndex] = pSectionEntry with { LPieceHidden = !pSectionEntry.LPieceHidden };
        PFlowSectionRecord(lSectionList[pSectionIndex].LPieceHidden ? "turned off" : "turned on", pSectionIndex);
        PFlowSectionUpdate();
    }

    public IReadOnlyList<LPiece> PFlowSectionsRead() => lSectionList.ToArray();

    internal IReadOnlyList<Cadroue.Core.LSidecarSectionRecord> PFlowSidecarRead() =>
        lSectionList
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
        if (lSpool is null || lSectionList.Count > 0 || lSidecarSections.Count == 0)
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

    public int? PFlowSelectionRead() => lSectionIndexActive;

    public void PFlowSectionsSet(IReadOnlyList<LPiece> lSections, int? lSectionSelect)
    {
        if ((!pFlowSectionEditable && !pFlowSidecarRestoring) || lSpool is null)
        {
            return;
        }

        lSectionList.Clear();
        foreach (LPiece lSection in lSections)
        {
            if (lSection.LPieceEnd <= lSpool.LSpoolDuration && lSection.LPieceStart < lSection.LPieceEnd)
            {
                lSectionList.Add(lSection);
            }
        }

        lSectionIndexActive = lSectionList.Count == 0 || lSectionSelect is not int lSelect
            ? null
            : Math.Clamp(lSelect, 0, lSectionList.Count - 1);
        PFlowSectionUpdate();
    }

    public bool PFlowSectionMove(int pSectionSource, int pSectionTarget)
    {
        if (!pFlowSectionEditable || pSectionSource < 0 || pSectionSource >= lSectionList.Count)
        {
            return false;
        }

        int pSectionInsert = Math.Clamp(
            pSectionSource < pSectionTarget ? pSectionTarget - 1 : pSectionTarget,
            0,
            lSectionList.Count - 1);
        if (pSectionInsert == pSectionSource)
        {
            return false;
        }

        LPiece pSectionMoved = lSectionList[pSectionSource];
        lSectionList.RemoveAt(pSectionSource);
        lSectionList.Insert(pSectionInsert, pSectionMoved);
        if (lSectionIndexActive == pSectionSource)
        {
            lSectionIndexActive = pSectionInsert;
        }

        PFlowSectionRecord($"moved from #{pSectionSource + 1} to", pSectionInsert);
        PFlowSectionUpdate();
        return true;
    }

    public bool PFlowSectionSort()
    {
        if (!pFlowSectionEditable || lSectionList.Count < 2)
        {
            return false;
        }

        LPiece? pSectionSelected = lSectionIndexActive is int pSelectIndex
            ? lSectionList[pSelectIndex]
            : null;

        List<LPiece> pSectionSorted = lSectionList
            .OrderBy(pSection => pSection.LPieceName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (pSectionSorted.SequenceEqual(lSectionList))
        {
            return false;
        }

        lSectionList.Clear();
        lSectionList.AddRange(pSectionSorted);

        if (pSectionSelected is { } pSectionKept)
        {
            int pSectionIndexNew = lSectionList.IndexOf(pSectionKept);
            lSectionIndexActive = pSectionIndexNew < 0 ? null : pSectionIndexNew;
        }

        LTraceLog.LTraceInfoRecord($"Sections sorted by name: {lSectionList.Count} section(s)");
        PFlowSectionUpdate();
        return true;
    }

    public void PFlowNameSet(int pSectionIndex, string pSectionName)
        => PFlowNameSet(pSectionIndex, pSectionName, null, null);

    public void PFlowNameSet(int pSectionIndex, string pSectionName, string? pSectionPrefix, string? pSectionSuffix)
    {
        if (!pFlowSectionEditable || pSectionIndex < 0 || pSectionIndex >= lSectionList.Count) return;

        LPiece pSectionEntry = lSectionList[pSectionIndex];
        string pSectionPrefixNew = pSectionPrefix ?? pSectionEntry.LPiecePrefix;
        string pSectionSuffixNew = pSectionSuffix ?? pSectionEntry.LPieceSuffix;
        if (string.Equals(pSectionEntry.LPieceName, pSectionName, StringComparison.Ordinal)
            && string.Equals(pSectionEntry.LPiecePrefix, pSectionPrefixNew, StringComparison.Ordinal)
            && string.Equals(pSectionEntry.LPieceSuffix, pSectionSuffixNew, StringComparison.Ordinal))
        {
            return;
        }

        string pSectionWas = pSectionEntry.LPieceName;
        lSectionList[pSectionIndex] = pSectionEntry with
        {
            LPieceName = pSectionName,
            LPiecePrefix = pSectionPrefixNew,
            LPieceSuffix = pSectionSuffixNew
        };
        PFlowSectionRecord(
            string.IsNullOrEmpty(pSectionWas) ? "named" : $"renamed from '{pSectionWas}' to",
            pSectionIndex);
        PFlowSectionUpdate();
    }

    private int PFlowColorRead() => lSectionList.Count % PSectionPalette.PSectionActiveCount;

    private void PFlowViewfinderSelect(int sectionIndex)
    {
        if (sectionIndex < 0 || sectionIndex >= lSectionList.Count) return;
        lSectionIndexActive = sectionIndex;
        PFlowSectionUpdate();
    }
}
