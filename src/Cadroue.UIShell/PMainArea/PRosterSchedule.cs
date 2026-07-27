using System.ComponentModel;
using System.Windows;
using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private void PRosterScheduleHandle(LSchedule lSchedule)
    {
        PRosterWatchStop();
        foreach (LWorkItem lWorkItem in lSchedule.LScheduleRecords)
        {
            lWorkItem.PropertyChanged += PRosterItemHandle;
            pRosterWatchedItems.Add(lWorkItem);
        }

        PRosterProgressUpdate();
    }

    private void PRosterItemHandle(object? pSender, PropertyChangedEventArgs pArguments)
    {
        PRosterProgressUpdate();
        if (ReferenceEquals(pSender, pRosterTable.SelectedItem))
        {
            PRosterDetailUpdate();
        }
    }

    private void PRosterProgressUpdate()
    {
        int pTotal = lRosterSchedule.LScheduleRecords.Count;
        int pDone = lRosterSchedule.LScheduleDoneCount;
        LWorkItem? pRunning = lRosterSchedule.LScheduleRecords
            .FirstOrDefault(pWorkItem => pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning);

        pRosterProgress.Maximum = 1;
        pRosterProgress.Value = pRunning?.LWorkProgress ?? 0;
        pRosterStartButton.IsEnabled = !lRosterSchedule.LScheduleRunning && pTotal > 0;
        pRosterPauseButton.IsEnabled = lRosterSchedule.LScheduleRunning;

        string pRunState = lRosterRunner.LRunnerSuspended
            ? "Suspended"
            : lRosterSchedule.LScheduleRunning ? "Running" : "Paused";
        string pQueueText = $"{pDone} of {pTotal} done, {lRosterSchedule.LSchedulePendingRead().Count} pending";

        pRosterStatus.Text = pTotal == 0
            ? "No work queued."
            : pRunning is null
                ? $"{pRunState}  -  {pQueueText}"
                : $"{pRunState}  -  {pRunning.LWorkOutputName}  {pRunning.LWorkProgress:P0}  -  {pQueueText}";
    }

    private void PRosterStartHandle(object pSender, RoutedEventArgs pArguments) => lRosterRunner.LRunnerStart();

    private void PRosterPauseHandle(object pSender, RoutedEventArgs pArguments) => lRosterRunner.LRunnerPause();

    private void PRosterCancelHandle(object pSender, RoutedEventArgs pArguments) => lRosterRunner.LRunnerCancel();

    private void PRosterDepotHandle()
    {
        Dispatcher.BeginInvoke(new Action(() => lRosterSchedule.LScheduleReload()));
    }

    private void PRosterUnloadHandle(object pSender, RoutedEventArgs pArguments)
    {
        lRosterSchedule.LScheduleChange -= PRosterScheduleHandle;
        lRosterDepotWatch.LDepotChange -= PRosterDepotHandle;
        lRosterDepotWatch.Dispose();
        Unloaded -= PRosterUnloadHandle;
        PRosterWatchStop();
    }

    private void PRosterWatchStop()
    {
        foreach (LWorkItem pWorkItem in pRosterWatchedItems)
        {
            pWorkItem.PropertyChanged -= PRosterItemHandle;
        }

        pRosterWatchedItems.Clear();
    }
}
