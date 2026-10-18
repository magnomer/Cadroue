
namespace Cadroue.UIDeportment;

public sealed record QSMonitorFace(string QSMonitorFaceIcon, string QSMonitorFaceTip);

// The normalize monitor's driver: it mirrors the viewer's clock, playback and bypass, and turns clicks into requests.
public sealed class QSMonitor : IDisposable
{
    private QViewer? qMonitorViewer;
    private TimeSpan qMonitorCursor;
    private bool qMonitorPlaying;
    private bool qMonitorBypass;

    public event Action<TimeSpan>? QSMonitorCursorChanged;
    public event Action<bool>? QSMonitorPlayingChanged;
    public event Action? QSMonitorBypassChanged;
    public event Action<bool>? QSMonitorBypassApplied;
    public event Action<TimeSpan>? QSMonitorSeekApplied;
    public event Action? QSMonitorPlayApplied;
    public event Action? QSMonitorPauseApplied;

    public QSMonitorWaveform QSMonitorWaveform { get; } = new();

    public QSMonitorViewport QSMonitorViewport { get; } = new();

    public TimeSpan QSMonitorCursor => qMonitorCursor;

    public bool QSMonitorPlaying => qMonitorPlaying;

    public bool QSMonitorBypass => qMonitorBypass;

    public QSMonitorFace QSMonitorFaceRead() => qMonitorPlaying
        ? new QSMonitorFace("PCompassPause.svg", QLocalizationCatalog.QLocalizationTextRead(
            "NormalizePreview.PauseTooltip"))
        : new QSMonitorFace("PCompassPlay.svg", QLocalizationCatalog.QLocalizationTextRead(
            "NormalizePreview.PlayTooltip"));

    public QSMonitorFrame QSMonitorFrameResolve(bool lAfter, double lWidth, double lHeight)
    {
        double[] lEnvelope = QSMonitorWaveform.QSMonitorEnvelopeRead(lAfter);
        return new QSMonitorFrame(
            QSMonitorPlan.QSMonitorOutlineResolve(
                lEnvelope, lWidth, lHeight, QSMonitorViewport.QSMonitorScale, QSMonitorViewport.QSMonitorOffset),
            QSMonitorPlan.QSMonitorLinesResolve(lHeight),
            QSMonitorPlan.QSMonitorLabelsResolve(lHeight));
    }

    public QSMonitorHead QSMonitorHeadResolve(double lWidth, TimeSpan lDuration) => QSMonitorPlan.QSMonitorHeadResolve(
        qMonitorCursor.TotalSeconds,
        lDuration.TotalSeconds,
        lWidth,
        QSMonitorViewport.QSMonitorScale,
        QSMonitorViewport.QSMonitorOffset);

    public void QSMonitorSeekHandle(bool lPressed, double lX, double lWidth, TimeSpan lDuration)
    {
        if (!lPressed)
        {
            return;
        }

        double? lSeconds = QSMonitorPlan.QSMonitorSeekResolve(
            lX, lWidth, lDuration.TotalSeconds, QSMonitorViewport.QSMonitorScale, QSMonitorViewport.QSMonitorOffset);
        if (lSeconds is { } lTarget)
        {
            QSMonitorSeekApplied?.Invoke(TimeSpan.FromSeconds(lTarget));
        }
    }

    public void QSMonitorPlayHandle()
    {
        if (qMonitorPlaying)
        {
            QSMonitorPauseApplied?.Invoke();
        }
        else
        {
            QSMonitorPlayApplied?.Invoke();
        }
    }

    public void QSMonitorViewerAttach(QViewer lViewer)
    {
        QSMonitorViewerSet(lViewer);
        QSMonitorHandlerAttach(lViewer);
        QSMonitorBypassSet(lViewer.QViewerAudio.QViewerBypass);
        QSMonitorPlayingSet(lViewer.QViewerPlaying);
    }

    public void QSMonitorViewerDetach()
    {
        if (qMonitorViewer is not { } lViewer)
        {
            return;
        }

        QSMonitorHandlerDetach(lViewer);
        QSMonitorViewerSet(null);
    }

    private void QSMonitorViewerSet(QViewer? lViewer) => qMonitorViewer = lViewer;

    private void QSMonitorHandlerAttach(QViewer lViewer)
    {
        lViewer.QViewerPlayback.QViewerClockTicked += QSMonitorCursorSet;
        lViewer.QViewerAudio.QViewerBypassChanged += QSMonitorBypassSet;
        lViewer.QViewerPlayingChanged += QSMonitorPlayingSet;
        QSMonitorBypassApplied += lViewer.QViewerAudio.QViewerBypassSet;
    }

    private void QSMonitorHandlerDetach(QViewer lViewer)
    {
        lViewer.QViewerPlayback.QViewerClockTicked -= QSMonitorCursorSet;
        lViewer.QViewerAudio.QViewerBypassChanged -= QSMonitorBypassSet;
        lViewer.QViewerPlayingChanged -= QSMonitorPlayingSet;
        QSMonitorBypassApplied -= lViewer.QViewerAudio.QViewerBypassSet;
    }

    public void QSMonitorBypassSet(bool lBypass)
    {
        qMonitorBypass = lBypass;
        QSMonitorBypassChanged?.Invoke();
    }

    public void QSMonitorRadioHandle(bool lBypass)
    {
        if (qMonitorBypass != lBypass)
        {
            QSMonitorBypassApplied?.Invoke(lBypass);
        }
    }

    public void QSMonitorCursorSet(TimeSpan lCursor)
    {
        qMonitorCursor = lCursor;
        QSMonitorCursorChanged?.Invoke(lCursor);
    }

    public void QSMonitorPlayingSet(bool lPlaying)
    {
        qMonitorPlaying = lPlaying;
        QSMonitorPlayingChanged?.Invoke(lPlaying);
    }

    public void Dispose() => QSMonitorWaveform.Dispose();
}
