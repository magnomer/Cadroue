using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

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

    public LSceneTabEntry PRosterLayoutRead() => QRoster.QRosterColumn.QRosterLayoutRead();

    public double PRosterWidthRead() => QRosterColumn.QRosterWidthResolve(pRosterLayout.PColumnTotalRead());

    public void PRosterClose() => pRosterElapsedTimer.Stop();

    private void PRosterStyleApply()
    {
        FocusVisualStyle = null;
        QScrollbar.QScrollbarAttach(this);
    }

    private PRosterQueue PRosterQueueBuild(ContextMenu pRosterMenu) =>
        new(lEntry => PRosterEntry.PRosterEntryBuild(QRoster, lEntry, pRosterMenu))
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
            QRoster.QRosterColumn.QRosterPlanCreate(
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
        UIElement pQueue = PRosterPanel.PRosterPanelBuild(pRosterQueuePanel, pRosterSharedBox, pRosterCompletedBox);
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

    private static TextBlock PRosterTitleBuild(string pTitle) => new()
    {
        Text = pTitle,
        FontSize = PRosterTheme.PRosterTitleSize,
        FontWeight = FontWeights.SemiBold,
        Foreground = PRosterTheme.PRosterTitleBrush,
        VerticalAlignment = VerticalAlignment.Center
    };
}
