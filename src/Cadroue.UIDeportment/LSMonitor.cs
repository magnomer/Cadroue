using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public readonly record struct LSMonitorEstimate(
    double[] LSMonitorBefore,
    double[] LSMonitorAfter,
    bool LSMonitorPending,
    bool LSMonitorFailed);

public sealed class LSMonitor : IDisposable
{
    private const int LSMonitorDebounceMs = 150;

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
    private TimeSpan lMonitorCursor;
    private bool lMonitorPlaying;
    private bool lMonitorRadioProgram;
    private double lMonitorScale = 1;
    private double lMonitorOffset;

    public event Action<LSMonitorEstimate>? LSMonitorReady;
    public event Action<TimeSpan>? LSMonitorCursorChange;
    public event Action<bool>? LSMonitorPlayingChange;
    public event Action? LSMonitorZoomChange;

    public TimeSpan LSMonitorCursor => lMonitorCursor;

    public bool LSMonitorPlaying => lMonitorPlaying;

    public bool LSMonitorRadioProgram => lMonitorRadioProgram;

    public double LSMonitorScale => lMonitorScale;

    public double LSMonitorOffset => lMonitorOffset;

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

    public void LSMonitorRadioSet(bool lProgram) => lMonitorRadioProgram = lProgram;

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
        lMonitorOffset = Math.Clamp(lOffset, 0, Math.Max(0, 1 - 1.0 / lMonitorScale));
        LSMonitorZoomChange?.Invoke();
    }

    public double LSMonitorFractionResolve(double lLocal) =>
        Math.Clamp(lMonitorOffset + Math.Clamp(lLocal, 0, 1) / lMonitorScale, 0, 1);

    public double LSMonitorLocalResolve(double lFraction) => (lFraction - lMonitorOffset) * lMonitorScale;

    public double LSMonitorColumnRead(double[] lEnvelope, int lColumn, int lColumns)
    {
        double lViewport = 1.0 / lMonitorScale;
        int lLength = lEnvelope.Length;
        double lFromF = (lMonitorOffset + (double)lColumn / lColumns * lViewport) * lLength;
        double lToF = (lMonitorOffset + (double)(lColumn + 1) / lColumns * lViewport) * lLength;
        int lFrom = Math.Clamp((int)Math.Floor(lFromF), 0, lLength - 1);
        int lTo = Math.Clamp((int)Math.Ceiling(lToF), lFrom + 1, lLength);

        double lPeak = 0;
        for (int lIndex = lFrom; lIndex < lTo; lIndex++)
        {
            if (lEnvelope[lIndex] > lPeak)
            {
                lPeak = lEnvelope[lIndex];
            }
        }

        return lPeak;
    }

    public LSMonitor()
    {
        lMonitorOrchestrator.LWaveformReady += LSMonitorPeaksHandle;
    }

    public void LSMonitorSourceOpen(string? lPath, TimeSpan lDuration, int lRate = 0)
    {
        lMonitorSourcePath = lPath;
        lMonitorDuration = lDuration;
        lMonitorRate = lRate;
        lMonitorOrchestrator.LWaveformStart(lPath, lDuration, lRate > 0);
    }

    public void LSMonitorPlanApply(LWorkAudio lPlan)
    {
        lMonitorPlan = lPlan;
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
        if (lMonitorDisposed)
        {
            return;
        }

        CancellationTokenSource lToken;
        string? lPath = lMonitorSourcePath;
        TimeSpan lDuration = lMonitorDuration;
        string lGraph = lMonitorPlan.LWorkAudioFormat(lMonitorRate);
        lock (lMonitorLock)
        {
            lMonitorCancelSource?.Cancel();
            lMonitorCancelSource?.Dispose();
            lMonitorCancelSource = new CancellationTokenSource();
            lToken = lMonitorCancelSource;

            if (lMonitorBefore.Length == 0)
            {
                return;
            }

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
            LSMonitorAfterScan(lPath, lDuration, lGraph, lToken.Token);
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

    private void LSMonitorPublish()
    {
        LSMonitorEstimate lEstimate;
        lock (lMonitorLock)
        {
            lEstimate = new LSMonitorEstimate(lMonitorBefore, lMonitorAfter, lMonitorPending, lMonitorFailed);
        }

        LSMonitorReady?.Invoke(lEstimate);
    }

    public void Dispose()
    {
        if (lMonitorDisposed)
        {
            return;
        }

        lMonitorDisposed = true;
        lMonitorOrchestrator.LWaveformReady -= LSMonitorPeaksHandle;
        lMonitorOrchestrator.Dispose();
        lock (lMonitorLock)
        {
            lMonitorCancelSource?.Cancel();
            lMonitorCancelSource?.Dispose();
            lMonitorCancelSource = null;
        }
    }
}
