using System.Windows;
using Cadroue.UIShell.PSCasement;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PToolbar;

public partial class PRail
{
    private async void PTabRelayRun(PTabRecord pTabRecord, Point pDevicePoint)
    {
        Window? pRelayWindow = Window.GetWindow(this);
        if (pRelayWindow is null || PTabInsideCheck(pRelayWindow, pDevicePoint))
        {
            return;
        }

        if (pTabRecord.PTabRelayState)
        {
            return;
        }

        if (pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabBusyCheck())
        {
            PTabBusyShow(pRelayWindow, pTabRecord);
            return;
        }

        Point pDipPoint = PTabDipRead(pRelayWindow, pDevicePoint);
        LRelay lRelay = pTabRecord.PTabWorkspace.PWorkspaceRelayCreate(pTabRecord, pDipPoint.X, pDipPoint.Y);
        string lRelayFilePath;
        try
        {
            lRelayFilePath = LRelayStore.LRelayFileSave(lRelay);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Relay payload could not be written; tab kept", lException);
            return;
        }

        pTabRecord.PTabRelayState = true;
        try
        {
            switch (await LRelayChannel.LRelayDispatch(lRelayFilePath, pDevicePoint.X, pDevicePoint.Y))
            {
                case LRelayOutcome.LRelayOutcomeExisting:
                case LRelayOutcome.LRelayOutcomeLaunched:
                    if (pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabBusyCheck())
                    {
                        LTraceLog.LTraceInfoRecord(
                            $"Tab '{pTabRecord.PTabTitle}' copied: the worklist started working during the relay");
                        return;
                    }

                    LTraceLog.LTraceInfoRecord($"Tab '{pTabRecord.PTabTitle}' relayed");
                    pStrip?.PStripClose(pTabRecord);
                    return;
                default:
                    PTabKeptShow(pRelayWindow, pTabRecord);
                    return;
            }
        }
        finally
        {
            pTabRecord.PTabRelayState = false;
        }
    }

    private static void PTabKeptShow(Window pRelayWindow, PTabRecord pTabRecord)
    {
        LTraceLog.LTraceInfoRecord($"Tab '{pTabRecord.PTabTitle}' kept: the other window did not take it");
        PSWarning.PSWarningShow(
            pRelayWindow,
            LLocalization.LLocalizationTextRead("Tab.Relay.KeptTitle"),
            LLocalization.LLocalizationTextRead("Tab.Relay.KeptMessage"));
    }

    private static void PTabBusyShow(Window pRelayWindow, PTabRecord pTabRecord)
    {
        LTraceLog.LTraceInfoRecord($"Tab '{pTabRecord.PTabTitle}' kept: the worklist is still working");
        PSAnnouncement.PSAnnouncementShow(
            pRelayWindow,
            LLocalization.LLocalizationTextRead("Tab.Relay.BusyTitle"),
            LLocalization.LLocalizationTextRead("Tab.Relay.BusyMessage"));
    }

    private static Point PTabDipRead(Window pRelayWindow, Point pDevicePoint)
    {
        return PresentationSource.FromVisual(pRelayWindow)?.CompositionTarget?
            .TransformFromDevice.Transform(pDevicePoint) ?? pDevicePoint;
    }

    private static bool PTabInsideCheck(Window pRelayWindow, Point pDevicePoint)
    {
        if (pRelayWindow.WindowState == WindowState.Minimized)
        {
            return false;
        }

        Point pWindowPoint = pRelayWindow.PointFromScreen(pDevicePoint);
        return pWindowPoint.X >= 0
            && pWindowPoint.Y >= 0
            && pWindowPoint.X <= pRelayWindow.ActualWidth
            && pWindowPoint.Y <= pRelayWindow.ActualHeight;
    }

}
