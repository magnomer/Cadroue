using System.Diagnostics;
using System.Globalization;

using Cadroue.Core;

namespace Cadroue.Media;

public readonly record struct LWaveformScanResult(bool LWaveformComplete, byte[] LWaveformPeaks, string LWaveformDetail)
{
    public static LWaveformScanResult LWaveformFailureCreate(string lWaveformDetail) =>
        new(false, Array.Empty<byte>(), lWaveformDetail);
}

public static class LWaveformScanner
{
    private const int LWaveformBufferBytes = 1 << 16;

    private const int LWaveformChannelCount = 2;

    private const int LWaveformBucketLimit = 4_000_000;

    private const int LWaveformExitMilliseconds = 5_000;

    private const int LWaveformDetailLimit = 400;

    private const int LWaveformChunkMilliseconds = 20_000;

    public static LWaveformScanResult LWaveformScan(
        string lWaveformSourcePath,
        TimeSpan lWaveformDuration,
        CancellationToken lWaveformCancelSource = default,
        string? lWaveformFilterGraph = null)
    {
        if (string.IsNullOrWhiteSpace(lWaveformSourcePath) || !File.Exists(lWaveformSourcePath))
        {
            return LWaveformScanResult.LWaveformFailureCreate("source file missing");
        }

        int lWaveformBucketExpected = LWaveform.LWaveformBucketsResolve(
            LWaveform.LWaveformMillisecondsResolve(lWaveformDuration));
        if (lWaveformBucketExpected <= 0 || lWaveformBucketExpected > LWaveformBucketLimit)
        {
            return LWaveformScanResult.LWaveformFailureCreate("duration outside the scannable range");
        }

        if (lWaveformFilterGraph is not null)
        {
            return LWaveformProcessRun(
                lWaveformSourcePath,
                lWaveformBucketExpected,
                lWaveformCancelSource,
                lWaveformFilterGraph,
                null);
        }

        var lWaveformPeaks = new List<byte>(lWaveformBucketExpected);
        long lWaveformMilliseconds = LWaveform.LWaveformMillisecondsResolve(lWaveformDuration);
        for (long lWaveformChunkStart = 0; lWaveformChunkStart < lWaveformMilliseconds;
             lWaveformChunkStart += LWaveformChunkMilliseconds)
        {
            long lWaveformChunkLength = Math.Min(LWaveformChunkMilliseconds, lWaveformMilliseconds - lWaveformChunkStart);
            LWaveformScanResult lWaveformChunk = LWaveformProcessRun(
                lWaveformSourcePath,
                LWaveform.LWaveformBucketsResolve(lWaveformChunkLength),
                lWaveformCancelSource,
                null,
                (lWaveformChunkStart, lWaveformChunkLength));
            if (!lWaveformChunk.LWaveformComplete)
            {
                return lWaveformChunk;
            }

            lWaveformPeaks.AddRange(lWaveformChunk.LWaveformPeaks);
        }

        return new LWaveformScanResult(
            true,
            LWaveformPeaksNormalize(lWaveformPeaks, lWaveformBucketExpected),
            string.Empty);
    }

    private static LWaveformScanResult LWaveformProcessRun(
        string lWaveformSourcePath,
        int lWaveformBucketExpected,
        CancellationToken lWaveformCancelSource,
        string? lWaveformFilterGraph,
        (long LWaveformOrigin, long LWaveformLength)? lWaveformChunk)
    {
        string lWaveformWindow = lWaveformChunk is { } lWaveformRange
            ? "-ss " + (lWaveformRange.LWaveformOrigin / 1000d).ToString("0.###", CultureInfo.InvariantCulture)
                + " -t " + (lWaveformRange.LWaveformLength / 1000d).ToString("0.###", CultureInfo.InvariantCulture)
                + " "
            : string.Empty;
        int lWaveformBucketSamples =
            LWaveform.LWaveformSampleRate * LWaveform.LWaveformBucketMilliseconds / 1000 * LWaveformChannelCount;
        string lWaveformGraph = "aresample=async=1:first_pts=0"
            + (string.IsNullOrWhiteSpace(lWaveformFilterGraph) ? string.Empty : "," + lWaveformFilterGraph);
        var lWaveformStart = new ProcessStartInfo(LTool.LToolFfmpegRead())
        {
            Arguments =
                "-v error -nostdin " + lWaveformWindow + "-i \"" + lWaveformSourcePath + "\""
                + " -vn -sn -dn -af \"" + lWaveformGraph + "\""
                + " -ac " + LWaveformChannelCount + " -ar " + LWaveform.LWaveformSampleRate
                + " -f s16le -",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var lWaveformPeaks = new List<byte>(lWaveformBucketExpected);
        Process? lWaveformProcess = null;
        Task<string> lWaveformDetail = Task.FromResult(string.Empty);
        int lWaveformExitCode;

        LMedia.LMediaScanClaim(lWaveformCancelSource);
        try
        {
            lWaveformProcess = Process.Start(lWaveformStart);
            if (lWaveformProcess is null)
            {
                return LWaveformScanResult.LWaveformFailureCreate("ffmpeg could not be started");
            }

            LCustody.LCustodyAttach(lWaveformProcess);
            LWaveformPrioritySet(lWaveformProcess);
            using var lWaveformKill = lWaveformCancelSource.Register(
                static lProcess => { try { ((Process)lProcess!).Kill(); } catch { } }, lWaveformProcess);

            lWaveformDetail = lWaveformProcess.StandardError.ReadToEndAsync();
            LWaveformStreamRead(
                lWaveformProcess.StandardOutput.BaseStream,
                lWaveformBucketSamples,
                lWaveformPeaks,
                lWaveformCancelSource);
            if (!lWaveformProcess.WaitForExit(LWaveformExitMilliseconds))
            {
                try { lWaveformProcess.Kill(); } catch { }
                lWaveformCancelSource.ThrowIfCancellationRequested();
                return LWaveformScanResult.LWaveformFailureCreate("ffmpeg did not exit after its output ended");
            }

            lWaveformExitCode = lWaveformProcess.ExitCode;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception lWaveformException)
        {
            lWaveformCancelSource.ThrowIfCancellationRequested();
            return LWaveformScanResult.LWaveformFailureCreate(lWaveformException.Message);
        }
        finally
        {
            if (lWaveformProcess is not null && !lWaveformProcess.HasExited)
            {
                try { lWaveformProcess.Kill(); } catch { }
            }

            lWaveformProcess?.Dispose();
            LMedia.LMediaScanRelease();
        }

        lWaveformCancelSource.ThrowIfCancellationRequested();
        if (lWaveformExitCode != 0)
        {
            return LWaveformScanResult.LWaveformFailureCreate(
                $"ffmpeg exit code {lWaveformExitCode}: {LWaveformDetailRead(lWaveformDetail)}");
        }

        if (lWaveformPeaks.Count == 0 && lWaveformChunk is not { LWaveformOrigin: > 0 })
        {
            return LWaveformScanResult.LWaveformFailureCreate("no audio samples were decoded");
        }

        return new LWaveformScanResult(
            true,
            LWaveformPeaksNormalize(lWaveformPeaks, lWaveformBucketExpected),
            string.Empty);
    }

    private static string LWaveformDetailRead(Task<string> lWaveformDetail)
    {
        string lWaveformText;
        try
        {
            lWaveformText = lWaveformDetail.GetAwaiter().GetResult().Trim();
        }
        catch
        {
            return string.Empty;
        }

        return lWaveformText.Length <= LWaveformDetailLimit
            ? lWaveformText
            : lWaveformText[^LWaveformDetailLimit..];
    }

    private static byte[] LWaveformPeaksNormalize(List<byte> lWaveformPeaks, int lWaveformBucketExpected)
    {
        var lWaveformFitted = new byte[lWaveformBucketExpected];
        lWaveformPeaks.CopyTo(0, lWaveformFitted, 0, Math.Min(lWaveformPeaks.Count, lWaveformBucketExpected));
        return lWaveformFitted;
    }

    private static void LWaveformPrioritySet(Process lWaveformProcess)
    {
        try
        {
            lWaveformProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        catch (Exception lWaveformException)
            when (lWaveformException is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
        }
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
