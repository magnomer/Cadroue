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

    public QViewerHost(QViewer lViewer)
    {
        qViewer = lViewer;
    }

    // The surface's facts reach the gate once, as the detail its host records read.
    public void QViewerFactsAttach(Func<QViewerHostFacts> lFactsSource) =>
        qViewer.QViewerTrace.CViewerHostAttach(() => QViewerHostFormat(lFactsSource()));

    public void QViewerHostShow(bool lVisible) =>
        qViewer.QViewerTrace.CViewerHostShow(lVisible, qViewer.QViewerRenderer.QViewerHostRaise);

    // Kept for the surface's own host stages (PViewer); the gate decides whether the record is taken.
    public void QViewerHostRecord(string lStage) => qViewer.QViewerTrace.CViewerHostRecord(lStage);

    // The host facts as the record's detail lines; the gate reads them only for a record it takes.
    private string QViewerHostFormat(QViewerHostFacts lFacts)
    {
        string lSurface = lFacts.QViewerFactsState is null
            ? "none"
            : $"{lFacts.QViewerFactsState} {lFacts.QViewerFactsWidth:0}x{lFacts.QViewerFactsHeight:0}";
        string lOverlay = lFacts.QViewerFactsOverlay is not { } lTransparent
            ? "none"
            : $"present, AllowsTransparency {lTransparent} (software-composited layer over the video)";
        return $"panel visible {lFacts.QViewerFactsPanel}, host visible {lFacts.QViewerFactsHost}, "
            + $"command {(qViewer.QViewerTrace.CViewerCommandActive ? "on" : "off")}\n"
            + $"surface {lSurface}, "
            + $"handle {(lFacts.QViewerFactsWindow == nint.Zero ? "none" : "set")}, "
            + $"disposed {lFacts.QViewerFactsDisposed}\n"
            + $"overlay {lOverlay}\n"
            + $"player {(qViewer.QPlayer.QPlayerReady ? "ready" : "none")}";
    }
}
