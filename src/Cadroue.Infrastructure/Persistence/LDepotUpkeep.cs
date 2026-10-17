namespace Cadroue.Infrastructure;

public static class LDepotUpkeep
{
    public static long LDepotSizeRead()
    {
        string lDepotRoot = LDepot.LDepotRootRead();
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
            foreach (string lDepotFilePath in LDepot.LDepotFilesRead(lDepotFolder).ToArray())
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
        string lDepotRoot = LDepot.LDepotRootRead();
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

    private static bool LDepotSpareCheck(string lDepotName)
    {
        if (string.Equals(lDepotName, LDepot.LDepotPaletteFolder, StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                lDepotName,
                Path.GetFileName(LMpvInstall.LMpvRootRead()),
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string lDepotFlyleafName = Path.GetFileName(LFlyleafFolder.LFlyleafRootRead());
        return string.Equals(lDepotName, lDepotFlyleafName, StringComparison.OrdinalIgnoreCase)
            || lDepotName.StartsWith(LDepot.LDepotIndexFile, StringComparison.OrdinalIgnoreCase);
    }

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
}
