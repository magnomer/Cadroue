using System.Windows;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PControlBar;

public partial class PRail
{
    private bool PTabRelayCheck(PTabRecord pTabRecord, Point pDevicePoint)
    {
        Window? pRelayWindow = Window.GetWindow(this);
        if (pRelayWindow is null)
        {
            return false;
        }

        Point pDipPoint = PTabDipRead(pRelayWindow, pDevicePoint);
        if (PTabInsideCheck(pRelayWindow, pDevicePoint))
        {
            return false;
        }

        if (pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabBusyCheck())
        {
            PTabBusyShow(pRelayWindow, pTabRecord);
            return false;
        }

        LRelay lRelay = pTabRecord.PTabWorkspace.PWorkspaceRelayCreate(pTabRecord, pDipPoint.X, pDipPoint.Y);
        string lRelayFilePath;
        try
        {
            lRelayFilePath = LRelayStore.LRelayFileSave(lRelay);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Relay payload could not be written; tab kept", lException);
            return false;
        }

        if (LRelayChannel.LRelayInstanceFind(pDevicePoint.X, pDevicePoint.Y) is int lTargetProcessId)
        {
            if (LRelayChannel.LRelayChannelSend(lTargetProcessId, lRelayFilePath))
            {
                LTraceLog.LTraceInfoRecord($"Tab '{pTabRecord.PTabTitle}' relayed to process {lTargetProcessId}");
                pStrip?.PStripClose(pTabRecord);
                return true;
            }

            LRelayStore.LRelayFileClear(lRelayFilePath);
            LTraceLog.LTraceErrorRecord($"Relay target {lTargetProcessId} refused the tab; tab kept", null);
            return false;
        }

        if (!LRelayChannel.LRelayInstanceStart(lRelayFilePath))
        {
            LRelayStore.LRelayFileClear(lRelayFilePath);
            return false;
        }

        LTraceLog.LTraceInfoRecord($"Tab '{pTabRecord.PTabTitle}' relayed to a new instance");
        pStrip?.PStripClose(pTabRecord);
        return true;
    }

    private static void PTabBusyShow(Window pRelayWindow, PTabRecord pTabRecord)
    {
        LTraceLog.LTraceInfoRecord($"Tab '{pTabRecord.PTabTitle}' kept: the worklist is still working");
        MessageBox.Show(
            pRelayWindow,
            LLocalization.LLocalizationTextRead("Tab.Relay.BusyMessage"),
            LLocalization.LLocalizationTextRead("Tab.Relay.BusyTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
