using System.ComponentModel;
using System.Windows;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private void PRosterScheduleHandle(LScheduleContract lSchedule)
    {
        PRosterWatchDetach();
        foreach (LWorkItem lWorkItem in lSchedule.LScheduleRecords)
        {
            lWorkItem.PropertyChanged += PRosterItemHandle;
            pRosterWatchedItems.Add(lWorkItem);
        }

        PRosterQueueRebuild();
        PRosterDetailUpdate();
    }

    private void PRosterItemHandle(object? pSender, PropertyChangedEventArgs pArguments)
    {
        if (pSender is not LWorkItem pWorkItem)
        {
            return;
        }

        PRosterRowUpdate(pWorkItem);

        if (pArguments.PropertyName != nameof(LWorkItem.LWorkProgress)
            && ReferenceEquals(pWorkItem, PRosterSelectRead()))
        {
            PRosterDetailUpdate();
        }
    }

    private void PRosterSelectHandle()
    {
        PRosterDetailUpdate();
        PConsole.PConsoleCurrent?.PConsoleUpdate();
    }

    private void PRosterUnloadHandle(object pSender, RoutedEventArgs pArguments) => PRosterClose();

    public void PRosterClose()
    {
        if (pRosterClosed)
        {
            return;
        }

        pRosterClosed = true;
        pRosterSchedule.LScheduleChange -= PRosterScheduleHandle;
        IsVisibleChanged -= PRosterVisibleHandle;
        Unloaded -= PRosterUnloadHandle;
        pRosterStation.LStationClose();
        PRosterWatchDetach();
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
