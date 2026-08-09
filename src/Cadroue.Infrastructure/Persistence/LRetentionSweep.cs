using System;
using System.IO;
using Cadroue.Application;

namespace Cadroue.Infrastructure;

public static class LRetentionSweep
{
    public static int LRetentionRun(int lRetentionDays)
    {
        string lRetentionRoot = LDepot.LDepotRootRead();
        if (!Directory.Exists(lRetentionRoot))
        {
            return 0;
        }

        if (LDepot.LDepotRunningCheck(lRetentionRoot))
        {
            LTraceLog.LTraceInfoRecord("Retention sweep skipped: a job is running");
            return 0;
        }

        DateTime lRetentionNow = DateTime.UtcNow;
        int lRetentionRemoved = 0;

        foreach (string lRetentionPath in Directory.EnumerateFiles(lRetentionRoot, "*", SearchOption.AllDirectories))
        {
            string lRetentionRelative = Path.GetRelativePath(lRetentionRoot, lRetentionPath);
            if (LRetention.LRetentionExcludedCheck(lRetentionRelative))
            {
                continue;
            }

            try
            {
                if (LRetention.LRetentionExpiredCheck(File.GetLastWriteTimeUtc(lRetentionPath), lRetentionNow, lRetentionDays))
                {
                    File.Delete(lRetentionPath);
                    lRetentionRemoved++;
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        try
        {
            LDepotIndex.LDepotIndexRebuild();
            LDepotIndex.LDepotIndexCompact();
        }
        catch (Exception lRetentionException) when (lRetentionException is IOException or InvalidOperationException)
        {
            LTraceLog.LTraceInfoRecord("Retention sweep could not rebuild the index", lRetentionException.Message);
        }

        return lRetentionRemoved;
    }
}
