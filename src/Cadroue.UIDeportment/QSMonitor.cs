
namespace Cadroue.UIDeportment;

public sealed record QSMonitorScrollbar(
    double QSMonitorScrollViewport,
    double QSMonitorScrollMaximum,
    double QSMonitorScrollValue,
    bool QSMonitorScrollEnabled,
    double QSMonitorScrollOpacity);

public sealed record QSMonitorFace(string QSMonitorFaceIcon, string QSMonitorFaceTip);

public sealed class QSMonitor : IDisposable
{
    private const double QSMonitorZoomStep = 2;
    private const double QSMonitorZoomMost = 32;
    private const double QSMonitorScrollDim = 0.35;

    private QViewer? qMonitorViewer;
    private TimeSpan qMonitorCursor;
    private bool qMonitorPlaying;
    private bool qMonitorBypass;
    private double qMonitorScale = 1;
    private double qMonitorOffset;

    public event Action<TimeSpan>? QSMonitorCursorChanged;
    public event Action<bool>? QSMonitorPlayingChanged;
    public event Action? QSMonitorZoomChanged;
    public event Action? QSMonitorBypassChanged;
    public event Action<bool>? QSMonitorBypassApplied;
    public event Action<TimeSpan>? QSMonitorSeekApplied;
    public event Action? QSMonitorPlayApplied;
    public event Action? QSMonitorPauseApplied;

    public QSMonitorWaveform QSMonitorWaveform { get; } = new();

    public TimeSpan QSMonitorCursor => qMonitorCursor;

    public bool QSMonitorPlaying => qMonitorPlaying;

    public bool QSMonitorBypass => qMonitorBypass;

    public double QSMonitorScale => qMonitorScale;

    public double QSMonitorOffset => qMonitorOffset;

    public QSMonitorFace QSMonitorFaceRead() => qMonitorPlaying
        ? new QSMonitorFace("PCompassPause.svg", QLocalizationCatalog.QLocalizationTextRead(
            "NormalizePreview.PauseTooltip"))
        : new QSMonitorFace("PCompassPlay.svg", QLocalizationCatalog.QLocalizationTextRead(
            "NormalizePreview.PlayTooltip"));

    public QSMonitorScrollbar QSMonitorScrollRead()
    {
        double lViewport = 1.0 / qMonitorScale;
        bool lEnabled = qMonitorScale > 1;
        return new QSMonitorScrollbar(
            lViewport, 1 - lViewport, qMonitorOffset, lEnabled, lEnabled ? 1 : QSMonitorScrollDim);
    }

    public QSMonitorFrame QSMonitorFrameResolve(bool lAfter, double lWidth, double lHeight)
    {
        double[] lEnvelope = QSMonitorWaveform.QSMonitorEnvelopeRead(lAfter);
        return new QSMonitorFrame(
            QSMonitorPlan.QSMonitorOutlineResolve(lEnvelope, lWidth, lHeight, qMonitorScale, qMonitorOffset),
            QSMonitorPlan.QSMonitorLinesResolve(lHeight),
            QSMonitorPlan.QSMonitorLabelsResolve(lHeight));
    }

    public QSMonitorHead QSMonitorHeadResolve(double lWidth, TimeSpan lDuration) => QSMonitorPlan.QSMonitorHeadResolve(
        qMonitorCursor.TotalSeconds, lDuration.TotalSeconds, lWidth, qMonitorScale, qMonitorOffset);

    public void QSMonitorSeekHandle(bool lPressed, double lX, double lWidth, TimeSpan lDuration)
    {
        if (!lPressed)
        {
            return;
        }

        double? lSeconds = QSMonitorPlan.QSMonitorSeekResolve(
            lX, lWidth, lDuration.TotalSeconds, qMonitorScale, qMonitorOffset);
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

    public void QSMonitorIncreaseZoom() => QSMonitorZoom(QSMonitorZoomStep, QSMonitorZoomMost);

    public void QSMonitorDecreaseZoom() => QSMonitorZoom(1 / QSMonitorZoomStep, QSMonitorZoomMost);

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

    public void QSMonitorZoom(double lFactor, double lMost)
    {
        (qMonitorScale, qMonitorOffset) = QSMonitorZoomResolve(lFactor, lMost);
        QSMonitorZoomChanged?.Invoke();
    }

    private (double, double) QSMonitorZoomResolve(double lFactor, double lMost)
    {
        double lCenter = qMonitorOffset + 1.0 / qMonitorScale / 2;
        double lScale = Math.Clamp(qMonitorScale * lFactor, 1, lMost);
        double lViewport = 1.0 / lScale;
        return (lScale, Math.Clamp(lCenter - lViewport / 2, 0, 1 - lViewport));
    }

    private double QSMonitorOffsetClamp(double lOffset) =>
        Math.Clamp(lOffset, 0, Math.Max(0, 1 - 1.0 / qMonitorScale));

    public void QSMonitorOffsetSet(double lOffset)
    {
        double lNormal = QSMonitorOffsetClamp(lOffset);
        if (Math.Abs(lNormal - qMonitorOffset) <= double.Epsilon)
        {
            return;
        }

        qMonitorOffset = lNormal;
        QSMonitorZoomChanged?.Invoke();
    }

    public double QSMonitorFractionResolve(double lLocal) =>
        Math.Clamp(qMonitorOffset + Math.Clamp(lLocal, 0, 1) / qMonitorScale, 0, 1);

    public double QSMonitorLocalResolve(double lFraction) => (lFraction - qMonitorOffset) * qMonitorScale;

    public double QSMonitorColumnRead(double[] lEnvelope, int lColumn, int lColumns) =>
        QSMonitorPlan.QSMonitorColumnResolve(lEnvelope, lColumn, lColumns, qMonitorScale, qMonitorOffset);

    public void Dispose() => QSMonitorWaveform.Dispose();
}
