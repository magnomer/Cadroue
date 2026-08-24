using Cadroue.Core;

namespace Cadroue.Infrastructure;

public enum LDepotFolder
{
    LDepotFolderScheduled,
    LDepotFolderRunning,
    LDepotFolderDone,
    LDepotFolderFailed,
    LDepotFolderCancelled
}

public static class LDepot
{
    private const string LDepotFolderName = "Cadroue";
    private const string LDepotWorkFolder = "workspace";

    private const string LDepotPaletteFolder = "palettes";

    private const string LDepotAudioFolder = "audiowork";

    private const string LDepotMergeFolder = "mergework";

    private const string LDepotBridgeFolder = "bridgework";

    public const string LDepotIndexFile = "work.db";

    private static string? lDepotRootOverride;

    public static void LDepotRootSet(string? lDepotRoot)
    {
        lDepotRootOverride = string.IsNullOrWhiteSpace(lDepotRoot) ? null : lDepotRoot.Trim();
    }

    public static string LDepotRootResolve(string? lDepotRoot) =>
        string.IsNullOrWhiteSpace(lDepotRoot) ? LDepotDefaultRead() : lDepotRoot.Trim();

    public static string LDepotRootRead()
    {
        if (lDepotRootOverride is { } lDepotRoot)
        {
            return lDepotRoot;
        }

        string lDepotApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lDepotApplicationData, LDepotFolderName, LDepotWorkFolder);
    }

    public static string LDepotDefaultRead()
    {
        string lDepotApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lDepotApplicationData, LDepotFolderName, LDepotWorkFolder);
    }

    public static string LDepotIndexFind() => Path.Combine(LDepotRootRead(), LDepotIndexFile);

    public static string LDepotPaletteRead()
    {
        string lDepotPalettes = Path.Combine(LDepotRootRead(), LDepotPaletteFolder);
        Directory.CreateDirectory(lDepotPalettes);
        return lDepotPalettes;
    }

    public static string LDepotAudioRead()
    {
        string lDepotAudio = Path.Combine(LDepotRootRead(), LDepotAudioFolder);
        Directory.CreateDirectory(lDepotAudio);
        return lDepotAudio;
    }

    public static string LDepotMergeRead()
    {
        string lDepotMerge = Path.Combine(LDepotRootRead(), LDepotMergeFolder);
        Directory.CreateDirectory(lDepotMerge);
        return lDepotMerge;
    }

    public static string LDepotBridgeRead()
    {
        string lDepotBridge = Path.Combine(LDepotRootRead(), LDepotBridgeFolder);
        Directory.CreateDirectory(lDepotBridge);
        return lDepotBridge;
    }

    public static string LDepotFolderRead(LDepotFolder lDepotFolder) =>
        Path.Combine(LDepotRootRead(), LDepotNameRead(lDepotFolder));

    public static string LDepotFileRead(LDepotFolder lDepotFolder, Guid lWorkId) =>
        Path.Combine(LDepotFolderRead(lDepotFolder), $"{lWorkId:N}.json");

    public static void LDepotCreate()
    {
        Directory.CreateDirectory(LDepotRootRead());
        foreach (LDepotFolder lDepotFolder in Enum.GetValues<LDepotFolder>())
        {
            Directory.CreateDirectory(LDepotFolderRead(lDepotFolder));
        }
    }

    public static IEnumerable<string> LDepotFilesRead(LDepotFolder lDepotFolder)
    {
        string lDepotFolderPath = LDepotFolderRead(lDepotFolder);
        return Directory.Exists(lDepotFolderPath)
            ? Directory.EnumerateFiles(lDepotFolderPath, "*.json")
            : Array.Empty<string>();
    }

    public static long LDepotSizeRead()
    {
        string lDepotRoot = LDepotRootRead();
        if (!Directory.Exists(lDepotRoot))
        {
            return 0;
        }

        long lDepotTotal = 0;
        foreach (string lDepotFilePath in Directory.EnumerateFiles(lDepotRoot, "*", SearchOption.AllDirectories))
        {
            try
            {
                lDepotTotal += new FileInfo(lDepotFilePath).Length;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return lDepotTotal;
    }

    public static int LDepotFolderClear(params LDepotFolder[] lDepotFolders)
    {
        int lDepotRemoved = 0;
        foreach (LDepotFolder lDepotFolder in lDepotFolders)
        {
            foreach (string lDepotFilePath in LDepotFilesRead(lDepotFolder).ToArray())
            {
                try
                {
                    File.Delete(lDepotFilePath);
                    lDepotRemoved++;
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        return lDepotRemoved;
    }

    public static void LDepotWorkspaceReset()
    {
        string lDepotRoot = LDepotRootRead();
        if (!Directory.Exists(lDepotRoot))
        {
            return;
        }

        var lDepotRootInfo = new DirectoryInfo(lDepotRoot);
        foreach (DirectoryInfo lDepotChild in lDepotRootInfo.GetDirectories())
        {
            if (LDepotSpareCheck(lDepotChild.Name))
            {
                continue;
            }

            LDepotContentClear(lDepotChild);
        }

        foreach (FileInfo lDepotFile in lDepotRootInfo.GetFiles())
        {
            if (LDepotSpareCheck(lDepotFile.Name))
            {
                continue;
            }

            LDepotFileDelete(lDepotFile);
        }
    }

    private static bool LDepotSpareCheck(string lDepotName) =>
        string.Equals(lDepotName, LDepotPaletteFolder, StringComparison.OrdinalIgnoreCase)
        || string.Equals(lDepotName, Path.GetFileName(LMpv.LMpvRootRead()), StringComparison.OrdinalIgnoreCase)
        || string.Equals(lDepotName, Path.GetFileName(LFlyleaf.LFlyleafRootRead()), StringComparison.OrdinalIgnoreCase)
        || lDepotName.StartsWith(LDepotIndexFile, StringComparison.OrdinalIgnoreCase);

    private static void LDepotContentClear(DirectoryInfo lDepotFolder)
    {
        foreach (FileInfo lDepotFile in lDepotFolder.GetFiles())
        {
            LDepotFileDelete(lDepotFile);
        }

        foreach (DirectoryInfo lDepotChild in lDepotFolder.GetDirectories())
        {
            LDepotContentClear(lDepotChild);
            LDepotFolderDelete(lDepotChild);
        }
    }

    private static void LDepotFileDelete(FileInfo lDepotFile)
    {
        try
        {
            lDepotFile.Delete();
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static bool LDepotRunningCheck(string lDepotRoot)
    {
        string lDepotRunning = Path.Combine(lDepotRoot, LDepotNameRead(LDepotFolder.LDepotFolderRunning));
        return Directory.Exists(lDepotRunning) && Directory.EnumerateFiles(lDepotRunning).Any();
    }

    public static bool LDepotFolderMove(string lDepotPrevious, string lDepotNext)
    {
        try
        {
            if (LDepotRunningCheck(lDepotPrevious))
            {
                LTraceLog.LTraceErrorRecord($"Workspace kept at {lDepotPrevious}: a job is running, so nothing was moved");
                return false;
            }

            if (!LTraceWriter.LTraceRootMove(() =>
                {
                    LDepotIndex.LDepotIndexRelease();
                    LDepotMove(lDepotPrevious, lDepotNext);
                    LDepotRootSet(lDepotNext);
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

        if (lDepotTargetFull.StartsWith(lDepotSourceFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("A workspace cannot be moved inside itself.");
        }

        LDepotTreeMove(new DirectoryInfo(lDepotSourceFull), Directory.CreateDirectory(lDepotTargetFull));
        LDepotFolderDelete(new DirectoryInfo(lDepotSourceFull));
    }

    private static void LDepotTreeMove(DirectoryInfo lDepotSource, DirectoryInfo lDepotTarget)
    {
        foreach (FileInfo lDepotFile in lDepotSource.GetFiles())
        {
            lDepotFile.MoveTo(Path.Combine(lDepotTarget.FullName, lDepotFile.Name), true);
        }

        foreach (DirectoryInfo lDepotChild in lDepotSource.GetDirectories())
        {
            LDepotTreeMove(lDepotChild, Directory.CreateDirectory(Path.Combine(lDepotTarget.FullName, lDepotChild.Name)));
            LDepotFolderDelete(lDepotChild);
        }
    }

    private static void LDepotFolderDelete(DirectoryInfo lDepotFolder)
    {
        try
        {
            if (!lDepotFolder.EnumerateFileSystemInfos().Any())
            {
                lDepotFolder.Delete();
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string LDepotNameRead(LDepotFolder lDepotFolder) => lDepotFolder switch
    {
        LDepotFolder.LDepotFolderRunning => "running",
        LDepotFolder.LDepotFolderDone => "done",
        LDepotFolder.LDepotFolderFailed => "failed",
        LDepotFolder.LDepotFolderCancelled => "cancelled",
        _ => "scheduled"
    };
}
