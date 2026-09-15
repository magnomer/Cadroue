using Cadroue.Infrastructure;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Cadroue.Media;
using Cadroue.UIShell.PToolbar;
using Cadroue.UIShell.PPanel;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.Core;
using Cadroue.Application;

namespace Cadroue.UIShell.PHouse;

public partial class PWindow
{
    private void PWindowRelayPlace(LRelay lRelay)
    {
        double pRelayLeft = lRelay.LRelayDropLeft - 120;
        double pRelayTop = lRelay.LRelayDropTop - 16;
        double pRelayRightLimit = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 200;
        double pRelayBottomLimit = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 120;

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = Math.Clamp(pRelayLeft, SystemParameters.VirtualScreenLeft, pRelayRightLimit);
        Top = Math.Clamp(pRelayTop, SystemParameters.VirtualScreenTop, pRelayBottomLimit);
    }

    private void PWindowRelayAccept(LRelay lRelay)
    {
        PTabRecord pRelayTabRecord = pStrip.PStripAdd(
            lRelay.LRelayLayoutKey,
            LPreset.LPresetStateCreate(lRelay.LRelayExport),
            lRelay.LRelayLayout);
        if (!string.IsNullOrWhiteSpace(lRelay.LRelayCustomName))
        {
            pStrip.PStripNameSet(pRelayTabRecord, lRelay.LRelayCustomName);
        }

        pStrip.PStripSelect(pRelayTabRecord);
        pRelayTabRecord.PTabWorkspace.PWorkspaceRelayApply(lRelay);
        if (pRelayTabRecord.PTabWorkspace.PWorkspaceSurface is PDeck.PFunnelTab pFunnelSurface)
        {
            pFunnelSurface.PFunnelTargetsResolve(pStrip.PStripRecords);
        }
    }

    private bool PWindowRelayHandle(LRelay lRelay)
    {
        try
        {
            return Dispatcher.Invoke(() =>
            {
                PWindowRelayAccept(lRelay);
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }

                Activate();
                return true;
            });
        }
        catch (Exception pRelayException)
        {
            LTraceLog.LTraceErrorRecord(
                $"Relayed '{lRelay.LRelayLayoutKey}' tab could not be taken; refused",
                pRelayException);
            return false;
        }
    }

}
