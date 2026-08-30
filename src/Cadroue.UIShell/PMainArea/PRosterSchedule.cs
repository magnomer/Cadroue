using System.Windows;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private void PRosterScheduleHandle(LScheduleContract lSchedule)
    {
        PRosterQueueRebuild();
        PRosterDetailUpdate();
    }

    private void PRosterItemHandle(LWorkItem pWorkItem, LScheduleNotice pNotice)
    {
        if (pNotice == LScheduleNotice.LScheduleNoticeStatus)
        {
            PRosterQueueRebuild();
        }
        else
        {
            PRosterRowUpdate(pWorkItem);
        }

        if (!ReferenceEquals(pWorkItem, PRosterSelectRead()))
        {
            return;
        }

        if (pNotice == LScheduleNotice.LScheduleNoticeProgress)
        {
            PRosterDetailDefer();
        }
        else
        {
            PRosterDetailUpdate();
        }
    }

    private void PRosterDetailDefer()
    {
        if (pRosterDetailPending)
        {
            return;
        }

        pRosterDetailPending = true;
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
        {
            pRosterDetailPending = false;
            if (!pRosterClosed)
            {
                PRosterDetailUpdate();
            }
        }));
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
        pRosterSchedule.LScheduleItemChange -= PRosterItemHandle;
        IsVisibleChanged -= PRosterVisibleHandle;
        Unloaded -= PRosterUnloadHandle;
        pRosterStation.LStationClose();
    }
}
