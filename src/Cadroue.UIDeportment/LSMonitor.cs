using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed record LSMonitorScrollbar(
    double LSMonitorScrollViewport,
    double LSMonitorScrollMaximum,
    double LSMonitorScrollValue,
    bool LSMonitorScrollEnabled,
    double LSMonitorScrollOpacity);

public sealed record LSMonitorFace(string LSMonitorFaceIcon, string LSMonitorFaceTip);

public sealed class LSMonitor : IDisposable
{
    private const int LSMonitorDebounceMs = 150;
    private const double LSMonitorZoomStep = 2;
    private const double LSMonitorZoomMost = 32;
    private const double LSMonitorScrollDim = 0.35;

    private readonly LWaveformOrchestrator lMonitorOrchestrator = new();
    private readonly object lMonitorLock = new();
    private string? lMonitorSourcePath;
    private TimeSpan lMonitorDuration;
    private int lMonitorRate;
    private LWorkAudio lMonitorPlan = LWorkAudio.LWorkAudioCreate();
    private double[] lMonitorBefore = Array.Empty<double>();
    private double[] lMonitorAfter = Array.Empty<double>();
    private bool lMonitorPending;
    private bool lMonitorFailed;
    private CancellationTokenSource? lMonitorCancelSource;
    private bool lMonitorDisposed;
    private LViewer? lMonitorViewer;
    private TimeSpan lMonitorCursor;
    private bool lMonitorPlaying;
    private bool lMonitorBypass;
    private double lMonitorScale = 1;
    private double lMonitorOffset;

    public event Action? LSMonitorReady;
    public event Action<TimeSpan>? LSMonitorCursorChanged;
    public event Action<bool>? LSMonitorPlayingChanged;
    public event Action? LSMonitorZoomChanged;
    public event Action? LSMonitorBypassChanged;
    public event Action<bool>? LSMonitorBypassApplied;
    public event Action<TimeSpan>? LSMonitorSeekApplied;
    public event Action? LSMonitorPlayApplied;
    public event Action? LSMonitorPauseApplied;

    public TimeSpan LSMonitorCursor => lMonitorCursor;

    public bool LSMonitorPlaying => lMonitorPlaying;

    public bool LSMonitorBypass => lMonitorBypass;

    public double LSMonitorScale => lMonitorScale;

    public double LSMonitorOffset => lMonitorOffset;

    public LSMonitorFace LSMonitorFaceRead() => lMonitorPlaying
        ? new LSMonitorFace("PCompassPause.svg", LLocalization.LLocalizationTextRead("NormalizePreview.PauseTooltip"))
        : new LSMonitorFace("PCompassPlay.svg", LLocalization.LLocalizationTextRead("NormalizePreview.PlayTooltip"));

    public LSMonitorScrollbar LSMonitorScrollRead()
    {
        double lViewport = 1.0 / lMonitorScale;
        bool lEnabled = lMonitorScale > 1;
        return new LSMonitorScrollbar(
            lViewport, 1 - lViewport, lMonitorOffset, lEnabled, lEnabled ? 1 : LSMonitorScrollDim);
    }

    public string LSMonitorStatusRead(bool lAfter)
    {
        double[] lEnvelope;
        bool lBeforeReady;
        bool lPending;
        bool lFailed;
        lock (lMonitorLock)
        {
            lEnvelope = lAfter ? lMonitorAfter : lMonitorBefore;
            lBeforeReady = lMonitorBefore.Length > 0;
            lPending = lMonitorPending;
            lFailed = lMonitorFailed;
        }

        string? lKey = lPending
            ? (lBeforeReady && lAfter ? "NormalizePreview.Updating" : "NormalizePreview.Loading")
            : lEnvelope.Length > 0
                ? null
                : lFailed ? "NormalizePreview.Unavailable" : "NormalizePreview.Empty";
        return lKey is null ? string.Empty : LLocalization.LLocalizationTextRead(lKey);
    }

    public LSMonitorFrame LSMonitorFrameResolve(bool lAfter, double lWidth, double lHeight)
    {
        double[] lEnvelope;
        lock (lMonitorLock)
        {
            lEnvelope = lAfter ? lMonitorAfter : lMonitorBefore;
        }

        return new LSMonitorFrame(
            LSMonitorPlan.LSMonitorOutlineResolve(lEnvelope, lWidth, lHeight, lMonitorScale, lMonitorOffset),
            LSMonitorPlan.LSMonitorLinesResolve(lHeight),
            LSMonitorPlan.LSMonitorLabelsResolve(lHeight));
    }

    public LSMonitorHead LSMonitorHeadResolve(double lWidth, TimeSpan lDuration) => LSMonitorPlan.LSMonitorHeadResolve(
        lMonitorCursor.TotalSeconds, lDuration.TotalSeconds, lWidth, lMonitorScale, lMonitorOffset);

    public void LSMonitorSeekHandle(bool lPressed, double lX, double lWidth, TimeSpan lDuration)
    {
        if (!lPressed)
        {
            return;
        }

        double? lSeconds = LSMonitorPlan.LSMonitorSeekResolve(
            lX, lWidth, lDuration.TotalSeconds, lMonitorScale, lMonitorOffset);
        if (lSeconds is { } lTarget)
        {
            LSMonitorSeekApplied?.Invoke(TimeSpan.FromSeconds(lTarget));
        }
    }

    public void LSMonitorPlayHandle()
    {
        if (lMonitorPlaying)
        {
            LSMonitorPauseApplied?.Invoke();
        }
        else
        {
            LSMonitorPlayApplied?.Invoke();
        }
    }

    public void LSMonitorViewerAttach(LViewer lViewer)
    {
        lMonitorViewer = lViewer;
        lViewer.LViewerPlayback.LViewerClockTicked += LSMonitorCursorSet;
        lViewer.LViewerBypassChanged += LSMonitorBypassSet;
        lViewer.LViewerPlayingChanged += LSMonitorPlayingSet;
        LSMonitorBypassApplied += lViewer.LViewerBypassSet;
        LSMonitorBypassSet(lViewer.LViewerBypass);
        LSMonitorPlayingSet(lViewer.LViewerPlaying);
    }

    public void LSMonitorViewerDetach()
    {
        if (lMonitorViewer is not { } lViewer)
        {
            return;
        }

        lViewer.LViewerPlayback.LViewerClockTicked -= LSMonitorCursorSet;
        lViewer.LViewerBypassChanged -= LSMonitorBypassSet;
        lViewer.LViewerPlayingChanged -= LSMonitorPlayingSet;
        LSMonitorBypassApplied -= lViewer.LViewerBypassSet;
        lMonitorViewer = null;
    }

    public void LSMonitorBypassSet(bool lBypass)
    {
        lMonitorBypass = lBypass;
        LSMonitorBypassChanged?.Invoke();
    }

    public void LSMonitorRadioHandle(bool lBypass)
    {
        if (lMonitorBypass != lBypass)
        {
            LSMonitorBypassApplied?.Invoke(lBypass);
        }
    }

    public void LSMonitorIncreaseZoom() => LSMonitorZoom(LSMonitorZoomStep, LSMonitorZoomMost);

    public void LSMonitorDecreaseZoom() => LSMonitorZoom(1 / LSMonitorZoomStep, LSMonitorZoomMost);

    public void LSMonitorCursorSet(TimeSpan lCursor)
    {
        lMonitorCursor = lCursor;
        LSMonitorCursorChanged?.Invoke(lCursor);
    }

    public void LSMonitorPlayingSet(bool lPlaying)
    {
        lMonitorPlaying = lPlaying;
        LSMonitorPlayingChanged?.Invoke(lPlaying);
    }

    public void LSMonitorZoom(double lFactor, double lMost)
    {
        double lCenter = lMonitorOffset + 1.0 / lMonitorScale / 2;
        lMonitorScale = Math.Clamp(lMonitorScale * lFactor, 1, lMost);
        double lViewport = 1.0 / lMonitorScale;
        lMonitorOffset = Math.Clamp(lCenter - lViewport / 2, 0, 1 - lViewport);
        LSMonitorZoomChanged?.Invoke();
    }

    public void LSMonitorOffsetSet(double lOffset)
    {
        double lNormal = Math.Clamp(lOffset, 0, Math.Max(0, 1 - 1.0 / lMonitorScale));
        if (Math.Abs(lNormal - lMonitorOffset) <= double.Epsilon)
        {
            return;
        }

        lMonitorOffset = lNormal;
        LSMonitorZoomChanged?.Invoke();
    }

    public double LSMonitorFractionResolve(double lLocal) =>
        Math.Clamp(lMonitorOffset + Math.Clamp(lLocal, 0, 1) / lMonitorScale, 0, 1);

    public double LSMonitorLocalResolve(double lFraction) => (lFraction - lMonitorOffset) * lMonitorScale;

    public double LSMonitorColumnRead(double[] lEnvelope, int lColumn, int lColumns) =>
        LSMonitorPlan.LSMonitorColumnResolve(lEnvelope, lColumn, lColumns, lMonitorScale, lMonitorOffset);

    public LSMonitor()
    {
        lMonitorOrchestrator.LWaveformReady += LSMonitorPeaksHandle;
    }

    public void LSMonitorSourceOpen(string? lPath, TimeSpan lDuration, int lRate = 0)
    {
        lock (lMonitorLock)
        {
            lMonitorSourcePath = lPath;
            lMonitorDuration = lDuration;
            lMonitorRate = lRate;
        }

        lMonitorOrchestrator.LWaveformStart(lPath, lDuration, lRate > 0);
    }

    public void LSMonitorPlanApply(LWorkAudio lPlan)
    {
        lock (lMonitorLock)
        {
            lMonitorPlan = lPlan;
        }

        LSMonitorAfterStart();
    }

    public void LSMonitorUpdate()
    {
        LSMonitorPublish();
    }

    private void LSMonitorPeaksHandle(LWaveformNotice lNotice)
    {
        lock (lMonitorLock)
        {
            lMonitorBefore = LWaveform.LWaveformEnvelopeRead(lNotice.LWaveformPeaks);
            lMonitorAfter = lMonitorBefore;
            lMonitorPending = lNotice.LWaveformPending;
            lMonitorFailed = !lNotice.LWaveformPending && lNotice.LWaveformPeaks.Length == 0;
        }

        LSMonitorPublish();
        if (lNotice.LWaveformPeaks.Length > 0)
        {
            LSMonitorAfterStart();
        }
    }

    private void LSMonitorAfterStart()
    {
        CancellationToken lToken;
        string? lPath;
        TimeSpan lDuration;
        string lGraph;
        lock (lMonitorLock)
        {
            if (lMonitorDisposed)
            {
                return;
            }

            lMonitorCancelSource?.Cancel();
            lMonitorCancelSource?.Dispose();
            lMonitorCancelSource = new CancellationTokenSource();
            lToken = lMonitorCancelSource.Token;

            if (lMonitorBefore.Length == 0)
            {
                return;
            }

            lPath = lMonitorSourcePath;
            lDuration = lMonitorDuration;
            lGraph = lMonitorPlan.LWorkAudioFormat(lMonitorRate);
            lMonitorFailed = false;
            if (string.IsNullOrEmpty(lGraph)
                || string.IsNullOrWhiteSpace(lPath)
                || lDuration <= TimeSpan.Zero)
            {
                lMonitorAfter = lMonitorBefore;
                lMonitorPending = false;
                lPath = null;
            }
            else
            {
                lMonitorPending = true;
            }
        }

        LSMonitorPublish();
        if (lPath is not null)
        {
            LSMonitorAfterScan(lPath, lDuration, lGraph, lToken);
        }
    }

    private void LSMonitorAfterScan(string lPath, TimeSpan lDuration, string lGraph, CancellationToken lToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(LSMonitorDebounceMs, lToken).ConfigureAwait(false);
                LWaveformScanResult lScanned = await LWaveformScanner.LWaveformScan(lPath, lDuration, lToken, lGraph)
                    .ConfigureAwait(false);
                if (!lScanned.LWaveformComplete)
                {
                    LTrace.LTraceRecord(
                        LTraceKind.LTraceWarning,
                        $"Monitor after-scan unavailable for {System.IO.Path.GetFileName(lPath)}",
                        lScanned.LWaveformDetail);
                }

                LSMonitorAfterApply(
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
                LSMonitorAfterApply(lToken, Array.Empty<double>(), true);
            }
        }, CancellationToken.None);
    }

    private void LSMonitorAfterApply(CancellationToken lToken, double[] lAfter, bool lFailed)
    {
        lock (lMonitorLock)
        {
            if (lToken.IsCancellationRequested)
            {
                return;
            }

            lMonitorAfter = lAfter;
            lMonitorFailed = lFailed;
            lMonitorPending = false;
        }

        LSMonitorPublish();
    }

    private void LSMonitorPublish() => LSMonitorReady?.Invoke();

    public void Dispose()
    {
        lock (lMonitorLock)
        {
            if (lMonitorDisposed)
            {
                return;
            }

            lMonitorDisposed = true;
            lMonitorCancelSource?.Cancel();
            lMonitorCancelSource?.Dispose();
            lMonitorCancelSource = null;
        }

        lMonitorOrchestrator.LWaveformReady -= LSMonitorPeaksHandle;
        lMonitorOrchestrator.Dispose();
    }
}
