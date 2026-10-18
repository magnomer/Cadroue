using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PGroup : UserControl
{
    private static readonly FontFamily pGroupFontFamily = new("Segoe UI");
    private static readonly Brush pGroupLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pGroupTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pGroupMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));

    private readonly StackPanel pGroupRowPanel;
    private readonly TextBlock pGroupEmptyNotice;
    private readonly PGroupAction pGroupAction;
    private readonly UIElement pGroupFullBody;
    private readonly UIElement pGroupStripBody;

    public QGroup QGroup { get; }

    public PGroup(QGroup lGroup)
    {
        QPanel.QPanelAttach(this);
        QGroup = lGroup;
        pGroupRowPanel = new StackPanel();
        pGroupAction = new PGroupAction(lGroup);
        pGroupEmptyNotice = PGroupNoticeBuild();
        ScrollViewer pScroll = PGroupScrollBuild();
        PGroupScrollAttach(pScroll);
        pGroupFullBody = PGroupRootBuild(pScroll);
        pGroupStripBody = PGroupStripBuild();
        PGroupFrameBuild();
        pGroupAction.PGroupActionUpdate();
        PGroupRebuild();
        PGroupHandlerAttach();
    }

    private static TextBlock PGroupNoticeBuild() => new()
    {
        Text = QLocalizationCatalog.QLocalizationTextRead("Group.Empty.Notice"),
        FontSize = 12,
        FontFamily = pGroupFontFamily,
        Foreground = pGroupMutedBrush,
        TextWrapping = TextWrapping.Wrap,
        HorizontalAlignment = HorizontalAlignment.Center,
        TextAlignment = TextAlignment.Center,
        Margin = new Thickness(16, 24, 16, 16),
        IsHitTestVisible = false
    };

    private ScrollViewer PGroupScrollBuild()
    {
        var pBody = new Grid();
        pBody.Children.Add(pGroupEmptyNotice);
        pBody.Children.Add(pGroupRowPanel);

        return new ScrollViewer
        {
            Content = pBody,
            Background = Brushes.Transparent,
            AllowDrop = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
    }

    private void PGroupScrollAttach(ScrollViewer pScroll)
    {
        pScroll.DragOver += PGroupDrop.PGroupOverHandle;
        pScroll.Drop += PGroupDropHandle;
    }

    private DockPanel PGroupRootBuild(ScrollViewer pScroll)
    {
        var pRoot = new DockPanel { LastChildFill = true };
        UIElement pHeader = PGroupHeaderBuild();
        DockPanel.SetDock(pHeader, Dock.Top);
        DockPanel.SetDock(pGroupAction.PGroupActionHost, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pGroupAction.PGroupActionHost);
        pRoot.Children.Add(pScroll);
        return pRoot;
    }

    private void PGroupFrameBuild()
    {
        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pGroupFullBody);
        pBodyHost.Children.Add(pGroupStripBody);
        Content = QPanel.QPanelBuild(pBodyHost);
    }

    private void PGroupHandlerAttach()
    {
        QGroup.QGroupChanged += PGroupRebuild;
        QGroup.QGroupFaceChanged += pGroupAction.PGroupActionUpdate;
        QGroup.QGroupMinimizeChanged += PGroupMinimizeHandle;
        Loaded += PGroupLoadedHandle;
    }

    private void PGroupLoadedHandle(object pSender, RoutedEventArgs pEvent) => QGroup.QGroupAutoUpdate();

    private void PGroupRebuild()
    {
        PGroupRowsBuild();
        PGroupEmptyApply();
    }

    private void PGroupRowsBuild()
    {
        pGroupRowPanel.Children.Clear();
        QGroup.QGroupFace.QGroupCardsRead()
            .Select(PGroupCardBuild)
            .ToList()
            .ForEach(pCard => pGroupRowPanel.Children.Add(pCard));
    }

    private void PGroupEmptyApply() => pGroupEmptyNotice.Visibility = PLook.PLookVisible[QGroup.QGroupEmpty];

    private Border PGroupCardBuild(QGroupCard lCard) => new PGroupCard(QGroup, lCard).PGroupCardBorder;

    private void PGroupMinimizeHandle(bool pGroupMinimized)
    {
        pGroupFullBody.Visibility = PLook.PLookVisible[!pGroupMinimized];
        pGroupStripBody.Visibility = PLook.PLookVisible[pGroupMinimized];
    }

    private UIElement PGroupHeaderBuild()
    {
        var pTitleLabel = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Group.Header.Title"),
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pGroupTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PGroupButton.PGroupButtonBuild(
            "/PAsset/PPanel/PListMinimize.svg",
            QLocalizationCatalog.QLocalizationTextRead("Group.Panel.HideTooltip"),
            (_, _) => QGroup.QGroupMinimizedSet(true));
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
        Button pMaximizeButton = PGroupButton.PGroupButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            QLocalizationCatalog.QLocalizationTextRead("Group.Panel.ShowTooltip"),
            (_, _) => QGroup.QGroupMinimizedSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White, Visibility = Visibility.Collapsed };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    private void PGroupDropHandle(object pSender, DragEventArgs pEvent)
    {
        var pMove = pEvent.Data.GetData(PGroupDrop.PGroupMoveKind) as PGroupDrop.PGroupMovePayload;
        pEvent.Handled = QGroup.QGroupDrag.QGroupPanelAccept(
            pEvent.Handled,
            pEvent.Data.GetData(DataFormats.FileDrop) as string[],
            pMove?.PGroupMoveIndex,
            pMove?.PGroupMovePath,
            pEvent.Data.GetData(PList.PListDragKind) as string[]);
    }
}
