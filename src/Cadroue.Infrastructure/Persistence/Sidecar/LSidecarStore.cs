using System.Security.Cryptography;
using System.Text;

using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static class LSidecarStore
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
                if (string.Equals(Path.GetExtension(lSidecarFilePath), LSidecarExtension, StringComparison.OrdinalIgnoreCase))
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
        string? lSidecarCoreJson = LSidecarFile.LSidecarFileReadText(lSidecarPreciousPath);
        if (lSidecarCoreJson is null || LSidecarParse.LSidecarCoreParse(lSidecarCoreJson) is not { } lSidecarCore)
        {
            return null;
        }

        LSidecarCacheRecord? lSidecarCache = LSidecarCacheStore.LSidecarCacheLoad(lSidecarPreciousPath, lSidecarCoreJson);
        return LSidecarParse.LSidecarCompose(lSidecarCore, lSidecarCache);
    }

    public static LSidecar? LSidecarLoad(LKeyframeSourceIdentity lSidecarIdentity)
    {
        LSidecar? lSidecar = LSidecarRead(LSidecarPathRead(lSidecarIdentity.LKeyframeSourcePath));
        return lSidecar is not null && lSidecar.LSidecarSourceMatch(lSidecarIdentity) ? lSidecar : null;
    }

    public static bool LSidecarSectionsSave(string lSidecarSourcePath, IReadOnlyList<LSidecarSectionRecord> lSidecarSections) =>
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
                string? lSidecarExistingJson = LSidecarFile.LSidecarFileReadText(lSidecarPreciousPath);
                LSidecarCacheStore.LSidecarCacheMigrate(lSidecarPreciousPath, lSidecarExistingJson);

                LSidecarCoreRecord lSidecarCore = lSidecarExistingJson is not null
                    && LSidecarParse.LSidecarCoreParse(lSidecarExistingJson) is { } lSidecarParsed
                        ? lSidecarParsed
                        : new LSidecarCoreRecord();
                lSidecarCore.LSidecarVersion = 2;
                lSidecarCore.LSidecarSource = LSidecarSourceCreate(lSidecarIdentity, lSidecarPreciousPath);

                lSidecarCoreSaved = LSidecarFile.LSidecarFileSave(lSidecarPreciousPath, LSidecarParse.LSidecarCoreFormat(lSidecarCore));
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

    public static LSidecarEditRecord? LSidecarEditRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCoreRead(lSidecarSourcePath)?.LSidecarEdit;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    public static bool LSidecarEditSave(string lSidecarSourcePath, LSidecarEditRecord? lSidecarEdit) =>
        LSidecarCoreSave(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarEdit = lSidecarEdit);

    public static LSidecarAudioRecord? LSidecarAudioRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCoreRead(lSidecarSourcePath)?.LSidecarAudio;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    public static bool LSidecarAudioSave(string lSidecarSourcePath, LSidecarAudioRecord? lSidecarAudio) =>
        LSidecarCoreSave(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarAudio = lSidecarAudio);

    public static LSidecarWaveformRecord? LSidecarWaveformRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCacheStore.LSidecarCacheRead(lSidecarSourcePath)?.LSidecarWaveform;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    public static bool LSidecarWaveformSave(string lSidecarSourcePath, LSidecarWaveformRecord? lSidecarWaveform) =>
        LSidecarCacheStore.LSidecarCacheMutate(lSidecarSourcePath, lSidecarCache => lSidecarCache.LSidecarWaveform = lSidecarWaveform);

    public static double LSidecarLoudnessRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCoreRead(lSidecarSourcePath)?.LSidecarLoudness ?? 0;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return 0;
        }
    }

    public static bool LSidecarLoudnessSave(string lSidecarSourcePath, double lSidecarLoudness) =>
        LSidecarCoreSave(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarLoudness = lSidecarLoudness);

    public static TimeSpan LSidecarDurationRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCoreRead(lSidecarSourcePath) is { LSidecarSource.LSidecarDurationMilliseconds: > 0 } lSidecarCore
                && LSidecarSource.LSidecarVerifyCheck(lSidecarSourcePath, lSidecarCore.LSidecarSource)
                ? TimeSpan.FromMilliseconds(lSidecarCore.LSidecarSource.LSidecarDurationMilliseconds)
                : TimeSpan.Zero;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return TimeSpan.Zero;
        }
    }

    public static TimeSpan LSidecarDurationResolve(string lSidecarSourcePath)
    {
        try
        {
            string lSidecarPreciousPath = LSidecarPathRead(lSidecarSourcePath);
            LSidecarCoreRecord? lSidecarCore;
            using (LLatch.LLatchClaim(lSidecarPreciousPath))
            {
                lSidecarCore = LSidecarCoreRead(lSidecarSourcePath);
            }

            if (lSidecarCore is { LSidecarSource.LSidecarDurationMilliseconds: > 0 } lSidecarKnown
                && LSidecarSource.LSidecarVerifyCheck(lSidecarSourcePath, lSidecarKnown.LSidecarSource))
            {
                return TimeSpan.FromMilliseconds(lSidecarKnown.LSidecarSource.LSidecarDurationMilliseconds);
            }

            TimeSpan lSidecarProbed;
            try
            {
                lSidecarProbed = LMedia.LMediaFfprobeRead(lSidecarSourcePath).LMediaInfoDuration;
            }
            catch (Exception)
            {
                return TimeSpan.Zero;
            }

            if (lSidecarProbed <= TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            LSidecarCoreSave(lSidecarSourcePath, lSidecarTarget =>
            {
                try
                {
                    lSidecarTarget.LSidecarSource = LSidecarSourceCreate(
                        LKeyframeSourceIdentity.LKeyframeIdentityCreate(lSidecarSourcePath, lSidecarProbed),
                        lSidecarPreciousPath);
                }
                catch (Exception lSidecarException) when (
                    lSidecarException is IOException or UnauthorizedAccessException or ArgumentException or FileNotFoundException)
                {
                    lSidecarTarget.LSidecarSource.LSidecarDurationMilliseconds = (long)Math.Round(lSidecarProbed.TotalMilliseconds);
                }
            });

            return lSidecarProbed;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException or TimeoutException)
        {
            return TimeSpan.Zero;
        }
    }

    public static LSidecarCoreRecord? LSidecarCoreRead(string lSidecarSourcePath)
    {
        string? lSidecarJson = LSidecarFile.LSidecarFileReadText(LSidecarPathRead(lSidecarSourcePath));
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
                string? lSidecarExistingJson = LSidecarFile.LSidecarFileReadText(lSidecarPreciousPath);
                LSidecarCacheStore.LSidecarCacheMigrate(lSidecarPreciousPath, lSidecarExistingJson);

                LSidecarCoreRecord lSidecarCore = lSidecarExistingJson is not null
                    && LSidecarParse.LSidecarCoreParse(lSidecarExistingJson) is { } lSidecarParsed
                        ? lSidecarParsed
                        : LSidecarStubCreate(lSidecarSourcePath, lSidecarPreciousPath);
                lSidecarMutate(lSidecarCore);
                return LSidecarFile.LSidecarFileSave(lSidecarPreciousPath, LSidecarParse.LSidecarCoreFormat(lSidecarCore));
            }
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException or TimeoutException)
        {
            return false;
        }
    }

    private static LSidecarSourceRecord LSidecarSourceCreate(LKeyframeSourceIdentity lSidecarIdentity, string lSidecarPreciousPath)
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
