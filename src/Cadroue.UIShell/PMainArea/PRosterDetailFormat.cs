using System.IO;
using Cadroue.Core;
using Cadroue.Media;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private static double? PRosterIntervalRead(string? pSourcePath)
    {
        if (string.IsNullOrWhiteSpace(pSourcePath))
        {
            return null;
        }

        IReadOnlyList<long> pKeyframes = Cadroue.Application.LLibrarian.LLibrarianKeyframesLoad(pSourcePath);
        if (pKeyframes.Count <= 1)
        {
            return null;
        }

        return (pKeyframes[^1] - pKeyframes[0]) / 1000d / (pKeyframes.Count - 1);
    }

    private static readonly Dictionary<string, double?> pRosterKeyframeCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> pRosterKeyframePending = new(StringComparer.OrdinalIgnoreCase);

    private double? PRosterKeyframeRead(string? pSourcePath, TimeSpan pDuration)
    {
        if (PRosterIntervalRead(pSourcePath) is { } pSidecarInterval)
        {
            return pSidecarInterval;
        }

        if (string.IsNullOrWhiteSpace(pSourcePath) || !File.Exists(pSourcePath) || pDuration <= TimeSpan.Zero)
        {
            return null;
        }

        if (pRosterKeyframeCache.TryGetValue(pSourcePath, out double? pCached))
        {
            return pCached;
        }

        PRosterKeyframeDefer(pSourcePath, pDuration);
        return null;
    }

    private void PRosterKeyframeDefer(string pSourcePath, TimeSpan pDuration)
    {
        if (!pRosterKeyframePending.Add(pSourcePath))
        {
            return;
        }

        Guid pRosterProbeId = PRosterSelectRead()?.LWorkId ?? Guid.Empty;
        _ = Task.Run(() =>
        {
            double? pInterval = null;
            try
            {
                IReadOnlyList<LKeyframeEntry> pKeyframes = LKeyframeSeeker.LKeyframeRangeScan(pSourcePath, TimeSpan.Zero, pDuration);
                if (pKeyframes.Count >= 2)
                {
                    double pSpanMilliseconds =
                        (pKeyframes[^1].LKeyframePresentationTime - pKeyframes[0].LKeyframePresentationTime).TotalMilliseconds;
                    pInterval = pSpanMilliseconds / 1000d / (pKeyframes.Count - 1);
                }
            }
            catch (Exception pScanError)
            {
                LTraceLog.LTraceErrorRecord($"Job detail could not scan keyframes '{Path.GetFileName(pSourcePath)}': {pScanError.Message}");
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                pRosterKeyframePending.Remove(pSourcePath);
                PRosterCacheSet(pRosterKeyframeCache, pSourcePath, pInterval);
                if (PRosterSelectRead()?.LWorkId == pRosterProbeId)
                {
                    PRosterDetailUpdate();
                }
            }));
        });
    }

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

    internal static long? PRosterSourceRead(LWorkItem pWorkItem)
    {
        if (pWorkItem.LWorkMergeSources.Count > 1)
        {
            long pMergeTotal = 0;
            foreach (string pMergeSource in pWorkItem.LWorkMergeSources)
            {
                if (PRosterSizeRead(pMergeSource) is not { } pMergeBytes)
                {
                    pMergeTotal = 0;
                    break;
                }

                pMergeTotal += pMergeBytes;
            }

            if (pMergeTotal > 0)
            {
                return pMergeTotal;
            }
        }

        return pWorkItem.LWorkSourceBytes ?? PRosterSizeRead(pWorkItem.LWorkSourcePath);
    }

    private static long? PRosterSizeRead(string? pFilePath)
    {
        if (string.IsNullOrWhiteSpace(pFilePath))
        {
            return null;
        }

        try
        {
            var pSizeFile = new FileInfo(pFilePath);
            return pSizeFile.Exists ? pSizeFile.Length : null;
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    private static long? PRosterBytesRead(LWorkItem pWorkItem)
    {
        if (pWorkItem.LWorkOutputBytes is { } pRecordedBytes)
        {
            return pRecordedBytes;
        }

        if (string.IsNullOrWhiteSpace(pWorkItem.LWorkOutputPath))
        {
            return null;
        }

        try
        {
            var pOutputFile = new FileInfo(pWorkItem.LWorkOutputPath);
            return pOutputFile.Exists ? pOutputFile.Length : null;
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

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
