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
        if (lCursor < PFlowFloorRead(section.LSegmentStart, lSectionIndexSelect.Value)) return;
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
        if (lCursor > PFlowLimitRead(section.LSegmentEnd, lCursor, lSectionIndexSelect.Value)) return;
        lSectionList[lSectionIndexSelect.Value] = section with { LSegmentEnd = lCursor };
        PFlowSectionRecord("end set", lSectionIndexSelect.Value);
        PFlowSectionUpdate();
    }

    public void PFlowSectionDelete()
    {
        if (lSectionIndexSelect is null) return;
        if (!PFlowDestructiveConfirm(LLocalization.LLocalizationTextRead("Flow.Section.DeleteConfirm"))) return;
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

    public void PFlowSectionSeek(int pSectionIndex)
    {
        if (!pFlowCommandActive
            || lSpool is null
            || pSectionIndex < 0
            || pSectionIndex >= lSectionList.Count)
        {
            return;
        }

        lSectionIndexSelect = pSectionIndex;
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

    internal IReadOnlyList<Cadroue.Media.LSidecarSectionRecord> PFlowSidecarSectionsRead() =>
        lSectionList
            .Select(lSection => new Cadroue.Media.LSidecarSectionRecord
            {
                StartMilliseconds = (long)lSection.LSegmentStart.TotalMilliseconds,
                EndMilliseconds = (long)lSection.LSegmentEnd.TotalMilliseconds,
                ColorIndex = lSection.LSegmentColorIndex,
                Name = lSection.LSegmentName,
                Prefix = lSection.LSegmentPrefix,
                Suffix = lSection.LSegmentSuffix,
                Hidden = lSection.LSegmentHidden
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
                        lSection.Name)
                    {
                        LSegmentPrefix = lSection.Prefix ?? string.Empty,
                        LSegmentSuffix = lSection.Suffix ?? string.Empty,
                        LSegmentHidden = lSection.Hidden
                    })
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

        LSegment pNameSection = lSectionList[pSectionIndex];
        TextBox pNameBox = PFlowNameFieldBuild(pNameSection.LSegmentName, PFlowNameWidth);
        TextBox pPrefixBox = PFlowNameFieldBuild(pNameSection.LSegmentPrefix, PFlowAffixWidth);
        TextBox pSuffixBox = PFlowNameFieldBuild(pNameSection.LSegmentSuffix, PFlowAffixWidth);

        var pFieldPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pFieldPanel.Children.Add(pNameBox);
        pFieldPanel.Children.Add(PFlowAffixSeparatorBuild(pPrefixBox));
        pFieldPanel.Children.Add(pPrefixBox);
        pFieldPanel.Children.Add(PFlowAffixSeparatorBuild(pSuffixBox));
        pFieldPanel.Children.Add(pSuffixBox);

        PFlowAffixShow(pPrefixBox, !string.IsNullOrEmpty(pNameSection.LSegmentPrefix));
        PFlowAffixShow(pSuffixBox, !string.IsNullOrEmpty(pNameSection.LSegmentSuffix));

        PFlowAffixStepWire(pNameBox, pPrefixBox);
        PFlowAffixStepWire(pPrefixBox, pSuffixBox);
        PFlowAffixStepWire(pSuffixBox, null);

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
                Child = pFieldPanel
            }
        };

        void PFlowNameKeyHandle(object pSender, KeyEventArgs pNameKeyEvent)
        {
            switch (pNameKeyEvent.Key)
            {
                case Key.Enter:
                    PFlowNameApply(pSectionIndex, pNameBox.Text, pPrefixBox.Text, pSuffixBox.Text);
                    PFlowNameClose();
                    pNameKeyEvent.Handled = true;
                    break;
                case Key.Escape:
                    PFlowNameClose();
                    pNameKeyEvent.Handled = true;
                    break;
            }
        }

        pNameBox.KeyDown += PFlowNameKeyHandle;
        pPrefixBox.KeyDown += PFlowNameKeyHandle;
        pSuffixBox.KeyDown += PFlowNameKeyHandle;

        pFlowNamePopup = pNamePopup;
        pNamePopup.IsOpen = true;
        pNameBox.Focus();
        Keyboard.Focus(pNameBox);
        pNameBox.SelectAll();
        return true;
    }

    private static TextBox PFlowNameFieldBuild(string pFieldText, double pFieldWidth)
    {
        var pFieldBox = new TextBox
        {
            Width = pFieldWidth,
            Height = PFlowNameHeight,
            Text = pFieldText,
            FontSize = PSection.PSectionNameSize,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        PTextbox.PTextboxApply(pFieldBox);
        return pFieldBox;
    }

    private static UIElement PFlowAffixSeparatorBuild(TextBox pAffixBox)
    {
        var pSeparator = new TextBlock
        {
            Text = "/",
            Margin = new Thickness(6, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E)),
            Visibility = Visibility.Collapsed
        };
        pAffixBox.Tag = pSeparator;
        return pSeparator;
    }

    private static void PFlowAffixShow(TextBox pAffixBox, bool pAffixVisible)
    {
        pAffixBox.Visibility = pAffixVisible ? Visibility.Visible : Visibility.Collapsed;
        if (pAffixBox.Tag is UIElement pSeparator)
        {
            pSeparator.Visibility = pAffixBox.Visibility;
        }
    }

    private static void PFlowAffixStepWire(TextBox pFieldBox, TextBox? pNextBox)
    {
        pFieldBox.PreviewTextInput += (_, pFieldEvent) =>
        {
            if (pFieldEvent.Text != ",")
            {
                return;
            }

            pFieldEvent.Handled = true;
            if (pNextBox is null)
            {
                return;
            }

            PFlowAffixShow(pNextBox, true);
            pNextBox.Focus();
            Keyboard.Focus(pNextBox);
            pNextBox.SelectAll();
        };
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

    private void PFlowNameApply(int pSectionIndex, string pSectionName, string pSectionPrefix, string pSectionSuffix)
    {
        PFlowNameSet(pSectionIndex, pSectionName.Trim(), pSectionPrefix.Trim(), pSectionSuffix.Trim());
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

    public bool PFlowSectionSort()
    {
        if (lSectionList.Count < 2)
        {
            return false;
        }

        LSegment? pSectionSelected = lSectionIndexSelect is int pSelectIndex
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
            lSectionIndexSelect = pSectionIndexNew < 0 ? null : pSectionIndexNew;
        }

        LAppLog.LInfo($"Sections sorted by name: {lSectionList.Count} section(s)");
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
        lSectionIndexSelect = sectionIndex;
        PFlowSectionUpdate();
    }
}
