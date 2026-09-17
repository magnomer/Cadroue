using Cadroue.Infrastructure;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Cadroue.Media;
using Cadroue.UIVeneer.PToolbar;
using Cadroue.UIVeneer.PPanel;
using PFlowControl = Cadroue.UIVeneer.PFlow.PFlow;
using Cadroue.Core;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PHouse;

public partial class PWindow
{
    private void PWindowMediaRestore(LPreferenceState lPreferenceState)
    {
        if (!lPreferenceState.LPreferenceMediaAutomatic
            || string.IsNullOrWhiteSpace(lPreferenceState.LPreferenceMediaPath)
            || !LUsher.LUsherFileExist(lPreferenceState.LPreferenceMediaPath))
        {
            return;
        }

        pWindowRestorePath = lPreferenceState.LPreferenceMediaPath;
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Loaded,
            new Action(() =>
            {
                if (pViewerActive is null)
                {
                    PTabRecord? pMediaTab = pStrip.PStripRecords.FirstOrDefault(
                        pTabRecord => pTabRecord.PTabWorkspace.PWorkspaceViewer is not null);
                    if (pMediaTab is not null)
                    {
                        pStrip.PStripSelect(pMediaTab);
                    }
                }

                PWindowMediaOpen();
            }));
    }

    private void PWindowMediaOpen()
    {
        if (pViewerActive is null || pWindowRestorePath is not { } pMediaPath)
        {
            return;
        }

        pWindowRestorePath = null;
        pViewerActive.PViewerSourceOpen(pMediaPath);
    }

    private void PWindowMediaHandle(LCargo mediaStatus)
    {
        if (mediaStatus.LCargoMediaInfo is LMediaInfo mediaInfo)
        {
            pFlowActive?.PFlowAttach(mediaInfo, mediaStatus.LCargoSourcePath, TimeSpan.Zero);
            return;
        }
        pFlowActive?.PFlowClear();
    }
    private void PWindowClockHandle(TimeSpan playbackPosition)
    {
        pFlowActive?.PFlowCursorUpdate(playbackPosition);
    }

}
