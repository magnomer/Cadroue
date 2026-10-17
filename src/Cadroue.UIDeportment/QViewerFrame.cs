using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class QViewerFrame
{
    private readonly QViewer qViewer;

    public QViewerFrame(QViewer lOwner)
    {
        qViewer = lOwner;
    }

    public async void QViewerFrameRead(Action<LMediaFrame?> lFrameApply)
    {
        if (qViewer.QViewerMediaInfo is not { LMediaVideoPresent: true } lMediaInfo)
        {
            lFrameApply(null);
            return;
        }

        int lWidth = lMediaInfo.LMediaVideoWidth;
        int lHeight = lMediaInfo.LMediaVideoHeight;
        string? lPath = qViewer.QViewerWorkspace.CWorkspaceSource;
        if (lWidth <= 0 || lHeight <= 0 || string.IsNullOrWhiteSpace(lPath))
        {
            lFrameApply(null);
            return;
        }

        TimeSpan lTime = qViewer.QViewerTimeRead();
        int lClaim = qViewer.QViewerTrace.CViewerSerial;
        LMediaFrame? lFrame = await LMedia.LMediaFrameStart(lPath, lTime, lWidth, lHeight);
        if (qViewer.QViewerTrace.CViewerUnloaded || lClaim != qViewer.QViewerTrace.CViewerSerial)
        {
            return;
        }

        lFrameApply(lFrame);
    }
}
