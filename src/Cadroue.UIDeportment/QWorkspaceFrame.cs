using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QWorkspaceFrame
{
    private static readonly IReadOnlyDictionary<bool, Func<QWorkspaceFrame, FrameworkElement>> qWorkspaceFrameRoots =
        new Dictionary<bool, Func<QWorkspaceFrame, FrameworkElement>>
        {
            [true] = QWorkspaceGridBuild,
            [false] = lFrame => lFrame.qWorkspaceFrameSurface,
        };

    private static readonly IReadOnlyDictionary<bool, Action<Action, Action>> qWorkspaceFrameCloses =
        new Dictionary<bool, Action<Action, Action>>
        {
            [true] = QWorkspacePairClose,
            [false] = (lFlowClose, lViewerClose) => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<QWorkspaceFrame>> qWorkspaceFrameAttaches =
        new Dictionary<bool, Action<QWorkspaceFrame>>
        {
            [true] = lFrame => lFrame.QWorkspaceFrameAttach(),
            [false] = lFrame => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<QWorkspaceFrame>> qWorkspaceFrameDetaches =
        new Dictionary<bool, Action<QWorkspaceFrame>>
        {
            [true] = lFrame => lFrame.QWorkspaceFrameDetach(),
            [false] = lFrame => { },
        };

    private readonly QWorkspace qWorkspace;
    private readonly FrameworkElement qWorkspaceFrameSurface;
    private readonly Func<UserControl> qWorkspaceFrameFactory;
    private readonly Func<QSource, UIElement> qWorkspaceSourceFactory;
    private readonly Func<QInfo, UIElement> qWorkspaceInfoFactory;
    private readonly Action qWorkspaceExportHandler;
    private readonly Func<ImageSource> qWorkspaceExportIcon;
    private readonly Action qWorkspaceSurfaceCloser;
    private readonly Action qWorkspaceFlowCloser;
    private readonly Action qWorkspaceViewerCloser;

    internal QWorkspaceFrame(
        QWorkspace lWorkspace,
        FrameworkElement lSurface,
        Func<UserControl> lFrameFactory,
        Func<QSource, UIElement> lSourceFactory,
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
        qWorkspaceSourceFactory = lSourceFactory;
        qWorkspaceInfoFactory = lInfoFactory;
        qWorkspaceExportHandler = lExportHandler;
        qWorkspaceExportIcon = lExportIcon;
        qWorkspaceSurfaceCloser = lSurfaceClose;
        qWorkspaceFlowCloser = lFlowClose;
        qWorkspaceViewerCloser = lViewerClose;
        qWorkspaceFrameAttaches[qWorkspace.QWorkspaceFlowPresent](this);
        QWorkspaceFrameRoot = qWorkspaceFrameRoots[qWorkspace.QWorkspaceFacts.QWorkspaceSourcePresent](this);
    }

    public FrameworkElement QWorkspaceFrameRoot { get; }

    public void QWorkspaceFrameClose()
    {
        QWorkspaceStepRun("subscriptions", () => qWorkspaceFrameDetaches[qWorkspace.QWorkspaceFlowPresent](this));
        QWorkspaceStepRun("deportment", qWorkspace.QWorkspaceClose);
        QWorkspaceStepRun("surface", qWorkspaceSurfaceCloser);
        qWorkspaceFrameCloses[qWorkspace.QWorkspaceFlowPresent](qWorkspaceFlowCloser, qWorkspaceViewerCloser);
    }

    private void QWorkspaceFrameAttach()
    {
        qWorkspace.QWorkspaceFlowAttached += QWorkspaceFlowAttach;
        qWorkspace.QWorkspaceFlowCleared += QWorkspaceFlowClear;
        qWorkspace.QWorkspaceMediaClosed += QWorkspaceMediaClose;
        qWorkspace.QWorkspaceLosslesscutFound += QWorkspaceLosslesscutDefer;
        qWorkspace.QWorkspaceRelay.QWorkspaceRelayDeferred += QWorkspaceRelayDefer;
        qWorkspace.QWorkspaceRelay.QWorkspaceRangeApplied += QWorkspaceRangeApply;
        qWorkspace.QWorkspaceRelay.QWorkspaceVolumeApplied += QWorkspaceVolumeApply;
        qWorkspace.QWorkspaceRelay.QWorkspaceSeekApplied += QWorkspaceSeekApply;
        qWorkspace.QWorkspaceRelay.QWorkspacePathsAdded += QWorkspacePathsAdd;
        qWorkspace.QWorkspaceRelay.QWorkspaceSourceSelected += QWorkspaceSourceSelect;
        qWorkspace.QWorkspaceRelay.QWorkspaceSourceOpened += QWorkspaceSourceOpen;
    }

    private void QWorkspaceFrameDetach()
    {
        qWorkspace.QWorkspaceFlowAttached -= QWorkspaceFlowAttach;
        qWorkspace.QWorkspaceFlowCleared -= QWorkspaceFlowClear;
        qWorkspace.QWorkspaceMediaClosed -= QWorkspaceMediaClose;
        qWorkspace.QWorkspaceLosslesscutFound -= QWorkspaceLosslesscutDefer;
        qWorkspace.QWorkspaceRelay.QWorkspaceRelayDeferred -= QWorkspaceRelayDefer;
        qWorkspace.QWorkspaceRelay.QWorkspaceRangeApplied -= QWorkspaceRangeApply;
        qWorkspace.QWorkspaceRelay.QWorkspaceVolumeApplied -= QWorkspaceVolumeApply;
        qWorkspace.QWorkspaceRelay.QWorkspaceSeekApplied -= QWorkspaceSeekApply;
        qWorkspace.QWorkspaceRelay.QWorkspacePathsAdded -= QWorkspacePathsAdd;
        qWorkspace.QWorkspaceRelay.QWorkspaceSourceSelected -= QWorkspaceSourceSelect;
        qWorkspace.QWorkspaceRelay.QWorkspaceSourceOpened -= QWorkspaceSourceOpen;
    }

    private static void QWorkspacePairClose(Action lFlowClose, Action lViewerClose)
    {
        QWorkspaceStepRun("flow", lFlowClose);
        QWorkspaceStepRun("viewer", lViewerClose);
    }

    private static void QWorkspaceStepRun(string lStepName, Action lStep)
    {
        try
        {
            lStep();
        }
        catch (Exception lStepException)
        {
            LTraceLog.LTraceErrorRecord(
                $"Workspace close step '{lStepName}' failed; teardown continues",
                lStepException);
        }
    }

    private void QWorkspaceFlowAttach(LMediaInfo lMediaInfo, string lSourcePath) =>
        qWorkspace.QWorkspaceFlow!.QFlowAttach(lMediaInfo, lSourcePath, TimeSpan.Zero);

    private void QWorkspaceFlowClear() => qWorkspace.QWorkspaceFlow!.QFlowClear();

    private void QWorkspaceMediaClose()
    {
        qWorkspace.QWorkspaceViewer!.QViewerMedia.QViewerMediaClose(true);
        qWorkspace.QWorkspaceFlow!.QFlowClear();
    }

    private void QWorkspaceLosslesscutDefer()
    {
        Action lFind = QWorkspaceLosslesscutFind;
        QWorkspaceFrameRoot.Dispatcher.BeginInvoke(DispatcherPriority.Background, lFind);
    }

    private void QWorkspaceLosslesscutFind() => qWorkspace.QWorkspaceFlow!.QFlowLosslesscut.QFlowLosslesscutFind();

    private void QWorkspaceRelayDefer(LRelay lRelay, TimeSpan lDuration) =>
        QWorkspaceFrameRoot.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => qWorkspace.QWorkspaceRelay.QWorkspaceRelayRestore(lRelay, lDuration)));

    private void QWorkspaceRangeApply(TimeSpan lOrigin, TimeSpan lLimit) =>
        qWorkspace.QWorkspaceFlow!.QFlowSpool.QFlowRangeSet(lOrigin, lLimit);

    private void QWorkspaceVolumeApply(double lVolume) =>
        qWorkspace.QWorkspaceViewer!.QViewerPlayback.QViewerVolumeSet(lVolume);

    private void QWorkspaceSeekApply(TimeSpan lPosition) =>
        qWorkspace.QWorkspaceViewer!.QViewerPlayback.QViewerSeek(lPosition);

    private async void QWorkspacePathsAdd(IReadOnlyList<string> lPaths)
    {
        await qWorkspace.QWorkspaceList!.QListPathsAdd(lPaths);
        qWorkspace.QWorkspaceRelay.QWorkspaceSourceRun();
    }

    private void QWorkspaceSourceSelect(string lPath) => qWorkspace.QWorkspaceList!.QListListedSelect(lPath);

    private void QWorkspaceSourceOpen(string lPath) =>
        qWorkspace.QWorkspaceViewer!.QViewerSource.QViewerSourceOpen(lPath);

    private static FrameworkElement QWorkspaceGridBuild(QWorkspaceFrame lFrame)
    {
        UserControl lRoot = lFrame.qWorkspaceFrameFactory();
        lFrame.QWorkspacePanelsPlace(lRoot);
        Button lExport = QContract.QContractFind<Button>(lRoot, "pWorkspaceExport");
        lFrame.QWorkspaceExportAttach(lExport);
        QButton.QButtonAttach(lExport, QButtonLook.QButtonLookSource);
        lFrame.QWorkspaceIconApply(lRoot);
        return lRoot;
    }

    private void QWorkspacePanelsPlace(UserControl lRoot)
    {
        QContract.QContractFind<Border>(lRoot, "pWorkspaceSource").Child =
            qWorkspaceSourceFactory(qWorkspace.QWorkspaceSource!);
        QContract.QContractFind<Border>(lRoot, "pWorkspaceInfo").Child =
            qWorkspaceInfoFactory(qWorkspace.QWorkspaceInfo!);
        QContract.QContractFind<Border>(lRoot, "pWorkspaceSurface").Child = qWorkspaceFrameSurface;
    }

    private void QWorkspaceExportAttach(Button lExport) => lExport.Click += (_, _) => qWorkspaceExportHandler();

    private void QWorkspaceIconApply(UserControl lRoot) =>
        QContract.QContractFind<Image>(lRoot, "pWorkspaceExportIcon").Source = qWorkspaceExportIcon();
}
