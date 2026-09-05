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
    private void PWindowTabHandle(PTabRecord? pTabRecord)
    {
        PWindowWidthDetach();
        PWindowWorkspaceDetach();
        if (pTabRecord is null)
        {
            return;
        }
        pFlowActive = pTabRecord.PTabWorkspace.PWorkspaceFlow;
        pViewerActive = pTabRecord.PTabWorkspace.PWorkspaceViewer;
        pListActive = pTabRecord.PTabWorkspace.PWorkspaceList;
        pWindowAudioAllowed = pTabRecord.PTabLayoutKey == "Audio";
        PWindowWorkspaceAttach(pTabRecord);
        PWindowWidthAttach(pTabRecord.PTabWorkspace.PWorkspaceSurface);
        PWindowMediaOpen();
    }

    private void PWindowWorkspaceAttach(PTabRecord pTabRecord)
    {
        if (pFlowActive is null || pViewerActive is null)
        {
            return;
        }
        pFlowActive.PFlowCommandSet(true);
        pFlowActive.PFlowSectionShow(pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabSectionVisible);
        pFlowActive.Height = LFrameStore.LFrameStateCurrent.LFrameFlowHeight;
        pFlowActive.PFlowOrderApply();
        pFlowActive.PFlowPlayingSource = pViewerActive.PViewerPlayingRead;
        pViewerActive.PViewerCommandSet(true);
        pViewerActive.PViewerMediaChange += PWindowMediaHandle;
        pViewerActive.PViewerClockTick += PWindowClockHandle;
        pFlowActive.PFlowCursorChange += pViewerActive.PViewerSeek;
        pFlowActive.PFlowDragChange += pViewerActive.PViewerDragSet;
        pFlowActive.PFlowPlay += pViewerActive.PViewerPlay;
        pFlowActive.PFlowPause += pViewerActive.PViewerPause;
        pViewerActive.PViewerPlayingChange += pFlowActive.PFlowPlayingRaise;
        pFlowActive.PFlowVolumeChange += pViewerActive.PViewerVolumeSet;
        PWindowVolumeSync(LPreference.LPreferenceStateCurrent);
    }
    private void PWindowWorkspaceDetach()
    {
        if (pFlowActive is not null && pViewerActive is not null)
        {
            pViewerActive.PViewerMediaChange -= PWindowMediaHandle;
            pViewerActive.PViewerClockTick -= PWindowClockHandle;
            pFlowActive.PFlowCursorChange -= pViewerActive.PViewerSeek;
            pFlowActive.PFlowDragChange -= pViewerActive.PViewerDragSet;
            pViewerActive.PViewerDragSet(false);
            pFlowActive.PFlowPlay -= pViewerActive.PViewerPlay;
            pFlowActive.PFlowPause -= pViewerActive.PViewerPause;
            pViewerActive.PViewerPlayingChange -= pFlowActive.PFlowPlayingRaise;
            pFlowActive.PFlowVolumeChange -= pViewerActive.PViewerVolumeSet;
            pFlowActive.PFlowPlayingSource = null;
            pFlowActive.PFlowSectionShow(false);
            pFlowActive.PFlowCommandSet(false);
            pViewerActive.PViewerCommandSet(false);
        }
        pFlowActive = null;
        pViewerActive = null;
    }

    private void PWindowVolumeSync(LPreferenceState lPreferenceState)
    {
        if (pFlowActive is null || pViewerActive is null) return;
        double pVolume = lPreferenceState.LPreferenceVolumeUnified ? lPreferenceState.LPreferenceVolume : pViewerActive.PViewerVolumeCurrent;
        if (lPreferenceState.LPreferenceVolumeUnified) pViewerActive.PViewerVolumeSet(pVolume);
        pFlowActive.PFlowVolumeSet(pVolume);
    }

}
