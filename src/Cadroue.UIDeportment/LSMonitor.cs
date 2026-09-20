using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed record LSMonitorScroll(
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
    public event Action<TimeSpan>? LSMonitorCursorChange;
    public event Action<bool>? LSMonitorPlayingChange;
    public event Action? LSMonitorZoomChange;
    public event Action? LSMonitorBypassChange;
    public event Action<bool>? LSMonitorBypassApply;
    public event Action<TimeSpan>? LSMonitorSeekApply;
    public event Action? LSMonitorPlayApply;
    public event Action? LSMonitorPauseApply;

    public TimeSpan LSMonitorCursor => lMonitorCursor;

    public bool LSMonitorPlaying => lMonitorPlaying;

    public bool LSMonitorBypass => lMonitorBypass;

    public double LSMonitorScale => lMonitorScale;

    public double LSMonitorOffset => lMonitorOffset;

    public LSMonitorFace LSMonitorFaceRead() => lMonitorPlaying
        ? new LSMonitorFace("PCompassPause.svg", LLocalization.LLocalizationTextRead("NormalizePreview.PauseTooltip"))
        : new LSMonitorFace("PCompassPlay.svg", LLocalization.LLocalizationTextRead("NormalizePreview.PlayTooltip"));

    public LSMonitorScroll LSMonitorScrollRead()
    {
        double lViewport = 1.0 / lMonitorScale;
        bool lEnabled = lMonitorScale > 1;
        return new LSMonitorScroll(
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
            LSMonitorSeekApply?.Invoke(TimeSpan.FromSeconds(lTarget));
        }
    }

    public void LSMonitorPlayHandle()
    {
        if (lMonitorPlaying)
        {
            LSMonitorPauseApply?.Invoke();
        }
        else
        {
            LSMonitorPlayApply?.Invoke();
        }
    }

    public void LSMonitorViewerAttach(LViewer lViewer)
    {
        lMonitorViewer = lViewer;
        lViewer.LViewerPlayback.LViewerClockTick += LSMonitorCursorSet;
        lViewer.LViewerBypassChange += LSMonitorBypassSet;
        lViewer.LViewerPlayingChange += LSMonitorPlayingSet;
        LSMonitorBypassApply += lViewer.LViewerBypassSet;
        LSMonitorBypassSet(lViewer.LViewerBypass);
        LSMonitorPlayingSet(lViewer.LViewerPlaying);
    }

    public void LSMonitorViewerDetach()
    {
        if (lMonitorViewer is not { } lViewer)
        {
            return;
        }

        lViewer.LViewerPlayback.LViewerClockTick -= LSMonitorCursorSet;
        lViewer.LViewerBypassChange -= LSMonitorBypassSet;
        lViewer.LViewerPlayingChange -= LSMonitorPlayingSet;
        LSMonitorBypassApply -= lViewer.LViewerBypassSet;
        lMonitorViewer = null;
    }

    public void LSMonitorBypassSet(bool lBypass)
    {
        lMonitorBypass = lBypass;
        LSMonitorBypassChange?.Invoke();
    }

    public void LSMonitorRadioHandle(bool lBypass)
    {
        if (lMonitorBypass != lBypass)
        {
            LSMonitorBypassApply?.Invoke(lBypass);
        }
    }

    public void LSMonitorIncreaseZoom() => LSMonitorZoom(LSMonitorZoomStep, LSMonitorZoomMost);

    public void LSMonitorDecreaseZoom() => LSMonitorZoom(1 / LSMonitorZoomStep, LSMonitorZoomMost);

    public void LSMonitorCursorSet(TimeSpan lCursor)
    {
        lMonitorCursor = lCursor;
        LSMonitorCursorChange?.Invoke(lCursor);
    }

    public void LSMonitorPlayingSet(bool lPlaying)
    {
        lMonitorPlaying = lPlaying;
        LSMonitorPlayingChange?.Invoke(lPlaying);
    }

    public void LSMonitorZoom(double lFactor, double lMost)
    {
        double lCenter = lMonitorOffset + 1.0 / lMonitorScale / 2;
        lMonitorScale = Math.Clamp(lMonitorScale * lFactor, 1, lMost);
        double lViewport = 1.0 / lMonitorScale;
        lMonitorOffset = Math.Clamp(lCenter - lViewport / 2, 0, 1 - lViewport);
        LSMonitorZoomChange?.Invoke();
    }

    public void LSMonitorOffsetSet(double lOffset)
    {
        double lNormal = Math.Clamp(lOffset, 0, Math.Max(0, 1 - 1.0 / lMonitorScale));
        if (Math.Abs(lNormal - lMonitorOffset) <= double.Epsilon)
        {
            return;
        }

        lMonitorOffset = lNormal;
        LSMonitorZoomChange?.Invoke();
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
                LWaveformScanResult lScanned = LWaveformScanner.LWaveformScan(lPath, lDuration, lToken, lGraph);
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
