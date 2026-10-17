using System.Security.Cryptography;
using System.Text;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LSidecarPath
{
    public const string LSidecarExtension = ".cad";
    public const string LSidecarCacheExtension = ".cadcache";
    public const string LSidecarRecordFolder = "filerecord";

    private static string? lSidecarRecordFolder;
    private static bool lSidecarRecordActive;

    public static void LSidecarFolderSet(string? lSidecarFolder, bool lSidecarActive)
    {
        lSidecarRecordFolder = string.IsNullOrWhiteSpace(lSidecarFolder) ? null : lSidecarFolder.Trim();
        lSidecarRecordActive = lSidecarActive && lSidecarRecordFolder is not null;
    }

    public static string LSidecarFolderRead() => lSidecarRecordFolder ?? string.Empty;

    public static string LSidecarFolderResolve() =>
        Path.Combine(LDepot.LDepotRootRead(), LSidecarRecordFolder);

    public static void LSidecarFolderApply(bool lSidecarActive) =>
        LSidecarFolderSet(LSidecarFolderResolve(), lSidecarActive);

    public static bool LSidecarFolderCheck() => lSidecarRecordActive;

    public static string LSidecarPathRead(string lSidecarSourcePath)
    {
        if (lSidecarRecordActive && lSidecarRecordFolder is { } lSidecarFolder)
        {
            return Path.Combine(lSidecarFolder, LSidecarKeyCreate(lSidecarSourcePath) + LSidecarExtension);
        }

        string lSidecarFullPath = Path.GetFullPath(lSidecarSourcePath);
        string lSidecarPreciousPath = lSidecarFullPath + LSidecarExtension;
        LSidecarLegacy.LSidecarLegacyMove(lSidecarFullPath, lSidecarPreciousPath);
        return lSidecarPreciousPath;
    }

    internal static string LSidecarCacheResolve(string lSidecarPreciousPath) =>
        Path.ChangeExtension(lSidecarPreciousPath, LSidecarCacheExtension);

    private static string LSidecarKeyCreate(string lSidecarSourcePath)
    {
        string lSidecarFullPath = Path.GetFullPath(lSidecarSourcePath).ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(lSidecarFullPath)));
    }

    public static int LSidecarFolderClear()
    {
        if (lSidecarRecordFolder is not { } lSidecarFolder || !Directory.Exists(lSidecarFolder))
        {
            return 0;
        }

        int lSidecarRemoved = 0;
        foreach (string lSidecarFilePath in Directory
                     .EnumerateFiles(lSidecarFolder, "*" + LSidecarExtension)
                     .Concat(Directory.EnumerateFiles(lSidecarFolder, "*" + LSidecarCacheExtension))
                     .ToArray())
        {
            try
            {
                File.Delete(lSidecarFilePath);
                if (string.Equals(
                    Path.GetExtension(lSidecarFilePath),
                    LSidecarExtension,
                    StringComparison.OrdinalIgnoreCase))
                {
                    lSidecarRemoved++;
                }
            }
            catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
            {
            }
        }

        return lSidecarRemoved;
    }

    public static bool LSidecarFileCheck(string lSidecarPath) =>
        string.Equals(Path.GetExtension(lSidecarPath), LSidecarExtension, StringComparison.OrdinalIgnoreCase);
}
