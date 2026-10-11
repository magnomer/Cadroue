namespace Cadroue.UIDeportment;

public static class QWorkspacePair
{
    public static void LWorkspacePairAttach(LFlow? lFlow, LViewer? lViewer)
    {
        if (lFlow is null || lViewer is null)
        {
            return;
        }

        lFlow.LFlowPlayingAttach(() => lViewer.LViewerPlaying);
        lViewer.LViewerPlayback.LViewerClockTicked += lFlow.LFlowCursorUpdate;
        lFlow.LFlowCursorChanged += lViewer.LViewerPlayback.LViewerSeek;
        lFlow.LFlowDragChanged += lViewer.LViewerPlayback.LViewerDragSet;
        lFlow.LFlowPlayed += lViewer.LViewerPlayback.LViewerPlay;
        lFlow.LFlowPaused += lViewer.LViewerPlayback.LViewerPause;
        lFlow.LFlowVolumeAdjusted += lViewer.LViewerPlayback.LViewerVolumeAdjust;
    }

    public static void LWorkspacePairDetach(LFlow? lFlow, LViewer? lViewer)
    {
        if (lFlow is null || lViewer is null)
        {
            return;
        }

        lViewer.LViewerPlayback.LViewerClockTicked -= lFlow.LFlowCursorUpdate;
        lFlow.LFlowCursorChanged -= lViewer.LViewerPlayback.LViewerSeek;
        lFlow.LFlowDragChanged -= lViewer.LViewerPlayback.LViewerDragSet;
        lFlow.LFlowPlayed -= lViewer.LViewerPlayback.LViewerPlay;
        lFlow.LFlowPaused -= lViewer.LViewerPlayback.LViewerPause;
        lFlow.LFlowVolumeAdjusted -= lViewer.LViewerPlayback.LViewerVolumeAdjust;
        lFlow.LFlowPlayingAttach(null);
    }
}
