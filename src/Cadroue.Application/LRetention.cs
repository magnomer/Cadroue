using System;

namespace Cadroue.Application;

public static class LRetention
{
    private static readonly string[] LRetentionExcludedRoots = { "scheduled", "running", "palettes" };

    public static bool LRetentionExpiredCheck(DateTime lRetentionWriteUtc, DateTime lRetentionNowUtc, int lRetentionDays)
    {
        if (lRetentionDays <= 0)
        {
            return false;
        }

        return lRetentionWriteUtc < lRetentionNowUtc - TimeSpan.FromDays(lRetentionDays);
    }

    public static bool LRetentionExcludedCheck(string lRetentionRelativePath)
    {
        if (string.IsNullOrWhiteSpace(lRetentionRelativePath))
        {
            return false;
        }

        string[] lRetentionSegments = lRetentionRelativePath.Split(
            new[] { '/', '\\' },
            StringSplitOptions.RemoveEmptyEntries);
        if (lRetentionSegments.Length == 0)
        {
            return false;
        }

        string lRetentionRoot = lRetentionSegments[0];
        foreach (string lRetentionExcludedRoot in LRetentionExcludedRoots)
        {
            if (string.Equals(lRetentionRoot, lRetentionExcludedRoot, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        string lRetentionName = lRetentionSegments[^1];
        return lRetentionName.StartsWith("work.db", StringComparison.OrdinalIgnoreCase);
    }
}
