namespace Cadroue.UIDeportment;

public sealed class QViewerLoupe
{
    private readonly QViewer qViewer;
    private readonly QViewerMedia qViewerMedia;

    public QViewerLoupe(QViewer lOwner, QViewerMedia lMedia)
    {
        qViewer = lOwner;
        qViewerMedia = lMedia;
    }

    public QSLoupe QViewerLoupeCreate() => new(qViewer);

    public void QViewerLoupeAttach()
    {
        qViewer.QViewerLoupeSet(true);
        qViewerMedia.QViewerHostShow(false);
        qViewerMedia.QViewerCommandApply(false);
    }

    public void QViewerLoupeDetach(TimeSpan lPosition, bool lPlaying)
    {
        qViewer.QViewerLoupeSet(false);
        qViewer.QViewerResumeSet(lPlaying);
        qViewerMedia.QViewerCommandApply(true);
        qViewerMedia.QViewerHostShow(true);
        qViewer.QViewerPlayback.QViewerSeek(lPosition);
        if (lPlaying)
        {
            qViewer.QViewerPlayback.QViewerPlay();
            return;
        }

        qViewer.QViewerPlayback.QViewerPause();
    }
}
