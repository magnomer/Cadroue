namespace Cadroue.UIDeportment;

public sealed class QViewerTeardown
{
    private readonly QViewer qViewer;
    private readonly QViewerMedia qViewerMedia;

    public QViewerTeardown(QViewer lOwner, QViewerMedia lMedia)
    {
        qViewer = lOwner;
        qViewerMedia = lMedia;
    }

    public void QViewerClose() =>
        qViewer.QViewerWorkspace.CWorkspaceClose(qViewerMedia.QViewerProbeDispose, qViewerMedia.QViewerPlayerStop);

    // Kept for the surface's own host stages (PViewer); the gate runs and traces each.
    public void QViewerStageRun(string lStage, Action lAction) => qViewer.QViewerTrace.CViewerStageRun(lStage, lAction);
}
