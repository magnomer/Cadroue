using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PFlow;
using Cadroue.UIShell.PMainArea;
using Cadroue.UIShell.PMainWindow;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PSection : UserControl
{
    private static readonly FontFamily pSectionFontFamily = new("Segoe UI");

    internal const double PSectionNameSize = 12;

    private const double PSectionBadgeSize = 18;
    private const double PSectionBadgePadding = 6;
    private const double PSectionAffixWidth = 62;
    private const double PSectionDisabledOpacity = 0.4;

    private PFlowControl? pFlowAttached;
    private IReadOnlyList<LSegment> pSectionListCurrent = Array.Empty<LSegment>();
    private int? pSectionIndexSelectCurrent;
    private readonly TextBlock pSectionCountLabel;
    private readonly StackPanel pSectionRowPanel;
    private int? pSectionIndexEditing;
    private TextBox? pSectionNameBoxCurrent;
    private TextBox? pSectionPrefixBoxCurrent;
    private TextBox? pSectionSuffixBoxCurrent;
    private bool pSectionRebuilding;
    private int? pSectionIndexDragging;
    private Point? pSectionDragStart;
    private bool pSectionDragActive;
    private Border? pSectionRowDragging;

    public PSection()
    {
        pSectionCountLabel = new TextBlock
        {
            Text = "Sections",
            FontSize = 12,
            FontFamily = pSectionFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            VerticalAlignment = VerticalAlignment.Center
        };

        UIElement pHeader = PSectionHeaderBuild();

        pSectionRowPanel = new StackPanel();
        pSectionRowPanel.PreviewMouseMove += PSectionDragMoveHandle;
        pSectionRowPanel.MouseLeftButtonUp += PSectionDragUpHandle;
        pSectionRowPanel.LostMouseCapture += PSectionDragLostHandle;

        var pScroll = new ScrollViewer
        {
            Content = pSectionRowPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        UIElement pActionBar = PSectionActionBuild();
        DockPanel.SetDock(pActionBar, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pActionBar);
        pRoot.Children.Add(pScroll);

        pSectionFullBody = pRoot;
        pSectionStripBody = PSectionStripBuild();
        pSectionStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pSectionFullBody);
        pBodyHost.Children.Add(pSectionStripBody);

        FocusVisualStyle = null;
        Content = PPanel.PPanelBorderBuild(pBodyHost);
    }

    private UIElement PSectionActionBuild()
    {
        var pActionLeft = new StackPanel { Orientation = Orientation.Horizontal };
        pActionLeft.Children.Add(PSectionButtonBuild(
            "/PAssets/PPanels/PExportMinus.svg",
            "Delete the selected section",
            PSectionDeleteHandle));

        var pActionRight = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Button pSectionSortButton = PSectionButtonBuild(
            "/PAssets/PPanels/PSort.svg",
            "Sort the sections by name",
            PSectionSortHandle);
        pSectionSortButton.Margin = new Thickness(0);
        pActionRight.Children.Add(pSectionSortButton);

        var pActionPanel = new Grid { Margin = new Thickness(10, 4, 10, 4) };
        pActionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pActionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pActionRight, 1);
        pActionPanel.Children.Add(pActionLeft);
        pActionPanel.Children.Add(pActionRight);

        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pActionPanel
        };
    }

    private Button PSectionButtonBuild(string pIconPath, string pTooltip, RoutedEventHandler pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D))),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Margin = new Thickness(0, 0, 2, 0),
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += pClick;
        return pButton;
    }

    private void PSectionDeleteHandle(object pSender, RoutedEventArgs pEvent)
    {
        PSectionEditCommit();
        pFlowAttached?.PFlowSectionDelete();
    }

    private void PSectionSortHandle(object pSender, RoutedEventArgs pEvent)
    {
        PSectionEditCommit();
        pFlowAttached?.PFlowSectionSort();
    }

    public void PSectionAttach(PFlowControl pFlow)
    {
        PSectionDetach();
        pFlowAttached = pFlow;
        pFlowAttached.PFlowSectionChange += PSectionUpdateHandle;
        PSectionRebuild();
    }

    public IReadOnlyList<LSplitSectionDescription> PSectionSplitRead()
    {
        return pSectionListCurrent
            .Select(pSection => new LSplitSectionDescription(
                pSection.LSegmentStart,
                pSection.LSegmentEnd,
                pSection.LSegmentName,
                pSection.LSegmentPrefix,
                pSection.LSegmentSuffix,
                pSection.LSegmentHidden))
            .ToArray();
    }

    private void PSectionDetach()
    {
        if (pFlowAttached is null) return;
        pFlowAttached.PFlowSectionChange -= PSectionUpdateHandle;
        pFlowAttached = null;
    }

    private void PSectionUpdateHandle(IReadOnlyList<LSegment> pSectionList, int? pSectionIndexSelect)
    {
        LSegment[] pSectionListNext = pSectionList.ToArray();
        bool pSectionListSame = pSectionListCurrent.SequenceEqual(pSectionListNext)
            && pSectionRowPanel.Children.Count == pSectionListNext.Length;
        pSectionListCurrent = pSectionListNext;
        pSectionIndexSelectCurrent = pSectionIndexSelect;

        if (pSectionDragActive)
        {
            return;
        }

        if (pSectionListSame)
        {
            PSectionSelectApply();
            return;
        }

        PSectionRebuild();
    }

    private void PSectionSelectApply()
    {
        for (int pIndex = 0; pIndex < pSectionRowPanel.Children.Count; pIndex++)
        {
            if (pSectionRowPanel.Children[pIndex] is Border pRow)
            {
                pRow.Background = pIndex == pSectionIndexSelectCurrent
                    ? new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB))
                    : Brushes.White;
            }
        }
    }

    private void PSectionRebuild()
    {
        pSectionRebuilding = true;
        try
        {
            pSectionRowPanel.Children.Clear();
            int pCount = pSectionListCurrent.Count;
            pSectionCountLabel.Text = pCount == 0 ? "Sections" : $"Sections  ({pCount})";
            for (int i = 0; i < pCount; i++)
            {
                pSectionRowPanel.Children.Add(PSectionRowBuild(i, pSectionListCurrent[i], i == pSectionIndexSelectCurrent));
            }
        }
        finally
        {
            pSectionRebuilding = false;
        }
    }

    private void PSectionNumberUpdate()
    {
        for (int pIndex = 0; pIndex < pSectionRowPanel.Children.Count; pIndex++)
        {
            if (pSectionRowPanel.Children[pIndex] is Border { Tag: TextBlock pBadgeText })
            {
                pBadgeText.Text = (pIndex + 1).ToString();
            }
        }
    }

    private void PSectionDragClear()
    {
        pSectionIndexDragging = null;
        pSectionDragStart = null;
        pSectionDragActive = false;
        pSectionRowDragging = null;
    }

    private void PSectionDragMoveHandle(object pSender, MouseEventArgs pEvent)
    {
        if (pSectionRowDragging is not { } pDragRow
            || pSectionIndexDragging is not int pDragIndex
            || pSectionIndexEditing is not null
            || pSectionDragStart is not Point pStart
            || pEvent.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point pCurrent = pEvent.GetPosition(pSectionRowPanel);
        if (!pSectionDragActive
            && Math.Abs(pCurrent.X - pStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(pCurrent.Y - pStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        pSectionDragActive = true;
        pDragRow.Opacity = 0.72;
        PSectionMoveLive(pDragIndex, PSectionIndexResolve(pCurrent), pDragRow);
        pEvent.Handled = true;
    }

    private void PSectionDragUpHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        if (pSectionRowDragging is not { } pDragRow)
        {
            return;
        }

        bool pDragMoved = pSectionDragActive;
        pDragRow.Opacity = 1;
        pSectionRowPanel.ReleaseMouseCapture();

        if (pDragMoved)
        {
            PSectionDragClear();
            PSectionRebuild();
            pEvent.Handled = true;
            return;
        }

        int pRowIndex = pSectionRowPanel.Children.IndexOf(pDragRow);
        PSectionDragClear();

        if (pRowIndex >= 0 && pSectionIndexEditing != pRowIndex)
        {
            PSectionEditCommit();
            pFlowAttached?.PFlowSectionSelect(pRowIndex);
        }

        pEvent.Handled = true;
    }

    private void PSectionDragLostHandle(object pSender, MouseEventArgs pEvent)
    {
        if (pSectionRowDragging is { } pDragRow)
        {
            pDragRow.Opacity = 1;
        }

        PSectionDragClear();
    }

    private int PSectionIndexResolve(Point pMousePoint)
    {
        int pTargetIndex = 0;
        for (int pIndex = 0; pIndex < pSectionRowPanel.Children.Count; pIndex++)
        {
            if (pSectionRowPanel.Children[pIndex] is not FrameworkElement pRow)
            {
                continue;
            }

            Point pRowPoint = pRow.TransformToAncestor(pSectionRowPanel).Transform(new Point(0, 0));
            if (pMousePoint.Y > pRowPoint.Y + pRow.ActualHeight / 2)
            {
                pTargetIndex = pIndex + 1;
            }
        }

        return Math.Clamp(pTargetIndex, 0, pSectionRowPanel.Children.Count);
    }

    private bool PSectionMoveLive(int pSectionIndex, int pTargetIndex, UIElement pSectionRow)
    {
        int pSourceIndex = pSectionRowPanel.Children.IndexOf(pSectionRow);
        if (pSourceIndex < 0 || pFlowAttached is null)
        {
            return false;
        }

        pTargetIndex = Math.Clamp(pTargetIndex, 0, pSectionRowPanel.Children.Count);
        int pInsertIndex = pSourceIndex < pTargetIndex ? pTargetIndex - 1 : pTargetIndex;
        if (pSourceIndex == pInsertIndex || !pFlowAttached.PFlowSectionMove(pSectionIndex, pTargetIndex))
        {
            return false;
        }

        pSectionRowPanel.Children.RemoveAt(pSourceIndex);
        pSectionRowPanel.Children.Insert(pInsertIndex, pSectionRow);
        pSectionIndexDragging = pInsertIndex;
        PSectionNumberUpdate();
        return true;
    }

    private void PSectionEditCommit()
    {
        if (pSectionIndexEditing is not int pEditingIndex || pSectionNameBoxCurrent is not { } pEditingBox)
        {
            return;
        }

        string pEditingName = pEditingBox.Text.Trim();
        string pEditingPrefix = pSectionPrefixBoxCurrent?.Text.Trim() ?? string.Empty;
        string pEditingSuffix = pSectionSuffixBoxCurrent?.Text.Trim() ?? string.Empty;

        pSectionIndexEditing = null;
        pSectionNameBoxCurrent = null;
        pSectionPrefixBoxCurrent = null;
        pSectionSuffixBoxCurrent = null;
        pFlowAttached?.PFlowNameSet(pEditingIndex, pEditingName, pEditingPrefix, pEditingSuffix);
        PSectionRebuild();
    }

    private Border PSectionRowBuild(int pSectionIndex, LSegment pSectionEntry, bool pSectionSelected)
    {
        int capturedIndex = pSectionIndex;

        Border? pRowBorderHost = null;

        var pBadgeText = new TextBlock
        {
            Text = (pSectionIndex + 1).ToString(),
            FontSize = PSectionNameSize,
            FontFamily = pSectionFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        var pColorDot = new Border
        {
            MinWidth = PSectionBadgeSize,
            Height = PSectionBadgeSize,
            CornerRadius = new CornerRadius(PSectionBadgeSize / 2),
            Background = PSectionPalette.PSectionBadgeRead(pSectionEntry.LSegmentColorIndex),
            Padding = new Thickness(PSectionBadgePadding, 0, PSectionBadgePadding, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            ToolTip = "Double click to turn this section off or on",
            Child = pBadgeText
        };
        pColorDot.MouseLeftButtonDown += (_, pEvent) =>
        {
            if (pEvent.ClickCount < 2)
            {
                return;
            }

            pSectionRowPanel.ReleaseMouseCapture();
            PSectionDragClear();
            PSectionEditCommit();
            if (pRowBorderHost is { } pToggleRow)
            {
                pFlowAttached?.PFlowSectionToggle(pSectionRowPanel.Children.IndexOf(pToggleRow));
            }

            pEvent.Handled = true;
        };

        UIElement pNameHost = pSectionIndex == pSectionIndexEditing
            ? PSectionEditorBuild(pSectionEntry)
            : PSectionNameTextBuild(pSectionIndex, pSectionEntry);

        var pTimeLabel = new TextBlock
        {
            Text = $"{PSectionTimeFormat(pSectionEntry.LSegmentStart)} → {PSectionTimeFormat(pSectionEntry.LSegmentEnd)}"
                + $"  ({PSectionTimeFormat(PSectionSpanRead(pSectionEntry))})",
            FontSize = 11,
            FontFamily = pSectionFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x56, 0x62, 0x73)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };

        var pRowContent = new Grid
        {
            Opacity = pSectionEntry.LSegmentHidden ? PSectionDisabledOpacity : 1
        };
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pColorDot, 0);
        Grid.SetColumn(pNameHost, 1);
        Grid.SetColumn(pTimeLabel, 2);
        pRowContent.Children.Add(pColorDot);
        pRowContent.Children.Add(pNameHost);
        pRowContent.Children.Add(pTimeLabel);

        var pRowBorder = new Border
        {
            Padding = new Thickness(12, 7, 12, 7),
            Background = pSectionSelected
                ? new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB))
                : Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Cursor = Cursors.Hand,
            Child = pRowContent,
            Tag = pBadgeText
        };
        pRowBorderHost = pRowBorder;
        pRowBorder.PreviewMouseLeftButtonDown += (_, pEvent) =>
        {
            if (pEvent.ClickCount >= 2)
            {
                return;
            }

            pSectionRowDragging = pRowBorder;
            pSectionIndexDragging = pSectionRowPanel.Children.IndexOf(pRowBorder);
            pSectionDragStart = pEvent.GetPosition(pSectionRowPanel);
            pSectionDragActive = false;
            pSectionRowPanel.CaptureMouse();
        };
        pRowBorder.MouseLeftButtonDown += (_, pEvent) =>
        {
            if (pEvent.ClickCount < 2)
            {
                return;
            }

            pSectionRowPanel.ReleaseMouseCapture();
            PSectionDragClear();
            int pRenameIndex = pSectionRowPanel.Children.IndexOf(pRowBorder);
            if (pRenameIndex < 0)
            {
                return;
            }

            pFlowAttached?.PFlowSectionSelect(pRenameIndex);
            pSectionIndexEditing = pRenameIndex;
            PSectionRebuild();
            pEvent.Handled = true;
        };
        return pRowBorder;
    }

    private TextBlock PSectionNameTextBuild(int pSectionIndex, LSegment pSectionEntry)
    {
        bool pSectionUnnamed = string.IsNullOrEmpty(pSectionEntry.LSegmentName);
        var pMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
        var pNameText = new TextBlock
        {
            FontSize = PSectionNameSize,
            FontFamily = pSectionFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)),
            Padding = new Thickness(2, 0, 2, 1),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        pNameText.Inlines.Add(new System.Windows.Documents.Run(
            pSectionUnnamed ? PSectionPlaceholderFormat(pSectionIndex) : pSectionEntry.LSegmentName)
        {
            Foreground = pSectionUnnamed ? pMutedBrush : pNameText.Foreground
        });

        PSectionAffixTextAdd(pNameText, pSectionEntry.LSegmentPrefix, pMutedBrush);
        PSectionAffixTextAdd(pNameText, pSectionEntry.LSegmentSuffix, pMutedBrush);
        return pNameText;
    }

    private static void PSectionAffixTextAdd(TextBlock pNameText, string pAffixValue, Brush pMutedBrush)
    {
        if (string.IsNullOrEmpty(pAffixValue))
        {
            return;
        }

        pNameText.Inlines.Add(new System.Windows.Documents.Run($"  /  {pAffixValue}") { Foreground = pMutedBrush });
    }

    private UIElement PSectionEditorBuild(LSegment pSectionEntry)
    {
        TextBox pNameBox = PSectionFieldBuild(pSectionEntry.LSegmentName, 0);
        TextBox pPrefixBox = PSectionFieldBuild(pSectionEntry.LSegmentPrefix, PSectionAffixWidth);
        TextBox pSuffixBox = PSectionFieldBuild(pSectionEntry.LSegmentSuffix, PSectionAffixWidth);
        pSectionNameBoxCurrent = pNameBox;
        pSectionPrefixBoxCurrent = pPrefixBox;
        pSectionSuffixBoxCurrent = pSuffixBox;

        UIElement pPrefixMark = PSectionMarkBuild(pPrefixBox);
        UIElement pSuffixMark = PSectionMarkBuild(pSuffixBox);
        PSectionAffixShow(pPrefixBox, !string.IsNullOrEmpty(pSectionEntry.LSegmentPrefix));
        PSectionAffixShow(pSuffixBox, !string.IsNullOrEmpty(pSectionEntry.LSegmentSuffix));

        PSectionStepWire(pNameBox, pPrefixBox);
        PSectionStepWire(pPrefixBox, pSuffixBox);
        PSectionStepWire(pSuffixBox, null);

        var pEditorPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pEditorPanel.Children.Add(pNameBox);
        pEditorPanel.Children.Add(pPrefixMark);
        pEditorPanel.Children.Add(pPrefixBox);
        pEditorPanel.Children.Add(pSuffixMark);
        pEditorPanel.Children.Add(pSuffixBox);

        pNameBox.Loaded += (_, _) =>
        {
            pNameBox.Focus();
            pNameBox.SelectAll();
        };
        return pEditorPanel;
    }

    private TextBox PSectionFieldBuild(string pFieldText, double pFieldWidth)
    {
        var pFieldBox = new TextBox
        {
            Text = pFieldText,
            MinWidth = 24,
            FontSize = PSectionNameSize,
            FontFamily = pSectionFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9)),
            Padding = new Thickness(2, 0, 2, 1),
            VerticalAlignment = VerticalAlignment.Center,
            FocusVisualStyle = null
        };

        if (pFieldWidth > 0)
        {
            pFieldBox.Width = pFieldWidth;
        }

        pFieldBox.LostFocus += (_, _) => PSectionEditLeave();
        pFieldBox.KeyDown += (_, pEvent) =>
        {
            if (pEvent.Key == Key.Return)
            {
                PSectionEditCommit();
                pEvent.Handled = true;
            }
            else if (pEvent.Key == Key.Escape)
            {
                PSectionEditCancel();
                pEvent.Handled = true;
            }
        };
        return pFieldBox;
    }

    private static UIElement PSectionMarkBuild(TextBox pAffixBox)
    {
        var pMark = new TextBlock
        {
            Text = "/",
            Margin = new Thickness(5, 0, 5, 0),
            FontSize = PSectionNameSize,
            FontFamily = pSectionFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E)),
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed
        };
        pAffixBox.Tag = pMark;
        return pMark;
    }

    private static void PSectionAffixShow(TextBox pAffixBox, bool pAffixVisible)
    {
        pAffixBox.Visibility = pAffixVisible ? Visibility.Visible : Visibility.Collapsed;
        if (pAffixBox.Tag is UIElement pMark)
        {
            pMark.Visibility = pAffixBox.Visibility;
        }
    }

    private static void PSectionStepWire(TextBox pFieldBox, TextBox? pNextBox)
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

            PSectionAffixShow(pNextBox, true);
            pNextBox.Focus();
            Keyboard.Focus(pNextBox);
            pNextBox.SelectAll();
        };
    }

    private void PSectionEditLeave()
    {
        if (pSectionRebuilding)
        {
            return;
        }

        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() =>
        {
            if (pSectionRebuilding || PSectionEditFocusCheck())
            {
                return;
            }

            PSectionEditCommit();
        }));
    }

    private bool PSectionEditFocusCheck()
    {
        return ReferenceEquals(Keyboard.FocusedElement, pSectionNameBoxCurrent)
            || ReferenceEquals(Keyboard.FocusedElement, pSectionPrefixBoxCurrent)
            || ReferenceEquals(Keyboard.FocusedElement, pSectionSuffixBoxCurrent);
    }

    private void PSectionEditCancel()
    {
        pSectionIndexEditing = null;
        pSectionNameBoxCurrent = null;
        pSectionPrefixBoxCurrent = null;
        pSectionSuffixBoxCurrent = null;
        PSectionRebuild();
    }

    private static string PSectionPlaceholderFormat(int pSectionIndex) => $"Section {pSectionIndex + 1}";

    private static TimeSpan PSectionSpanRead(LSegment pSectionEntry)
    {
        TimeSpan pSectionSpan = pSectionEntry.LSegmentEnd - pSectionEntry.LSegmentStart;
        return pSectionSpan < TimeSpan.Zero ? TimeSpan.Zero : pSectionSpan;
    }

    private static string PSectionTimeFormat(TimeSpan pTime) =>
        pTime.TotalHours >= 1
            ? $"{(int)pTime.TotalHours}:{pTime.Minutes:D2}:{pTime.Seconds:D2}"
            : $"{pTime.Minutes}:{pTime.Seconds:D2}";
}
