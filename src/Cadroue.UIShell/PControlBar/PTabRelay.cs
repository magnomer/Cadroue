using System.Diagnostics;
using System.Windows;

namespace Cadroue.UIShell.PControlBar;

public partial class PToolbar
{
    private bool PTabRelayCheck(PTabRecord pTabRecord, Point pDevicePoint)
    {
        Window? pRelayWindow = Window.GetWindow(this);
        if (pRelayWindow is null)
        {
            return false;
        }

        Point pDipPoint = PTabRelayDipRead(pRelayWindow, pDevicePoint);
        if (PTabRelayInsideCheck(pRelayWindow, pDipPoint))
        {
            return false;
        }

        LRelay lRelay = LRelay.LRelayTabCreate(pTabRecord, pDipPoint.X, pDipPoint.Y);
        string lRelayFilePath;
        try
        {
            lRelayFilePath = LRelayStore.LRelayFileSave(lRelay);
        }
        catch (Exception lException)
        {
            LAppLog.LError("Relay payload could not be written; tab kept", lException);
            return false;
        }

        if (LRelayChannel.LRelayInstanceFind(pDevicePoint.X, pDevicePoint.Y) is int lTargetProcessId)
        {
            if (LRelayChannel.LRelayChannelSend(lTargetProcessId, lRelayFilePath))
            {
                LAppLog.LInfo($"Tab '{pTabRecord.PTabTitle}' relayed to process {lTargetProcessId}");
                lTabset?.LTabsetClose(pTabRecord);
                return true;
            }

            LRelayStore.LRelayFileClear(lRelayFilePath);
            LAppLog.LError($"Relay target {lTargetProcessId} refused the tab; tab kept", null);
            return false;
        }

        if (!PTabRelayLaunch(lRelayFilePath))
        {
            LRelayStore.LRelayFileClear(lRelayFilePath);
            return false;
        }

        LAppLog.LInfo($"Tab '{pTabRecord.PTabTitle}' relayed to a new instance");
        lTabset?.LTabsetClose(pTabRecord);
        return true;
    }

    private static Point PTabRelayDipRead(Window pRelayWindow, Point pDevicePoint)
    {
        return PresentationSource.FromVisual(pRelayWindow)?.CompositionTarget?
            .TransformFromDevice.Transform(pDevicePoint) ?? pDevicePoint;
    }

    private static bool PTabRelayInsideCheck(Window pRelayWindow, Point pDipPoint)
    {
        if (pRelayWindow.WindowState == WindowState.Minimized)
        {
            return false;
        }

        var pWindowBounds = new Rect(
            pRelayWindow.Left,
            pRelayWindow.Top,
            pRelayWindow.ActualWidth,
            pRelayWindow.ActualHeight);

        return pWindowBounds.Contains(pDipPoint);
    }

    private static bool PTabRelayLaunch(string lRelayFilePath)
    {
        string? pRelayProgramPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(pRelayProgramPath))
        {
            LAppLog.LError("Relay launch skipped: program path unknown", null);
            return false;
        }

        try
        {
            var pRelayStart = new ProcessStartInfo(pRelayProgramPath) { UseShellExecute = false };
            pRelayStart.ArgumentList.Add(PTabRelayArgument);
            pRelayStart.ArgumentList.Add(lRelayFilePath);
            return Process.Start(pRelayStart) is not null;
        }
        catch (Exception lException)
        {
            LAppLog.LError("Relay launch failed; tab kept", lException);
            return false;
        }
    }

    internal const string PTabRelayArgument = "--relay";
}
