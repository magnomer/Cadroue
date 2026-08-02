namespace Cadroue.Media;

public static class LWaveform
{
    public const int LWaveformBucketMilliseconds = 40;

    public const int LWaveformSampleRate = 8000;

    public const int LWaveformPeakMaximum = 255;

    public static LSidecarWaveformRecord LWaveformRecordCreate(
        IReadOnlyCollection<byte> lWaveformPeaks,
        IReadOnlyCollection<byte> lWaveformRms,
        TimeSpan lWaveformDuration)
    {
        return new LSidecarWaveformRecord
        {
            LSidecarBucketMilliseconds = LWaveformBucketMilliseconds,
            LSidecarDurationMilliseconds = (long)Math.Round(lWaveformDuration.TotalMilliseconds),
            LSidecarPeaks = Convert.ToBase64String(lWaveformPeaks.ToArray()),
            LSidecarRms = Convert.ToBase64String(lWaveformRms.ToArray())
        };
    }

    public static byte[] LWaveformPeaksRead(LSidecarWaveformRecord? lWaveformRecord) =>
        LWaveformBytesRead(lWaveformRecord?.LSidecarPeaks);

    public static byte[] LWaveformRmsRead(LSidecarWaveformRecord? lWaveformRecord) =>
        LWaveformBytesRead(lWaveformRecord?.LSidecarRms);

    private static byte[] LWaveformBytesRead(string? lWaveformEncoded)
    {
        if (string.IsNullOrWhiteSpace(lWaveformEncoded))
        {
            return Array.Empty<byte>();
        }

        try
        {
            return Convert.FromBase64String(lWaveformEncoded);
        }
        catch (FormatException)
        {
            return Array.Empty<byte>();
        }
    }

    public static bool LWaveformRecordMatch(LSidecarWaveformRecord? lWaveformRecord, TimeSpan lWaveformDuration)
    {
        if (lWaveformRecord is null
            || lWaveformRecord.LSidecarBucketMilliseconds != LWaveformBucketMilliseconds
            || lWaveformRecord.LSidecarPeaks.Length == 0
            || lWaveformRecord.LSidecarRms.Length == 0)
        {
            return false;
        }

        long lWaveformExpected = (long)Math.Round(lWaveformDuration.TotalMilliseconds);
        return Math.Abs(lWaveformRecord.LSidecarDurationMilliseconds - lWaveformExpected) <= LWaveformBucketMilliseconds;
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
