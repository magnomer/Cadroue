using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QViewerRenderer
{
    private readonly QViewer qViewer;
    private readonly CViewer qViewerTrace;

    public QViewerRenderer(QViewer lOwner)
    {
        qViewer = lOwner;
        qViewerTrace = lOwner.QViewerTrace;
    }

    public event Action? QViewerHostApplied;

    private QPlayer QPlayer => qViewer.QPlayer;

    public void QViewerHostRaise() => QViewerHostApplied?.Invoke();

    public bool QViewerOverlayHandle(bool lHostVisible, double lWidth, double lHeight)
    {
        return qViewerTrace.CViewerOverlay.CViewerOverlayCheck(lHostVisible, qViewer.QViewerMpvShown, lWidth, lHeight);
    }

    public void QViewerEngineHandle()
    {
        if (qViewerTrace.CViewerUnloaded || !qViewerTrace.CViewerCommandActive)
        {
            return;
        }

        if (qViewerTrace.CViewerMpvActive == (LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv))
        {
            return;
        }

        QViewerEngineRestore();
    }

    public bool QViewerEngineUpdate()
    {
        bool lWantMpv = LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;
        if (lWantMpv == qViewerTrace.CViewerMpvActive)
        {
            return false;
        }

        qViewer.QViewerMedia.QViewerPlayerStop();
        QViewerEngineApply(lWantMpv);
        return true;
    }

    public void QViewerEngineApply(bool lMpv)
    {
        qViewerTrace.CViewerMpvSet(lMpv);
        QPlayer.QPlayerAppliedReset();
        qViewer.QViewerEngineSet(lMpv ? LPreviewEngine.LPreviewEngineMpv : LPreviewEngine.LPreviewEngineFlyleaf);
        QViewerHostApplied?.Invoke();
    }

    public bool QViewerEngineRestore()
    {
        bool lPlaying = qViewer.QViewerResumeInactive || qViewer.QViewerPlaying;
        TimeSpan lPosition = qViewer.QViewerTimeRead();
        bool lSwapped = QViewerEngineUpdate();
        return (lSwapped || qViewer.QViewerWorkspace.CWorkspaceIntent is not null)
            && qViewer.QViewerWorkspace.CWorkspaceRestore(lPosition, lPlaying);
    }

}
