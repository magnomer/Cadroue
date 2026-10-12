using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LDepotRelocation
{
    public static bool LDepotRunningCheck(string lDepotRoot)
    {
        string lDepotRunning = LDepot.LDepotFolderRead(LDepotFolder.LDepotFolderRunning, lDepotRoot);
        return Directory.Exists(lDepotRunning) && Directory.EnumerateFiles(lDepotRunning).Any();
    }

    public static bool LDepotFolderMove(string lDepotPrevious, string lDepotNext)
    {
        try
        {
            if (LDepotRunningCheck(lDepotPrevious))
            {
                LTraceLog.LTraceErrorRecord(
                    $"Workspace kept at {lDepotPrevious}: a job is running, so nothing was moved");
                return false;
            }

            if (LDepotOccupiedCheck(lDepotNext))
            {
                LTraceLog.LTraceErrorRecord(
                    $"Workspace kept at {lDepotPrevious}: {lDepotNext} already contains files, so nothing was moved");
                return false;
            }

            if (!LTraceWriter.LTraceRootMove(() =>
                {
                    LDepotIndex.LDepotIndexRelease();
                    LDepotMove(lDepotPrevious, lDepotNext);
                    LDepot.LDepotRootSet(lDepotNext);
                }))
            {
                LTraceLog.LTraceErrorRecord(
                    $"Workspace kept at {lDepotPrevious}: pending log entries could not be saved");
                return false;
            }

            LTraceLog.LTraceInfoRecord($"Workspace moved from {lDepotPrevious}");
            return true;
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord($"Workspace kept at {lDepotPrevious}: the move failed", lException);
            return false;
        }
    }

    public static bool LDepotOccupiedCheck(string lDepotRoot) =>
        Directory.Exists(lDepotRoot) && Directory.EnumerateFileSystemEntries(lDepotRoot).Any();

    public static void LDepotMove(string lDepotSource, string lDepotTarget)
    {
        if (!Directory.Exists(lDepotSource))
        {
            return;
        }

        string lDepotSourceFull = Path.GetFullPath(lDepotSource);
        string lDepotTargetFull = Path.GetFullPath(lDepotTarget);
        if (string.Equals(lDepotSourceFull, lDepotTargetFull, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (lDepotTargetFull.StartsWith(
                lDepotSourceFull + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("A workspace cannot be moved inside itself.");
        }

        if (LDepotOccupiedCheck(lDepotTargetFull))
        {
            throw new IOException("A workspace cannot be moved onto a folder that already contains files.");
        }

        if (Path.GetDirectoryName(lDepotTargetFull) is { } lDepotParent)
        {
            Directory.CreateDirectory(lDepotParent);
        }

        if (Directory.Exists(lDepotTargetFull))
        {
            Directory.Delete(lDepotTargetFull);
        }

        try
        {
            Directory.Move(lDepotSourceFull, lDepotTargetFull);
            return;
        }
        catch (IOException)
        {
        }

        try
        {
            LDepotTreeCopy(new DirectoryInfo(lDepotSourceFull), Directory.CreateDirectory(lDepotTargetFull));
        }
        catch
        {
            Directory.Delete(lDepotTargetFull, recursive: true);
            throw;
        }

        try
        {
            Directory.Delete(lDepotSourceFull, recursive: true);
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
            LTraceLog.LTraceWarningRecord(
                $"Workspace copied to {lDepotTargetFull}, but files still in use remain at {lDepotSourceFull}");
        }
    }

    private static void LDepotTreeCopy(DirectoryInfo lDepotSource, DirectoryInfo lDepotTarget)
    {
        foreach (FileInfo lDepotFile in lDepotSource.GetFiles())
        {
            lDepotFile.CopyTo(Path.Combine(lDepotTarget.FullName, lDepotFile.Name), overwrite: false);
        }

        foreach (DirectoryInfo lDepotChild in lDepotSource.GetDirectories())
        {
            LDepotTreeCopy(
                lDepotChild,
                Directory.CreateDirectory(Path.Combine(lDepotTarget.FullName, lDepotChild.Name)));
        }
    }
}
