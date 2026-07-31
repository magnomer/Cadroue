namespace Cadroue.Core;

public enum LDepotFolder
{
    LDepotFolderScheduled,
    LDepotFolderRunning,
    LDepotFolderDone,
    LDepotFolderFailed
}

public static class LDepot
{
    private const string LDepotFolderName = "Cadroue";
    private const string LDepotWorkFolder = "workspace";

    private const string LDepotPaletteFolder = "palettes";

    private const string LDepotAudioFolder = "audiowork";

    private const string LDepotMergeFolder = "mergework";

    public const string LDepotIndexFile = "work.db";

    private static string? lDepotRootOverride;

    public static void LDepotRootSet(string? lDepotRoot)
    {
        lDepotRootOverride = string.IsNullOrWhiteSpace(lDepotRoot) ? null : lDepotRoot.Trim();
    }

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

    public static bool LDepotRunningCheck(string lDepotRoot)
    {
        string lDepotRunning = Path.Combine(lDepotRoot, LDepotNameRead(LDepotFolder.LDepotFolderRunning));
        return Directory.Exists(lDepotRunning) && Directory.EnumerateFiles(lDepotRunning).Any();
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
        _ => "scheduled"
    };
}
