using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QWorkspacePair
{
    public QWorkspacePair(QWorkspaceFacts qWorkspaceKey, CUsher lUsher)
    {
        QWorkspaceFlow = new QFlow(lUsher);
        QWorkspaceViewer = new QViewer(lUsher);
        QWorkspaceViewer.QViewerEligibleSet(
            qWorkspaceKey.QWorkspaceAudioOnly, qWorkspaceKey.QWorkspaceEdit, qWorkspaceKey.QWorkspaceEdit);
        QWorkspaceViewer.QViewerAllowSet(qWorkspaceKey.QWorkspaceAudioOnly);
        QWorkspaceCompass = new QCompass(QWorkspaceFlow, QWorkspaceViewer, qWorkspaceKey.QWorkspaceSectionVisible);
        QWorkspaceSource = qWorkspaceKey.QWorkspaceSourcePresent
            ? new QSource(lUsher, QWorkspaceViewer, qWorkspaceKey.QWorkspaceAudioOnly)
            : null;
        QWorkspaceInfo = qWorkspaceKey.QWorkspaceSourcePresent ? new QInfo(QWorkspaceViewer) : null;
    }

    public QFlow QWorkspaceFlow { get; }

    public QViewer QWorkspaceViewer { get; }

    public QCompass QWorkspaceCompass { get; }

    public QSource? QWorkspaceSource { get; }

    public QInfo? QWorkspaceInfo { get; }

    public void QWorkspacePairAttach()
    {
        QFlow lFlow = QWorkspaceFlow;
        QViewer lViewer = QWorkspaceViewer;
        lFlow.QFlowWheel.QFlowPlayingAttach(() => lViewer.QViewerPlaying);
        lViewer.QViewerPlayback.QViewerClockTicked += lFlow.QFlowCursorUpdate;
        lFlow.QFlowCursorChanged += lViewer.QViewerPlayback.QViewerSeek;
        lFlow.QFlowWheel.QFlowDragChanged += lViewer.QViewerPlayback.QViewerDragSet;
        lFlow.QFlowPlayed += lViewer.QViewerPlayback.QViewerPlay;
        lFlow.QFlowPaused += lViewer.QViewerPlayback.QViewerPause;
        lFlow.QFlowWheel.QFlowVolumeAdjusted += lViewer.QViewerPlayback.QViewerVolumeAdjust;
    }

    public void QWorkspacePairDetach()
    {
        QFlow lFlow = QWorkspaceFlow;
        QViewer lViewer = QWorkspaceViewer;
        lViewer.QViewerPlayback.QViewerClockTicked -= lFlow.QFlowCursorUpdate;
        lFlow.QFlowCursorChanged -= lViewer.QViewerPlayback.QViewerSeek;
        lFlow.QFlowWheel.QFlowDragChanged -= lViewer.QViewerPlayback.QViewerDragSet;
        lFlow.QFlowPlayed -= lViewer.QViewerPlayback.QViewerPlay;
        lFlow.QFlowPaused -= lViewer.QViewerPlayback.QViewerPause;
        lFlow.QFlowWheel.QFlowVolumeAdjusted -= lViewer.QViewerPlayback.QViewerVolumeAdjust;
        lFlow.QFlowWheel.QFlowPlayingAttach(null);
    }
}
