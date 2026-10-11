namespace Cadroue.UIDeportment;

public sealed class QViewerLoupe
{
    private readonly LViewer lViewer;
    private readonly LViewerMedia lViewerMedia;

    public QViewerLoupe(LViewer lOwner, LViewerMedia lMedia)
    {
        lViewer = lOwner;
        lViewerMedia = lMedia;
    }

    public void LViewerLoupeAttach()
    {
        lViewer.LViewerLoupeSet(true);
        lViewerMedia.LViewerHostShow(false);
        lViewerMedia.LViewerCommandApply(false);
    }

    public void LViewerLoupeDetach(TimeSpan lPosition, bool lPlaying)
    {
        lViewer.LViewerLoupeSet(false);
        lViewer.LViewerResumeSet(lPlaying);
        lViewerMedia.LViewerCommandApply(true);
        lViewerMedia.LViewerHostShow(true);
        lViewer.LViewerPlayback.LViewerSeek(lPosition);
        if (lPlaying)
        {
            lViewer.LViewerPlayback.LViewerPlay();
            return;
        }

        lViewer.LViewerPlayback.LViewerPause();
    }
}
