using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PGroup : PPanel
{
    internal const string PGroupMoveKind = "CadroueGroupMove";

    private static readonly FontFamily pGroupFontFamily = new("Segoe UI");
    private static readonly Brush pGroupLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pGroupTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pGroupMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pGroupIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pGroupActiveBrush = new SolidColorBrush(Color.FromRgb(0xCE, 0xE1, 0xFB));

    private static readonly IReadOnlyDictionary<bool, Brush> pGroupSegmentBrushes = new Dictionary<bool, Brush>
    {
        [true] = pGroupTitleBrush,
        [false] = pGroupMutedBrush,
    };

    private static readonly IReadOnlyDictionary<bool, Brush> pGroupSegmentFills = new Dictionary<bool, Brush>
    {
        [true] = pGroupActiveBrush,
        [false] = Brushes.Transparent,
    };

    private static readonly IReadOnlyDictionary<bool, DragDropEffects> pGroupEffects =
        new Dictionary<bool, DragDropEffects>
        {
            [true] = DragDropEffects.Move,
            [false] = DragDropEffects.Copy,
        };

    private static readonly IReadOnlyDictionary<bool, Func<PGroup, UIElement>> pGroupTrailings =
        new Dictionary<bool, Func<PGroup, UIElement>>
        {
            [true] = pGroup => pGroup.PGroupSwitchesBuild(),
            [false] = pGroup => pGroup.PGroupManualBuild(),
        };

    private readonly StackPanel pGroupRowPanel;
    private readonly TextBlock pGroupEmptyNotice;
    private readonly Border pGroupActionHost;
    private readonly UIElement pGroupFullBody;
    private readonly UIElement pGroupStripBody;

    public LGroup LGroup { get; }

    public PGroup(LGroup lGroup) : base("")
    {
        LGroup = lGroup;
        pGroupRowPanel = new StackPanel();
        pGroupActionHost = new Border
        {
            BorderBrush = pGroupLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White
        };

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
        pScroll.DragOver += PGroupOverHandle;
        pScroll.Drop += PGroupDropHandle;

        var pRoot = new DockPanel { LastChildFill = true };
        UIElement pHeader = PGroupHeaderBuild();
        DockPanel.SetDock(pHeader, Dock.Top);
        DockPanel.SetDock(pGroupActionHost, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pGroupActionHost);
        pRoot.Children.Add(pScroll);

        pGroupFullBody = pRoot;
        pGroupStripBody = PGroupStripBuild();
        pGroupStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pGroupFullBody);
        pBodyHost.Children.Add(pGroupStripBody);

        Content = PPanelBorderBuild(pBodyHost);
        PGroupActionUpdate();
        PGroupRebuild();
        LGroup.LGroupChange += PGroupRebuild;
        LGroup.LGroupFaceChange += PGroupActionUpdate;
        LGroup.LGroupMinimizeChange += PGroupMinimizeHandle;
        Loaded += PGroupLoadedHandle;
    }

    private void PGroupLoadedHandle(object pSender, RoutedEventArgs pEvent) => LGroup.LGroupAutoUpdate();

    private void PGroupRebuild()
    {
        pGroupRowPanel.Children.Clear();
        LGroup.LGroupFace.LGroupCardsRead()
            .Select(PGroupCardBuild)
            .ToList()
            .ForEach(pCard => pGroupRowPanel.Children.Add(pCard));
        pGroupEmptyNotice.Visibility = PLook.PLookVisible[LGroup.LGroupEmpty];
    }

    private Border PGroupCardBuild(LGroupCard lCard) => new PGroupCard(this, lCard).PGroupCardBorder;

    private void PGroupMinimizeHandle(bool pGroupMinimized)
    {
        pGroupFullBody.Visibility = PLook.PLookVisible[!pGroupMinimized];
        pGroupStripBody.Visibility = PLook.PLookVisible[pGroupMinimized];
    }

    private void PGroupActionUpdate() => pGroupActionHost.Child = PGroupContentBuild();

    private UIElement PGroupContentBuild()
    {
        var pActionGrid = new Grid { Margin = new Thickness(10, 4, 10, 6), MinHeight = 26 };
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Border pModeToggle = PGroupToggleBuild(LGroup.LGroupFace.LGroupModeRead());
        pModeToggle.HorizontalAlignment = HorizontalAlignment.Left;
        Grid.SetColumn(pModeToggle, 0);
        pActionGrid.Children.Add(pModeToggle);

        UIElement pTrailing = pGroupTrailings[LGroup.LGroupAuto](this);
        Grid.SetColumn(pTrailing, 2);
        pActionGrid.Children.Add(pTrailing);
        return pActionGrid;
    }

    private StackPanel PGroupSwitchesBuild()
    {
        var pSwitches = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        LGroup.LGroupFace.LGroupSwitchesRead()
            .Select(PGroupSwitchBuild)
            .ToList()
            .ForEach(pToggle => pSwitches.Children.Add(pToggle));
        return pSwitches;
    }

    private Border PGroupSwitchBuild(LGroupToggle lToggle)
    {
        Border pToggle = PGroupToggleBuild(lToggle);
        pToggle.Margin = new Thickness(8, 0, 0, 0);
        return pToggle;
    }

    private StackPanel PGroupManualBuild()
    {
        var pButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        pButtons.Children.Add(PGroupButtonBuild(
            "/PAsset/PPanel/PGroupStrict.svg",
            LLocalization.LLocalizationTextRead("Group.Strict.Tooltip"),
            (_, _) => LGroup.LGroupStrictApply()));
        pButtons.Children.Add(PGroupButtonBuild(
            "/PAsset/PPanel/PGroupLoose.svg",
            LLocalization.LLocalizationTextRead("Group.Loose.Tooltip"),
            (_, _) => LGroup.LGroupLooseApply()));
        Button pSortButton = PGroupButtonBuild(
            "/PAsset/PPanel/PSort.svg",
            LLocalization.LLocalizationTextRead("Group.Sort.Tooltip"),
            (_, _) => LGroup.LGroupSort());
        pSortButton.Margin = new Thickness(0);
        pButtons.Children.Add(pSortButton);
        return pButtons;
    }

    private Border PGroupToggleBuild(LGroupToggle lToggle)
    {
        var pRow = new StackPanel { Orientation = Orientation.Horizontal };
        pRow.Children.Add(PGroupSegmentBuild(
            lToggle.LGroupToggleLeft, () => LGroup.LGroupFace.LGroupToggleRun(lToggle.LGroupToggleKey, false)));
        pRow.Children.Add(new Border { Width = 1, Background = pGroupLineBrush });
        pRow.Children.Add(PGroupSegmentBuild(
            lToggle.LGroupToggleRight, () => LGroup.LGroupFace.LGroupToggleRun(lToggle.LGroupToggleKey, true)));

        return new Border
        {
            BorderBrush = pGroupLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            SnapsToDevicePixels = true,
            Child = pRow
        };
    }

    private static Border PGroupSegmentBuild(LGroupSide lSide, Action pGroupClick)
    {
        var pLabel = new TextBlock
        {
            Text = lSide.LGroupSideText,
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            FontWeight = PLook.PLookWeight[lSide.LGroupSideActive],
            Foreground = pGroupSegmentBrushes[lSide.LGroupSideActive],
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var pSegment = new Border
        {
            Background = pGroupSegmentFills[lSide.LGroupSideActive],
            Padding = new Thickness(12, 3, 12, 3),
            Cursor = Cursors.Hand,
            ToolTip = lSide.LGroupSideTip,
            Child = pLabel
        };
        pSegment.MouseLeftButtonUp += (_, _) => pGroupClick();
        return pSegment;
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
            "/PAsset/PPanel/PListMinimize.svg",
            LLocalization.LLocalizationTextRead("Group.Panel.HideTooltip"),
            (_, _) => LGroup.LGroupMinimizedSet(true));
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

    private UIElement PGroupStripBuild()
    {
        Button pMaximizeButton = PGroupButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            LLocalization.LLocalizationTextRead("Group.Panel.ShowTooltip"),
            (_, _) => LGroup.LGroupMinimizedSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    internal static Button PGroupButtonBuild(string pIconPath, string pTooltip, RoutedEventHandler pClick)
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

    internal static void PGroupOverHandle(object pSender, DragEventArgs pEvent)
    {
        pEvent.Effects = pGroupEffects[pEvent.Data.GetDataPresent(PGroupMoveKind)];
        pEvent.Handled = true;
    }

    private void PGroupDropHandle(object pSender, DragEventArgs pEvent)
    {
        var pMove = pEvent.Data.GetData(PGroupMoveKind) as PGroupMovePayload;
        pEvent.Handled = LGroup.LGroupDrag.LGroupPanelAccept(
            pEvent.Handled,
            pEvent.Data.GetData(DataFormats.FileDrop) as string[],
            pMove?.PGroupMoveIndex,
            pMove?.PGroupMovePath,
            pEvent.Data.GetData(PList.PListDragKind) as string[]);
    }

    internal sealed record PGroupMovePayload(int PGroupMoveIndex, string PGroupMovePath);
}
