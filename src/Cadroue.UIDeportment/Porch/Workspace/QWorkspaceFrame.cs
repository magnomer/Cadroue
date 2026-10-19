using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public sealed class QWorkspaceFrame
{
    private static readonly IReadOnlyDictionary<bool, Func<QWorkspaceFrame, FrameworkElement>> qWorkspaceFrameRoots =
        new Dictionary<bool, Func<QWorkspaceFrame, FrameworkElement>>
        {
            [true] = QWorkspaceGridBuild,
            [false] = lFrame => lFrame.qWorkspaceFrameSurface,
        };

    private readonly QWorkspace qWorkspace;
    private readonly FrameworkElement qWorkspaceFrameSurface;
    private readonly Func<UserControl> qWorkspaceFrameFactory;
    private readonly Func<QInfo, UIElement> qWorkspaceInfoFactory;
    private readonly Action qWorkspaceExportHandler;
    private readonly Func<ImageSource> qWorkspaceExportIcon;

    // The notices subscribe before the root is built; they defer on the surface's dispatcher, the root's own, since
    // the surface is built on the same thread and placed in the root.
    internal QWorkspaceFrame(
        QWorkspace lWorkspace,
        FrameworkElement lSurface,
        Func<UserControl> lFrameFactory,
        Func<QInfo, UIElement> lInfoFactory,
        Action lExportHandler,
        Func<ImageSource> lExportIcon,
        Action lSurfaceClose,
        Action lFlowClose,
        Action lViewerClose)
    {
        qWorkspace = lWorkspace;
        qWorkspaceFrameSurface = lSurface;
        qWorkspaceFrameFactory = lFrameFactory;
        qWorkspaceInfoFactory = lInfoFactory;
        qWorkspaceExportHandler = lExportHandler;
        qWorkspaceExportIcon = lExportIcon;
        var lNotice = new QWorkspaceFrameNotice(
            lWorkspace.QWorkspaceMedia,
            lWorkspace.QWorkspaceRelay,
            lWorkspace.QWorkspaceFlow,
            lWorkspace.QWorkspaceViewer,
            lWorkspace.QWorkspaceList,
            lSurface.Dispatcher);
        QWorkspaceTeardown = new QWorkspaceTeardown(
            lWorkspace.QWorkspaceFlowPresent, lWorkspace.QWorkspaceClose, lNotice, lSurfaceClose, lFlowClose,
            lViewerClose);
        QWorkspaceFrameRoot = qWorkspaceFrameRoots[qWorkspace.QWorkspaceFacts.QWorkspaceSourcePresent](this);
    }

    public FrameworkElement QWorkspaceFrameRoot { get; }

    public QWorkspaceTeardown QWorkspaceTeardown { get; }

    private static FrameworkElement QWorkspaceGridBuild(QWorkspaceFrame lFrame)
    {
        UserControl lRoot = lFrame.qWorkspaceFrameFactory();
        QLook.QLookStyleAttach(lRoot);
        lFrame.QWorkspacePanelsPlace(lRoot);
        Button lExport = QContract.QContractFind<Button>(lRoot, "pWorkspaceExport");
        lFrame.QWorkspaceExportAttach(lExport);
        lFrame.QWorkspaceIconApply(lRoot);
        return lRoot;
    }

    private void QWorkspacePanelsPlace(UserControl lRoot)
    {
        QContract.QContractFind<Border>(lRoot, "pWorkspaceSource").Child =
            qWorkspace.QWorkspaceSource!.QSourceAttach();
        QContract.QContractFind<Border>(lRoot, "pWorkspaceInfo").Child =
            qWorkspaceInfoFactory(qWorkspace.QWorkspaceInfo!);
        QContract.QContractFind<Border>(lRoot, "pWorkspaceSurface").Child = qWorkspaceFrameSurface;
    }

    private void QWorkspaceExportAttach(Button lExport) => lExport.Click += (_, _) => qWorkspaceExportHandler();

    private void QWorkspaceIconApply(UserControl lRoot) =>
        QContract.QContractFind<Image>(lRoot, "pWorkspaceExportIcon").Source = qWorkspaceExportIcon();
}
