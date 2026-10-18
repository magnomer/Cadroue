using Cadroue.Application;

namespace Cadroue.UIDeportment;

// The held preview reaches the player: as the mpv filter, or as Flyleaf's color and geometry.
public sealed class QViewerFilter
{
    private readonly QViewer qViewer;

    public QViewerFilter(QViewer lOwner)
    {
        qViewer = lOwner;
    }

    private QPlayer QPlayer => qViewer.QPlayer;

    public void QViewerFilterUpdate()
    {
        if (!QViewerFilterApply())
        {
            return;
        }

        QViewerRefreshRun();
    }

    // The paused player redraws with the new filter; a refused refresh is recorded.
    private void QViewerRefreshRun()
    {
        try
        {
            QPlayer.QPlayerUpdate();
        }
        catch (Exception lRefreshException)
        {
            qViewer.QViewerTrace.CViewerRefreshRecord(lRefreshException);
        }
    }

    // The preview filter goes to mpv; true when the filter changed on a paused player, which then needs a refresh.
    public bool QViewerFilterApply()
    {
        if (!qViewer.QViewerTrace.CViewerMpvActive || !QPlayer.QPlayerReady)
        {
            return false;
        }

        string lFilter = LPreviewFilter.LPreviewFilterResolve(qViewer.QViewerRenderRead());
        bool lChanged = lFilter != QPlayer.QPlayerFilterApplied;
        if (!QPlayer.QPlayerFilterApply(lFilter) || !lChanged)
        {
            return false;
        }

        return !qViewer.QViewerPlaying;
    }

    public void QViewerPreviewApply()
    {
        if (qViewer.QViewerTrace.CViewerMpvActive)
        {
            QViewerFilterUpdate();
            qViewer.QViewerPreviewRaise();
            return;
        }

        QPlayer.QPlayerPreviewApply(qViewer.QViewerRenderRead(), "preview color/geometry");
        QPlayer.QPlayerFactsRecord("Preview color applied");
        qViewer.QViewerPreviewRaise();
    }
}
