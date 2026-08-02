using Cadroue.Media;

namespace Cadroue.Infrastructure;

public sealed class LWaveformOrchestrator : IDisposable
{
    private readonly object lWaveformLock = new();
    private CancellationTokenSource? lWaveformCancelSource;
    private string? lWaveformSourcePath;
    private byte[] lWaveformPeaks = Array.Empty<byte>();
    private byte[] lWaveformRms = Array.Empty<byte>();
    private bool lWaveformDisposed;

    public event Action<byte[]>? LWaveformReady;

    public byte[] LWaveformCurrent => lWaveformPeaks;

    public byte[] LWaveformRmsCurrent => lWaveformRms;

    public void LWaveformStart(string? lWaveformPath, TimeSpan lWaveformDuration)
    {
        if (lWaveformDisposed || string.IsNullOrWhiteSpace(lWaveformPath) || lWaveformDuration <= TimeSpan.Zero)
        {
            LWaveformClear();
            return;
        }

        CancellationTokenSource lWaveformToken;
        lock (lWaveformLock)
        {
            if (string.Equals(lWaveformSourcePath, lWaveformPath, StringComparison.OrdinalIgnoreCase)
                && lWaveformPeaks.Length > 0)
            {
                LWaveformReady?.Invoke(lWaveformPeaks);
                return;
            }

            lWaveformCancelSource?.Cancel();
            lWaveformCancelSource?.Dispose();
            lWaveformCancelSource = new CancellationTokenSource();
            lWaveformToken = lWaveformCancelSource;
            lWaveformSourcePath = lWaveformPath;
            lWaveformPeaks = Array.Empty<byte>();
            lWaveformRms = Array.Empty<byte>();
        }

        LWaveformReady?.Invoke(Array.Empty<byte>());

        LSidecarWaveformRecord? lWaveformStored = LSidecarStore.LSidecarWaveformRead(lWaveformPath);
        if (LWaveform.LWaveformRecordMatch(lWaveformStored, lWaveformDuration))
        {
            LWaveformApply(
                lWaveformPath,
                LWaveform.LWaveformPeaksRead(lWaveformStored),
                LWaveform.LWaveformRmsRead(lWaveformStored));
            return;
        }

        LWaveformScanStart(lWaveformPath, lWaveformDuration, lWaveformToken.Token);
    }

    public void LWaveformSuspend()
    {
        CancellationTokenSource? lWaveformPrevious;
        lock (lWaveformLock)
        {
            lWaveformPrevious = lWaveformCancelSource;
            lWaveformCancelSource = null;
        }

        lWaveformPrevious?.Cancel();
        lWaveformPrevious?.Dispose();
    }

    public void LWaveformClear()
    {
        LWaveformSuspend();
        lock (lWaveformLock)
        {
            lWaveformSourcePath = null;
            lWaveformPeaks = Array.Empty<byte>();
            lWaveformRms = Array.Empty<byte>();
        }

        LWaveformReady?.Invoke(Array.Empty<byte>());
    }

    private void LWaveformScanStart(string lWaveformPath, TimeSpan lWaveformDuration, CancellationToken lWaveformToken)
    {
        _ = Task.Run(() =>
        {
            var lWaveformClock = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                LWaveformScanResult lWaveformScanned = LWaveformScanner.LWaveformScan(lWaveformPath, lWaveformDuration, lWaveformToken);
                if (lWaveformScanned.LWaveformPeaks.Length == 0 || lWaveformToken.IsCancellationRequested)
                {
                    return;
                }

                LSidecarStore.LSidecarWaveformSave(
                    lWaveformPath,
                    LWaveform.LWaveformRecordCreate(lWaveformScanned.LWaveformPeaks, lWaveformScanned.LWaveformRms, lWaveformDuration));
                LTrace.LTraceRecord(
                    LTraceKind.LTraceWork,
                    $"Waveform generated for {System.IO.Path.GetFileName(lWaveformPath)}",
                    $"{lWaveformScanned.LWaveformPeaks.Length} peak(s) at {LWaveform.LWaveformBucketMilliseconds} ms stored in the sidecar",
                    lWaveformClock.Elapsed.TotalMilliseconds);
                LWaveformApply(lWaveformPath, lWaveformScanned.LWaveformPeaks, lWaveformScanned.LWaveformRms);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception lWaveformException)
            {
                LTraceLog.LTraceErrorRecord("Waveform could not be generated", lWaveformException);
            }
        }, CancellationToken.None);
    }

    private void LWaveformApply(string lWaveformPath, byte[] lWaveformScanned, byte[] lWaveformScannedRms)
    {
        lock (lWaveformLock)
        {
            if (!string.Equals(lWaveformSourcePath, lWaveformPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            lWaveformPeaks = lWaveformScanned;
            lWaveformRms = lWaveformScannedRms;
        }

        LWaveformReady?.Invoke(lWaveformScanned);
    }

    public void Dispose()
    {
        if (lWaveformDisposed)
        {
            return;
        }

        lWaveformDisposed = true;
        LWaveformSuspend();
    }
}
