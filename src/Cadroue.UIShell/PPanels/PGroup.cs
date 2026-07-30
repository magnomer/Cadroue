using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PGroup : PPanel
{
    private static readonly FontFamily pGroupFontFamily = new("Segoe UI");
    private static readonly Brush pGroupLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pGroupTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pGroupRowBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly Brush pGroupMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pGroupCardBrush = new SolidColorBrush(Color.FromRgb(0xF6, 0xF8, 0xFB));
    private static readonly Brush pGroupIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));

    private readonly List<PGroupRecord> pGroupRecords = [];
    private readonly StackPanel pGroupRowPanel;
    private readonly TextBlock pGroupEmptyNotice;
    private readonly UIElement pGroupFullBody;
    private readonly UIElement pGroupStripBody;
    private bool pGroupMinimized;

    public Func<IReadOnlyList<string>, IReadOnlyList<string>>? PGroupFileLoad { get; set; }

    public Func<IReadOnlyList<string>>? PGroupSourceFiles { get; set; }

    public event Action<string>? PGroupItemOpen;

    public PGroup() : base("")
    {
        UIElement pHeader = PGroupHeaderBuild();

        pGroupRowPanel = new StackPanel();

        pGroupEmptyNotice = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Group.Empty.Notice"),
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            Foreground = pGroupMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 24, 16, 16),
            IsHitTestVisible = false
        };

        var pBody = new Grid();
        pBody.Children.Add(pGroupEmptyNotice);
        pBody.Children.Add(pGroupRowPanel);

        var pScroll = new ScrollViewer
        {
            Content = pBody,
            Background = Brushes.Transparent,
            AllowDrop = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        pScroll.DragOver += PGroupDragOverHandle;
        pScroll.Drop += PGroupContainerDropHandle;

        var pRoot = new DockPanel { LastChildFill = true };
        UIElement pActionBar = PGroupActionBuild();
        DockPanel.SetDock(pHeader, Dock.Top);
        DockPanel.SetDock(pActionBar, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pActionBar);
        pRoot.Children.Add(pScroll);

        pGroupFullBody = pRoot;
        pGroupStripBody = PGroupStripBuild();
        pGroupStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pGroupFullBody);
        pBodyHost.Children.Add(pGroupStripBody);

        Content = PPanelBorderBuild(pBodyHost);
        PGroupRebuild();
    }

    private void PGroupSort()
    {
        if (pGroupRecords.Count == 0)
        {
            return;
        }

        foreach (PGroupRecord pRecord in pGroupRecords)
        {
            pRecord.PGroupRecordPaths.Sort((pLeft, pRight) =>
                string.Compare(Path.GetFileName(pLeft), Path.GetFileName(pRight), StringComparison.OrdinalIgnoreCase));
        }

        PGroupRebuild();
    }

    public void PGroupClear()
    {
        if (pGroupRecords.Count == 0)
        {
            return;
        }

        pGroupRecords.Clear();
        PGroupRebuild();
    }

    public IReadOnlyList<PGroupSelection> PGroupGroupsRead() =>
        pGroupRecords
            .Select(pRecord => new PGroupSelection(pRecord.PGroupRecordName, pRecord.PGroupRecordPaths.ToArray()))
            .ToArray();

    private void PGroupMinimizeSet(bool pGroupMinimizeRequest)
    {
        if (pGroupMinimized == pGroupMinimizeRequest)
        {
            return;
        }

        pGroupMinimized = pGroupMinimizeRequest;
        pGroupFullBody.Visibility = pGroupMinimized ? Visibility.Collapsed : Visibility.Visible;
        pGroupStripBody.Visibility = pGroupMinimized ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PGroupRebuild()
    {
        pGroupRowPanel.Children.Clear();
        for (int pIndex = 0; pIndex < pGroupRecords.Count; pIndex++)
        {
            pGroupRowPanel.Children.Add(PGroupCardBuild(pIndex, pGroupRecords[pIndex]));
        }

        pGroupEmptyNotice.Visibility = pGroupRecords.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private Border PGroupCardBuild(int pGroupIndex, PGroupRecord pRecord)
    {
        var pFileRows = new StackPanel();
        for (int pOrderIndex = 0; pOrderIndex < pRecord.PGroupRecordPaths.Count; pOrderIndex++)
        {
            pFileRows.Children.Add(PGroupFileRowBuild(pGroupIndex, pOrderIndex, pRecord.PGroupRecordPaths[pOrderIndex]));
        }

        var pCardBody = new StackPanel();
        pCardBody.Children.Add(PGroupCardHeaderBuild(pGroupIndex, pRecord));
        pCardBody.Children.Add(pFileRows);

        var pCard = new Border
        {
            Margin = new Thickness(8, 8, 8, 0),
            Padding = new Thickness(0, 0, 0, 6),
            CornerRadius = new CornerRadius(8),
            BorderBrush = pGroupLineBrush,
            BorderThickness = new Thickness(1),
            Background = pGroupCardBrush,
            AllowDrop = true,
            Child = pCardBody,
            Tag = pFileRows
        };
        pCard.DragOver += PGroupDragOverHandle;
        pCard.Drop += (pSender, pEvent) => PGroupCardDropHandle(pGroupIndex, pFileRows, pEvent);
        return pCard;
    }

    private UIElement PGroupCardHeaderBuild(int pGroupIndex, PGroupRecord pRecord)
    {
        var pHeaderGrid = new Grid { Margin = new Thickness(10, 4, 4, 4) };
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pNameLabel = new TextBlock
        {
            Text = pRecord.PGroupRecordName,
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pGroupTitleBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            ToolTip = LLocalization.LLocalizationTextRead("Group.Rename.Tooltip")
        };
        pNameLabel.MouseLeftButtonDown += (_, pNameEvent) =>
        {
            if (pNameEvent.ClickCount == 2)
            {
                PGroupNameEditBegin(pGroupIndex, pHeaderGrid, pRecord);
                pNameEvent.Handled = true;
            }
        };

        Button pRemoveButton = PGroupButtonBuild(
            "/PAssets/PPanels/PExportMinus.svg",
            LLocalization.LLocalizationTextRead("Group.Remove.Tooltip"),
            (_, _) => PGroupRemove(pGroupIndex));
        pRemoveButton.HorizontalAlignment = HorizontalAlignment.Right;

        Grid.SetColumn(pRemoveButton, 1);
        pHeaderGrid.Children.Add(pNameLabel);
        pHeaderGrid.Children.Add(pRemoveButton);
        return pHeaderGrid;
    }

    private void PGroupNameEditBegin(int pGroupIndex, Grid pHeaderGrid, PGroupRecord pRecord)
    {
        var pNameBox = new TextBox
        {
            Text = pRecord.PGroupRecordName,
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(2, 0, 2, 0),
            Margin = new Thickness(0, 0, 6, 0)
        };
        Grid.SetColumn(pNameBox, 0);

        if (pHeaderGrid.Children.Count > 0 && pHeaderGrid.Children[0] is TextBlock pNameLabel)
        {
            pHeaderGrid.Children.Remove(pNameLabel);
        }

        pHeaderGrid.Children.Add(pNameBox);
        pNameBox.Loaded += (_, _) =>
        {
            pNameBox.Focus();
            pNameBox.SelectAll();
        };

        bool pNameCommitted = false;
        void PGroupNameCommit(bool pNameApply)
        {
            if (pNameCommitted)
            {
                return;
            }

            pNameCommitted = true;
            if (pNameApply)
            {
                string pNameTrimmed = pNameBox.Text.Trim();
                if (pNameTrimmed.Length > 0)
                {
                    pRecord.PGroupRecordName = pNameTrimmed;
                }
            }

            PGroupRebuild();
        }

        pNameBox.KeyDown += (_, pKeyEvent) =>
        {
            if (pKeyEvent.Key == Key.Enter)
            {
                PGroupNameCommit(true);
                pKeyEvent.Handled = true;
            }
            else if (pKeyEvent.Key == Key.Escape)
            {
                PGroupNameCommit(false);
                pKeyEvent.Handled = true;
            }
        };
        pNameBox.LostKeyboardFocus += (_, _) => PGroupNameCommit(true);
    }

    private Border PGroupFileRowBuild(int pGroupIndex, int pOrderIndex, string pPath)
    {
        var pRowContent = new StackPanel { Orientation = Orientation.Horizontal };
        pRowContent.Children.Add(new TextBlock
        {
            Text = (pOrderIndex + 1).ToString(),
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pGroupMutedBrush,
            Width = 18,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
        pRowContent.Children.Add(new TextBlock
        {
            Text = Path.GetFileName(pPath),
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            Foreground = pGroupRowBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var pRowBorder = new Border
        {
            Padding = new Thickness(20, 6, 12, 6),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            ToolTip = pPath,
            Child = pRowContent,
            Tag = pPath
        };
        pRowBorder.MouseLeftButtonDown += (_, pRowEvent) =>
        {
            pGroupDragStart = pRowEvent.GetPosition(null);
            pGroupDragSourceIndex = pGroupIndex;
            pGroupDragPath = pPath;
            pRowBorder.CaptureMouse();
            PGroupItemOpen?.Invoke(pPath);
        };
        pRowBorder.MouseMove += (pRowSender, pRowEvent) => PGroupRowDragHandle(pRowSender, pRowEvent);
        pRowBorder.MouseLeftButtonUp += (_, _) =>
        {
            pRowBorder.ReleaseMouseCapture();
            PGroupDragClear();
        };
        return pRowBorder;
    }

    private void PGroupRemove(int pGroupIndex)
    {
        if (pGroupIndex < 0 || pGroupIndex >= pGroupRecords.Count)
        {
            return;
        }

        pGroupRecords.RemoveAt(pGroupIndex);
        PGroupRebuild();
    }

    private UIElement PGroupHeaderBuild()
    {
        var pTitleLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Group.Header.Title"),
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pGroupTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PGroupButtonBuild(
            "/PAssets/PPanels/PListMinimize.svg",
            LLocalization.LLocalizationTextRead("Group.Panel.HideTooltip"),
            (_, _) => PGroupMinimizeSet(true));
        pMinimizeButton.Margin = new Thickness(0);
        pMinimizeButton.HorizontalAlignment = HorizontalAlignment.Right;

        var pHeaderGrid = new Grid();
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pMinimizeButton, 1);
        pHeaderGrid.Children.Add(pTitleLabel);
        pHeaderGrid.Children.Add(pMinimizeButton);

        return new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            BorderBrush = pGroupLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
        };
    }

    private UIElement PGroupActionBuild()
    {
        var pLeftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pLeftPanel.Children.Add(PGroupButtonBuild(
            "/PAssets/PPanels/PGroupStrict.svg",
            LLocalization.LLocalizationTextRead("Group.Strict.Tooltip"),
            (_, _) => PGroupStrictApply()));
        pLeftPanel.Children.Add(PGroupButtonBuild(
            "/PAssets/PPanels/PGroupLoose.svg",
            LLocalization.LLocalizationTextRead("Group.Loose.Tooltip"),
            (_, _) => PGroupLooseApply()));

        var pRightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Button pSortButton = PGroupButtonBuild(
            "/PAssets/PPanels/PSort.svg",
            LLocalization.LLocalizationTextRead("Group.Sort.Tooltip"),
            (_, _) => PGroupSort());
        pSortButton.Margin = new Thickness(0);
        pRightPanel.Children.Add(pSortButton);

        var pActionGrid = new Grid { Margin = new Thickness(10, 4, 10, 6), MinHeight = 26 };
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pLeftPanel, 0);
        Grid.SetColumn(pRightPanel, 2);
        pActionGrid.Children.Add(pLeftPanel);
        pActionGrid.Children.Add(pRightPanel);

        return new Border
        {
            BorderBrush = pGroupLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pActionGrid
        };
    }

    private UIElement PGroupStripBuild()
    {
        Button pMaximizeButton = PGroupButtonBuild(
            "/PAssets/PPanels/PListMaximize.svg",
            LLocalization.LLocalizationTextRead("Group.Panel.ShowTooltip"),
            (_, _) => PGroupMinimizeSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    private static Button PGroupButtonBuild(string pIconPath, string pTooltip, RoutedEventHandler pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, pGroupIconBrush),
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

    private sealed class PGroupRecord
    {
        public string PGroupRecordName { get; set; } = string.Empty;

        public List<string> PGroupRecordPaths { get; } = [];
    }

    public sealed record PGroupSelection(string PGroupSelectionName, IReadOnlyList<string> PGroupSelectionPaths);
}
