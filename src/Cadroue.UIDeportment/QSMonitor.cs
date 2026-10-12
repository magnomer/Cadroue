using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

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
    private const int QSMonitorDebounceMs = 150;
    private const double QSMonitorZoomStep = 2;
    private const double QSMonitorZoomMost = 32;
    private const double QSMonitorScrollDim = 0.35;

    private readonly LWaveformOrchestrator qMonitorOrchestrator = new();
    private readonly object qMonitorLock = new();
    private string? qMonitorSourcePath;
    private TimeSpan qMonitorDuration;
    private int qMonitorRate;
    private LWorkAudio qMonitorPlan = LWorkAudio.LWorkAudioCreate();
    private double[] qMonitorBefore = Array.Empty<double>();
    private double[] qMonitorAfter = Array.Empty<double>();
    private bool qMonitorPending;
    private bool qMonitorFailed;
    private CancellationTokenSource? qMonitorCancelSource;
    private bool qMonitorDisposed;
    private QViewer? qMonitorViewer;
    private TimeSpan qMonitorCursor;
    private bool qMonitorPlaying;
    private bool qMonitorBypass;
    private double qMonitorScale = 1;
    private double qMonitorOffset;

    public event Action? QSMonitorReady;
    public event Action<TimeSpan>? QSMonitorCursorChanged;
    public event Action<bool>? QSMonitorPlayingChanged;
    public event Action? QSMonitorZoomChanged;
    public event Action? QSMonitorBypassChanged;
    public event Action<bool>? QSMonitorBypassApplied;
    public event Action<TimeSpan>? QSMonitorSeekApplied;
    public event Action? QSMonitorPlayApplied;
    public event Action? QSMonitorPauseApplied;

    public TimeSpan QSMonitorCursor => qMonitorCursor;

    public bool QSMonitorPlaying => qMonitorPlaying;

    public bool QSMonitorBypass => qMonitorBypass;

    public double QSMonitorScale => qMonitorScale;

    public double QSMonitorOffset => qMonitorOffset;

    public QSMonitorFace QSMonitorFaceRead() => qMonitorPlaying
        ? new QSMonitorFace("PCompassPause.svg", LLocalization.LLocalizationTextRead("NormalizePreview.PauseTooltip"))
        : new QSMonitorFace("PCompassPlay.svg", LLocalization.LLocalizationTextRead("NormalizePreview.PlayTooltip"));

    public QSMonitorScrollbar QSMonitorScrollRead()
    {
        double lViewport = 1.0 / qMonitorScale;
        bool lEnabled = qMonitorScale > 1;
        return new QSMonitorScrollbar(
            lViewport, 1 - lViewport, qMonitorOffset, lEnabled, lEnabled ? 1 : QSMonitorScrollDim);
    }

    public string QSMonitorStatusRead(bool lAfter)
    {
        double[] lEnvelope;
        bool lBeforeReady;
        bool lPending;
        bool lFailed;
        lock (qMonitorLock)
        {
            lEnvelope = lAfter ? qMonitorAfter : qMonitorBefore;
            lBeforeReady = qMonitorBefore.Length > 0;
            lPending = qMonitorPending;
            lFailed = qMonitorFailed;
        }

        string? lKey = lPending
            ? (lBeforeReady && lAfter ? "NormalizePreview.Updating" : "NormalizePreview.Loading")
            : lEnvelope.Length > 0
                ? null
                : lFailed ? "NormalizePreview.Unavailable" : "NormalizePreview.Empty";
        return lKey is null ? string.Empty : LLocalization.LLocalizationTextRead(lKey);
    }

    public QSMonitorFrame QSMonitorFrameResolve(bool lAfter, double lWidth, double lHeight)
    {
        double[] lEnvelope;
        lock (qMonitorLock)
        {
            lEnvelope = lAfter ? qMonitorAfter : qMonitorBefore;
        }

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
        qMonitorViewer = lViewer;
        lViewer.QViewerPlayback.QViewerClockTicked += QSMonitorCursorSet;
        lViewer.QViewerBypassChanged += QSMonitorBypassSet;
        lViewer.QViewerPlayingChanged += QSMonitorPlayingSet;
        QSMonitorBypassApplied += lViewer.QViewerBypassSet;
        QSMonitorBypassSet(lViewer.QViewerBypass);
        QSMonitorPlayingSet(lViewer.QViewerPlaying);
    }

    public void QSMonitorViewerDetach()
    {
        if (qMonitorViewer is not { } lViewer)
        {
            return;
        }

        lViewer.QViewerPlayback.QViewerClockTicked -= QSMonitorCursorSet;
        lViewer.QViewerBypassChanged -= QSMonitorBypassSet;
        lViewer.QViewerPlayingChanged -= QSMonitorPlayingSet;
        QSMonitorBypassApplied -= lViewer.QViewerBypassSet;
        qMonitorViewer = null;
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
        double lCenter = qMonitorOffset + 1.0 / qMonitorScale / 2;
        qMonitorScale = Math.Clamp(qMonitorScale * lFactor, 1, lMost);
        double lViewport = 1.0 / qMonitorScale;
        qMonitorOffset = Math.Clamp(lCenter - lViewport / 2, 0, 1 - lViewport);
        QSMonitorZoomChanged?.Invoke();
    }

    public void QSMonitorOffsetSet(double lOffset)
    {
        double lNormal = Math.Clamp(lOffset, 0, Math.Max(0, 1 - 1.0 / qMonitorScale));
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

    public QSMonitor()
    {
        qMonitorOrchestrator.LWaveformReady += QSMonitorPeaksHandle;
    }

    public void QSMonitorSourceOpen(string? lPath, TimeSpan lDuration, int lRate = 0)
    {
        lock (qMonitorLock)
        {
            qMonitorSourcePath = lPath;
            qMonitorDuration = lDuration;
            qMonitorRate = lRate;
        }

        qMonitorOrchestrator.LWaveformStart(lPath, lDuration, lRate > 0);
    }

    public void QSMonitorPlanApply(LWorkAudio lPlan)
    {
        lock (qMonitorLock)
        {
            qMonitorPlan = lPlan;
        }

        QSMonitorAfterStart();
    }

    public void QSMonitorUpdate()
    {
        QSMonitorPublish();
    }

    private void QSMonitorPeaksHandle(LWaveformNotice lNotice)
    {
        lock (qMonitorLock)
        {
            qMonitorBefore = LWaveform.LWaveformEnvelopeRead(lNotice.LWaveformPeaks);
            qMonitorAfter = qMonitorBefore;
            qMonitorPending = lNotice.LWaveformPending;
            qMonitorFailed = !lNotice.LWaveformPending && lNotice.LWaveformPeaks.Length == 0;
        }

        QSMonitorPublish();
        if (lNotice.LWaveformPeaks.Length > 0)
        {
            QSMonitorAfterStart();
        }
    }

    private void QSMonitorAfterStart()
    {
        CancellationToken lToken;
        string? lPath;
        TimeSpan lDuration;
        string lGraph;
        lock (qMonitorLock)
        {
            if (qMonitorDisposed)
            {
                return;
            }

            qMonitorCancelSource?.Cancel();
            qMonitorCancelSource?.Dispose();
            qMonitorCancelSource = new CancellationTokenSource();
            lToken = qMonitorCancelSource.Token;

            if (qMonitorBefore.Length == 0)
            {
                return;
            }

            lPath = qMonitorSourcePath;
            lDuration = qMonitorDuration;
            lGraph = qMonitorPlan.LWorkAudioFormat(qMonitorRate);
            qMonitorFailed = false;
            if (string.IsNullOrEmpty(lGraph)
                || string.IsNullOrWhiteSpace(lPath)
                || lDuration <= TimeSpan.Zero)
            {
                qMonitorAfter = qMonitorBefore;
                qMonitorPending = false;
                lPath = null;
            }
            else
            {
                qMonitorPending = true;
            }
        }

        QSMonitorPublish();
        if (lPath is not null)
        {
            QSMonitorAfterScan(lPath, lDuration, lGraph, lToken);
        }
    }

    private void QSMonitorAfterScan(string lPath, TimeSpan lDuration, string lGraph, CancellationToken lToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(QSMonitorDebounceMs, lToken).ConfigureAwait(false);
                LWaveformScanResult lScanned = await LWaveformScanner.LWaveformScan(lPath, lDuration, lToken, lGraph)
                    .ConfigureAwait(false);
                if (!lScanned.LWaveformComplete)
                {
                    LTrace.LTraceRecord(
                        LTraceKind.LTraceWarning,
                        $"Monitor after-scan unavailable for {System.IO.Path.GetFileName(lPath)}",
                        lScanned.LWaveformDetail);
                }

                QSMonitorAfterApply(
                    lToken,
                    LWaveform.LWaveformEnvelopeRead(lScanned.LWaveformPeaks),
                    !lScanned.LWaveformComplete);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception lException)
            {
                LTraceLog.LTraceErrorRecord("Monitor after-scan could not be generated", lException);
                QSMonitorAfterApply(lToken, Array.Empty<double>(), true);
            }
        }, CancellationToken.None);
    }

    private void QSMonitorAfterApply(CancellationToken lToken, double[] lAfter, bool lFailed)
    {
        lock (qMonitorLock)
        {
            if (lToken.IsCancellationRequested)
            {
                return;
            }

            qMonitorAfter = lAfter;
            qMonitorFailed = lFailed;
            qMonitorPending = false;
        }

        QSMonitorPublish();
    }

    private void QSMonitorPublish() => QSMonitorReady?.Invoke();

    public void Dispose()
    {
        lock (qMonitorLock)
        {
            if (qMonitorDisposed)
            {
                return;
            }

            qMonitorDisposed = true;
            qMonitorCancelSource?.Cancel();
            qMonitorCancelSource?.Dispose();
            qMonitorCancelSource = null;
        }

        qMonitorOrchestrator.LWaveformReady -= QSMonitorPeaksHandle;
        qMonitorOrchestrator.Dispose();
    }
}
