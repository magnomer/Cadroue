using System.Security.Cryptography;
using System.Text;

using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static partial class LSidecarStore
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

    public static bool LSidecarFolderCheck() => lSidecarRecordActive;

    public static string LSidecarPathRead(string lSidecarSourcePath) =>
        lSidecarRecordActive && lSidecarRecordFolder is { } lSidecarFolder
            ? Path.Combine(lSidecarFolder, LSidecarKeyCreate(lSidecarSourcePath) + LSidecarExtension)
            : Path.ChangeExtension(Path.GetFullPath(lSidecarSourcePath), LSidecarExtension);

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

    public static LSidecar? LSidecarRead(string lSidecarPreciousPath)
    {
        string? lSidecarCoreJson = LSidecarFile.LSidecarFileRead(lSidecarPreciousPath);
        if (lSidecarCoreJson is null || LSidecarParse.LSidecarCoreParse(lSidecarCoreJson) is not { } lSidecarCore)
        {
            return null;
        }

        LSidecarCacheRecord? lSidecarCache = LSidecarCacheStore.LSidecarCacheLoad(
            lSidecarPreciousPath,
            lSidecarCoreJson);
        return LSidecarParse.LSidecarCreate(lSidecarCore, lSidecarCache);
    }

    public static LSidecar? LSidecarLoad(LKeyframeSourceIdentity lSidecarIdentity)
    {
        LSidecar? lSidecar = LSidecarRead(LSidecarPathRead(lSidecarIdentity.LKeyframeSourcePath));
        return lSidecar is not null && lSidecar.LSidecarSourceMatch(lSidecarIdentity) ? lSidecar : null;
    }

    public static bool LSidecarSectionsSave(
        string lSidecarSourcePath,
        IReadOnlyList<LSidecarSectionRecord> lSidecarSections) =>
        LSidecarCoreSave(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarSections = lSidecarSections.ToList());

    public static bool LSidecarSave(
        LKeyframeSourceIdentity lSidecarIdentity,
        IReadOnlyCollection<long> lSidecarKeyframeMilliseconds,
        IReadOnlyCollection<int> lSidecarScannedSpans,
        int lSidecarSpanGridMilliseconds)
    {
        string lSidecarPreciousPath = LSidecarPathRead(lSidecarIdentity.LKeyframeSourcePath);
        bool lSidecarCoreSaved;
        try
        {
            using (LLatch.LLatchClaim(lSidecarPreciousPath))
            {
                string? lSidecarExistingJson = LSidecarFile.LSidecarFileRead(lSidecarPreciousPath);
                LSidecarCacheStore.LSidecarCacheMove(lSidecarPreciousPath, lSidecarExistingJson);

                LSidecarCoreRecord lSidecarCore = lSidecarExistingJson is not null
                    && LSidecarParse.LSidecarCoreParse(lSidecarExistingJson) is { } lSidecarParsed
                        ? lSidecarParsed
                        : new LSidecarCoreRecord();
                lSidecarCore.LSidecarVersion = 2;
                lSidecarCore.LSidecarSource = LSidecarSourceCreate(lSidecarIdentity, lSidecarPreciousPath);

                lSidecarCoreSaved = LSidecarFile.LSidecarFileSave(
                    lSidecarPreciousPath,
                    LSidecarParse.LSidecarCoreFormat(lSidecarCore));
            }
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or TimeoutException)
        {
            return false;
        }

        bool lSidecarCacheSaved = LSidecarCacheStore.LSidecarCacheSave(
            lSidecarIdentity,
            lSidecarPreciousPath,
            lSidecarKeyframeMilliseconds,
            lSidecarScannedSpans,
            lSidecarSpanGridMilliseconds);

        return lSidecarCoreSaved && lSidecarCacheSaved;
    }

    public static LSidecarCoreRecord? LSidecarCoreRead(string lSidecarSourcePath)
    {
        string? lSidecarJson = LSidecarFile.LSidecarFileRead(LSidecarPathRead(lSidecarSourcePath));
        return lSidecarJson is null ? null : LSidecarParse.LSidecarCoreParse(lSidecarJson);
    }

    public static IReadOnlyList<long> LSidecarKeyframesRead(string lSidecarSourcePath) =>
        LSidecarRead(LSidecarPathRead(lSidecarSourcePath))?.LSidecarKeyframesRead() ?? Array.Empty<long>();

    private static bool LSidecarCoreSave(string lSidecarSourcePath, Action<LSidecarCoreRecord> lSidecarMutate)
    {
        try
        {
            string lSidecarPreciousPath = LSidecarPathRead(lSidecarSourcePath);
            using (LLatch.LLatchClaim(lSidecarPreciousPath))
            {
                string? lSidecarExistingJson = LSidecarFile.LSidecarFileRead(lSidecarPreciousPath);
                LSidecarCacheStore.LSidecarCacheMove(lSidecarPreciousPath, lSidecarExistingJson);

                LSidecarCoreRecord lSidecarCore = lSidecarExistingJson is not null
                    && LSidecarParse.LSidecarCoreParse(lSidecarExistingJson) is { } lSidecarParsed
                        ? lSidecarParsed
                        : LSidecarStubCreate(lSidecarSourcePath, lSidecarPreciousPath);
                lSidecarMutate(lSidecarCore);
                return LSidecarFile.LSidecarFileSave(
                    lSidecarPreciousPath,
                    LSidecarParse.LSidecarCoreFormat(lSidecarCore));
            }
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException
                or TimeoutException)
        {
            return false;
        }
    }

    private static LSidecarSourceRecord LSidecarSourceCreate(
        LKeyframeSourceIdentity lSidecarIdentity,
        string lSidecarPreciousPath)
    {
        string lSidecarFolder = Path.GetDirectoryName(Path.GetFullPath(lSidecarPreciousPath)) ?? string.Empty;
        string lSidecarSourcePath = lSidecarIdentity.LKeyframeSourcePath;
        return new LSidecarSourceRecord
        {
            LSidecarFileName = Path.GetFileName(lSidecarSourcePath),
            LSidecarRelativePath = LSidecarRelativeCreate(lSidecarFolder, lSidecarSourcePath),
            LSidecarAbsolutePath = lSidecarSourcePath,
            LSidecarLength = lSidecarIdentity.LKeyframeSourceLength,
            LSidecarWriteTicks = lSidecarIdentity.LKeyframeWriteTicks,
            LSidecarDurationMilliseconds = lSidecarIdentity.LKeyframeSourceDuration,
            LSidecarPartialHash = lSidecarIdentity.LKeyframePartialHash
        };
    }

    private static LSidecarCoreRecord LSidecarStubCreate(string lSidecarSourcePath, string lSidecarPreciousPath)
    {
        string lSidecarFullPath = Path.GetFullPath(lSidecarSourcePath);
        var lSidecarFile = new FileInfo(lSidecarFullPath);
        string lSidecarFolder = Path.GetDirectoryName(Path.GetFullPath(lSidecarPreciousPath)) ?? string.Empty;

        return new LSidecarCoreRecord
        {
            LSidecarSource = new LSidecarSourceRecord
            {
                LSidecarFileName = Path.GetFileName(lSidecarFullPath),
                LSidecarRelativePath = string.IsNullOrWhiteSpace(lSidecarFolder)
                    ? string.Empty
                    : LSidecarRelativeCreate(lSidecarFolder, lSidecarFullPath),
                LSidecarAbsolutePath = lSidecarFullPath,
                LSidecarLength = lSidecarFile.Exists ? lSidecarFile.Length : 0,
                LSidecarWriteTicks = lSidecarFile.Exists ? lSidecarFile.LastWriteTimeUtc.Ticks : 0
            }
        };
    }

    private static string LSidecarRelativeCreate(string lSidecarFolder, string lSidecarSourcePath)
    {
        if (string.IsNullOrWhiteSpace(lSidecarFolder))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetRelativePath(lSidecarFolder, lSidecarSourcePath);
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
    }
}
