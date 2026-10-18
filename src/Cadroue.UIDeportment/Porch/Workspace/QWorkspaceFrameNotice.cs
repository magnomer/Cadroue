using System.Windows.Threading;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The tab's media and relay notices forwarded to its flow, viewer and list; the deferred ones wait on the frame's
// dispatcher.
public sealed class QWorkspaceFrameNotice
{
    private static readonly IReadOnlyDictionary<bool, Action<QWorkspaceFrameNotice>> qWorkspaceFrameAttaches =
        new Dictionary<bool, Action<QWorkspaceFrameNotice>>
        {
            [true] = lNotice => lNotice.QWorkspaceFrameAttach(),
            [false] = lNotice => { },
        };

    private readonly QWorkspaceMedia qWorkspaceMedia;
    private readonly QWorkspaceRelay qWorkspaceRelay;
    private readonly QFlow? qWorkspaceFlow;
    private readonly QViewer? qWorkspaceViewer;
    private readonly QList? qWorkspaceList;
    private readonly Dispatcher qWorkspaceFrameDispatcher;

    // Subscribes when the tab has a flow, before the frame builds its root. The tab's pieces are fixed when it is
    // built, so the frame hands them once.
    internal QWorkspaceFrameNotice(
        QWorkspaceMedia lMedia,
        QWorkspaceRelay lRelay,
        QFlow? lFlow,
        QViewer? lViewer,
        QList? lList,
        Dispatcher lDispatcher)
    {
        qWorkspaceMedia = lMedia;
        qWorkspaceRelay = lRelay;
        qWorkspaceFlow = lFlow;
        qWorkspaceViewer = lViewer;
        qWorkspaceList = lList;
        qWorkspaceFrameDispatcher = lDispatcher;
        qWorkspaceFrameAttaches[qWorkspaceFlow is not null](this);
    }

    private void QWorkspaceFrameAttach()
    {
        qWorkspaceMedia.QWorkspaceFlowAttached += QWorkspaceFlowAttach;
        qWorkspaceMedia.QWorkspaceFlowCleared += QWorkspaceFlowClear;
        qWorkspaceMedia.QWorkspaceMediaClosed += QWorkspaceMediaClose;
        qWorkspaceMedia.QWorkspaceLosslesscutFound += QWorkspaceLosslesscutDefer;
        qWorkspaceRelay.QWorkspaceRelayDeferred += QWorkspaceRelayDefer;
        qWorkspaceRelay.QWorkspaceRangeApplied += QWorkspaceRangeApply;
        qWorkspaceRelay.QWorkspaceVolumeApplied += QWorkspaceVolumeApply;
        qWorkspaceRelay.QWorkspaceSeekApplied += QWorkspaceSeekApply;
        qWorkspaceRelay.QWorkspacePathsAdded += QWorkspacePathsAdd;
        qWorkspaceRelay.QWorkspaceSourceSelected += QWorkspaceSourceSelect;
        qWorkspaceRelay.QWorkspaceSourceOpened += QWorkspaceSourceOpen;
    }

    internal void QWorkspaceFrameDetach()
    {
        qWorkspaceMedia.QWorkspaceFlowAttached -= QWorkspaceFlowAttach;
        qWorkspaceMedia.QWorkspaceFlowCleared -= QWorkspaceFlowClear;
        qWorkspaceMedia.QWorkspaceMediaClosed -= QWorkspaceMediaClose;
        qWorkspaceMedia.QWorkspaceLosslesscutFound -= QWorkspaceLosslesscutDefer;
        qWorkspaceRelay.QWorkspaceRelayDeferred -= QWorkspaceRelayDefer;
        qWorkspaceRelay.QWorkspaceRangeApplied -= QWorkspaceRangeApply;
        qWorkspaceRelay.QWorkspaceVolumeApplied -= QWorkspaceVolumeApply;
        qWorkspaceRelay.QWorkspaceSeekApplied -= QWorkspaceSeekApply;
        qWorkspaceRelay.QWorkspacePathsAdded -= QWorkspacePathsAdd;
        qWorkspaceRelay.QWorkspaceSourceSelected -= QWorkspaceSourceSelect;
        qWorkspaceRelay.QWorkspaceSourceOpened -= QWorkspaceSourceOpen;
    }

    private void QWorkspaceFlowAttach(LMediaInfo lMediaInfo, string lSourcePath) =>
        qWorkspaceFlow!.QFlowAttach(lMediaInfo, lSourcePath, TimeSpan.Zero);

    private void QWorkspaceFlowClear() => qWorkspaceFlow!.QFlowClear();

    private void QWorkspaceMediaClose()
    {
        qWorkspaceViewer!.QViewerMedia.QViewerMediaClose(true);
        qWorkspaceFlow!.QFlowClear();
    }

    private void QWorkspaceLosslesscutDefer()
    {
        Action lFind = QWorkspaceLosslesscutFind;
        qWorkspaceFrameDispatcher.BeginInvoke(DispatcherPriority.Background, lFind);
    }

    private void QWorkspaceLosslesscutFind() => qWorkspaceFlow!.QFlowLosslesscut.QFlowLosslesscutFind();

    private void QWorkspaceRelayDefer(LRelay lRelay, TimeSpan lDuration) =>
        qWorkspaceFrameDispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => qWorkspaceRelay.QWorkspaceRelayRestore(lRelay, lDuration)));

    private void QWorkspaceRangeApply(TimeSpan lOrigin, TimeSpan lLimit) =>
        qWorkspaceFlow!.QFlowSpool.QFlowRangeSet(lOrigin, lLimit);

    private void QWorkspaceVolumeApply(double lVolume) =>
        qWorkspaceViewer!.QViewerPlayback.QViewerVolumeSet(lVolume);

    private void QWorkspaceSeekApply(TimeSpan lPosition) =>
        qWorkspaceViewer!.QViewerPlayback.QViewerSeek(lPosition);

    private async void QWorkspacePathsAdd(IReadOnlyList<string> lPaths)
    {
        await qWorkspaceList!.QListPathsAdd(lPaths);
        qWorkspaceRelay.QWorkspaceSourceRun();
    }

    private void QWorkspaceSourceSelect(string lPath) => qWorkspaceList!.QListListedSelect(lPath);

    private void QWorkspaceSourceOpen(string lPath) =>
        qWorkspaceViewer!.QViewerSource.QViewerSourceOpen(lPath);
}
