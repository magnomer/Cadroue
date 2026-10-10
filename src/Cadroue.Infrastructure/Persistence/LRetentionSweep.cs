using System;
using System.IO;
using Cadroue.Application;

namespace Cadroue.Infrastructure;

public static class LRetentionSweep
{
    public static void LRetentionSweepStart(bool lRetentionActive, int lRetentionDays)
    {
        if (!lRetentionActive)
        {
            return;
        }

        _ = System.Threading.Tasks.Task.Run(() =>
        {
            int lRetentionRemoved = LRetentionRun(lRetentionDays);
            LTraceLog.LTraceInfoRecord($"Retention sweep removed {lRetentionRemoved} old records");
        });
    }

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

        foreach (string lRetentionPath in LRetentionFolderRead(lRetentionRoot))
        {
            string lRetentionRelative = Path.GetRelativePath(lRetentionRoot, lRetentionPath);
            if (!LRetention.LRetentionSweptCheck(lRetentionRelative))
            {
                continue;
            }

            if (LRetentionFileRemove(lRetentionPath, lRetentionNow, lRetentionDays))
            {
                lRetentionRemoved++;
            }
        }

        try
        {
            LDepotIndex.LDepotIndexRebuild(lRetentionRoot);
            LDepotIndex.LDepotIndexCompact(lRetentionRoot);
        }
        catch (Exception lRetentionException) when (lRetentionException is IOException or InvalidOperationException)
        {
            LTraceLog.LTraceInfoRecord("Retention sweep could not rebuild the index", lRetentionException.Message);
        }

        return lRetentionRemoved;
    }

    private static string[] LRetentionFolderRead(string lRetentionFolder)
    {
        try
        {
            return Directory.GetFiles(lRetentionFolder, "*", SearchOption.AllDirectories);
        }
        catch (Exception lRetentionException) when (lRetentionException is IOException or UnauthorizedAccessException)
        {
            LTraceLog.LTraceInfoRecord("Retention sweep could not read the folder", lRetentionException.Message);
            return [];
        }
    }

    private static bool LRetentionFileRemove(string lRetentionPath, DateTime lRetentionNow, int lRetentionDays)
    {
        try
        {
            var lRetentionFile = new FileInfo(lRetentionPath);
            if (!lRetentionFile.Exists
                || !LRetention.LRetentionExpiredCheck(
                    lRetentionFile.LastWriteTimeUtc,
                    lRetentionNow,
                    lRetentionDays))
            {
                return false;
            }

            new FileStream(
                lRetentionPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose)
                .Dispose();
            return true;
        }
        catch (Exception lRetentionException) when (lRetentionException is IOException or UnauthorizedAccessException)
        {
            LTraceLog.LTraceInfoRecord("Retention sweep could not remove a file", lRetentionException.Message);
            return false;
        }
    }
}
