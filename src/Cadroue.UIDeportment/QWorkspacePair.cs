using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QWorkspacePair
{
    public QWorkspacePair(QWorkspaceKey qWorkspaceKey, CUsher lUsher)
    {
        LWorkspaceFlow = new LFlow(lUsher);
        LWorkspaceViewer = new LViewer(lUsher);
        LWorkspaceViewer.LViewerEligibleSet(
            qWorkspaceKey.LWorkspaceAudioOnly, qWorkspaceKey.LWorkspaceEdit, qWorkspaceKey.LWorkspaceEdit);
        LWorkspaceViewer.LViewerAllowSet(qWorkspaceKey.LWorkspaceAudioOnly);
        LWorkspaceCompass = new LCompass(LWorkspaceFlow, LWorkspaceViewer, qWorkspaceKey.LWorkspaceSectionVisible);
        LWorkspaceSource = qWorkspaceKey.LWorkspaceSourcePresent
            ? new LSource(lUsher, LWorkspaceViewer, qWorkspaceKey.LWorkspaceAudioOnly)
            : null;
        LWorkspaceInfo = qWorkspaceKey.LWorkspaceSourcePresent ? new LInfo(LWorkspaceViewer) : null;
    }

    public LFlow LWorkspaceFlow { get; }

    public LViewer LWorkspaceViewer { get; }

    public LCompass LWorkspaceCompass { get; }

    public LSource? LWorkspaceSource { get; }

    public LInfo? LWorkspaceInfo { get; }

    public void LWorkspacePairAttach()
    {
        LFlow lFlow = LWorkspaceFlow;
        LViewer lViewer = LWorkspaceViewer;
        lFlow.LFlowPlayingAttach(() => lViewer.LViewerPlaying);
        lViewer.LViewerPlayback.LViewerClockTicked += lFlow.LFlowCursorUpdate;
        lFlow.LFlowCursorChanged += lViewer.LViewerPlayback.LViewerSeek;
        lFlow.LFlowDragChanged += lViewer.LViewerPlayback.LViewerDragSet;
        lFlow.LFlowPlayed += lViewer.LViewerPlayback.LViewerPlay;
        lFlow.LFlowPaused += lViewer.LViewerPlayback.LViewerPause;
        lFlow.LFlowVolumeAdjusted += lViewer.LViewerPlayback.LViewerVolumeAdjust;
    }

    public void LWorkspacePairDetach()
    {
        LFlow lFlow = LWorkspaceFlow;
        LViewer lViewer = LWorkspaceViewer;
        lViewer.LViewerPlayback.LViewerClockTicked -= lFlow.LFlowCursorUpdate;
        lFlow.LFlowCursorChanged -= lViewer.LViewerPlayback.LViewerSeek;
        lFlow.LFlowDragChanged -= lViewer.LViewerPlayback.LViewerDragSet;
        lFlow.LFlowPlayed -= lViewer.LViewerPlayback.LViewerPlay;
        lFlow.LFlowPaused -= lViewer.LViewerPlayback.LViewerPause;
        lFlow.LFlowVolumeAdjusted -= lViewer.LViewerPlayback.LViewerVolumeAdjust;
        lFlow.LFlowPlayingAttach(null);
    }
}
