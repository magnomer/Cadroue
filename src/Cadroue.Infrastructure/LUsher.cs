using System.Diagnostics;

namespace Cadroue.Infrastructure;

public static class LUsher
{
    public static bool LUsherFileExist(string? lUsherPath) =>
        !string.IsNullOrWhiteSpace(lUsherPath) && File.Exists(lUsherPath);

    public static bool LUsherFolderExist(string? lUsherFolder) =>
        !string.IsNullOrWhiteSpace(lUsherFolder) && Directory.Exists(lUsherFolder);

    public static string LUsherNameRead(string lUsherPath) => Path.GetFileName(lUsherPath);

    public static string LUsherStemRead(string lUsherPath) => Path.GetFileNameWithoutExtension(lUsherPath);

    public static string LUsherExtensionRead(string lUsherPath) => Path.GetExtension(lUsherPath).TrimStart('.');

    public static string LUsherPathResolve(string lUsherPath)
    {
        try
        {
            return Path.GetFullPath(lUsherPath);
        }
        catch (Exception lPathError) when (
            lPathError is ArgumentException or IOException or NotSupportedException)
        {
            return lUsherPath;
        }
    }

    public static string? LUsherPathOpen(string lUsherPath, string? lUsherFallback = null)
    {
        try
        {
            if (File.Exists(lUsherPath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{lUsherPath}\"")
                {
                    UseShellExecute = true
                });
                return null;
            }

            string? lUsherFolder = lUsherFallback ?? Path.GetDirectoryName(lUsherPath);
            if (string.IsNullOrWhiteSpace(lUsherFolder))
            {
                return null;
            }

            if (lUsherFallback is not null)
            {
                Directory.CreateDirectory(lUsherFolder);
            }
            else if (!Directory.Exists(lUsherFolder))
            {
                return null;
            }

            Process.Start(new ProcessStartInfo(lUsherFolder) { UseShellExecute = true });
            return null;
        }
        catch (Exception lUsherException)
        {
            return lUsherException.Message;
        }
    }

    public static string? LUsherFolderOpen(string lUsherFolder)
    {
        if (!LUsherFolderExist(lUsherFolder))
        {
            return null;
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{lUsherFolder}\"") { UseShellExecute = true });
            return null;
        }
        catch (Exception lUsherException)
        {
            return lUsherException.Message;
        }
    }

    public static string? LUsherLinkOpen(string lUsherTarget)
    {
        try
        {
            Process.Start(new ProcessStartInfo(lUsherTarget) { UseShellExecute = true });
            return null;
        }
        catch (Exception lUsherException)
        {
            return lUsherException.Message;
        }
    }
}
