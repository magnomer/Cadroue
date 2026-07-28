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

        PConsoleStationRead().LStationAutoActive = pConsoleAutoBox.IsChecked == true;
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

    private LWorkItem? PConsoleOwnedRunning => pConsoleSchedule.LScheduleRecords
        .FirstOrDefault(pWorkItem => pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning
            && PConsoleStationRead().LStationRunner.LRunnerOwnerCheck(pWorkItem));

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
        LWorkItem? pRunning = PConsoleOwnedRunning;

        PConsoleProgressSet(pRunning?.LWorkProgress ?? 0);
        pConsoleStartButton.IsEnabled = !pRunner.LRunnerRunning && pTotal > 0;
        pConsolePauseButton.IsEnabled = pRunner.LRunnerRunning;
        pConsoleCancelButton.IsEnabled = pRunning is not null;

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
                : $"{pRunning.LWorkOutputName}  •  {pRunning.LWorkProgress:P0}  •  {pDoneText}";

        pConsoleStationLabel.Text = pBoard.Length > 1
            ? $"{pStation.LStationLabel} {Array.IndexOf(pBoard, pStation) + 1} of {pBoard.Length}"
            : pStation.LStationLabel;
    }

    private void PConsoleStartHandle(object pSender, RoutedEventArgs pArguments)
    {
        LRunner pRunner = PConsoleStationRead().LStationRunner;
        pRunner.LRunnerProgramPath = App.LRendererProgramCurrent;
        pRunner.LRunnerStart();
    }

    private void PConsolePauseHandle(object pSender, RoutedEventArgs pArguments)
        => PConsoleStationRead().LStationRunner.LRunnerPause();

    private void PConsoleCancelHandle(object pSender, RoutedEventArgs pArguments)
    {
        if (PConsoleOwnedRunning is not { } pRunning)
        {
            return;
        }

        MessageBoxResult pAnswer = MessageBox.Show(
            $"Cancel '{pRunning.LWorkOutputName}'?\n\nThe partly written output file is deleted and the job returns to Pending.",
            "Cancel running job",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (pAnswer == MessageBoxResult.Yes)
        {
            PConsoleStationRead().LStationRunner.LRunnerCancel();
        }
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
        pConsoleSchedule.LScheduleDoneClear();
    }

    private void PConsoleAllHandle(object pSender, RoutedEventArgs pArguments)
    {
        pConsoleSchedule.LScheduleAllClear();
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
