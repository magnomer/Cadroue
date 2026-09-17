using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.ShellEngine;
using LEncode = Cadroue.ShellEngine.LEncode;

namespace Cadroue.UIVeneer.PDeck;

public sealed partial class PConsole
{
    private static readonly Duration PConsoleProgressGlide =
        new(TimeSpan.FromSeconds(LEncode.LEncodeStatsPeriod));

    private bool pConsoleProgressPending;
    private double pConsoleProgressShown;

    private void PConsoleProgressSet(double pConsoleTarget)
    {
        double pConsoleClamped = Math.Clamp(pConsoleTarget, 0, 1);
        if (pConsoleClamped.Equals(pConsoleProgressShown))
        {
            return;
        }

        bool pConsoleBackward = pConsoleClamped < pConsoleProgressShown;
        pConsoleProgressShown = pConsoleClamped;

        if (pConsoleClamped <= 0 || pConsoleBackward)
        {
            pConsoleProgress.BeginAnimation(RangeBase.ValueProperty, null);
            pConsoleProgress.Value = pConsoleClamped;
            return;
        }

        pConsoleProgress.BeginAnimation(
            RangeBase.ValueProperty,
            new DoubleAnimation
            {
                To = pConsoleClamped,
                Duration = PConsoleProgressGlide,
                FillBehavior = FillBehavior.HoldEnd
            });
    }

    private void PConsoleProgressDefer()
    {
        if (pConsoleProgressPending)
        {
            return;
        }

        pConsoleProgressPending = true;
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
        {
            pConsoleProgressPending = false;
            PConsoleProgressUpdate();
        }));
    }

    private void PConsoleProgressUpdate()
    {
        int pTotal = pConsoleSchedule.LScheduleRecords.Count;
        int pDone = pConsoleSchedule.LScheduleDoneCount;
        LStation[] pBoard = LStation.LStationBoardRead();
        LStation pStation = PConsoleStationRead();
        LRunner pRunner = pStation.LStationRunner;
        LWorkItem[] pRunningItems = PConsoleRunningRead();
        LWorkItem? pRunning = pRunningItems.FirstOrDefault();

        PConsoleProgressSet(
            pRunningItems.Length == 0
                ? 0
                : pRunningItems.Average(pWorkItem => pWorkItem.LWorkProgress));
        bool pRunnerActive = pRunner.LRunnerRunning;
        pConsoleStartButton.Visibility = pRunnerActive ? Visibility.Collapsed : Visibility.Visible;
        pConsoleStartButton.IsEnabled = pTotal > 0;
        pConsolePauseButton.Visibility = pRunnerActive ? Visibility.Visible : Visibility.Collapsed;
        pConsolePauseButton.IsEnabled = true;
        pConsoleCancelButton.IsEnabled = pRunning is not null;
        pConsoleStopButton.IsEnabled = pRunner.LRunnerRunning || pRunner.LRunnerSuspended;

        pConsoleRemoveButton.IsEnabled = pStation.LStationSelectionRead()
            .Any(pWorkItem => pWorkItem.LWorkStateCurrent != LWorkState.LWorkStateRunning);
        pConsoleClearButton.IsEnabled = pConsoleSchedule.LScheduleRecords.Any(pWorkItem =>
            pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateDone);
        pConsoleEmptyButton.IsEnabled = pConsoleSchedule.LScheduleRecords.Any(pWorkItem =>
            pWorkItem.LWorkStateCurrent != LWorkState.LWorkStateRunning);
        Visibility pSwitchVisibility = pBoard.Length > 1 ? Visibility.Visible : Visibility.Collapsed;
        pConsolePreviousButton.Visibility = pSwitchVisibility;
        pConsoleNextButton.Visibility = pSwitchVisibility;

        bool pRunningState = pRunner.LRunnerRunning;
        pConsoleSpinner.Visibility = pRunningState ? Visibility.Visible : Visibility.Collapsed;
        pConsoleRestIcon.Visibility = pRunningState ? Visibility.Collapsed : Visibility.Visible;
        PConsoleSpinnerSet(pRunningState);

        pConsoleAutoApplying = true;
        pConsoleAutoBox.IsChecked = pStation.LStationAutoActive;
        pConsoleAutoApplying = false;

        bool pPausedState = !pRunner.LRunnerRunning && !pRunner.LRunnerSuspended;
        string pRunState = LLocalization.LLocalizationTextRead(
            pRunner.LRunnerSuspended
                ? "Console.State.Suspended"
                : pRunner.LRunnerRunning ? "Console.State.Running" : "Console.State.Paused");
        string pDoneText = LLocalization.LLocalizationFormat("Console.Done.Format", pDone, pTotal);

        string pStatusText = pTotal == 0
            ? LLocalization.LLocalizationTextRead("Console.Status.Empty")
            : pRunning is null
                ? LLocalization.LLocalizationFormat(
                    "Console.Status.Pending",
                    pRunState,
                    pDoneText,
                    pConsoleSchedule.LSchedulePendingRead().Count)
                : pRunningItems.Length > 1
                    ? LLocalization.LLocalizationFormat(
                        "Console.Status.MultipleRunning",
                        pRunningItems.Length,
                        pRunningItems.Average(pWorkItem => pWorkItem.LWorkProgress),
                        pDoneText)
                    : LLocalization.LLocalizationFormat(
                        "Console.Status.Running",
                        pRunning.LWorkOutputName,
                        pDoneText,
                        PConsoleStageFormat(pRunning.LWorkStageCurrent),
                        pRunning.LWorkProgress);
        PConsoleStatusSet(pStatusText, pTotal > 0 && pPausedState ? pRunState : null);

        string pStationLabel = string.Equals(pStation.LStationLabel, "Background worklist", StringComparison.Ordinal)
            ? LLocalization.LLocalizationTextRead("Console.Station.Background")
            : pStation.LStationLabel;
        pConsoleStationLabel.Text = pBoard.Length > 1
            ? LLocalization.LLocalizationFormat(
                "Console.Station.Numbered",
                pStationLabel,
                Array.IndexOf(pBoard, pStation) + 1,
                pBoard.Length)
            : pStationLabel;
    }

    private void PConsoleStatusSet(string pStatusText, string? pAccentText)
    {
        pConsoleStatus.Inlines.Clear();
        int pAccentIndex = string.IsNullOrEmpty(pAccentText)
            ? -1
            : pStatusText.IndexOf(pAccentText, StringComparison.Ordinal);
        if (pAccentIndex < 0)
        {
            pConsoleStatus.Inlines.Add(new Run(pStatusText));
            return;
        }

        if (pAccentIndex > 0)
        {
            pConsoleStatus.Inlines.Add(new Run(pStatusText[..pAccentIndex]));
        }

        pConsoleStatus.Inlines.Add(new Run(pAccentText!)
        {
            Foreground = PRosterTheme.PRosterDoneBrush,
            FontWeight = FontWeights.Bold
        });
        int pAccentEnd = pAccentIndex + pAccentText!.Length;
        if (pAccentEnd < pStatusText.Length)
        {
            pConsoleStatus.Inlines.Add(new Run(pStatusText[pAccentEnd..]));
        }
    }

    private static string PConsoleStageFormat(LWorkStage pWorkStage) =>
        LLocalization.LLocalizationTextRead(pWorkStage switch
    {
        LWorkStage.LWorkStageExtract => "Console.Stage.Extract",
        LWorkStage.LWorkStageAnalyze => "Console.Stage.Analyze",
        LWorkStage.LWorkStageProcess => "Console.Stage.Process",
        LWorkStage.LWorkStageMux => "Console.Stage.Mux",
        LWorkStage.LWorkStagePassthrough => "Console.Stage.Copy",
        _ => "Console.Stage.Encode"
    });
}
