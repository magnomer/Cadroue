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
        lSectionList.Add(new LSegment(lCursor, lSpool.LSpoolDuration, PFlowColorRead(), string.Empty));
        lSectionIndexSelect = lSectionList.Count - 1;
        PFlowSectionRecord("added", lSectionIndexSelect.Value);
        PFlowSectionUpdate();
    }

    private void PFlowStartSet()
    {
        if (lSpool is null) return;
        if (lSectionIndexSelect is null) { PFlowSectionAdd(); return; }
        LSegment section = lSectionList[lSectionIndexSelect.Value];
        if (section.LSegmentEnd < lCursor) { PFlowSectionAdd(); return; }
        if (lCursor >= section.LSegmentEnd) return;
        lSectionList[lSectionIndexSelect.Value] = section with { LSegmentStart = lCursor };
        PFlowSectionRecord("start set", lSectionIndexSelect.Value);
        PFlowSectionUpdate();
    }

    private void PFlowSectionSplit()
    {
        if (lSectionIndexSelect is null) return;
        LSegment section = lSectionList[lSectionIndexSelect.Value];
        if (lCursor <= section.LSegmentStart || lCursor >= section.LSegmentEnd) return;
        int index = lSectionIndexSelect.Value;
        int secondColorIndex = PFlowColorRead();
        lSectionList.RemoveAt(index);
        lSectionList.Insert(index, new LSegment(lCursor, section.LSegmentEnd, secondColorIndex, string.Empty));
        lSectionList.Insert(index, section with { LSegmentEnd = lCursor });
        lSectionIndexSelect = index;
        PFlowSectionRecord($"split at {lCursor:hh\\:mm\\:ss\\.fff}, left half", index);
        PFlowSectionRecord("split, right half", index + 1);
        PFlowSectionUpdate();
    }

    private void PFlowEndSet()
    {
        if (lSectionIndexSelect is null) return;
        LSegment section = lSectionList[lSectionIndexSelect.Value];
        if (lCursor <= section.LSegmentStart) return;
        lSectionList[lSectionIndexSelect.Value] = section with { LSegmentEnd = lCursor };
        PFlowSectionRecord("end set", lSectionIndexSelect.Value);
        PFlowSectionUpdate();
    }

    public void PFlowSectionDelete()
    {
        if (lSectionIndexSelect is null) return;
        int index = lSectionIndexSelect.Value;
        PFlowSectionRecord("deleted", index);
        lSectionList.RemoveAt(index);
        lSectionIndexSelect = lSectionList.Count == 0 ? null : Math.Min(index, lSectionList.Count - 1);
        PFlowSectionUpdate();
    }

    private void PFlowSectionRecord(string pFlowAction, int pFlowIndex)
    {
        string pFlowSource = string.IsNullOrWhiteSpace(lSourcePath)
            ? "(no media)"
            : System.IO.Path.GetFileName(lSourcePath);

        if (pFlowIndex < 0 || pFlowIndex >= lSectionList.Count)
        {
            LAppLog.LInfo($"Section {pFlowAction} in '{pFlowSource}': {lSectionList.Count} section(s) remain");
            return;
        }

        LSegment pFlowSection = lSectionList[pFlowIndex];
        string pFlowName = string.IsNullOrEmpty(pFlowSection.LSegmentName)
            ? "unnamed"
            : $"'{pFlowSection.LSegmentName}'";
        LAppLog.LInfo(
            $"Section {pFlowAction} #{pFlowIndex + 1} of {lSectionList.Count} in '{pFlowSource}': {pFlowName} " +
            $"{pFlowSection.LSegmentStart:hh\\:mm\\:ss\\.fff}-{pFlowSection.LSegmentEnd:hh\\:mm\\:ss\\.fff}");
    }

    private void PFlowSectionUpdate()
    {
        pViewfinder.PViewfinderSectionsUpdate(lSectionList, lSectionIndexSelect);
        pMap.PMapSectionsUpdate(lSectionList, lSectionIndexSelect);
        PFlowSectionChange?.Invoke(lSectionList.AsReadOnly(), lSectionIndexSelect);
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
            LAppLog.LInfo(
                $"Sidecar written for '{System.IO.Path.GetFileName(lSourcePath)}': " +
                $"{lSectionList.Count} section(s)");
        }
        catch (Exception pFlowException)
        {
            LAppLog.LError(
                $"Sidecar could not be written for '{System.IO.Path.GetFileName(lSourcePath)}'",
                pFlowException);
        }
    }

    public void PFlowSectionSelect(int pSectionIndex)
    {
        PFlowViewfinderSelect(pSectionIndex);
    }

    public IReadOnlyList<LSegment> PFlowSectionsRead() => lSectionList.ToArray();

    internal IReadOnlyList<Cadroue.Media.LSidecarSectionRecord> PFlowSidecarSectionsRead() =>
        lSectionList
            .Select(lSection => new Cadroue.Media.LSidecarSectionRecord
            {
                StartMilliseconds = (long)lSection.LSegmentStart.TotalMilliseconds,
                EndMilliseconds = (long)lSection.LSegmentEnd.TotalMilliseconds,
                ColorIndex = lSection.LSegmentColorIndex,
                Name = lSection.LSegmentName
            })
            .ToArray();

    internal void PFlowSidecarSectionsApply(IReadOnlyList<Cadroue.Media.LSidecarSectionRecord> lSidecarSections)
    {
        if (lSpool is null || lSectionList.Count > 0 || lSidecarSections.Count == 0)
        {
            return;
        }

        LAppLog.LInfo(
            $"Sidecar restored for '{System.IO.Path.GetFileName(lSourcePath)}': " +
            $"{lSidecarSections.Count} section(s)");
        pFlowSidecarRestoring = true;
        try
        {
            PFlowSectionsSet(
                lSidecarSections
                    .Select(lSection => new LSegment(
                        TimeSpan.FromMilliseconds(lSection.StartMilliseconds),
                        TimeSpan.FromMilliseconds(lSection.EndMilliseconds),
                        lSection.ColorIndex,
                        lSection.Name))
                    .ToArray(),
                null);
        }
        finally
        {
            pFlowSidecarRestoring = false;
        }
    }

    public int? PFlowSectionSelectRead() => lSectionIndexSelect;

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

        lSectionIndexSelect = lSectionList.Count == 0 || lSectionSelect is not int lSelect
            ? null
            : Math.Clamp(lSelect, 0, lSectionList.Count - 1);
        PFlowSectionUpdate();
    }

    private bool PFlowNameShow()
    {
        if (lSectionIndexSelect is not int pSectionIndex || pSectionIndex >= lSectionList.Count)
        {
            return false;
        }

        PFlowNameClose();

        var pNameBox = new TextBox
        {
            Width = 220,
            Height = PFlowNameHeight,
            Text = lSectionList[pSectionIndex].LSegmentName,
            FontSize = PSection.PSectionNameSize,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        PTextbox.PTextboxApply(pNameBox);

        Rect pSectionRect = pViewfinder.PViewfinderSectionRead(pSectionIndex);
        var pNamePopup = new Popup
        {
            PlacementTarget = pViewfinder,
            Placement = PlacementMode.Center,
            HorizontalOffset = pSectionRect.IsEmpty ? 0 : pSectionRect.Left + pSectionRect.Width / 2 - pViewfinder.ActualWidth / 2,
            VerticalOffset = pSectionRect.IsEmpty ? 0 : pSectionRect.Top + pSectionRect.Height / 2 - pViewfinder.ActualHeight / 2,
            StaysOpen = false,
            AllowsTransparency = true,
            Child = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xD7, 0xDF, 0xEA)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Child = pNameBox
            }
        };

        pNameBox.KeyDown += (_, pNameKeyEvent) =>
        {
            switch (pNameKeyEvent.Key)
            {
                case Key.Enter:
                    PFlowNameApply(pSectionIndex, pNameBox.Text);
                    PFlowNameClose();
                    pNameKeyEvent.Handled = true;
                    break;
                case Key.Escape:
                    PFlowNameClose();
                    pNameKeyEvent.Handled = true;
                    break;
            }
        };

        pFlowNamePopup = pNamePopup;
        pNamePopup.IsOpen = true;
        pNameBox.Focus();
        Keyboard.Focus(pNameBox);
        pNameBox.SelectAll();
        return true;
    }

    private void PFlowNameClose()
    {
        if (pFlowNamePopup is null)
        {
            return;
        }

        pFlowNamePopup.IsOpen = false;
        pFlowNamePopup = null;
    }

    private void PFlowNameApply(int pSectionIndex, string pSectionName)
    {
        PFlowNameSet(pSectionIndex, pSectionName.Trim());
        PFlowSectionChange?.Invoke(lSectionList.AsReadOnly(), lSectionIndexSelect);
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
        if (lSectionIndexSelect == pSectionSource)
        {
            lSectionIndexSelect = pSectionInsert;
        }

        PFlowSectionRecord($"moved from #{pSectionSource + 1} to", pSectionInsert);
        PFlowSectionUpdate();
        return true;
    }

    public void PFlowNameSet(int pSectionIndex, string pSectionName)
    {
        if (pSectionIndex < 0 || pSectionIndex >= lSectionList.Count) return;
        if (string.Equals(lSectionList[pSectionIndex].LSegmentName, pSectionName, StringComparison.Ordinal)) return;
        string pSectionWas = lSectionList[pSectionIndex].LSegmentName;
        lSectionList[pSectionIndex] = lSectionList[pSectionIndex] with { LSegmentName = pSectionName };
        PFlowSectionRecord(
            string.IsNullOrEmpty(pSectionWas) ? "named" : $"renamed from '{pSectionWas}' to",
            pSectionIndex);
        PFlowSidecarSave();
    }

    private int PFlowColorRead() => lSectionList.Count % LSectionPaletteCount;

    private void PFlowViewfinderSelect(int sectionIndex)
    {
        if (sectionIndex < 0 || sectionIndex >= lSectionList.Count) return;
        lSectionIndexSelect = sectionIndex;
        PFlowSectionUpdate();
    }
}
