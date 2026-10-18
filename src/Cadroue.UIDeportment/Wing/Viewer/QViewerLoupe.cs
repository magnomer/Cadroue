namespace Cadroue.UIDeportment;

public sealed class QViewerLoupe
{
    private readonly QViewer qViewer;

    public QViewerLoupe(QViewer lOwner)
    {
        qViewer = lOwner;
    }

    public QSLoupe QViewerLoupeCreate() => new(qViewer);

    public void QViewerLoupeAttach()
    {
        qViewer.QViewerLoupeSet(true);
        qViewer.QViewerTrace.CViewerLoupeAttach(
            qViewer.QViewerRenderer.QViewerHostRaise, qViewer.QViewerPlayback.QViewerSuspend);
    }

    // The gate turns the command on, shows the host and hands the loupe's place and play state to the workspace;
    // the renderer's restore and raise and the player's resume stay here as the commands it runs.
    public void QViewerLoupeDetach(TimeSpan lPosition, bool lPlaying)
    {
        qViewer.QViewerLoupeSet(false);
        qViewer.QViewerResumeSet(lPlaying);
        qViewer.QViewerWorkspace.CWorkspaceLoupeDetach(
            lPosition,
            lPlaying,
            qViewer.QViewerRenderer.QViewerEngineRestore,
            qViewer.QViewerPlayback.QViewerResume,
            qViewer.QViewerRenderer.QViewerHostRaise);
    }
}
