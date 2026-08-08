using System.Windows;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

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
        PRosterRowUpdate(pWorkItem);

        if (pNotice != LScheduleNotice.LScheduleNoticeProgress
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
        pRosterSchedule.LScheduleItemChange -= PRosterItemHandle;
        LMediaProbe.LMediaProbeReady -= PRosterMediaHandle;
        LMediaProbe.LMediaLoudnessReady -= PRosterLoudnessHandle;
        IsVisibleChanged -= PRosterVisibleHandle;
        Unloaded -= PRosterUnloadHandle;
        pRosterStation.LStationClose();
    }
}
