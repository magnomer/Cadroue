using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PConsole : UserControl
{
    private static readonly Duration pConsoleProgressGlide =
        new(TimeSpan.FromSeconds(LEncode.LEncodeStatsPeriod));

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

    private readonly IReadOnlyDictionary<bool, Action<double>> pConsoleProgressWrites;
    private IReadOnlyDictionary<bool, UIElement?> pConsoleSceneContents = null!;
    private readonly Button pConsolePreviousButton;
    private readonly Button pConsoleNextButton;
    private readonly CheckBox pConsoleAutoBox;
    private readonly TextBlock pConsoleStationLabel;
    private readonly ProgressBar pConsoleProgress;
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
    private LConsole lConsole = null!;
    private readonly ContentControl pConsoleSceneHost;
    private readonly Border pConsoleSceneSeparator;

    public PConsole()
    {
        FocusVisualStyle = null;
        pConsoleProgressWrites = new Dictionary<bool, Action<double>>
        {
            [true] = PConsoleGlideApply,
            [false] = PConsoleProgressSet,
        };
        pConsoleProgress = PConsoleIndicator.PConsoleProgressBuild();
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
            LLocalization.LLocalizationTextRead("Console.Previous.Tooltip"),
            PConsolePreviousHandle);
        pConsoleNextButton = PConsoleControl.PConsoleSwitchBuild(
            "PConsoleNext.svg",
            LLocalization.LLocalizationTextRead("Console.Next.Tooltip"),
            PConsoleNextHandle);

        Content = PConsoleBuild();
    }

    public void PConsoleAttach(LConsole lConsoleOwner)
    {
        lConsole = lConsoleOwner;
        pConsoleScene = new PConsoleScene(lConsole.LConsoleScene);
        pConsoleSceneHost.Content = pConsoleScene;
        pConsoleSceneContents = new Dictionary<bool, UIElement?>
        {
            [true] = pConsoleScene,
            [false] = null,
        };
        lConsole.LConsoleStatusApplied += PConsoleStatusApply;
        lConsole.LConsoleProgressApplied += PConsoleProgressApply;
        lConsole.LConsoleUpdateDeferred += PConsoleUpdateDefer;
        lConsole.LConsoleWarningShown += PConsoleWarningShow;
        lConsole.LConsoleStation.LConsoleWatchChanged += PConsoleWatchHandle;
        lConsole.LConsoleStation.LConsoleWatchStart();
        Unloaded += PConsoleUnloadHandle;

        lConsole.LConsoleUpdate();
        Dispatcher.BeginInvoke(new Action(lConsole.LConsoleScheduleLoad));
    }

    public PConsoleScene PConsoleSceneRead() => pConsoleScene;

    public void PConsoleSceneSet(bool pShown)
    {
        pConsoleSceneHost.Content = pConsoleSceneContents[pShown];
        pConsoleSceneSeparator.Visibility = PLook.PLookVisible[pShown];
    }

    private void PConsoleStartHandle(object pSender, RoutedEventArgs pArguments) => lConsole.LConsoleStart();

    private void PConsolePauseHandle(object pSender, RoutedEventArgs pArguments) => lConsole.LConsolePause();

    private void PConsoleCancelHandle(object pSender, RoutedEventArgs pArguments) => lConsole.LConsoleCancel();

    private void PConsoleStopHandle(object pSender, RoutedEventArgs pArguments) => lConsole.LConsoleStop();

    private void PConsoleRemoveHandle(object pSender, RoutedEventArgs pArguments) => lConsole.LConsoleRemove();

    private void PConsoleDoneHandle(object pSender, RoutedEventArgs pArguments) => lConsole.LConsoleDoneClear();

    private void PConsoleAllHandle(object pSender, RoutedEventArgs pArguments) => lConsole.LConsoleAllClear();

    private void PConsoleTabsHandle(object pSender, RoutedEventArgs pArguments) => lConsole.LConsoleTabsClear();

    private void PConsoleAutoHandle(object pSender, RoutedEventArgs pArguments) =>
        lConsole.LConsoleAutoSet(pConsoleAutoBox.IsChecked);

    private void PConsolePreviousHandle(object pSender, RoutedEventArgs pArguments) =>
        lConsole.LConsoleStation.LConsoleStationMove(LConsoleStation.LConsoleStepBack);

    private void PConsoleNextHandle(object pSender, RoutedEventArgs pArguments) =>
        lConsole.LConsoleStation.LConsoleStationMove(LConsoleStation.LConsoleStepForward);

    private void PConsoleWatchHandle() => Dispatcher.BeginInvoke(new Action(lConsole.LConsoleScheduleLoad));

    private void PConsoleUpdateDefer() =>
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(lConsole.LConsoleUpdate));

    private void PConsoleWarningShow(string pTitle, string pMessage) =>
        PSWarning.PSWarningShow(Window.GetWindow(this), pTitle, pMessage);

    private void PConsoleUnloadHandle(object pSender, RoutedEventArgs pArguments)
    {
        lConsole.LConsoleClose();
        Unloaded -= PConsoleUnloadHandle;
    }

    private void PConsoleStatusApply(LConsoleStatus lStatus)
    {
        pConsoleStatus.Inlines.Clear();
        lStatus.LConsoleStatusRuns.ToList().ForEach(PConsoleRunAdd);
        pConsoleStartButton.Visibility = PLook.PLookVisible[!lStatus.LConsoleStatusRunning];
        pConsoleStartButton.IsEnabled = lStatus.LConsoleStatusLoaded;
        pConsolePauseButton.Visibility = PLook.PLookVisible[lStatus.LConsoleStatusRunning];
        pConsolePauseButton.IsEnabled = true;
        pConsoleCancelButton.IsEnabled = lStatus.LConsoleStatusBusy;
        pConsoleStopButton.IsEnabled = lStatus.LConsoleStatusActive;
        pConsoleRemoveButton.IsEnabled = lStatus.LConsoleStatusRemovable;
        pConsoleClearButton.IsEnabled = lStatus.LConsoleStatusDone;
        pConsoleEmptyButton.IsEnabled = lStatus.LConsoleStatusClearable;
        pConsolePreviousButton.Visibility = PLook.PLookVisible[lStatus.LConsoleStatusBoard];
        pConsoleNextButton.Visibility = PLook.PLookVisible[lStatus.LConsoleStatusBoard];
        pConsoleSpinner.Visibility = PLook.PLookVisible[lStatus.LConsoleStatusRunning];
        pConsoleRestIcon.Visibility = PLook.PLookVisible[!lStatus.LConsoleStatusRunning];
        pConsoleAutoBox.IsChecked = PLook.PLookChecked[lStatus.LConsoleStatusAuto];
        pConsoleStationLabel.Text = lStatus.LConsoleStatusStation;
    }

    private void PConsoleRunAdd(LConsoleSpan lRun) =>
        pConsoleStatus.Inlines.Add(new Run(lRun.LConsoleRunText)
        {
            Foreground = pConsoleRunBrushes[lRun.LConsoleRunAccent],
            FontWeight = pConsoleRunWeights[lRun.LConsoleRunAccent]
        });

    private void PConsoleProgressApply(double pValue, bool pGlide) => pConsoleProgressWrites[pGlide](pValue);

    private void PConsoleProgressSet(double pValue)
    {
        pConsoleProgress.BeginAnimation(RangeBase.ValueProperty, null);
        pConsoleProgress.Value = pValue;
    }

    private void PConsoleGlideApply(double pValue) =>
        pConsoleProgress.BeginAnimation(
            RangeBase.ValueProperty,
            new DoubleAnimation
            {
                To = pValue,
                Duration = pConsoleProgressGlide,
                FillBehavior = FillBehavior.HoldEnd
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
        pStack.Children.Add(pConsoleProgress);

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
