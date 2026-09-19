using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PWing;
using static Cadroue.UIVeneer.PWing.PPanel;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PRoster : UserControl
{
    private readonly Grid pRosterBody = new();
    private readonly PColumn pRosterLayout;
    private readonly StackPanel pRosterQueuePanel = new();
    private readonly StackPanel pRosterDetailPanel = new();
    private readonly TextBlock pRosterDetailTitle;
    private readonly CheckBox pRosterSharedBox;
    private readonly CheckBox pRosterCompletedBox;
    private readonly DispatcherTimer pRosterElapsedTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    public LRoster LRoster { get; } = LRoster.LRosterCreate();

    public PRoster(LSceneTabRecord? lPreferenceTabLayout = null)
    {
        FocusVisualStyle = null;
        PScrollbar.PScrollbarApply(this);
        pRosterDetailTitle = PRosterTitleBuild(LLocalization.LLocalizationTextRead("Roster.Title.JobDetail"));
        pRosterSharedBox = PRosterOptionBuild(
            LLocalization.LLocalizationTextRead("Roster.Queue.Shared"), LRoster.LRosterShared, 0);
        pRosterCompletedBox = PRosterOptionBuild(
            LLocalization.LLocalizationTextRead("Roster.Queue.CollapseCompleted"), LRoster.LRosterCollapseDone, 18);
        pRosterLayout = PRosterBuild(lPreferenceTabLayout);
        Content = new Grid { Margin = new Thickness(8, 8, 8, 8), Children = { pRosterBody } };

        pRosterSharedBox.Checked += PRosterSharedHandle;
        pRosterSharedBox.Unchecked += PRosterSharedHandle;
        pRosterCompletedBox.Checked += PRosterCompletedHandle;
        pRosterCompletedBox.Unchecked += PRosterCompletedHandle;
        LRoster.LRosterCardsApply += PRosterCardsApply;
        LRoster.LRosterCardApply += PRosterCardApply;
        LRoster.LRosterDetailApply += PRosterDetailUpdate;
        LRoster.LRosterDetailDefer += PRosterDetailDefer;
        LRoster.LRosterWarningShow += PRosterWarningShow;
        IsVisibleChanged += PRosterVisibleHandle;
        Unloaded += PRosterUnloadHandle;
        pRosterElapsedTimer.Tick += PRosterElapsedTick;
        pRosterElapsedTimer.Start();

        LRoster.LRosterRebuild();
        PRosterDetailUpdate();
    }

    public LStation PRosterStation => LRoster.LRosterStation;

    public LSceneTabRecord PRosterLayoutRead() => LRoster.LRosterLayoutRead(pRosterLayout.PColumnWeightsRead());

    public double PRosterWidthRead() => LRoster.LRosterWidthResolve(pRosterLayout.PColumnTotalRead());

    public void PRosterClose()
    {
        pRosterElapsedTimer.Stop();
        LRoster.LRosterClose();
    }

    private void PRosterUnloadHandle(object pSender, RoutedEventArgs pArguments) => PRosterClose();

    private void PRosterElapsedTick(object? pSender, EventArgs pArguments) => LRoster.LRosterElapsedTick(IsVisible);

    private void PRosterVisibleHandle(object pSender, DependencyPropertyChangedEventArgs pArguments)
    {
        pRosterSharedBox.IsChecked = PLook.PLookChecked[LRoster.LRosterShared];
        pRosterCompletedBox.IsChecked = PLook.PLookChecked[LRoster.LRosterCollapseDone];
    }

    private void PRosterSharedHandle(object pSender, RoutedEventArgs pArguments) =>
        LRoster.LRosterSharedSet(pRosterSharedBox.IsChecked);

    private void PRosterCompletedHandle(object pSender, RoutedEventArgs pArguments) =>
        LRoster.LRosterDoneSet(pRosterCompletedBox.IsChecked);

    private void PRosterCardsApply()
    {
        pRosterQueuePanel.Children.Clear();
        LRoster.LRosterCards.ToList().ForEach(PRosterCardAdd);
    }

    private void PRosterCardAdd(LRosterCard lCard) =>
        pRosterQueuePanel.Children.Add(PRosterCard.PRosterCardBuild(LRoster, lCard));

    private void PRosterCardApply(int pIndex, LRosterCard lCard)
    {
        pRosterQueuePanel.Children.RemoveAt(pIndex);
        pRosterQueuePanel.Children.Insert(pIndex, PRosterCard.PRosterCardBuild(LRoster, lCard));
    }

    private void PRosterDetailUpdate() => PRosterDetail.PRosterDetailUpdate(pRosterDetailPanel, LRoster);

    private void PRosterDetailDefer() =>
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(LRoster.LRosterDetailRun));

    private void PRosterWarningShow(string pTitle, string pMessage) =>
        PSWarning.PSWarningShow(Window.GetWindow(this), pTitle, pMessage);

    private PColumn PRosterBuild(LSceneTabRecord? lPreferenceTabLayout)
    {
        var pLeftColumn = new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 320 };
        var pRightColumn = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 240 };
        pRosterBody.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        pRosterBody.ColumnDefinitions.Add(pLeftColumn);
        pRosterBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        pRosterBody.ColumnDefinitions.Add(pRightColumn);

        UIElement pQueue = PRosterPanelBuild();
        Grid.SetColumn(pQueue, 0);
        pRosterBody.Children.Add(pQueue);

        PColumn pLayout = PColumn.PColumnAttach(
            pRosterBody, new[] { pLeftColumn, pRightColumn }, lPreferenceTabLayout?.LScenePanelWidths);
        UIElement pSplitter = pLayout.PColumnSplitterBuild(0);
        Grid.SetColumn(pSplitter, 1);
        pRosterBody.Children.Add(pSplitter);

        UIElement pDetail = PRosterDetail.PRosterDetailBuild(pRosterDetailTitle, pRosterDetailPanel);
        Grid.SetColumn(pDetail, 2);
        pRosterBody.Children.Add(pDetail);
        return pLayout;
    }

    private UIElement PRosterPanelBuild()
    {
        var pOptions = new StackPanel { Orientation = Orientation.Horizontal };
        pOptions.Children.Add(pRosterSharedBox);
        pOptions.Children.Add(pRosterCompletedBox);
        Border pOptionRow = PRosterBandBuild(pOptions);
        Border pColumnHeader = PRosterBandBuild(PRosterHeaderBuild());

        var pScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            FocusVisualStyle = null,
            Padding = new Thickness(8, 8, 8, 8),
            Content = pRosterQueuePanel
        };
        PScrollbar.PScrollbarApply(pScroll);

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pOptionRow, Dock.Top);
        DockPanel.SetDock(pColumnHeader, Dock.Top);
        pRoot.Children.Add(pOptionRow);
        pRoot.Children.Add(pColumnHeader);
        pRoot.Children.Add(pScroll);
        return PPanelBorderBuild(pRoot);
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
        var pBox = new CheckBox
        {
            Content = pText,
            FontSize = PRosterTheme.PRosterRowSize,
            IsChecked = PLook.PLookChecked[pChecked],
            Margin = new Thickness(pLeftMargin, 0, 0, 0)
        };
        PCheckbox.PCheckboxApply(pBox);
        return pBox;
    }

    private static Grid PRosterHeaderBuild()
    {
        Grid pGrid = PRosterRow.PRosterColumnsCreate();
        PRosterHeadAdd(pGrid, 0, LLocalization.LLocalizationTextRead("Roster.Queue.Step"));
        PRosterHeadAdd(pGrid, 1, LLocalization.LLocalizationTextRead("Roster.Queue.Priority"));
        PRosterHeadAdd(pGrid, 2, LLocalization.LLocalizationTextRead("Roster.Queue.Length"));
        PRosterHeadAdd(pGrid, 3, LLocalization.LLocalizationTextRead("Roster.Queue.Progress"));
        PRosterHeadAdd(pGrid, 4, LLocalization.LLocalizationTextRead("Roster.Queue.Percentage"));
        PRosterHeadAdd(pGrid, 5, LLocalization.LLocalizationTextRead("Roster.Queue.State"));
        PRosterHeadAdd(pGrid, 6, LLocalization.LLocalizationTextRead("Roster.Queue.Owner"));
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
