using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    private void PFlowSectionAdd()
    {
        if (lSpool is null || string.IsNullOrWhiteSpace(lSourcePath)) return;
        if (lCursor >= lSpool.LSpoolDuration) return;
        if (PFlowInsideCheck(lCursor, -1)) return;
        TimeSpan pSectionEnd = PFlowLimitRead(lCursor, lSpool.LSpoolDuration, -1);
        if (pSectionEnd <= lCursor) return;
        lSectionList.Add(new LSegment(lCursor, pSectionEnd, PFlowColorRead(), string.Empty));
        lSectionIndexActive = lSectionList.Count - 1;
        PFlowSectionRecord("added", lSectionIndexActive.Value);
        PFlowSectionUpdate();
    }

    private void PFlowStartSet()
    {
        if (lSpool is null) return;
        if (lSectionIndexActive is null) { PFlowSectionAdd(); return; }
        LSegment section = lSectionList[lSectionIndexActive.Value];
        if (section.LSegmentEnd < lCursor) { PFlowSectionAdd(); return; }
        if (lCursor >= section.LSegmentEnd) return;
        if (lCursor < PFlowFloorRead(section.LSegmentStart, lSectionIndexActive.Value)) return;
        lSectionList[lSectionIndexActive.Value] = section with { LSegmentStart = lCursor };
        PFlowSectionRecord("start set", lSectionIndexActive.Value);
        PFlowSectionUpdate();
    }

    private void PFlowSectionDivide()
    {
        if (lSectionIndexActive is null) return;
        LSegment section = lSectionList[lSectionIndexActive.Value];
        if (lCursor <= section.LSegmentStart || lCursor >= section.LSegmentEnd) return;
        int index = lSectionIndexActive.Value;
        int secondColorIndex = PFlowColorRead();
        lSectionList.RemoveAt(index);
        lSectionList.Insert(index, new LSegment(lCursor, section.LSegmentEnd, secondColorIndex, string.Empty));
        lSectionList.Insert(index, section with { LSegmentEnd = lCursor });
        lSectionIndexActive = index;
        PFlowSectionRecord($"split at {lCursor:hh\\:mm\\:ss\\.fff}, left half", index);
        PFlowSectionRecord("split, right half", index + 1);
        PFlowSectionUpdate();
    }

    private void PFlowEndSet()
    {
        if (lSectionIndexActive is null) return;
        LSegment section = lSectionList[lSectionIndexActive.Value];
        if (lCursor <= section.LSegmentStart) return;
        if (lCursor > PFlowLimitRead(section.LSegmentEnd, lCursor, lSectionIndexActive.Value)) return;
        lSectionList[lSectionIndexActive.Value] = section with { LSegmentEnd = lCursor };
        PFlowSectionRecord("end set", lSectionIndexActive.Value);
        PFlowSectionUpdate();
    }

    public void PFlowSectionDelete()
    {
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

        LSegment pFlowSection = lSectionList[pFlowIndex];
        string pFlowName = string.IsNullOrEmpty(pFlowSection.LSegmentName)
            ? "unnamed"
            : $"'{pFlowSection.LSegmentName}'";
        LTraceLog.LTraceInfoRecord(
            $"Section {pFlowAction} #{pFlowIndex + 1} of {lSectionList.Count} in '{pFlowSource}': {pFlowName} " +
            $"{pFlowSection.LSegmentStart:hh\\:mm\\:ss\\.fff}-{pFlowSection.LSegmentEnd:hh\\:mm\\:ss\\.fff}");
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
        if (pFlowSidecarRestoring || lSourcePath is null)
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
        PFlowCursorPropagate(lSectionList[pSectionIndex].LSegmentStart, true, true);
    }

    public void PFlowSectionToggle(int pSectionIndex)
    {
        if (pSectionIndex < 0 || pSectionIndex >= lSectionList.Count)
        {
            return;
        }

        LSegment pSectionEntry = lSectionList[pSectionIndex];
        lSectionList[pSectionIndex] = pSectionEntry with { LSegmentHidden = !pSectionEntry.LSegmentHidden };
        PFlowSectionRecord(lSectionList[pSectionIndex].LSegmentHidden ? "turned off" : "turned on", pSectionIndex);
        PFlowSectionUpdate();
    }

    public IReadOnlyList<LSegment> PFlowSectionsRead() => lSectionList.ToArray();

    internal IReadOnlyList<Cadroue.Media.LSidecarSectionRecord> PFlowSidecarRead() =>
        lSectionList
            .Select(lSection => new Cadroue.Media.LSidecarSectionRecord
            {
                LSidecarStartMilliseconds = (long)lSection.LSegmentStart.TotalMilliseconds,
                LSidecarEndMilliseconds = (long)lSection.LSegmentEnd.TotalMilliseconds,
                LSidecarColorIndex = lSection.LSegmentColorIndex,
                LSidecarName = lSection.LSegmentName,
                LSidecarPrefix = lSection.LSegmentPrefix,
                LSidecarSuffix = lSection.LSegmentSuffix,
                LSidecarHidden = lSection.LSegmentHidden
            })
            .ToArray();

    internal void PFlowSidecarApply(IReadOnlyList<Cadroue.Media.LSidecarSectionRecord> lSidecarSections)
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
                    .Select(lSection => new LSegment(
                        TimeSpan.FromMilliseconds(lSection.LSidecarStartMilliseconds),
                        TimeSpan.FromMilliseconds(lSection.LSidecarEndMilliseconds),
                        lSection.LSidecarColorIndex,
                        lSection.LSidecarName)
                    {
                        LSegmentPrefix = lSection.LSidecarPrefix ?? string.Empty,
                        LSegmentSuffix = lSection.LSidecarSuffix ?? string.Empty,
                        LSegmentHidden = lSection.LSidecarHidden
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

    public void PFlowSectionsSet(IReadOnlyList<LSegment> lSections, int? lSectionSelect)
    {
        if (lSpool is null)
        {
            return;
        }

        lSectionList.Clear();
        foreach (LSegment lSection in lSections)
        {
            if (lSection.LSegmentEnd <= lSpool.LSpoolDuration && lSection.LSegmentStart < lSection.LSegmentEnd)
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
        if (pSectionSource < 0 || pSectionSource >= lSectionList.Count)
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

        LSegment pSectionMoved = lSectionList[pSectionSource];
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
        if (lSectionList.Count < 2)
        {
            return false;
        }

        LSegment? pSectionSelected = lSectionIndexActive is int pSelectIndex
            ? lSectionList[pSelectIndex]
            : null;

        List<LSegment> pSectionSorted = lSectionList
            .OrderBy(pSection => pSection.LSegmentName, StringComparer.CurrentCultureIgnoreCase)
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
        if (pSectionIndex < 0 || pSectionIndex >= lSectionList.Count) return;

        LSegment pSectionEntry = lSectionList[pSectionIndex];
        string pSectionPrefixNew = pSectionPrefix ?? pSectionEntry.LSegmentPrefix;
        string pSectionSuffixNew = pSectionSuffix ?? pSectionEntry.LSegmentSuffix;
        if (string.Equals(pSectionEntry.LSegmentName, pSectionName, StringComparison.Ordinal)
            && string.Equals(pSectionEntry.LSegmentPrefix, pSectionPrefixNew, StringComparison.Ordinal)
            && string.Equals(pSectionEntry.LSegmentSuffix, pSectionSuffixNew, StringComparison.Ordinal))
        {
            return;
        }

        string pSectionWas = pSectionEntry.LSegmentName;
        lSectionList[pSectionIndex] = pSectionEntry with
        {
            LSegmentName = pSectionName,
            LSegmentPrefix = pSectionPrefixNew,
            LSegmentSuffix = pSectionSuffixNew
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
