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

public sealed class PSection : UserControl
{
    private static readonly Color[] pSectionPaletteColors =
    {
        Color.FromRgb(0x4A, 0x90, 0xD9),
        Color.FromRgb(0x27, 0xAE, 0x60),
        Color.FromRgb(0xE6, 0x7E, 0x22),
        Color.FromRgb(0x8E, 0x44, 0xAD),
        Color.FromRgb(0xE7, 0x4C, 0x3C),
        Color.FromRgb(0x16, 0xA0, 0x85),
    };

    private static readonly FontFamily pSectionFontFamily = new("Segoe UI");

    internal const double PSectionNameSize = 12;

    private PFlowControl? pFlowAttached;
    private IReadOnlyList<LSegment> pSectionListCurrent = Array.Empty<LSegment>();
    private int? pSectionIndexSelectCurrent;
    private readonly TextBlock pSectionCountLabel;
    private readonly StackPanel pSectionRowPanel;
    private int? pSectionIndexEditing;
    private TextBox? pSectionNameBoxCurrent;
    private bool pSectionRebuilding;
    private int? pSectionIndexDragging;
    private Point? pSectionDragStart;
    private bool pSectionDragActive;

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

        var pHeader = new Border
        {
            Padding = new Thickness(12, 10, 12, 10),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pSectionCountLabel
        };

        pSectionRowPanel = new StackPanel();

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

        FocusVisualStyle = null;
        Content = PPanel.PPanelBorderBuild(pRoot);
    }

    private UIElement PSectionActionBuild()
    {
        var pActionPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(10, 4, 10, 4)
        };
        pActionPanel.Children.Add(PSectionButtonBuild(
            "/PAssets/PPanels/PExportMinus.svg",
            "Delete the selected section",
            PSectionDeleteHandle));

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
                pSection.LSegmentName))
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
        pSectionListCurrent = pSectionList;
        pSectionIndexSelectCurrent = pSectionIndexSelect;

        if (pSectionDragActive)
        {
            return;
        }

        PSectionRebuild();
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

    private void PSectionDragClear()
    {
        pSectionIndexDragging = null;
        pSectionDragStart = null;
        pSectionDragActive = false;
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
        return true;
    }

    private void PSectionEditCommit()
    {
        if (pSectionIndexEditing is not int pEditingIndex || pSectionNameBoxCurrent is not { } pEditingBox)
        {
            return;
        }

        pSectionIndexEditing = null;
        pSectionNameBoxCurrent = null;
        pFlowAttached?.PFlowNameSet(pEditingIndex, pEditingBox.Text.Trim());
        PSectionRebuild();
    }

    private Border PSectionRowBuild(int pSectionIndex, LSegment pSectionEntry, bool pSectionSelected)
    {
        int capturedIndex = pSectionIndex;

        Color pColor = pSectionPaletteColors[Math.Abs(pSectionEntry.LSegmentColorIndex) % pSectionPaletteColors.Length];

        var pColorDot = new Border
        {
            Width = 10,
            Height = 10,
            CornerRadius = new CornerRadius(5),
            Background = new SolidColorBrush(pColor),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        UIElement pNameHost = pSectionIndex == pSectionIndexEditing
            ? PSectionNameBoxBuild(capturedIndex, pSectionEntry.LSegmentName)
            : PSectionNameTextBuild(pSectionIndex, pSectionEntry.LSegmentName);

        var pTimeLabel = new TextBlock
        {
            Text = $"{PSectionTimeFormat(pSectionEntry.LSegmentStart)} → {PSectionTimeFormat(pSectionEntry.LSegmentEnd)}",
            FontSize = 11,
            FontFamily = pSectionFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x56, 0x62, 0x73)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };

        var pRowContent = new Grid();
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
            Child = pRowContent
        };
        pRowBorder.PreviewMouseLeftButtonDown += (_, pEvent) =>
        {
            pSectionIndexDragging = pSectionRowPanel.Children.IndexOf(pRowBorder);
            pSectionDragStart = pEvent.GetPosition(pRowBorder);
            pSectionDragActive = false;
            pRowBorder.CaptureMouse();
        };
        pRowBorder.MouseLeftButtonDown += (_, pEvent) =>
        {
            if (pEvent.ClickCount < 2)
            {
                return;
            }

            pRowBorder.ReleaseMouseCapture();
            PSectionDragClear();
            pFlowAttached?.PFlowSectionSelect(capturedIndex);
            pSectionIndexEditing = pSectionRowPanel.Children.IndexOf(pRowBorder);
            PSectionRebuild();
            pEvent.Handled = true;
        };
        pRowBorder.PreviewMouseMove += (_, pEvent) =>
        {
            if (pSectionIndexDragging is not int pDragIndex
                || pSectionIndexEditing is not null
                || pSectionDragStart is not Point pStart
                || pEvent.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point pCurrent = pEvent.GetPosition(pRowBorder);
            if (Math.Abs(pCurrent.X - pStart.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(pCurrent.Y - pStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            pSectionDragActive = true;
            pRowBorder.Opacity = 0.72;
            if (PSectionMoveLive(pDragIndex, PSectionIndexResolve(pEvent.GetPosition(pSectionRowPanel)), pRowBorder))
            {
                pSectionDragStart = pEvent.GetPosition(pRowBorder);
            }

            pEvent.Handled = true;
        };
        pRowBorder.MouseLeftButtonUp += (_, pEvent) =>
        {
            pRowBorder.ReleaseMouseCapture();
            if (pSectionDragActive)
            {
                pRowBorder.Opacity = 1;
                PSectionDragClear();
                PSectionRebuild();
                pEvent.Handled = true;
                return;
            }

            PSectionDragClear();

            if (pSectionIndexEditing != pSectionRowPanel.Children.IndexOf(pRowBorder))
            {
                PSectionEditCommit();
                pFlowAttached?.PFlowSectionSelect(capturedIndex);
            }

            pEvent.Handled = true;
        };
        pRowBorder.LostMouseCapture += (_, _) =>
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                return;
            }

            pRowBorder.Opacity = 1;
            PSectionDragClear();
        };

        return pRowBorder;
    }

    private TextBlock PSectionNameTextBuild(int pSectionIndex, string pSectionName)
    {
        bool pSectionUnnamed = string.IsNullOrEmpty(pSectionName);
        return new TextBlock
        {
            Text = pSectionUnnamed ? PSectionPlaceholderFormat(pSectionIndex) : pSectionName,
            FontSize = PSectionNameSize,
            FontFamily = pSectionFontFamily,
            Foreground = new SolidColorBrush(pSectionUnnamed
                ? Color.FromRgb(0x8A, 0x93, 0x9E)
                : Color.FromRgb(0x11, 0x18, 0x27)),
            Padding = new Thickness(2, 0, 2, 1),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private TextBox PSectionNameBoxBuild(int pSectionIndex, string pSectionName)
    {
        var pNameBox = new TextBox
        {
            Text = pSectionName,
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
        pSectionNameBoxCurrent = pNameBox;
        pNameBox.Loaded += (_, _) =>
        {
            pNameBox.Focus();
            pNameBox.SelectAll();
        };
        pNameBox.LostFocus += (_, _) =>
        {
            if (pSectionRebuilding)
            {
                return;
            }

            PSectionEditCommit();
        };
        pNameBox.KeyDown += (_, pEvent) =>
        {
            if (pEvent.Key == Key.Return)
            {
                PSectionEditCommit();
                pEvent.Handled = true;
            }
            else if (pEvent.Key == Key.Escape)
            {
                pSectionIndexEditing = null;
                pSectionNameBoxCurrent = null;
                PSectionRebuild();
                pEvent.Handled = true;
            }
        };
        return pNameBox;
    }

    private static string PSectionPlaceholderFormat(int pSectionIndex) => $"Section {pSectionIndex + 1}";

    private static string PSectionTimeFormat(TimeSpan pTime) =>
        pTime.TotalHours >= 1
            ? $"{(int)pTime.TotalHours}:{pTime.Minutes:D2}:{pTime.Seconds:D2}"
            : $"{pTime.Minutes}:{pTime.Seconds:D2}";
}
