using System.Windows;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PCabin;

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
        if (LRoster.LRosterDetailPending)
        {
            return;
        }

        LRoster.LRosterPendingSet(true);
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
        {
            LRoster.LRosterPendingSet(false);
            if (!LRoster.LRosterClosed)
            {
                PRosterDetailUpdate();
            }
        }));
    }

    private void PRosterSelectHandle()
    {
        PRosterDetailUpdate();
        PWindow.PWindowConsoleRead()?.LConsole.LConsoleUpdate();
    }

    private void PRosterUnloadHandle(object pSender, RoutedEventArgs pArguments) => PRosterClose();

    public void PRosterClose()
    {
        if (!LRoster.LRosterCloseSet())
        {
            return;
        }

        pRosterElapsedTimer.Stop();
        pRosterElapsedTimer.Tick -= PRosterElapsedTick;
        pRosterSchedule.LScheduleChange -= PRosterScheduleHandle;
        pRosterSchedule.LScheduleItemChange -= PRosterItemHandle;
        IsVisibleChanged -= PRosterVisibleHandle;
        Unloaded -= PRosterUnloadHandle;
        pRosterStation.LStationClose();
    }
}
