using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record QViewerHostFacts(
    bool QViewerFactsPanel,
    bool QViewerFactsHost,
    string? QViewerFactsState,
    double? QViewerFactsWidth,
    double? QViewerFactsHeight,
    nint QViewerFactsWindow,
    bool QViewerFactsDisposed,
    bool? QViewerFactsOverlay);

public sealed class QViewerHost
{
    private readonly QViewer qViewer;
    private Func<QViewerHostFacts>? qViewerFactsSource;
    private int qViewerHostStamp;

    public QViewerHost(QViewer lViewer)
    {
        qViewer = lViewer;
    }

    public void QViewerFactsAttach(Func<QViewerHostFacts> lFactsSource) => qViewerFactsSource = lFactsSource;

    public void QViewerHostShow(bool lVisible)
    {
        bool lChanged = lVisible != qViewer.QViewerHostVisible;
        qViewer.QViewerHostSet(lVisible);
        qViewer.QViewerRenderer.QViewerHostRaise();
        if (lChanged && !qViewer.QViewerMpvActive)
        {
            QViewerHostRecord($"host {(lVisible ? "shown" : "hidden")}");
        }
    }

    public void QViewerHostRecord(string lStage)
    {
        if (qViewerFactsSource is null || !LTrace.LTraceCheck(LTraceKind.LTraceUi))
        {
            qViewerHostStamp++;
            return;
        }

        QViewerHostFacts lFacts = qViewerFactsSource();
        string lSurface = lFacts.QViewerFactsState is null
            ? "none"
            : $"{lFacts.QViewerFactsState} {lFacts.QViewerFactsWidth:0}x{lFacts.QViewerFactsHeight:0}";
        string lOverlay = lFacts.QViewerFactsOverlay is not { } lTransparent
            ? "none"
            : $"present, AllowsTransparency {lTransparent} (software-composited layer over the video)";
        LTrace.LTraceRecord(
            LTraceKind.LTraceUi,
            $"Viewer host [{++qViewerHostStamp}] {lStage}",
            $"panel visible {lFacts.QViewerFactsPanel}, host visible {lFacts.QViewerFactsHost}, "
            + $"command {(qViewer.QViewerCommandActive ? "on" : "off")}\n"
            + $"surface {lSurface}, "
            + $"handle {(lFacts.QViewerFactsWindow == nint.Zero ? "none" : "set")}, "
            + $"disposed {lFacts.QViewerFactsDisposed}\n"
            + $"overlay {lOverlay}\n"
            + $"player {(qViewer.QPlayer.QPlayerReady ? "ready" : "none")}");
    }
}
