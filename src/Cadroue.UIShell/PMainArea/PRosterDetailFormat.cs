using System.IO;
using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private static string PRosterStampFormat(DateTimeOffset? pStamp) =>
        pStamp is { } pValue ? pValue.ToString("yyyy-MM-dd HH:mm:ss") : "Not yet";

    private static string PRosterSpentFormat(LWorkItem pWorkItem)
    {
        if (PRosterSpentRead(pWorkItem) is not { } pSpent)
        {
            return "Not yet";
        }

        return $"{pSpent:hh\\:mm\\:ss\\.fff}";
    }

    private static string PRosterSpeedFormat(LWorkItem pWorkItem)
    {
        if (PRosterSpentRead(pWorkItem) is not { } pSpent
            || pSpent.TotalSeconds <= 0
            || PRosterBytesRead(pWorkItem) is not { } pBytes
            || pBytes <= 0)
        {
            return "Not yet";
        }

        double pMebibytes = pBytes / 1048576d;
        return $"{pMebibytes / pSpent.TotalSeconds:0.##} MiB/s";
    }

    private static string PRosterOutputSizeRead(LWorkItem pWorkItem)
    {
        string pOutputSize = PRosterSizeFormat(PRosterBytesRead(pWorkItem));
        return PRosterRatioRead(pWorkItem) is { } pRosterRatio
            ? $"{pOutputSize}  ({pRosterRatio:P1})"
            : pOutputSize;
    }

    internal static double? PRosterRatioRead(LWorkItem pWorkItem)
    {
        if (PRosterBytesRead(pWorkItem) is not { } pOutputWhole
            || PRosterSourceBytesRead(pWorkItem) is not { } pSourceWhole
            || pSourceWhole <= 0)
        {
            return null;
        }

        return (double)pOutputWhole / pSourceWhole;
    }

    internal static string PRosterRatioFormat(LWorkItem pWorkItem) =>
        PRosterRatioRead(pWorkItem) is { } pRosterRatio ? $"{pRosterRatio:P1}" : "-";

    private static string PRosterSizeFormat(long? pSizeBytes)
    {
        if (pSizeBytes is not { } pWholeBytes || pWholeBytes < 0)
        {
            return "Unknown";
        }

        const double pRosterKibi = 1024d;
        const double pRosterMebi = pRosterKibi * 1024d;
        const double pRosterGibi = pRosterMebi * 1024d;

        return pWholeBytes >= pRosterGibi
            ? $"{pWholeBytes / pRosterGibi:0.##} GiB"
            : pWholeBytes >= pRosterMebi
                ? $"{pWholeBytes / pRosterMebi:0.##} MiB"
                : $"{pWholeBytes / pRosterKibi:0.##} KiB";
    }

    private static long? PRosterSourceBytesRead(LWorkItem pWorkItem) =>
        pWorkItem.LWorkSourceBytes ?? PRosterSizeRead(pWorkItem.LWorkSourcePath);

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
