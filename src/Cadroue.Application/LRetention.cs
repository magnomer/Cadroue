using System;

namespace Cadroue.Application;

public static class LRetention
{
    private static readonly string[] LRetentionSweptRoots =
    {
        "done", "failed", "cancelled", "audiowork", "mergework", "bridgework", "passwork"
    };

    public static bool LRetentionExpiredCheck(
        DateTime lRetentionWriteUtc,
        DateTime lRetentionNowUtc,
        int lRetentionDays)
    {
        if (lRetentionDays <= 0)
        {
            return false;
        }

        return lRetentionWriteUtc < lRetentionNowUtc - TimeSpan.FromDays(lRetentionDays);
    }

    public static bool LRetentionSweptCheck(string lRetentionRelativePath)
    {
        if (string.IsNullOrWhiteSpace(lRetentionRelativePath))
        {
            return false;
        }

        string[] lRetentionSegments = lRetentionRelativePath.Split(
            new[] { '/', '\\' },
            StringSplitOptions.RemoveEmptyEntries);
        if (lRetentionSegments.Length < 2)
        {
            return false;
        }

        string lRetentionRoot = lRetentionSegments[0];
        foreach (string lRetentionSweptRoot in LRetentionSweptRoots)
        {
            if (string.Equals(lRetentionRoot, lRetentionSweptRoot, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
