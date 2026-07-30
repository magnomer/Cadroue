using System.Windows;
using System.Windows.Media;
using Cadroue.Media;

namespace Cadroue.UIShell.PFlow;

public sealed class LWaveformOrchestrator : IDisposable
{
    private readonly object lWaveformLock = new();
    private CancellationTokenSource? lWaveformCancel;
    private string? lWaveformSourcePath;
    private byte[] lWaveformPeaks = Array.Empty<byte>();
    private bool lWaveformDisposed;

    public event Action<byte[]>? LWaveformReady;

    public byte[] LWaveformCurrent => lWaveformPeaks;

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

            lWaveformCancel?.Cancel();
            lWaveformCancel?.Dispose();
            lWaveformCancel = new CancellationTokenSource();
            lWaveformToken = lWaveformCancel;
            lWaveformSourcePath = lWaveformPath;
            lWaveformPeaks = Array.Empty<byte>();
        }

        LWaveformReady?.Invoke(Array.Empty<byte>());

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
            lWaveformPrevious = lWaveformCancel;
            lWaveformCancel = null;
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

        LWaveformReady?.Invoke(Array.Empty<byte>());
    }

    public static Geometry? LWaveformGeometryCreate(
        byte[] lWaveformPeaks,
        double lWaveformWidth,
        double lWaveformRailTop,
        double lWaveformRailHeight,
        TimeSpan lWaveformRangeStart,
        TimeSpan lWaveformRangeEnd)
    {
        int lWaveformColumnCount = (int)Math.Ceiling(lWaveformWidth);
        if (lWaveformColumnCount <= 1 || lWaveformRailHeight <= 2)
        {
            return null;
        }

        double[] lWaveformColumns = LWaveform.LWaveformRangeRead(
            lWaveformPeaks, lWaveformRangeStart, lWaveformRangeEnd, lWaveformColumnCount);
        if (lWaveformColumns.Length == 0)
        {
            return null;
        }

        double lWaveformColumnWidth = lWaveformWidth / lWaveformColumnCount;
        double lWaveformCenterY = lWaveformRailTop + lWaveformRailHeight / 2;
        double lWaveformHalfHeight = lWaveformRailHeight / 2 - 1;
        var lWaveformGeometry = new StreamGeometry();
        using (StreamGeometryContext lWaveformContext = lWaveformGeometry.Open())
        {
            lWaveformContext.BeginFigure(
                new Point(0, lWaveformCenterY - lWaveformColumns[0] * lWaveformHalfHeight), true, true);
            for (int lWaveformColumn = 1; lWaveformColumn < lWaveformColumns.Length; lWaveformColumn++)
            {
                lWaveformContext.LineTo(
                    new Point(
                        lWaveformColumn * lWaveformColumnWidth,
                        lWaveformCenterY - lWaveformColumns[lWaveformColumn] * lWaveformHalfHeight),
                    false,
                    false);
            }

            for (int lWaveformColumn = lWaveformColumns.Length - 1; lWaveformColumn >= 0; lWaveformColumn--)
            {
                lWaveformContext.LineTo(
                    new Point(
                        lWaveformColumn * lWaveformColumnWidth,
                        lWaveformCenterY + lWaveformColumns[lWaveformColumn] * lWaveformHalfHeight),
                    false,
                    false);
            }
        }

        lWaveformGeometry.Freeze();
        return lWaveformGeometry;
    }

    private void LWaveformScanStart(string lWaveformPath, TimeSpan lWaveformDuration, CancellationToken lWaveformToken)
    {
        _ = Task.Run(() =>
        {
            var lWaveformClock = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                byte[] lWaveformScanned = LWaveformScanner.LWaveformScan(lWaveformPath, lWaveformDuration, lWaveformToken);
                if (lWaveformScanned.Length == 0 || lWaveformToken.IsCancellationRequested)
                {
                    return;
                }

                LSidecarStore.LSidecarWaveformSave(
                    lWaveformPath,
                    LWaveform.LWaveformRecordCreate(lWaveformScanned, lWaveformDuration));
                LTrace.LTraceRecord(
                    LTraceKind.LTraceWork,
                    $"Waveform generated for {System.IO.Path.GetFileName(lWaveformPath)}",
                    $"{lWaveformScanned.Length} peak(s) at {LWaveform.LWaveformBucketMilliseconds} ms stored in the sidecar",
                    lWaveformClock.Elapsed.TotalMilliseconds);
                LWaveformApply(lWaveformPath, lWaveformScanned);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception lWaveformException)
            {
                LAppLog.LError("Waveform could not be generated", lWaveformException);
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
