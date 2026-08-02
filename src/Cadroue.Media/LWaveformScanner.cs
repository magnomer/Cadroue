using System.Diagnostics;

namespace Cadroue.Media;

public readonly record struct LWaveformScanResult(byte[] LWaveformPeaks, byte[] LWaveformRms);

public static class LWaveformScanner
{
    private const int LWaveformBufferBytes = 1 << 16;

    private const int LWaveformBucketLimit = 4_000_000;

    public static LWaveformScanResult LWaveformScan(
        string lWaveformSourcePath,
        TimeSpan lWaveformDuration,
        CancellationToken lWaveformCancelSource = default)
    {
        if (string.IsNullOrWhiteSpace(lWaveformSourcePath)
            || !File.Exists(lWaveformSourcePath)
            || lWaveformDuration <= TimeSpan.Zero)
        {
            return new LWaveformScanResult(Array.Empty<byte>(), Array.Empty<byte>());
        }

        lWaveformCancelSource.ThrowIfCancellationRequested();

        int lWaveformBucketSamples = LWaveform.LWaveformSampleRate * LWaveform.LWaveformBucketMilliseconds / 1000;
        long lWaveformBucketExpected = (long)Math.Ceiling(
            lWaveformDuration.TotalMilliseconds / LWaveform.LWaveformBucketMilliseconds);
        if (lWaveformBucketExpected <= 0 || lWaveformBucketExpected > LWaveformBucketLimit)
        {
            return new LWaveformScanResult(Array.Empty<byte>(), Array.Empty<byte>());
        }

        var lWaveformStart = new ProcessStartInfo(LTool.LToolFfmpegRead())
        {
            Arguments =
                "-v quiet -nostdin -i \"" + lWaveformSourcePath + "\""
                + " -vn -ac 1 -ar " + LWaveform.LWaveformSampleRate
                + " -f s16le -",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var lWaveformPeaks = new List<byte>((int)lWaveformBucketExpected);
        var lWaveformRms = new List<byte>((int)lWaveformBucketExpected);
        Process? lWaveformProcess = null;

        try
        {
            lWaveformProcess = Process.Start(lWaveformStart);
            if (lWaveformProcess is null)
            {
                return new LWaveformScanResult(Array.Empty<byte>(), Array.Empty<byte>());
            }

            using var lWaveformKill = lWaveformCancelSource.Register(
                static lProcess => { try { ((Process)lProcess!).Kill(); } catch { } }, lWaveformProcess);

            LWaveformStreamRead(
                lWaveformProcess.StandardOutput.BaseStream,
                lWaveformBucketSamples,
                lWaveformPeaks,
                lWaveformRms,
                lWaveformCancelSource);
            lWaveformProcess.WaitForExit();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            lWaveformCancelSource.ThrowIfCancellationRequested();
            return new LWaveformScanResult(Array.Empty<byte>(), Array.Empty<byte>());
        }
        finally
        {
            if (lWaveformProcess is not null && !lWaveformProcess.HasExited)
            {
                try { lWaveformProcess.Kill(); } catch { }
            }

            lWaveformProcess?.Dispose();
        }

        lWaveformCancelSource.ThrowIfCancellationRequested();
        return lWaveformPeaks.Count == 0
            ? new LWaveformScanResult(Array.Empty<byte>(), Array.Empty<byte>())
            : new LWaveformScanResult(lWaveformPeaks.ToArray(), lWaveformRms.ToArray());
    }

    private static void LWaveformStreamRead(
        Stream lWaveformStream,
        int lWaveformBucketSamples,
        List<byte> lWaveformPeaks,
        List<byte> lWaveformRms,
        CancellationToken lWaveformCancelSource)
    {
        byte[] lWaveformBuffer = new byte[LWaveformBufferBytes];
        int lWaveformCarry = -1;
        int lWaveformSampleCount = 0;
        int lWaveformBucketPeak = 0;
        double lWaveformBucketSquares = 0;
        int lWaveformRead;

        while ((lWaveformRead = lWaveformStream.Read(lWaveformBuffer, 0, lWaveformBuffer.Length)) > 0)
        {
            lWaveformCancelSource.ThrowIfCancellationRequested();
            int lWaveformOffset = 0;
            if (lWaveformCarry >= 0)
            {
                LWaveformSampleAdd(
                    (short)(lWaveformCarry | (lWaveformBuffer[0] << 8)),
                    ref lWaveformBucketPeak,
                    ref lWaveformBucketSquares,
                    ref lWaveformSampleCount,
                    lWaveformBucketSamples,
                    lWaveformPeaks,
                    lWaveformRms);
                lWaveformCarry = -1;
                lWaveformOffset = 1;
            }

            for (; lWaveformOffset + 1 < lWaveformRead; lWaveformOffset += 2)
            {
                LWaveformSampleAdd(
                    (short)(lWaveformBuffer[lWaveformOffset] | (lWaveformBuffer[lWaveformOffset + 1] << 8)),
                    ref lWaveformBucketPeak,
                    ref lWaveformBucketSquares,
                    ref lWaveformSampleCount,
                    lWaveformBucketSamples,
                    lWaveformPeaks,
                    lWaveformRms);
            }

            if (lWaveformOffset < lWaveformRead)
            {
                lWaveformCarry = lWaveformBuffer[lWaveformOffset];
            }
        }

        if (lWaveformSampleCount > 0)
        {
            lWaveformPeaks.Add((byte)lWaveformBucketPeak);
            lWaveformRms.Add(LWaveformRmsRead(lWaveformBucketSquares, lWaveformSampleCount));
        }
    }

    private static void LWaveformSampleAdd(
        short lWaveformSample,
        ref int lWaveformBucketPeak,
        ref double lWaveformBucketSquares,
        ref int lWaveformSampleCount,
        int lWaveformBucketSamples,
        List<byte> lWaveformPeaks,
        List<byte> lWaveformRms)
    {
        int lWaveformLevel = lWaveformSample == short.MinValue ? short.MaxValue : Math.Abs((int)lWaveformSample);
        lWaveformBucketSquares += (double)lWaveformLevel * lWaveformLevel;
        lWaveformLevel = lWaveformLevel * LWaveform.LWaveformPeakMaximum / short.MaxValue;
        if (lWaveformLevel > lWaveformBucketPeak)
        {
            lWaveformBucketPeak = lWaveformLevel;
        }

        if (++lWaveformSampleCount < lWaveformBucketSamples)
        {
            return;
        }

        lWaveformPeaks.Add((byte)lWaveformBucketPeak);
        lWaveformRms.Add(LWaveformRmsRead(lWaveformBucketSquares, lWaveformSampleCount));
        lWaveformBucketPeak = 0;
        lWaveformBucketSquares = 0;
        lWaveformSampleCount = 0;
    }

    private static byte LWaveformRmsRead(double lWaveformSquares, int lWaveformCount)
    {
        double lWaveformRoot = Math.Sqrt(lWaveformSquares / lWaveformCount);
        int lWaveformLevel = (int)(lWaveformRoot * LWaveform.LWaveformPeakMaximum / short.MaxValue);
        return (byte)Math.Clamp(lWaveformLevel, 0, LWaveform.LWaveformPeakMaximum);
    }
}
