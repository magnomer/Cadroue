using System.Windows;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIVeneer.PDeck;

public sealed partial class PConsole
{
    private static LDepotWatch? pConsoleDepotWatch;

    private void PConsoleScheduleHandle(LScheduleContract lSchedule)
    {
        PConsoleProgressUpdate();
    }

    private void PConsoleItemHandle(LWorkItem pWorkItem, LScheduleNotice pNotice)
    {
        if (pNotice == LScheduleNotice.LScheduleNoticeProgress)
        {
            if (PConsoleStationRead().LStationRunner.LRunnerOwnerCheck(pWorkItem))
            {
                PConsoleProgressSet(pWorkItem.LWorkProgress);
            }

            return;
        }

        PConsoleProgressDefer();
    }

    private void PConsoleDepotAttach()
    {
        pConsoleDepotWatch ??= PConsoleWatchCreate();
        pConsoleDepotWatch.LDepotChange += PConsoleDepotHandle;
    }

    private static LDepotWatch PConsoleWatchCreate()
    {
        var pDepotWatch = new LDepotWatch();
        pDepotWatch.LDepotWatchStart();
        return pDepotWatch;
    }

    private void PConsoleDepotHandle()
    {
        Dispatcher.BeginInvoke(new Action(() => pConsoleSchedule.LScheduleLoad()));
    }

    private void PConsoleUnloadHandle(object pSender, RoutedEventArgs pArguments)
    {
        pConsoleSchedule.LScheduleChange -= PConsoleScheduleHandle;
        pConsoleSchedule.LScheduleItemChange -= PConsoleItemHandle;
        LStation.LStationChange -= PConsoleStationHandle;
        if (pConsoleDepotWatch is not null)
        {
            pConsoleDepotWatch.LDepotChange -= PConsoleDepotHandle;
        }

        Unloaded -= PConsoleUnloadHandle;
        if (ReferenceEquals(PConsoleCurrent, this))
        {
            PConsoleCurrent = null;
        }
    }
}
