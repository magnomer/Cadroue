using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QViewerSource
{
    private readonly QViewer qViewer;

    public QViewerSource(QViewer lOwner, CUsher lUsher)
    {
        qViewer = lOwner;
        QViewerDrop = new QViewerDrop(lOwner, lUsher, () => QViewerPathsDrop);
        QViewerSourceAttach();
    }

    public event Action<IReadOnlyList<string>>? QViewerPathsDrop;

    public QViewerSidecar QViewerSidecar { get; } = new();

    public QViewerDrop QViewerDrop { get; }

    private bool QViewerCropPersistent => qViewer.QCrop.QCropPersistent;

    private void QViewerSourceAttach() => qViewer.QViewerOpenRequest += QViewerSourceOpen;

    public void QViewerSourceOpen(string lSourcePath)
    {
        if (qViewer.QViewerWorkspace.CWorkspaceRequestResolve(
                lSourcePath,
                qViewer.QViewerTrace.CViewerCommandActive,
                qViewer.QViewerTrace.CViewerUnloaded,
                qViewer.QViewerEngine.ToString(),
                QViewerSidecar.QViewerSourceResolve) is not { } lLoadPath)
        {
            return;
        }

        lSourcePath = lLoadPath;
        LPreference.LPreferenceMediaSet(lSourcePath);
        if (!QViewerCropPersistent)
        {
            qViewer.QViewerPreviewSet(qViewer.QViewerPreview.LRotateFlipChange(LRotateFlip.LRotateDefaultCreate()));
        }

        _ = qViewer.QViewerMedia.QViewerLoadStart(lSourcePath);
    }
}
