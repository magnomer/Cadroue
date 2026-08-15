using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public readonly record struct LSMonitorEstimate(double[] LSMonitorBefore, double[] LSMonitorAfter);

public sealed class LSMonitor : IDisposable
{
    private const int LSMonitorDebounceMs = 150;

    private readonly LWaveformOrchestrator lMonitorOrchestrator = new();
    private readonly object lMonitorLock = new();
    private byte[] lMonitorPeaks = Array.Empty<byte>();
    private string? lMonitorSourcePath;
    private TimeSpan lMonitorDuration;
    private LWorkAudio lMonitorPlan = LWorkAudio.LWorkAudioCreate();
    private double[] lMonitorBefore = Array.Empty<double>();
    private double[] lMonitorAfter = Array.Empty<double>();
    private CancellationTokenSource? lMonitorCancelSource;
    private bool lMonitorScanning;
    private bool lMonitorDisposed;

    public event Action<LSMonitorEstimate>? LSMonitorReady;

    public LSMonitor()
    {
        lMonitorOrchestrator.LWaveformReady += LSMonitorPeaksHandle;
    }

    public byte[] LSMonitorPeaks => lMonitorPeaks;

    public bool LSMonitorScanning => lMonitorScanning;

    public void LSMonitorSourceOpen(string? lPath, TimeSpan lDuration)
    {
        lMonitorSourcePath = lPath;
        lMonitorDuration = lDuration;
        lMonitorScanning = !string.IsNullOrWhiteSpace(lPath) && lDuration > TimeSpan.Zero;
        lMonitorOrchestrator.LWaveformStart(lPath, lDuration);
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

    private void LSMonitorPeaksHandle(byte[] lPeaks)
    {
        lMonitorPeaks = lPeaks;
        lMonitorBefore = LWaveformEstimate.LWaveformEnvelopeRead(lPeaks);
        lMonitorAfter = lMonitorBefore;
        if (lPeaks.Length > 0)
        {
            lMonitorScanning = false;
        }

        LSMonitorPublish();
        if (lPeaks.Length > 0)
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
        lock (lMonitorLock)
        {
            lMonitorCancelSource?.Cancel();
            lMonitorCancelSource?.Dispose();
            lMonitorCancelSource = new CancellationTokenSource();
            lToken = lMonitorCancelSource;
        }

        if (lMonitorPeaks.Length == 0)
        {
            return;
        }

        string? lPath = lMonitorSourcePath;
        TimeSpan lDuration = lMonitorDuration;
        string lGraph = lMonitorPlan.LWorkAudioFormat();

        if (string.IsNullOrEmpty(lGraph)
            || string.IsNullOrWhiteSpace(lPath)
            || lDuration <= TimeSpan.Zero)
        {
            lMonitorAfter = lMonitorBefore;
            lMonitorScanning = false;
            LSMonitorPublish();
            return;
        }

        lMonitorScanning = true;
        LSMonitorAfterScan(lPath, lDuration, lGraph, lToken.Token);
    }

    private void LSMonitorAfterScan(string lPath, TimeSpan lDuration, string lGraph, CancellationToken lToken)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(LSMonitorDebounceMs, lToken).ConfigureAwait(false);
                LWaveformScanResult lScanned = LWaveformScanner.LWaveformScan(lPath, lDuration, lToken, lGraph);
                if (lToken.IsCancellationRequested)
                {
                    return;
                }

                double[] lAfter = LWaveformEstimate.LWaveformEnvelopeRead(lScanned.LWaveformPeaks);
                lMonitorAfter = lAfter.Length > 0 ? lAfter : lMonitorBefore;
                lMonitorScanning = false;
                LSMonitorPublish();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception lException)
            {
                LTraceLog.LTraceErrorRecord("Monitor after-scan could not be generated", lException);
            }
        }, CancellationToken.None);
    }

    private void LSMonitorPublish()
    {
        LSMonitorReady?.Invoke(new LSMonitorEstimate(lMonitorBefore, lMonitorAfter));
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
