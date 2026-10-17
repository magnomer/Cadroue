namespace Cadroue.Media;

internal static class LWaveformSample
{
    private const int LWaveformBufferBytes = 1 << 16;

    internal static async Task LWaveformStreamRead(
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
