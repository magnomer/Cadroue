using System.ComponentModel;
using System.Windows;
using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private static LDepotWatch? pRosterDepotWatchShared;

    private bool pRosterProgressPending;

    private LWorkItem? PRosterOwnedRunning => pRosterSchedule.LScheduleRecords
        .FirstOrDefault(pWorkItem => pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning
            && pRosterRunner.LRunnerOwnerCheck(pWorkItem));

    private void PRosterScheduleHandle(LSchedule lSchedule)
    {
        PRosterWatchDetach();
        foreach (LWorkItem lWorkItem in lSchedule.LScheduleRecords)
        {
            lWorkItem.PropertyChanged += PRosterItemHandle;
            pRosterWatchedItems.Add(lWorkItem);
        }

        PRosterQueueRebuild();
        PRosterProgressUpdate();
        PRosterDetailUpdate();
    }

    private void PRosterItemHandle(object? pSender, PropertyChangedEventArgs pArguments)
    {
        if (pSender is not LWorkItem pWorkItem)
        {
            return;
        }

        PRosterRowUpdate(pWorkItem);
        PRosterProgressSchedule();

        if (pArguments.PropertyName != nameof(LWorkItem.LWorkProgress)
            && ReferenceEquals(pWorkItem, PRosterSelectRead()))
        {
            PRosterDetailUpdate();
        }
    }

    private void PRosterProgressSchedule()
    {
        if (pRosterProgressPending)
        {
            return;
        }

        pRosterProgressPending = true;
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
        {
            pRosterProgressPending = false;
            PRosterProgressUpdate();
        }));
    }

    private void PRosterProgressUpdate()
    {
        int pTotal = pRosterSchedule.LScheduleRecords.Count;
        int pDone = pRosterSchedule.LScheduleDoneCount;
        LWorkItem? pRunning = PRosterOwnedRunning;

        pRosterProgress.Value = pRunning?.LWorkProgress ?? 0;
        pRosterStartButton.IsEnabled = !pRosterRunner.LRunnerRunning && pTotal > 0;
        pRosterPauseButton.IsEnabled = pRosterRunner.LRunnerRunning;
        pRosterCancelButton.IsEnabled = pRunning is not null;

        LWorkItem? pSelected = PRosterSelectRead();
        pRosterRemoveButton.IsEnabled = pSelected is not null
            && pSelected.LWorkStateCurrent != LWorkState.LWorkStateRunning;
        pRosterClearButton.IsEnabled = pRosterSchedule.LScheduleRecords.Any(pWorkItem =>
            pWorkItem.LWorkStateCurrent is LWorkState.LWorkStateDone or LWorkState.LWorkStateFailed);

        string pRunState = pRosterRunner.LRunnerSuspended
            ? "Suspended"
            : pRosterRunner.LRunnerRunning ? "Running" : "Paused";
        string pQueueText = $"{pDone} of {pTotal} done, {pRosterSchedule.LSchedulePendingRead().Count} pending";

        pRosterStatus.Text = pTotal == 0
            ? "No work queued."
            : pRunning is null
                ? $"{pRunState}  •  {pQueueText}"
                : $"{pRunState}  •  {pRunning.LWorkOutputName}  {pRunning.LWorkProgress:P0}  •  {pQueueText}";
    }

    private void PRosterSelectHandle()
    {
        PRosterDetailUpdate();
        PRosterProgressUpdate();
    }

    private void PRosterStartHandle(object pSender, RoutedEventArgs pArguments)
    {
        pRosterRunner.LRunnerProgramPath = App.LRendererProgramCurrent;
        pRosterRunner.LRunnerStart();
    }

    private void PRosterPauseHandle(object pSender, RoutedEventArgs pArguments) => pRosterRunner.LRunnerPause();

    private void PRosterCancelHandle(object pSender, RoutedEventArgs pArguments)
    {
        if (PRosterOwnedRunning is not { } pRunning)
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
            pRosterRunner.LRunnerCancel();
        }
    }

    private void PRosterRemoveHandle(object pSender, RoutedEventArgs pArguments)
    {
        if (PRosterSelectRead() is not { } pWorkItem
            || pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning)
        {
            return;
        }

        pRosterSchedule.LScheduleRemove(pWorkItem.LWorkId);
    }

    private void PRosterClearHandle(object pSender, RoutedEventArgs pArguments)
    {
        pRosterSchedule.LScheduleClear();
    }

    private void PRosterDepotAttach()
    {
        pRosterDepotWatchShared ??= PRosterDepotWatchCreate();
        pRosterDepotWatchShared.LDepotChange += PRosterDepotHandle;
    }

    private static LDepotWatch PRosterDepotWatchCreate()
    {
        var pDepotWatch = new LDepotWatch();
        pDepotWatch.LDepotWatchStart();
        return pDepotWatch;
    }

    private void PRosterDepotHandle()
    {
        Dispatcher.BeginInvoke(new Action(() => pRosterSchedule.LScheduleReload()));
    }

    private void PRosterUnloadHandle(object pSender, RoutedEventArgs pArguments)
    {
        pRosterSchedule.LScheduleChange -= PRosterScheduleHandle;
        if (pRosterDepotWatchShared is not null)
        {
            pRosterDepotWatchShared.LDepotChange -= PRosterDepotHandle;
        }

        Unloaded -= PRosterUnloadHandle;
        PRosterWatchDetach();
        pRosterRunner.LRunnerDispose();
    }

    private void PRosterWatchDetach()
    {
        foreach (LWorkItem pWorkItem in pRosterWatchedItems)
        {
            pWorkItem.PropertyChanged -= PRosterItemHandle;
        }

        pRosterWatchedItems.Clear();
    }
}
