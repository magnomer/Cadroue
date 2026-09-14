using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public readonly record struct LWaveformNotice(byte[] LWaveformPeaks, bool LWaveformPending);

public sealed class LWaveformOrchestrator : IDisposable
{
    private readonly object lWaveformLock = new();
    private CancellationTokenSource? lWaveformCancelSource;
    private string? lWaveformSourcePath;
    private byte[] lWaveformPeaks = Array.Empty<byte>();
    private bool lWaveformDisposed;

    public event Action<LWaveformNotice>? LWaveformReady;

    public byte[] LWaveformCurrent => lWaveformPeaks;

    public void LWaveformStart(string? lWaveformPath, TimeSpan lWaveformDuration, bool lWaveformAudioPresent)
    {
        if (lWaveformDisposed
            || !lWaveformAudioPresent
            || string.IsNullOrWhiteSpace(lWaveformPath)
            || lWaveformDuration <= TimeSpan.Zero)
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
                LWaveformReady?.Invoke(new LWaveformNotice(lWaveformPeaks, false));
                return;
            }

            lWaveformCancelSource?.Cancel();
            lWaveformCancelSource?.Dispose();
            lWaveformCancelSource = new CancellationTokenSource();
            lWaveformToken = lWaveformCancelSource;
            lWaveformSourcePath = lWaveformPath;
            lWaveformPeaks = Array.Empty<byte>();
        }

        LWaveformReady?.Invoke(new LWaveformNotice(Array.Empty<byte>(), true));

        LSidecarWaveformRecord? lWaveformStored = LSidecarStore.LSidecarWaveformRead(lWaveformPath);
        if (LWaveform.LWaveformRecordMatch(lWaveformStored, lWaveformDuration))
        {
            LWaveformApply(lWaveformPath, LWaveform.LWaveformPeaksRead(lWaveformStored));
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
        }

        LWaveformReady?.Invoke(new LWaveformNotice(Array.Empty<byte>(), false));
    }

    private void LWaveformScanStart(string lWaveformPath, TimeSpan lWaveformDuration, CancellationToken lWaveformToken)
    {
        _ = Task.Run(() =>
        {
            var lWaveformClock = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                LWaveformScanResult lWaveformScanned = LWaveformScanner.LWaveformScan(
                    lWaveformPath,
                    lWaveformDuration,
                    lWaveformToken);
                if (lWaveformToken.IsCancellationRequested)
                {
                    return;
                }

                if (!lWaveformScanned.LWaveformComplete)
                {
                    LTrace.LTraceRecord(
                        LTraceKind.LTraceWarning,
                        $"Waveform unavailable for {System.IO.Path.GetFileName(lWaveformPath)}",
                        lWaveformScanned.LWaveformDetail,
                        lWaveformClock.Elapsed.TotalMilliseconds);
                    LWaveformApply(lWaveformPath, Array.Empty<byte>());
                    return;
                }

                LSidecarStore.LSidecarWaveformSave(
                    lWaveformPath,
                    LWaveform.LWaveformRecordCreate(lWaveformScanned.LWaveformPeaks, lWaveformDuration));
                LTrace.LTraceRecord(
                    LTraceKind.LTraceWork,
                    $"Waveform generated for {System.IO.Path.GetFileName(lWaveformPath)}",
                    $"{lWaveformScanned.LWaveformPeaks.Length} peak(s) at {LWaveform.LWaveformBucketMilliseconds} ms " +
                    "stored in the sidecar",
                    lWaveformClock.Elapsed.TotalMilliseconds);
                LWaveformApply(lWaveformPath, lWaveformScanned.LWaveformPeaks);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception lWaveformException)
            {
                LTraceLog.LTraceErrorRecord("Waveform could not be generated", lWaveformException);
                LWaveformApply(lWaveformPath, Array.Empty<byte>());
            }
        }, CancellationToken.None);
    }

    private void LWaveformApply(string lWaveformPath, byte[] lWaveformScanned)
    {
        lock (lWaveformLock)
        {
            if (!string.Equals(lWaveformSourcePath, lWaveformPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            lWaveformPeaks = lWaveformScanned;
        }

        LWaveformReady?.Invoke(new LWaveformNotice(lWaveformScanned, false));
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
