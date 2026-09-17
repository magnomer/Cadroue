using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PDeck;

public sealed partial class PConsole : UserControl
{
    private const double PConsoleStatusSize = 13;
    private const double PConsoleStationSize = 12;
    private const double PConsoleSwitchWidth = 34;
    private const double PConsoleSwitchSize = 18;

    private readonly LScheduleContract pConsoleSchedule = PProgram.LScheduleCurrent;
    private readonly Button pConsolePreviousButton;
    private readonly Button pConsoleNextButton;
    private readonly CheckBox pConsoleAutoBox;
    private readonly ComboBox pConsoleRelayCombo;
    private readonly Button pConsoleSaveButton;
    private readonly Button pConsoleExportButton;
    private readonly Button pConsoleImportButton;
    private readonly TextBlock pConsoleStationLabel;
    private readonly ProgressBar pConsoleProgress;
    private readonly TextBlock pConsoleStatus;
    private readonly Grid pConsoleRestIcon;
    private readonly Path pConsoleSpinner;
    private readonly RotateTransform pConsoleSpinnerRotate = new(0);
    private bool pConsoleSpinning;
    private readonly Button pConsoleStartButton;
    private readonly Button pConsolePauseButton;
    private readonly Button pConsoleCancelButton;
    private readonly Button pConsoleStopButton;
    private readonly Button pConsoleRemoveButton;
    private readonly Button pConsoleClearButton;
    private readonly Button pConsoleEmptyButton;
    private readonly Button pConsoleTabsButton;
    private readonly StackPanel pConsoleSceneControls;
    private readonly ContentControl pConsoleSceneHost;
    private readonly Border pConsoleSceneSeparator;

    public PConsole()
    {
        FocusVisualStyle = null;
        pConsoleProgress = PConsoleProgressBuild();
        pConsoleStatus = PConsoleLabelBuild(PRosterTheme.PRosterTextBrush, PConsoleStatusSize);
        pConsoleRestIcon = PConsoleRestBuild();
        pConsoleSpinner = PConsoleSpinnerBuild();
        pConsoleStationLabel = PConsoleLabelBuild(PRosterTheme.PRosterMutedBrush, PConsoleStationSize);
        pConsoleStartButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.Start"),
            "PRosterStart.svg",
            LLocalization.LLocalizationTextRead("Console.Button.StartTooltip"),
            PRosterTheme.PRosterDoneBrush,
            PConsoleStartHandle);
        pConsolePauseButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.Pause"),
            "PRosterPause.svg",
            LLocalization.LLocalizationTextRead("Console.Button.PauseTooltip"),
            null,
            PConsolePauseHandle);
        pConsoleCancelButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.Cancel"),
            "PRosterCancel.svg",
            LLocalization.LLocalizationTextRead("Console.Button.CancelTooltip"),
            null,
            PConsoleCancelHandle);
        pConsoleStopButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.Stop"),
            "PRosterStop.svg",
            LLocalization.LLocalizationTextRead("Console.Button.StopTooltip"),
            PRosterTheme.PRosterFailBrush,
            PConsoleStopHandle);
        pConsoleRemoveButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.Remove"),
            "PRosterRemove.svg",
            LLocalization.LLocalizationTextRead("Console.Button.RemoveTooltip"),
            null,
            PConsoleRemoveHandle);
        pConsoleClearButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.ClearDone"),
            "PRosterClearDone.svg",
            LLocalization.LLocalizationTextRead("Console.Button.ClearDoneTooltip"),
            null,
            PConsoleDoneHandle);
        pConsoleEmptyButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.ClearAll"),
            "PRosterClearAll.svg",
            LLocalization.LLocalizationTextRead("Console.Button.ClearAllTooltip"),
            null,
            PConsoleAllHandle);
        pConsoleTabsButton = PConsoleButtonBuild(
            LLocalization.LLocalizationTextRead("Console.Button.ClearTabs"),
            "PConsoleClearTabs.svg",
            LLocalization.LLocalizationTextRead("Console.Button.ClearTabsTooltip"),
            null,
            PConsoleTabsHandle);
        pConsoleAutoBox = PConsoleAutoBuild();
        pConsoleRelayCombo = PConsoleComboBuild();
        pConsoleSaveButton = PConsoleInlineBuild("PConsoleSave.svg");
        pConsoleExportButton = PConsoleInlineBuild("PExportExport.svg");
        pConsoleImportButton = PConsoleInlineBuild("PExportImport.svg");
        pConsoleSceneControls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pConsoleSceneControls.Children.Add(pConsoleRelayCombo);
        pConsoleSceneControls.Children.Add(pConsoleSaveButton);
        pConsoleSceneControls.Children.Add(pConsoleExportButton);
        pConsoleSceneControls.Children.Add(pConsoleImportButton);
        pConsoleSceneHost = new ContentControl { Content = pConsoleSceneControls };
        pConsoleSceneSeparator = PConsoleSeparatorBuild();
        pConsolePreviousButton = PConsoleSwitchBuild(
            "PConsolePrevious.svg",
            LLocalization.LLocalizationTextRead("Console.Previous.Tooltip"),
            PConsolePreviousHandle);
        pConsoleNextButton = PConsoleSwitchBuild(
            "PConsoleNext.svg",
            LLocalization.LLocalizationTextRead("Console.Next.Tooltip"),
            PConsoleNextHandle);

        Content = PConsoleBuild();
        PConsoleCurrent = this;
        PConsoleSceneAttach();

        pConsoleSchedule.LScheduleChange += PConsoleScheduleHandle;
        pConsoleSchedule.LScheduleItemChange += PConsoleItemHandle;
        LStation.LStationChange += PConsoleStationHandle;
        PConsoleDepotAttach();
        Unloaded += PConsoleUnloadHandle;

        PConsoleScheduleHandle(pConsoleSchedule);
        Dispatcher.BeginInvoke(new Action(() => pConsoleSchedule.LScheduleLoad()));
    }

    public static PConsole? PConsoleCurrent { get; private set; }

    public void PConsoleUpdate() => PConsoleProgressUpdate();

    public UIElement PConsoleSceneRead() => pConsoleSceneControls;

    public void PConsoleSceneSet(UIElement? pSceneControls)
    {
        pConsoleSceneHost.Content = pSceneControls;
        pConsoleSceneSeparator.Visibility = pSceneControls is null ? Visibility.Collapsed : Visibility.Visible;
    }

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
        pRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PConsoleSwitchWidth) });
        pRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PConsoleSwitchWidth) });
        Grid.SetColumn(pConsolePreviousButton, 0);
        Grid.SetColumn(pCard, 1);
        Grid.SetColumn(pConsoleNextButton, 2);
        pRoot.Children.Add(pConsolePreviousButton);
        pRoot.Children.Add(pCard);
        pRoot.Children.Add(pConsoleNextButton);
        return pRoot;
    }
}
