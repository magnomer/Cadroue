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
        PWindowWorkspaceAttach(pTabRecord);
        PWindowWidthAttach(pTabRecord.PTabWorkspace.PWorkspaceSurface);
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
        pViewerActive.LViewer.LViewerMediaChange += PWindowMediaHandle;
        pViewerActive.PViewerClockTick += PWindowClockHandle;
        pFlowActive.PFlowCursorChange += pViewerActive.PViewerSeek;
        pFlowActive.PFlowDragChange += pViewerActive.PViewerDragSet;
        pFlowActive.PFlowPlay += pViewerActive.PViewerPlay;
        pFlowActive.PFlowPause += pViewerActive.PViewerPause;
        pFlowActive.PFlowVolumeAdjust += pViewerActive.PViewerVolumeAdjust;
        PWindowVolumeSync(LPreference.LPreferenceStateCurrent);
    }
    private void PWindowWorkspaceDetach()
    {
        if (pFlowActive is not null && pViewerActive is not null)
        {
            pViewerActive.LViewer.LViewerMediaChange -= PWindowMediaHandle;
            pViewerActive.PViewerClockTick -= PWindowClockHandle;
            pFlowActive.PFlowCursorChange -= pViewerActive.PViewerSeek;
            pFlowActive.PFlowDragChange -= pViewerActive.PViewerDragSet;
            pViewerActive.PViewerDragSet(false);
            pFlowActive.PFlowPlay -= pViewerActive.PViewerPlay;
            pFlowActive.PFlowPause -= pViewerActive.PViewerPause;
            pFlowActive.PFlowVolumeAdjust -= pViewerActive.PViewerVolumeAdjust;
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
        if (pViewerActive is null || !lPreferenceState.LPreferenceVolumeUnified) return;
        pViewerActive.PViewerVolumeSet(lPreferenceState.LPreferenceVolume);
    }

}
