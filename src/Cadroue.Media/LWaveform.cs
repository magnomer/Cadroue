using Cadroue.Core;

namespace Cadroue.Media;

public static class LWaveform
{
    public const int LWaveformBucketMilliseconds = 40;

    public const int LWaveformSampleRate = 8000;

    public const int LWaveformPeakMaximum = 255;

    public static long LWaveformMillisecondsResolve(TimeSpan lWaveformDuration) =>
        (long)Math.Round(lWaveformDuration.TotalMilliseconds);

    public static int LWaveformBucketsResolve(long lWaveformMilliseconds) =>
        lWaveformMilliseconds <= 0
            ? 0
            : (int)((lWaveformMilliseconds + LWaveformBucketMilliseconds - 1) / LWaveformBucketMilliseconds);

    public static LSidecarWaveformRecord LWaveformRecordCreate(
        IReadOnlyCollection<byte> lWaveformPeaks,
        TimeSpan lWaveformDuration)
    {
        return new LSidecarWaveformRecord
        {
            LSidecarBucketMilliseconds = LWaveformBucketMilliseconds,
            LSidecarDurationMilliseconds = LWaveformMillisecondsResolve(lWaveformDuration),
            LSidecarPeaks = Convert.ToBase64String(lWaveformPeaks.ToArray())
        };
    }

    public static byte[] LWaveformPeaksRead(LSidecarWaveformRecord? lWaveformRecord)
    {
        if (string.IsNullOrWhiteSpace(lWaveformRecord?.LSidecarPeaks))
        {
            return Array.Empty<byte>();
        }

        try
        {
            return Convert.FromBase64String(lWaveformRecord.LSidecarPeaks);
        }
        catch (FormatException)
        {
            return Array.Empty<byte>();
        }
    }

    public static double[] LWaveformEnvelopeRead(byte[] lWaveformPeaks)
    {
        if (lWaveformPeaks.Length == 0)
        {
            return Array.Empty<double>();
        }

        var lWaveformEnvelope = new double[lWaveformPeaks.Length];
        for (int lWaveformIndex = 0; lWaveformIndex < lWaveformPeaks.Length; lWaveformIndex++)
        {
            lWaveformEnvelope[lWaveformIndex] = lWaveformPeaks[lWaveformIndex] / (double)LWaveformPeakMaximum;
        }

        return lWaveformEnvelope;
    }

    public static bool LWaveformRecordMatch(LSidecarWaveformRecord? lWaveformRecord, TimeSpan lWaveformDuration)
    {
        if (lWaveformRecord is null
            || lWaveformRecord.LSidecarBucketMilliseconds != LWaveformBucketMilliseconds
            || lWaveformRecord.LSidecarPeaks.Length == 0)
        {
            return false;
        }

        long lWaveformExpected = LWaveformMillisecondsResolve(lWaveformDuration);
        if (Math.Abs(lWaveformRecord.LSidecarDurationMilliseconds - lWaveformExpected) > LWaveformBucketMilliseconds)
        {
            return false;
        }

        return LWaveformPeaksRead(lWaveformRecord).Length
            == LWaveformBucketsResolve(lWaveformRecord.LSidecarDurationMilliseconds);
    }

    public static double[] LWaveformRangeRead(
        byte[] lWaveformPeaks,
        TimeSpan lWaveformRangeStart,
        TimeSpan lWaveformRangeEnd,
        int lWaveformColumnCount)
    {
        if (lWaveformPeaks.Length == 0 || lWaveformColumnCount <= 0 || lWaveformRangeEnd <= lWaveformRangeStart)
        {
            return Array.Empty<double>();
        }

        double lWaveformStartBucket = lWaveformRangeStart.TotalMilliseconds / LWaveformBucketMilliseconds;
        double lWaveformEndBucket = lWaveformRangeEnd.TotalMilliseconds / LWaveformBucketMilliseconds;
        double lWaveformBucketSpan = (lWaveformEndBucket - lWaveformStartBucket) / lWaveformColumnCount;
        var lWaveformColumns = new double[lWaveformColumnCount];

        for (int lWaveformColumn = 0; lWaveformColumn < lWaveformColumnCount; lWaveformColumn++)
        {
            double lWaveformFrom = lWaveformStartBucket + lWaveformColumn * lWaveformBucketSpan;
            double lWaveformTo = lWaveformFrom + lWaveformBucketSpan;
            int lWaveformFirst = (int)Math.Floor(lWaveformFrom);
            int lWaveformLast = (int)Math.Ceiling(lWaveformTo) - 1;
            if (lWaveformLast < lWaveformFirst)
            {
                lWaveformLast = lWaveformFirst;
            }

            lWaveformFirst = Math.Max(0, lWaveformFirst);
            lWaveformLast = Math.Min(lWaveformPeaks.Length - 1, lWaveformLast);
            if (lWaveformFirst > lWaveformLast)
            {
                continue;
            }

            byte lWaveformPeak = 0;
            for (int lWaveformIndex = lWaveformFirst; lWaveformIndex <= lWaveformLast; lWaveformIndex++)
            {
                if (lWaveformPeaks[lWaveformIndex] > lWaveformPeak)
                {
                    lWaveformPeak = lWaveformPeaks[lWaveformIndex];
                }
            }

            lWaveformColumns[lWaveformColumn] = lWaveformPeak / (double)LWaveformPeakMaximum;
        }

        return lWaveformColumns;
    }
}
