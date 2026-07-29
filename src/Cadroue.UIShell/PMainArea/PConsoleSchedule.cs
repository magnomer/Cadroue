using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using Cadroue.Core;
using Cadroue.ShellEngine;
using LEncode = Cadroue.ShellEngine.LEncode;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PConsole
{
    private static LDepotWatch? pConsoleDepotWatchShared;

    private static readonly Duration PConsoleProgressGlide =
        new(TimeSpan.FromSeconds(LEncode.LEncodeStatsPeriod));

    private bool pConsoleProgressPending;
    private bool pConsoleAutoApplying;
    private double pConsoleProgressShown;
    private LStation? pConsoleStation;

    private void PConsoleAutoHandle(object pSender, RoutedEventArgs pArguments)
    {
        if (pConsoleAutoApplying)
        {
            return;
        }

        bool pConsoleAutoResume = pConsoleAutoBox.IsChecked == true;
        PConsoleStationRead().LStationAutoActive = pConsoleAutoResume;
        App.LPreferenceAutoResumeSet(pConsoleAutoResume);
        PConsoleProgressUpdate();
    }

    private LStation PConsoleStationRead()
    {
        LStation[] pBoard = LStation.LStationBoardRead();
        if (pConsoleStation is null || !pBoard.Contains(pConsoleStation))
        {
            pConsoleStation = pBoard[0];
        }

        return pConsoleStation;
    }

    public void PConsoleStationSet(LStation pStation)
    {
        if (ReferenceEquals(pConsoleStation, pStation))
        {
            return;
        }

        pConsoleStation = pStation;
        PConsoleProgressUpdate();
    }

    private void PConsoleStationHandle() => PConsoleProgressUpdate();

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

    private void PConsoleStationStep(int pStep)
    {
        LStation[] pBoard = LStation.LStationBoardRead();
        if (pBoard.Length <= 1)
        {
            return;
        }

        int pIndex = Array.IndexOf(pBoard, PConsoleStationRead());
        pConsoleStation = pBoard[((pIndex + pStep) % pBoard.Length + pBoard.Length) % pBoard.Length];
        PConsoleProgressUpdate();
    }

    private void PConsolePreviousHandle(object pSender, RoutedEventArgs pArguments) => PConsoleStationStep(-1);

    private void PConsoleNextHandle(object pSender, RoutedEventArgs pArguments) => PConsoleStationStep(1);

    private LWorkItem[] PConsoleRunningRead() => pConsoleSchedule.LScheduleRecords
        .Where(pWorkItem => pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning
            && PConsoleStationRead().LStationRunner.LRunnerOwnerCheck(pWorkItem))
        .ToArray();

    private void PConsoleScheduleHandle(LSchedule lSchedule)
    {
        PConsoleWatchDetach();
        foreach (LWorkItem lWorkItem in lSchedule.LScheduleRecords)
        {
            lWorkItem.PropertyChanged += PConsoleItemHandle;
            pConsoleWatchedItems.Add(lWorkItem);
        }

        PConsoleProgressUpdate();
    }

    private void PConsoleItemHandle(object? pSender, PropertyChangedEventArgs pArguments)
    {
        PConsoleProgressSchedule();
    }

    private void PConsoleProgressSchedule()
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

        PConsoleProgressSet(pRunningItems.Length == 0 ? 0 : pRunningItems.Average(pWorkItem => pWorkItem.LWorkProgress));
        pConsoleStartButton.IsEnabled = !pRunner.LRunnerRunning && pTotal > 0;
        pConsolePauseButton.IsEnabled = pRunner.LRunnerRunning;
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

        pConsoleAutoApplying = true;
        pConsoleAutoBox.IsChecked = pStation.LStationAutoActive;
        pConsoleAutoApplying = false;

        string pRunState = pRunner.LRunnerSuspended
            ? "Suspended"
            : pRunner.LRunnerRunning ? "Running" : "Paused";
        string pDoneText = $"{pDone} of {pTotal} done";

        pConsoleStatus.Text = pTotal == 0
            ? "No work queued."
            : pRunning is null
                ? $"{pRunState}  •  {pDoneText}, {pConsoleSchedule.LSchedulePendingRead().Count} pending"
                : pRunningItems.Length > 1
                    ? $"{pRunningItems.Length} jobs running  •  {pRunningItems.Average(pWorkItem => pWorkItem.LWorkProgress):P0}  •  {pDoneText}"
                    : $"{pRunning.LWorkOutputName}  •  {pRunning.LWorkProgress:P0}  •  {pDoneText}";

        pConsoleStationLabel.Text = pBoard.Length > 1
            ? $"{pStation.LStationLabel} {Array.IndexOf(pBoard, pStation) + 1} of {pBoard.Length}"
            : pStation.LStationLabel;
    }

    private void PConsoleStartHandle(object pSender, RoutedEventArgs pArguments)
    {
        LRunner pRunner = PConsoleStationRead().LStationRunner;
        PConsoleRunnerApply(pRunner);
        pRunner.LRunnerStart();
    }

    private void PConsolePauseHandle(object pSender, RoutedEventArgs pArguments)
        => PConsoleStationRead().LStationRunner.LRunnerPause();

    private void PConsoleCancelHandle(object pSender, RoutedEventArgs pArguments)
    {
        LWorkItem[] pRunningItems = PConsoleRunningRead();
        if (pRunningItems.Length == 0)
        {
            return;
        }

        string pCancelSubject = pRunningItems.Length > 1
            ? $"all {pRunningItems.Length} running jobs"
            : $"'{pRunningItems[0].LWorkOutputName}'";
        MessageBoxResult pAnswer = MessageBox.Show(
            $"Cancel {pCancelSubject}?\n\nThe partly written output file is deleted and processing moves on to the next job.",
            "Cancel running job",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (pAnswer == MessageBoxResult.Yes)
        {
            LRunner pCancelRunner = PConsoleStationRead().LStationRunner;
            foreach (LWorkItem pCancelItem in pRunningItems)
            {
                pCancelRunner.LRunnerJobCancel(pCancelItem.LWorkId);
            }
        }
    }

    private void PConsoleStopHandle(object pSender, RoutedEventArgs pArguments)
    {
        LStation pStopStation = PConsoleStationRead();
        pStopStation.LStationAutoActive = false;
        pStopStation.LStationRunner.LRunnerCancel();
    }

    private void PConsoleRemoveHandle(object pSender, RoutedEventArgs pArguments)
    {
        LWorkItem[] pConsoleRemovable = PConsoleStationRead().LStationSelectionRead()
            .Where(pWorkItem => pWorkItem.LWorkStateCurrent != LWorkState.LWorkStateRunning)
            .ToArray();
        if (pConsoleRemovable.Length == 0)
        {
            return;
        }

        foreach (LWorkItem pWorkItem in pConsoleRemovable)
        {
            pConsoleSchedule.LScheduleRemove(pWorkItem.LWorkId);
        }
    }

    private void PConsoleDoneHandle(object pSender, RoutedEventArgs pArguments)
    {
        if (!PConsoleDestructiveConfirm("Remove every finished job from the queue?")) return;
        pConsoleSchedule.LScheduleDoneClear();
    }

    private void PConsoleAllHandle(object pSender, RoutedEventArgs pArguments)
    {
        if (!PConsoleDestructiveConfirm("Remove every queued job except the running one?")) return;
        pConsoleSchedule.LScheduleAllClear();
    }

    private bool PConsoleDestructiveConfirm(string pConsoleQuestion)
    {
        if (!App.LPreferenceStateCurrent.LPreferenceConfirmDestructive)
        {
            return true;
        }

        return MessageBox.Show(
            pConsoleQuestion,
            "Confirm",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    private static void PConsoleRunnerApply(LRunner pRunner)
    {
        LPreferenceState lPreferenceState = App.LPreferenceStateCurrent;
        pRunner.LRunnerProgramPath = App.LRendererProgramCurrent;
        pRunner.LRunnerParallelMaximum = (int)lPreferenceState.LPreferenceParallelMaximum;
        pRunner.LRunnerFailurePaused = lPreferenceState.LPreferenceFailurePaused;
        pRunner.LRunnerRetryAllowed = lPreferenceState.LPreferenceRetryAllowed;
        pRunner.LRunnerRetryMaximum = (int)lPreferenceState.LPreferenceRetryMaximum;
    }

    private void PConsoleDepotAttach()
    {
        pConsoleDepotWatchShared ??= PConsoleDepotWatchCreate();
        pConsoleDepotWatchShared.LDepotChange += PConsoleDepotHandle;
    }

    private static LDepotWatch PConsoleDepotWatchCreate()
    {
        var pDepotWatch = new LDepotWatch();
        pDepotWatch.LDepotWatchStart();
        return pDepotWatch;
    }

    private void PConsoleDepotHandle()
    {
        Dispatcher.BeginInvoke(new Action(() => pConsoleSchedule.LScheduleReload()));
    }

    private void PConsoleUnloadHandle(object pSender, RoutedEventArgs pArguments)
    {
        pConsoleSchedule.LScheduleChange -= PConsoleScheduleHandle;
        LStation.LStationChange -= PConsoleStationHandle;
        if (pConsoleDepotWatchShared is not null)
        {
            pConsoleDepotWatchShared.LDepotChange -= PConsoleDepotHandle;
        }

        Unloaded -= PConsoleUnloadHandle;
        PConsoleWatchDetach();
        if (ReferenceEquals(PConsoleCurrent, this))
        {
            PConsoleCurrent = null;
        }
    }

    private void PConsoleWatchDetach()
    {
        foreach (LWorkItem pWorkItem in pConsoleWatchedItems)
        {
            pWorkItem.PropertyChanged -= PConsoleItemHandle;
        }

        pConsoleWatchedItems.Clear();
    }
}
