using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PPorch;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PRoster : UserControl
{
    private readonly Grid pRosterBody = new();
    private readonly PColumn pRosterLayout;
    private readonly PRosterQueue pRosterQueuePanel;
    private readonly StackPanel pRosterDetailPanel = new();
    private readonly TextBlock pRosterDetailTitle;
    private readonly CheckBox pRosterSharedBox;
    private readonly CheckBox pRosterCompletedBox;
    private readonly DispatcherTimer pRosterElapsedTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    public QRoster QRoster { get; }

    public PRoster(QRoster lRoster, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        QRoster = lRoster;
        PRosterStyleApply();
        ContextMenu pRosterMenu = QMenu.QMenuBuild();
        pRosterQueuePanel = PRosterQueueBuild(pRosterMenu);
        pRosterDetailTitle = PRosterTitleBuild(QLocalizationCatalog.QLocalizationTextRead("Roster.Title.JobDetail"));
        pRosterSharedBox = PRosterOptionBuild(
            QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.Shared"), QRoster.QRosterShared, 0);
        pRosterCompletedBox = PRosterOptionBuild(
            QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.CollapseCompleted"), QRoster.QRosterCollapseDone,
                18);
        pRosterLayout = PRosterBuild(lPreferenceTabLayout);
        Content = PRosterFrameBuild();
        PRosterEventsAttach();
        pRosterElapsedTimer.Start();

        QRoster.QRosterRebuild();
        PRosterDetailUpdate();
    }

    public LSceneTabEntry PRosterLayoutRead() => QRoster.QRosterLayoutRead();

    public double PRosterWidthRead() => QRoster.QRosterWidthResolve(pRosterLayout.PColumnTotalRead());

    public void PRosterClose() => pRosterElapsedTimer.Stop();

    private void PRosterStyleApply()
    {
        FocusVisualStyle = null;
        QScrollbar.QScrollbarAttach(this);
    }

    private PRosterQueue PRosterQueueBuild(ContextMenu pRosterMenu) =>
        new(lEntry => PRosterCard.PRosterEntryBuild(QRoster, lEntry, pRosterMenu))
        {
            ItemsSource = QRoster.QRosterFeed.QRosterEntries
        };

    private Grid PRosterFrameBuild() => new() { Margin = new Thickness(8, 8, 8, 8), Children = { pRosterBody } };

    private void PRosterEventsAttach()
    {
        pRosterSharedBox.Checked += PRosterSharedHandle;
        pRosterSharedBox.Unchecked += PRosterSharedHandle;
        pRosterCompletedBox.Checked += PRosterCompletedHandle;
        pRosterCompletedBox.Unchecked += PRosterCompletedHandle;
        QRoster.QRosterDetailApplied += PRosterDetailUpdate;
        QRoster.QRosterDeferral.QRosterDeferralRaised += PRosterDetailDefer;
        QRoster.QRosterRemoval.QRosterWarningShown += PRosterWarningShow;
        IsVisibleChanged += PRosterVisibleHandle;
        Unloaded += PRosterUnloadHandle;
        pRosterElapsedTimer.Tick += PRosterElapsedTick;
    }

    private void PRosterUnloadHandle(object pSender, RoutedEventArgs pArguments) => PRosterClose();

    private void PRosterElapsedTick(object? pSender, EventArgs pArguments) => QRoster.QRosterElapsedTick(IsVisible);

    private void PRosterVisibleHandle(object pSender, DependencyPropertyChangedEventArgs pArguments)
    {
        pRosterSharedBox.IsChecked = PLook.PLookChecked[QRoster.QRosterShared];
        pRosterCompletedBox.IsChecked = PLook.PLookChecked[QRoster.QRosterCollapseDone];
    }

    private void PRosterSharedHandle(object pSender, RoutedEventArgs pArguments) =>
        QRoster.QRosterSharedSet(pRosterSharedBox.IsChecked);

    private void PRosterCompletedHandle(object pSender, RoutedEventArgs pArguments) =>
        QRoster.QRosterDoneSet(pRosterCompletedBox.IsChecked);

    private void PRosterDetailUpdate() => PRosterDetail.PRosterDetailUpdate(pRosterDetailPanel, QRoster);

    private void PRosterDetailDefer() =>
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(QRoster.QRosterDeferral.QRosterDeferralRun));

    private void PRosterWarningShow(string pTitle, string pMessage) =>
        PSWarning.PSWarningShow(Window.GetWindow(this), pTitle, pMessage);

    private PColumn PRosterBuild(LSceneTabEntry? lPreferenceTabLayout)
    {
        (ColumnDefinition pLeftColumn, ColumnDefinition pRightColumn) = PRosterColumnsBuild();
        PRosterQueueAdd();
        PColumn pLayout = PColumn.PColumnAttach(
            pRosterBody,
            new[] { pLeftColumn, pRightColumn },
            QRoster.QRosterPlanCreate(
                [pLeftColumn.MinWidth, pRightColumn.MinWidth], lPreferenceTabLayout?.LScenePanelWidths));
        PRosterSplitterAdd(pLayout);
        PRosterDetailAdd();
        return pLayout;
    }

    private (ColumnDefinition, ColumnDefinition) PRosterColumnsBuild()
    {
        var pLeftColumn = new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 320 };
        var pRightColumn = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 240 };
        pRosterBody.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        pRosterBody.ColumnDefinitions.Add(pLeftColumn);
        pRosterBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        pRosterBody.ColumnDefinitions.Add(pRightColumn);
        return (pLeftColumn, pRightColumn);
    }

    private void PRosterQueueAdd()
    {
        UIElement pQueue = PRosterPanelBuild();
        Grid.SetColumn(pQueue, 0);
        pRosterBody.Children.Add(pQueue);
    }

    private void PRosterSplitterAdd(PColumn pLayout)
    {
        UIElement pSplitter = pLayout.PColumnSplitterBuild(0);
        Grid.SetColumn(pSplitter, 1);
        pRosterBody.Children.Add(pSplitter);
    }

    private void PRosterDetailAdd()
    {
        UIElement pDetail = PRosterDetail.PRosterDetailBuild(pRosterDetailTitle, pRosterDetailPanel);
        Grid.SetColumn(pDetail, 2);
        pRosterBody.Children.Add(pDetail);
    }

    private UIElement PRosterPanelBuild()
    {
        StackPanel pOptions = PRosterOptionsBuild();
        Border pOptionRow = PRosterBandBuild(pOptions);
        Border pColumnHeader = PRosterBandBuild(PRosterHeaderBuild());
        QScrollbar.QScrollbarAttach(pRosterQueuePanel);
        PRosterScrollAttach(pColumnHeader);
        return PRosterDockBuild(pOptionRow, pColumnHeader);
    }

    private StackPanel PRosterOptionsBuild()
    {
        var pOptions = new StackPanel { Orientation = Orientation.Horizontal };
        pOptions.Children.Add(pRosterSharedBox);
        pOptions.Children.Add(pRosterCompletedBox);
        return pOptions;
    }

    private void PRosterScrollAttach(Border pColumnHeader) =>
        pRosterQueuePanel.AddHandler(
            ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler((pSender, pArgs) =>
                PRosterPaddingSet(pColumnHeader, PRosterPaddingResolve(pArgs))));

    private static Thickness PRosterPaddingResolve(ScrollChangedEventArgs pArgs) =>
        new(21, 10, 5 + ((ScrollViewer)pArgs.OriginalSource).ActualWidth - pArgs.ViewportWidth, 10);

    private static void PRosterPaddingSet(Border pColumnHeader, Thickness pPadding) =>
        pColumnHeader.Padding = pPadding;

    private UIElement PRosterDockBuild(Border pOptionRow, Border pColumnHeader)
    {
        var pRoot = new DockPanel { LastChildFill = true };
        Grid.SetIsSharedSizeScope(pRoot, true);
        DockPanel.SetDock(pOptionRow, Dock.Top);
        DockPanel.SetDock(pColumnHeader, Dock.Top);
        pRoot.Children.Add(pOptionRow);
        pRoot.Children.Add(pColumnHeader);
        pRoot.Children.Add(pRosterQueuePanel);
        return QPanel.QPanelBuild(pRoot);
    }

    private static Border PRosterBandBuild(UIElement pChild) => new()
    {
        Padding = PRosterTheme.PRosterHeaderPadding,
        Background = PRosterTheme.PRosterHeaderBrush,
        BorderBrush = PRosterTheme.PRosterLineBrush,
        BorderThickness = new Thickness(0, 0, 0, 1),
        Child = pChild
    };

    private static CheckBox PRosterOptionBuild(string pText, bool pChecked, double pLeftMargin)
    {
        CheckBox pBox = PRosterBoxBuild(pText, pChecked, pLeftMargin);
        QInteractionClick.QInteractionClickAttach(pBox);
        QCheckbox.QCheckboxAttach(pBox);
        return pBox;
    }

    private static CheckBox PRosterBoxBuild(string pText, bool pChecked, double pLeftMargin) => new()
    {
        Content = pText,
        FontSize = PRosterTheme.PRosterRowSize,
        IsChecked = PLook.PLookChecked[pChecked],
        Margin = new Thickness(pLeftMargin, 0, 0, 0)
    };

    private static Grid PRosterHeaderBuild()
    {
        Grid pGrid = PRosterRow.PRosterColumnsCreate();
        PRosterHeadAdd(pGrid, 0, QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.Step"));
        PRosterHeadAdd(pGrid, 1, QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.Priority"));
        PRosterHeadAdd(pGrid, 2, QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.Length"));
        PRosterHeadAdd(pGrid, 3, QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.Progress"));
        PRosterHeadAdd(pGrid, 4, QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.Percentage"));
        PRosterHeadAdd(pGrid, 5, QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.State"));
        PRosterHeadAdd(pGrid, 6, QLocalizationCatalog.QLocalizationTextRead("Roster.Queue.Owner"));
        return pGrid;
    }

    private static void PRosterHeadAdd(Grid pGrid, int pColumn, string pText)
    {
        var pCell = new TextBlock
        {
            Text = pText,
            FontSize = PRosterTheme.PRosterRowSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = PRosterTheme.PRosterMutedBrush
        };
        Grid.SetColumn(pCell, pColumn);
        pGrid.Children.Add(pCell);
    }

    private static TextBlock PRosterTitleBuild(string pTitle) => new()
    {
        Text = pTitle,
        FontSize = PRosterTheme.PRosterTitleSize,
        FontWeight = FontWeights.SemiBold,
        Foreground = PRosterTheme.PRosterTitleBrush,
        VerticalAlignment = VerticalAlignment.Center
    };
}
