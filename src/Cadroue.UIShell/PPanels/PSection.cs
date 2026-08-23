using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
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

    private const double PSectionActionGap = 16;
    private const double PSectionBadgeSize = 18;
    private const double PSectionBadgePadding = 6;
    private const double PSectionDisabledOpacity = 0.4;

    private PFlowControl? pFlowAttached;
    private IReadOnlyList<LPiece> pSectionListCurrent = Array.Empty<LPiece>();
    private HashSet<int> pSectionSelectedCurrent = new();
    private readonly TextBlock pSectionCountLabel;
    private readonly StackPanel pSectionRowPanel;
    private bool pSectionRebuilding;

    public PSection()
    {
        pSectionCountLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Section.Header.Title"),
            FontSize = 12,
            FontFamily = pSectionFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            VerticalAlignment = VerticalAlignment.Center
        };

        UIElement pHeader = PSectionHeaderBuild();

        pSectionRowPanel = new StackPanel();
        pSectionRowPanel.PreviewMouseMove += PSectionMoveHandle;
        pSectionRowPanel.MouseLeftButtonUp += PSectionUpHandle;
        pSectionRowPanel.LostMouseCapture += PSectionLostHandle;

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
        PScrollbar.PScrollbarApply(this);
        Content = PPanel.PPanelBorderBuild(pBodyHost);
    }

    private UIElement PSectionActionBuild()
    {
        var pActionLeft = new StackPanel { Orientation = Orientation.Horizontal };
        pActionLeft.Children.Add(PSectionButtonBuild(
            "/PAssets/PPanels/PSort.svg",
            LLocalization.LLocalizationTextRead("Section.Sort.Tooltip"),
            PSectionSortHandle));

        Button pSectionRemoveAllButton = PSectionButtonBuild(
            "/PAssets/PPanels/PListRemoveAll.svg",
            LLocalization.LLocalizationTextRead("Section.RemoveAll.Tooltip"),
            PSectionClearHandle);
        pSectionRemoveAllButton.Margin = new Thickness(PSectionActionGap, 0, 0, 0);
        var pActionRight = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pActionRight.Children.Add(PSectionButtonBuild(
            "/PAssets/PPanels/PExportMinus.svg",
            LLocalization.LLocalizationTextRead("Section.Delete.Tooltip"),
            PSectionDeleteHandle));
        pActionRight.Children.Add(pSectionRemoveAllButton);

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

    private void PSectionClearHandle(object pSender, RoutedEventArgs pEvent)
    {
        PSectionEditCommit();
        pFlowAttached?.PFlowSectionClear();
    }

    public void PSectionAttach(PFlowControl pFlow)
    {
        PSectionDetach();
        pFlowAttached = pFlow;
        pFlowAttached.PFlowSectionChange += PSectionUpdateHandle;
        PSectionRebuild();
    }

    private void PSectionDetach()
    {
        if (pFlowAttached is null) return;
        pFlowAttached.PFlowSectionChange -= PSectionUpdateHandle;
        pFlowAttached = null;
    }

    private void PSectionUpdateHandle(IReadOnlyList<LPiece> pSectionList, int? pSectionIndexSelect)
    {
        LPiece[] pSectionListNext = pSectionList.ToArray();
        bool pSectionListSame = pSectionListCurrent.SequenceEqual(pSectionListNext)
            && pSectionRowPanel.Children.Count == pSectionListNext.Length;
        pSectionListCurrent = pSectionListNext;
        pSectionSelectedCurrent = pFlowAttached is null
            ? new HashSet<int>()
            : new HashSet<int>(pFlowAttached.PFlowSelectedRead());

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
                pRow.Background = pSectionSelectedCurrent.Contains(pIndex)
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
            pSectionCountLabel.Text = pCount == 0 ? LLocalization.LLocalizationTextRead("Section.Header.Title") : LLocalization.LLocalizationFormat("Section.Header.Count", pCount);
            for (int i = 0; i < pCount; i++)
            {
                pSectionRowPanel.Children.Add(PSectionRowBuild(i, pSectionListCurrent[i], pSectionSelectedCurrent.Contains(i)));
            }
        }
        finally
        {
            pSectionRebuilding = false;
        }
    }

    private Border PSectionRowBuild(int pSectionIndex, LPiece pSectionEntry, bool pSectionSelected)
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
            Background = PSectionPalette.PSectionBadgeRead(pSectionEntry.LPieceColorIndex),
            Padding = new Thickness(PSectionBadgePadding, 0, PSectionBadgePadding, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            ToolTip = LLocalization.LLocalizationTextRead("Section.Toggle.Tooltip"),
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
            : PSectionTextBuild(pSectionIndex, pSectionEntry);

        var pTimeLabel = new TextBlock
        {
            Text = $"{PSectionTimeFormat(pSectionEntry.LPieceOrigin)} → {PSectionTimeFormat(pSectionEntry.LPieceEnd)}"
                + $"  ({PSectionTimeFormat(PSectionSpanRead(pSectionEntry))})",
            FontSize = 11,
            FontFamily = pSectionFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x56, 0x62, 0x73)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };

        var pRowContent = new Grid
        {
            Opacity = pSectionEntry.LPieceHidden ? PSectionDisabledOpacity : 1
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
            pSectionDragOrigin = pEvent.GetPosition(pSectionRowPanel);
            pSectionGrabOffset = pEvent.GetPosition(pRowBorder);
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

    private TextBlock PSectionTextBuild(int pSectionIndex, LPiece pSectionEntry)
    {
        bool pSectionUnnamed = string.IsNullOrEmpty(pSectionEntry.LPieceName);
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
            pSectionUnnamed ? PSectionPlaceholderFormat(pSectionIndex) : pSectionEntry.LPieceName)
        {
            Foreground = pSectionUnnamed ? pMutedBrush : pNameText.Foreground
        });

        PSectionAffixAdd(pNameText, pSectionEntry.LPiecePrefix, pMutedBrush);
        PSectionAffixAdd(pNameText, pSectionEntry.LPieceSuffix, pMutedBrush);
        return pNameText;
    }

    private static void PSectionAffixAdd(TextBlock pNameText, string pAffixValue, Brush pMutedBrush)
    {
        if (string.IsNullOrEmpty(pAffixValue))
        {
            return;
        }

        pNameText.Inlines.Add(new System.Windows.Documents.Run($"  /  {pAffixValue}") { Foreground = pMutedBrush });
    }

    private static string PSectionPlaceholderFormat(int pSectionIndex) => LLocalization.LLocalizationFormat("Section.DefaultName", pSectionIndex + 1);

    private static TimeSpan PSectionSpanRead(LPiece pSectionEntry)
    {
        TimeSpan pSectionSpan = pSectionEntry.LPieceEnd - pSectionEntry.LPieceOrigin;
        return pSectionSpan < TimeSpan.Zero ? TimeSpan.Zero : pSectionSpan;
    }

    private static string PSectionTimeFormat(TimeSpan pTime) =>
        pTime.TotalHours >= 1
            ? $"{(int)pTime.TotalHours}:{pTime.Minutes:D2}:{pTime.Seconds:D2}"
            : $"{pTime.Minutes}:{pTime.Seconds:D2}";
}
