using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PConsole : UserControl
{
    private static readonly IReadOnlyDictionary<bool, Brush> pConsoleRunBrushes = new Dictionary<bool, Brush>
    {
        [true] = PRosterTheme.PRosterDoneBrush,
        [false] = PRosterTheme.PRosterTextBrush,
    };

    private static readonly IReadOnlyDictionary<bool, FontWeight> pConsoleRunWeights =
        new Dictionary<bool, FontWeight>
        {
            [true] = FontWeights.Bold,
            [false] = FontWeights.Normal,
        };

    private IReadOnlyDictionary<bool, UIElement?> pConsoleSceneContents = null!;
    private readonly Button pConsolePreviousButton;
    private readonly Button pConsoleNextButton;
    private readonly CheckBox pConsoleAutoBox;
    private readonly TextBlock pConsoleStationLabel;
    private readonly PConsoleProgress pConsoleProgress;
    private readonly TextBlock pConsoleStatus;
    private readonly Grid pConsoleRestIcon;
    private readonly Image pConsoleSpinner;
    private readonly Button pConsoleStartButton;
    private readonly Button pConsolePauseButton;
    private readonly Button pConsoleCancelButton;
    private readonly Button pConsoleStopButton;
    private readonly Button pConsoleRemoveButton;
    private readonly Button pConsoleClearButton;
    private readonly Button pConsoleEmptyButton;
    private readonly Button pConsoleTabsButton;
    private PConsoleScene pConsoleScene = null!;
    private QConsole lConsole = null!;
    private readonly ContentControl pConsoleSceneHost;
    private readonly Border pConsoleSceneSeparator;

    public PConsole()
    {
        PConsoleStyleApply();
        pConsoleProgress = new PConsoleProgress(() => lConsole.QConsoleProgressGlide);
        pConsoleStatus = PConsoleControl.PConsoleLabelBuild(
            PRosterTheme.PRosterTextBrush, PConsoleControl.PConsoleStatusSize);
        pConsoleRestIcon = PConsoleIndicator.PConsoleRestBuild();
        pConsoleSpinner = PConsoleIndicator.PConsoleSpinnerBuild();
        pConsoleStationLabel = PConsoleControl.PConsoleLabelBuild(
            PRosterTheme.PRosterMutedBrush, PConsoleControl.PConsoleStationSize);
        pConsoleStartButton = PConsoleControl.PConsoleButtonBuild(
            "Start", "PRosterStart.svg", PRosterTheme.PRosterDoneBrush, PConsoleStartHandle);
        pConsolePauseButton = PConsoleControl.PConsoleButtonBuild(
            "Pause", "PRosterPause.svg", PRosterTheme.PRosterTextBrush, PConsolePauseHandle);
        pConsoleCancelButton = PConsoleControl.PConsoleButtonBuild(
            "Cancel", "PRosterCancel.svg", PRosterTheme.PRosterTextBrush, PConsoleCancelHandle);
        pConsoleStopButton = PConsoleControl.PConsoleButtonBuild(
            "Stop", "PRosterStop.svg", PRosterTheme.PRosterFailBrush, PConsoleStopHandle);
        pConsoleRemoveButton = PConsoleControl.PConsoleButtonBuild(
            "Remove", "PRosterRemove.svg", PRosterTheme.PRosterTextBrush, PConsoleRemoveHandle);
        pConsoleClearButton = PConsoleControl.PConsoleButtonBuild(
            "ClearDone", "PRosterClearDone.svg", PRosterTheme.PRosterTextBrush, PConsoleDoneHandle);
        pConsoleEmptyButton = PConsoleControl.PConsoleButtonBuild(
            "ClearAll", "PRosterClearAll.svg", PRosterTheme.PRosterTextBrush, PConsoleAllHandle);
        pConsoleTabsButton = PConsoleControl.PConsoleButtonBuild(
            "ClearTabs", "PConsoleClearTabs.svg", PRosterTheme.PRosterTextBrush, PConsoleTabsHandle);
        pConsoleAutoBox = PConsoleControl.PConsoleAutoBuild(PConsoleAutoHandle);
        pConsoleSceneHost = new ContentControl();
        pConsoleSceneSeparator = PConsoleControl.PConsoleSeparatorBuild();
        pConsolePreviousButton = PConsoleControl.PConsoleSwitchBuild(
            "PConsolePrevious.svg",
            QLocalizationCatalog.QLocalizationTextRead("Console.Previous.Tooltip"),
            PConsolePreviousHandle);
        pConsoleNextButton = PConsoleControl.PConsoleSwitchBuild(
            "PConsoleNext.svg",
            QLocalizationCatalog.QLocalizationTextRead("Console.Next.Tooltip"),
            PConsoleNextHandle);

        Content = PConsoleBuild();
    }

    public void PConsoleAttach(QConsole lConsoleOwner)
    {
        PConsoleOwnerSet(lConsoleOwner);
        PConsoleSceneBuild();
        PConsoleContentsSet();
        PConsoleEventsAttach();
        lConsole.QConsoleStation.QConsoleWatchStart();
        PConsoleUnloadAttach();

        lConsole.QConsoleUpdate();
        Dispatcher.BeginInvoke(new Action(lConsole.QConsoleScheduleLoad));
    }

    private void PConsoleStyleApply() => FocusVisualStyle = null;

    private void PConsoleOwnerSet(QConsole lConsoleOwner) => lConsole = lConsoleOwner;

    private void PConsoleSceneBuild()
    {
        pConsoleScene = new PConsoleScene(lConsole.QConsoleScene);
        pConsoleSceneHost.Content = pConsoleScene;
    }

    private void PConsoleContentsSet() =>
        pConsoleSceneContents = new Dictionary<bool, UIElement?>
        {
            [true] = pConsoleScene,
            [false] = null,
        };

    private void PConsoleEventsAttach()
    {
        lConsole.QConsoleStatusApplied += PConsoleStatusApply;
        lConsole.QConsoleProgressApplied += pConsoleProgress.PConsoleProgressApply;
        lConsole.QConsoleUpdateDeferred += PConsoleUpdateDefer;
        lConsole.QConsoleWarningShown += PConsoleWarningShow;
        lConsole.QConsoleStation.QConsoleWatchChanged += PConsoleWatchHandle;
    }

    private void PConsoleUnloadAttach() => Unloaded += PConsoleUnloadHandle;

    private void PConsoleUnloadDetach() => Unloaded -= PConsoleUnloadHandle;

    public PConsoleScene PConsoleSceneRead() => pConsoleScene;

    public void PConsoleSceneSet(bool pShown)
    {
        pConsoleSceneHost.Content = pConsoleSceneContents[pShown];
        pConsoleSceneSeparator.Visibility = PLook.PLookVisible[pShown];
    }

    private void PConsoleStartHandle(object pSender, RoutedEventArgs pArguments) => lConsole.QConsoleStart();

    private void PConsolePauseHandle(object pSender, RoutedEventArgs pArguments) => lConsole.QConsolePause();

    private void PConsoleCancelHandle(object pSender, RoutedEventArgs pArguments) => lConsole.QConsoleCancel();

    private void PConsoleStopHandle(object pSender, RoutedEventArgs pArguments) => lConsole.QConsoleStop();

    private void PConsoleRemoveHandle(object pSender, RoutedEventArgs pArguments) => lConsole.QConsoleRemove();

    private void PConsoleDoneHandle(object pSender, RoutedEventArgs pArguments) => lConsole.QConsoleDoneClear();

    private void PConsoleAllHandle(object pSender, RoutedEventArgs pArguments) => lConsole.QConsoleAllClear();

    private void PConsoleTabsHandle(object pSender, RoutedEventArgs pArguments) => lConsole.QConsoleTabsClear();

    private void PConsoleAutoHandle(object pSender, RoutedEventArgs pArguments) =>
        lConsole.QConsoleAutoSet(pConsoleAutoBox.IsChecked);

    private void PConsolePreviousHandle(object pSender, RoutedEventArgs pArguments) =>
        lConsole.QConsoleStation.QConsoleStationMove(QConsoleStation.QConsoleStepBack);

    private void PConsoleNextHandle(object pSender, RoutedEventArgs pArguments) =>
        lConsole.QConsoleStation.QConsoleStationMove(QConsoleStation.QConsoleStepForward);

    private void PConsoleWatchHandle() => Dispatcher.BeginInvoke(new Action(lConsole.QConsoleScheduleLoad));

    private void PConsoleUpdateDefer() =>
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(lConsole.QConsoleUpdate));

    private void PConsoleWarningShow(string pTitle, string pMessage) =>
        PSWarning.PSWarningShow(Window.GetWindow(this), pTitle, pMessage);

    private void PConsoleUnloadHandle(object pSender, RoutedEventArgs pArguments)
    {
        lConsole.QConsoleClose();
        PConsoleUnloadDetach();
    }

    private void PConsoleStatusApply(QConsoleStatus lStatus)
    {
        pConsoleStatus.Inlines.Clear();
        lStatus.QConsoleStatusRuns.ToList().ForEach(PConsoleRunAdd);
        pConsoleStartButton.Visibility = PLook.PLookVisible[!lStatus.QConsoleStatusRunning];
        pConsoleStartButton.IsEnabled = lStatus.QConsoleStatusLoaded;
        pConsolePauseButton.Visibility = PLook.PLookVisible[lStatus.QConsoleStatusRunning];
        pConsolePauseButton.IsEnabled = true;
        pConsoleCancelButton.IsEnabled = lStatus.QConsoleStatusBusy;
        pConsoleStopButton.IsEnabled = lStatus.QConsoleStatusActive;
        pConsoleRemoveButton.IsEnabled = lStatus.QConsoleStatusRemovable;
        pConsoleClearButton.IsEnabled = lStatus.QConsoleStatusDone;
        pConsoleEmptyButton.IsEnabled = lStatus.QConsoleStatusClearable;
        pConsolePreviousButton.Visibility = PLook.PLookVisible[lStatus.QConsoleStatusBoard];
        pConsoleNextButton.Visibility = PLook.PLookVisible[lStatus.QConsoleStatusBoard];
        pConsoleSpinner.Visibility = PLook.PLookVisible[lStatus.QConsoleStatusRunning];
        pConsoleRestIcon.Visibility = PLook.PLookVisible[!lStatus.QConsoleStatusRunning];
        pConsoleAutoBox.IsChecked = PLook.PLookChecked[lStatus.QConsoleStatusAuto];
        pConsoleStationLabel.Text = lStatus.QConsoleStatusStation;
    }

    private void PConsoleRunAdd(QConsoleSpan lRun) =>
        pConsoleStatus.Inlines.Add(new Run(lRun.QConsoleRunText)
        {
            Foreground = pConsoleRunBrushes[lRun.QConsoleRunAccent],
            FontWeight = pConsoleRunWeights[lRun.QConsoleRunAccent]
        });

    private UIElement PConsoleBuild()
    {
        var pButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pButtons.Children.Add(pConsoleStartButton);
        pButtons.Children.Add(pConsolePauseButton);
        pButtons.Children.Add(pConsoleCancelButton);
        pButtons.Children.Add(pConsoleStopButton);
        pButtons.Children.Add(new Border { Width = 10 });
        pButtons.Children.Add(pConsoleRemoveButton);
        pButtons.Children.Add(pConsoleClearButton);
        pButtons.Children.Add(pConsoleEmptyButton);
        pButtons.Children.Add(new Border { Width = 10 });
        pButtons.Children.Add(pConsoleTabsButton);

        var pRow = new Grid();
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var pAutoRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pAutoRow.Children.Add(pConsoleAutoBox);
        pAutoRow.Children.Add(pConsoleSceneSeparator);
        pAutoRow.Children.Add(pConsoleSceneHost);

        var pStatusRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(28, 0, 12, 0)
        };
        pStatusRow.Children.Add(pConsoleRestIcon);
        pStatusRow.Children.Add(pConsoleSpinner);
        pStatusRow.Children.Add(pConsoleStatus);

        Grid.SetColumn(pButtons, 0);
        Grid.SetColumn(pStatusRow, 1);
        Grid.SetColumn(pConsoleStationLabel, 2);
        Grid.SetColumn(pAutoRow, 3);
        pConsoleStationLabel.Margin = new Thickness(0, 0, 4, 0);
        pRow.Children.Add(pButtons);
        pRow.Children.Add(pStatusRow);
        pRow.Children.Add(pConsoleStationLabel);
        pRow.Children.Add(pAutoRow);

        var pStack = new StackPanel();
        pStack.Children.Add(pRow);
        pStack.Children.Add(new Border { Height = 8 });
        pStack.Children.Add(pConsoleProgress.PConsoleProgressBar);

        var pCard = new Border
        {
            Padding = new Thickness(8, 10, 8, 10),
            Background = Brushes.White,
            SnapsToDevicePixels = true,
            Child = pStack
        };

        Background = Brushes.White;
        var pRoot = new Grid();
        var pSwitchWidth = new GridLength(PConsoleControl.PConsoleSwitchWidth);
        pRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = pSwitchWidth });
        pRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = pSwitchWidth });
        Grid.SetColumn(pConsolePreviousButton, 0);
        Grid.SetColumn(pCard, 1);
        Grid.SetColumn(pConsoleNextButton, 2);
        pRoot.Children.Add(pConsolePreviousButton);
        pRoot.Children.Add(pCard);
        pRoot.Children.Add(pConsoleNextButton);
        return pRoot;
    }
}
