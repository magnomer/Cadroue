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

    private const int LWaveformDetailLimit = 400;

    private const int LWaveformChunkMilliseconds = 20_000;

    public static async Task<LWaveformScanResult> LWaveformScan(
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
            return await LWaveformProcessRun(
                lWaveformSourcePath,
                lWaveformBucketExpected,
                lWaveformCancelSource,
                lWaveformFilterGraph,
                null).ConfigureAwait(false);
        }

        var lWaveformPeaks = new List<byte>(lWaveformBucketExpected);
        long lWaveformMilliseconds = LWaveform.LWaveformMillisecondsResolve(lWaveformDuration);
        for (long lWaveformChunkStart = 0; lWaveformChunkStart < lWaveformMilliseconds;
             lWaveformChunkStart += LWaveformChunkMilliseconds)
        {
            long lWaveformChunkLength = Math.Min(
                LWaveformChunkMilliseconds, lWaveformMilliseconds - lWaveformChunkStart);
            LWaveformScanResult lWaveformChunk = await LWaveformProcessRun(
                lWaveformSourcePath,
                LWaveform.LWaveformBucketsResolve(lWaveformChunkLength),
                lWaveformCancelSource,
                null,
                (lWaveformChunkStart, lWaveformChunkLength)).ConfigureAwait(false);
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

    private static async Task<LWaveformScanResult> LWaveformProcessRun(
        string lWaveformSourcePath,
        int lWaveformBucketExpected,
        CancellationToken lWaveformCancelSource,
        string? lWaveformFilterGraph,
        (long LWaveformOrigin, long LWaveformLength)? lWaveformChunk)
    {
        int lWaveformBucketSamples =
            LWaveform.LWaveformSampleRate * LWaveform.LWaveformBucketMilliseconds / 1000 * LWaveformChannelCount;
        string lWaveformGraph = "aresample=async=1:first_pts=0"
            + (string.IsNullOrWhiteSpace(lWaveformFilterGraph) ? string.Empty : "," + lWaveformFilterGraph);
        var lWaveformArguments = new List<string> { "-v", "error", "-nostdin" };
        if (lWaveformChunk is { } lWaveformRange)
        {
            lWaveformArguments.AddRange(
            [
                "-ss", (lWaveformRange.LWaveformOrigin / 1000d).ToString("0.###", CultureInfo.InvariantCulture),
                "-t", (lWaveformRange.LWaveformLength / 1000d).ToString("0.###", CultureInfo.InvariantCulture)
            ]);
        }

        lWaveformArguments.AddRange(
        [
            "-i", lWaveformSourcePath,
            "-vn", "-sn", "-dn",
            "-af", lWaveformGraph,
            "-ac", LWaveformChannelCount.ToString(CultureInfo.InvariantCulture),
            "-ar", LWaveform.LWaveformSampleRate.ToString(CultureInfo.InvariantCulture),
            "-f", "s16le", "-"
        ]);

        var lWaveformPeaks = new List<byte>(lWaveformBucketExpected);
        LEmployerResult lWaveformResult;
        await LMedia.LMediaScanClaim(lWaveformCancelSource).ConfigureAwait(false);
        try
        {
            var lWaveformEmployer = new LEmployer(LTool.LToolFfmpegRead()) { LEmployerBackground = true };
            lWaveformResult = await lWaveformEmployer.LEmployerStreamRun(
                lWaveformArguments,
                lWaveformCancelSource,
                (lWaveformStream, lWaveformCancel) => LWaveformStreamRead(
                    lWaveformStream, lWaveformBucketSamples, lWaveformPeaks, lWaveformCancel)).ConfigureAwait(false);
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
            LMedia.LMediaScanRelease();
        }

        lWaveformCancelSource.ThrowIfCancellationRequested();
        if (lWaveformResult.LEmployerExit != 0)
        {
            string lWaveformDetail = LWaveformDetailRead(lWaveformResult.LEmployerError);
            return LWaveformScanResult.LWaveformFailureCreate(
                $"ffmpeg exit code {lWaveformResult.LEmployerExit}: {lWaveformDetail}");
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

    private static string LWaveformDetailRead(string lWaveformDetail)
    {
        string lWaveformText = lWaveformDetail.Trim();
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

    private static async Task LWaveformStreamRead(
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

        while ((lWaveformRead = await lWaveformStream
            .ReadAsync(lWaveformBuffer.AsMemory(), lWaveformCancelSource)
            .ConfigureAwait(false)) > 0)
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
