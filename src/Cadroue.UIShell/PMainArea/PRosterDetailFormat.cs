using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private static string PRosterRateFormat(double? pRatePerSecond) =>
        pRatePerSecond is { } pRate && pRate > 0
            ? LLocalization.LLocalizationFormat("Roster.Field.KeyframeRate", pRate)
            : LLocalization.LLocalizationTextRead("Roster.Value.Unknown");

    private static string PRosterStampFormat(DateTimeOffset? pStamp) =>
        pStamp is { } pValue ? pValue.ToString("yyyy-MM-dd HH:mm:ss") : LLocalization.LLocalizationTextRead("Roster.Value.NotYet");

    private static string PRosterSpentFormat(LWorkItem pWorkItem)
    {
        if (PRosterSpentRead(pWorkItem) is not { } pSpent)
        {
            return LLocalization.LLocalizationTextRead("Roster.Value.NotYet");
        }

        if (pSpent < TimeSpan.Zero)
        {
            pSpent = TimeSpan.Zero;
        }

        var pRounded = TimeSpan.FromSeconds(Math.Max(1, (long)Math.Ceiling(pSpent.TotalSeconds)));
        int pHours = (int)pRounded.TotalHours;
        return pHours > 0
            ? $"{pHours}:{pRounded.Minutes:00}:{pRounded.Seconds:00}"
            : $"{pRounded.Minutes:00}:{pRounded.Seconds:00}";
    }

    private static string PRosterSpeedFormat(LWorkItem pWorkItem)
    {
        if (PRosterSpentRead(pWorkItem) is not { } pSpent
            || pSpent.TotalSeconds <= 0
            || PRosterBytesRead(pWorkItem) is not { } pBytes
            || pBytes <= 0)
        {
            return LLocalization.LLocalizationTextRead("Roster.Value.NotYet");
        }

        double pMebibytes = pBytes / 1048576d;
        return $"{pMebibytes / pSpent.TotalSeconds:0.##} MiB/s";
    }

    internal static double? PRosterRatioRead(LWorkItem pWorkItem)
    {
        if (PRosterBytesRead(pWorkItem) is not { } pOutputWhole
            || PRosterSourceRead(pWorkItem) is not { } pSourceWhole
            || pSourceWhole <= 0)
        {
            return null;
        }

        return (double)pOutputWhole / pSourceWhole;
    }

    internal static string PRosterRatioFormat(LWorkItem pWorkItem) =>
        PRosterRatioRead(pWorkItem) is { } pRosterRatio ? $"{pRosterRatio:P1}" : "-";

    private static string PRosterMebiFormat(long? pSizeBytes)
    {
        if (pSizeBytes is not { } pWholeBytes || pWholeBytes < 0)
        {
            return LLocalization.LLocalizationTextRead("Roster.Value.Unknown");
        }

        double pMebibytes = pWholeBytes / 1048576d;
        return pMebibytes >= 1024d
            ? $"{pMebibytes / 1024d:0.#} GiB"
            : $"{Math.Round(pMebibytes)} MiB";
    }

    private static string PRosterClockFormat(TimeSpan pSpan)
    {
        if (pSpan < TimeSpan.Zero)
        {
            pSpan = TimeSpan.Zero;
        }

        int pHours = (int)pSpan.TotalHours;
        return pHours > 0
            ? $"{pHours}:{pSpan.Minutes:00}:{pSpan.Seconds:00}"
            : $"{pSpan.Minutes}:{pSpan.Seconds:00}";
    }

    // Source size is read only from the record measured while the job ran; a merge item
    // stores one byte total per input. Nothing is measured from disk here, so a deleted
    // source still shows its recorded size.
    internal static long? PRosterSourceRead(LWorkItem pWorkItem)
    {
        if (pWorkItem.LWorkMergeSources.Count > 1 && pWorkItem.LWorkMergeBytes.Count > 0)
        {
            long pMergeTotal = 0;
            foreach (long pMergeBytes in pWorkItem.LWorkMergeBytes)
            {
                pMergeTotal += pMergeBytes;
            }

            if (pMergeTotal > 0)
            {
                return pMergeTotal;
            }
        }

        return pWorkItem.LWorkSourceBytes;
    }

    private static long? PRosterBytesRead(LWorkItem pWorkItem) => pWorkItem.LWorkOutputBytes;

    private static TimeSpan? PRosterSpentRead(LWorkItem pWorkItem)
    {
        if (pWorkItem.LWorkStartTime is not { } pStarted)
        {
            return null;
        }

        DateTimeOffset pFinished = pWorkItem.LWorkFinishTime ?? DateTimeOffset.Now;
        TimeSpan pSpent = pFinished - pStarted;
        return pSpent < TimeSpan.Zero ? null : pSpent;
    }
}
