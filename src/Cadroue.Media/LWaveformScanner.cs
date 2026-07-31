using System.Diagnostics;

namespace Cadroue.Media;

public static class LWaveformScanner
{
    private const int LWaveformBufferBytes = 1 << 16;

    private const int LWaveformBucketLimit = 4_000_000;

    public static byte[] LWaveformScan(
        string lWaveformSourcePath,
        TimeSpan lWaveformDuration,
        CancellationToken lWaveformCancelSource = default)
    {
        if (string.IsNullOrWhiteSpace(lWaveformSourcePath)
            || !File.Exists(lWaveformSourcePath)
            || lWaveformDuration <= TimeSpan.Zero)
        {
            return Array.Empty<byte>();
        }

        lWaveformCancelSource.ThrowIfCancellationRequested();

        int lWaveformBucketSamples = LWaveform.LWaveformSampleRate * LWaveform.LWaveformBucketMilliseconds / 1000;
        long lWaveformBucketExpected = (long)Math.Ceiling(
            lWaveformDuration.TotalMilliseconds / LWaveform.LWaveformBucketMilliseconds);
        if (lWaveformBucketExpected <= 0 || lWaveformBucketExpected > LWaveformBucketLimit)
        {
            return Array.Empty<byte>();
        }

        var lWaveformStart = new ProcessStartInfo("ffmpeg")
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
        Process? lWaveformProcess = null;

        try
        {
            lWaveformProcess = Process.Start(lWaveformStart);
            if (lWaveformProcess is null)
            {
                return Array.Empty<byte>();
            }

            using var lWaveformKill = lWaveformCancelSource.Register(
                static lProcess => { try { ((Process)lProcess!).Kill(); } catch { } }, lWaveformProcess);

            LWaveformStreamRead(
                lWaveformProcess.StandardOutput.BaseStream,
                lWaveformBucketSamples,
                lWaveformPeaks,
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
            return Array.Empty<byte>();
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
        return lWaveformPeaks.Count == 0 ? Array.Empty<byte>() : lWaveformPeaks.ToArray();
    }

    private static void LWaveformStreamRead(
        Stream lWaveformStream,
        int lWaveformBucketSamples,
        List<byte> lWaveformPeaks,
        CancellationToken lWaveformCancelSource)
    {
        byte[] lWaveformBuffer = new byte[LWaveformBufferBytes];
        int lWaveformCarry = -1;
        int lWaveformSampleCount = 0;
        int lWaveformBucketPeak = 0;
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
                    ref lWaveformSampleCount,
                    lWaveformBucketSamples,
                    lWaveformPeaks);
                lWaveformCarry = -1;
                lWaveformOffset = 1;
            }

            for (; lWaveformOffset + 1 < lWaveformRead; lWaveformOffset += 2)
            {
                LWaveformSampleAdd(
                    (short)(lWaveformBuffer[lWaveformOffset] | (lWaveformBuffer[lWaveformOffset + 1] << 8)),
                    ref lWaveformBucketPeak,
                    ref lWaveformSampleCount,
                    lWaveformBucketSamples,
                    lWaveformPeaks);
            }

            if (lWaveformOffset < lWaveformRead)
            {
                lWaveformCarry = lWaveformBuffer[lWaveformOffset];
            }
        }

        if (lWaveformSampleCount > 0)
        {
            lWaveformPeaks.Add((byte)lWaveformBucketPeak);
        }
    }

    private static void LWaveformSampleAdd(
        short lWaveformSample,
        ref int lWaveformBucketPeak,
        ref int lWaveformSampleCount,
        int lWaveformBucketSamples,
        List<byte> lWaveformPeaks)
    {
        int lWaveformLevel = lWaveformSample == short.MinValue ? short.MaxValue : Math.Abs((int)lWaveformSample);
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
        lWaveformBucketPeak = 0;
        lWaveformSampleCount = 0;
    }
}
