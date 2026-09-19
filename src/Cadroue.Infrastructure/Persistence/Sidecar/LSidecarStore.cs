using System.Security.Cryptography;
using System.Text;

using Cadroue.Application;
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

    public static void LSidecarFolderApply(bool lSidecarActive) =>
        LSidecarFolderSet(Path.Combine(LDepot.LDepotRootRead(), LSidecarRecordFolder), lSidecarActive);

    public static bool LSidecarFolderCheck() => lSidecarRecordActive;

    public static string LSidecarPathRead(string lSidecarSourcePath)
    {
        if (lSidecarRecordActive && lSidecarRecordFolder is { } lSidecarFolder)
        {
            return Path.Combine(lSidecarFolder, LSidecarKeyCreate(lSidecarSourcePath) + LSidecarExtension);
        }

        string lSidecarFullPath = Path.GetFullPath(lSidecarSourcePath);
        string lSidecarPreciousPath = lSidecarFullPath + LSidecarExtension;
        LSidecarLegacyMove(lSidecarFullPath, lSidecarPreciousPath);
        return lSidecarPreciousPath;
    }

    internal static string LSidecarCacheResolve(string lSidecarPreciousPath) =>
        Path.ChangeExtension(lSidecarPreciousPath, LSidecarCacheExtension);

    private static string LSidecarKeyCreate(string lSidecarSourcePath)
    {
        string lSidecarFullPath = Path.GetFullPath(lSidecarSourcePath).ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(lSidecarFullPath)));
    }

    private static void LSidecarLegacyMove(string lSidecarFullPath, string lSidecarPreciousPath)
    {
        string lSidecarLegacyPath = Path.ChangeExtension(lSidecarFullPath, LSidecarExtension);
        if (string.Equals(lSidecarLegacyPath, lSidecarPreciousPath, StringComparison.OrdinalIgnoreCase)
            || File.Exists(lSidecarPreciousPath)
            || !File.Exists(lSidecarLegacyPath))
        {
            return;
        }

        try
        {
            using (LLatch.LLatchClaim(lSidecarLegacyPath))
            {
                if (File.Exists(lSidecarPreciousPath) || !File.Exists(lSidecarLegacyPath))
                {
                    return;
                }

                string? lSidecarLegacyJson = LSidecarFile.LSidecarFileRead(lSidecarLegacyPath);
                if (lSidecarLegacyJson is null
                    || LSidecarParse.LSidecarCoreParse(lSidecarLegacyJson) is not { } lSidecarLegacy
                    || !string.Equals(
                        lSidecarLegacy.LSidecarSource.LSidecarFileName,
                        Path.GetFileName(lSidecarFullPath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string lSidecarLegacyCache = LSidecarCacheResolve(lSidecarLegacyPath);
                if (File.Exists(lSidecarLegacyCache))
                {
                    File.Move(lSidecarLegacyCache, LSidecarCacheResolve(lSidecarPreciousPath), overwrite: false);
                }

                File.Move(lSidecarLegacyPath, lSidecarPreciousPath, overwrite: false);
            }
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or TimeoutException)
        {
        }
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
        string lSidecarSourcePath = lSidecarIdentity.LKeyframeSourcePath;
        bool lSidecarCoreSaved = LSidecarCoreSave(
            lSidecarSourcePath,
            lSidecarCore =>
            {
                lSidecarCore.LSidecarVersion = 2;
                lSidecarCore.LSidecarSource =
                    LSidecarSourceCreate(
                        lSidecarIdentity,
                        LSidecarPathRead(lSidecarSourcePath));
            });

        bool lSidecarCacheSaved = LSidecarCacheStore.LSidecarCacheSave(
            lSidecarIdentity,
            LSidecarPathRead(lSidecarSourcePath),
            lSidecarKeyframeMilliseconds,
            lSidecarScannedSpans,
            lSidecarSpanGridMilliseconds);

        return lSidecarCoreSaved && lSidecarCacheSaved;
    }

    public static LSidecarCoreRecord? LSidecarCoreRead(string lSidecarSourcePath)
    {
        LSidecarCoreResult lSidecarResult =
            LSidecarCoreResolve(lSidecarSourcePath, LSidecarPathRead(lSidecarSourcePath));
        return lSidecarResult.LSidecarCoreState == LSidecarReadKind.LSidecarReadMatched
            ? lSidecarResult.LSidecarCoreValue
            : null;
    }

    public static IReadOnlyList<long> LSidecarKeyframesRead(string lSidecarSourcePath) =>
        LSidecarRead(LSidecarPathRead(lSidecarSourcePath)) is { } lSidecar
        && LSidecarSource.LSidecarSourceMatch(lSidecarSourcePath, lSidecar.LSidecarSource)
            ? lSidecar.LSidecarKeyframesRead()
            : Array.Empty<long>();

    internal static LSidecarCoreResult LSidecarCoreResolve(string lSidecarSourcePath, string lSidecarPreciousPath)
    {
        if (!File.Exists(lSidecarPreciousPath))
        {
            return new LSidecarCoreResult(LSidecarReadKind.LSidecarReadMissing, null, null);
        }

        string? lSidecarJson = LSidecarFile.LSidecarFileRead(lSidecarPreciousPath);
        if (lSidecarJson is null)
        {
            return new LSidecarCoreResult(LSidecarReadKind.LSidecarReadUnreadable, null, null);
        }

        if (LSidecarParse.LSidecarCoreParse(lSidecarJson) is not { } lSidecarCore)
        {
            return new LSidecarCoreResult(LSidecarReadKind.LSidecarReadMalformed, null, lSidecarJson);
        }

        return LSidecarSource.LSidecarSourceMatch(lSidecarSourcePath, lSidecarCore.LSidecarSource)
            ? new LSidecarCoreResult(LSidecarReadKind.LSidecarReadMatched, lSidecarCore, lSidecarJson)
            : new LSidecarCoreResult(LSidecarReadKind.LSidecarReadForeign, lSidecarCore, lSidecarJson);
    }

    private static bool LSidecarCoreSave(string lSidecarSourcePath, Action<LSidecarCoreRecord> lSidecarMutate)
    {
        try
        {
            string lSidecarFullPath = Path.GetFullPath(lSidecarSourcePath);
            if (!File.Exists(lSidecarFullPath))
            {
                return false;
            }

            string lSidecarPreciousPath = LSidecarPathRead(lSidecarFullPath);
            using (LLatch.LLatchClaim(lSidecarPreciousPath))
            {
                LSidecarCoreResult lSidecarResult = LSidecarCoreResolve(lSidecarFullPath, lSidecarPreciousPath);
                switch (lSidecarResult.LSidecarCoreState)
                {
                    case LSidecarReadKind.LSidecarReadUnreadable:
                        return false;
                    case LSidecarReadKind.LSidecarReadMalformed
                        when !LSidecarFile.LSidecarBrokenMove(lSidecarPreciousPath):
                        return false;
                }

                bool lSidecarMatched = lSidecarResult.LSidecarCoreState == LSidecarReadKind.LSidecarReadMatched;
                string? lSidecarExistingJson = lSidecarMatched ? lSidecarResult.LSidecarCoreJson : null;
                LSidecarCacheStore.LSidecarCacheMove(lSidecarPreciousPath, lSidecarExistingJson);

                LSidecarCoreRecord lSidecarCore = lSidecarMatched
                    ? lSidecarResult.LSidecarCoreValue!
                    : new LSidecarCoreRecord();
                if (string.IsNullOrWhiteSpace(lSidecarCore.LSidecarSource.LSidecarPartialHash))
                {
                    lSidecarCore.LSidecarSource = LSidecarSourceCreate(
                        LKeyframeSourceIdentity.LKeyframeIdentityCreate(
                            lSidecarFullPath,
                            TimeSpan.FromMilliseconds(lSidecarCore.LSidecarSource.LSidecarDurationMilliseconds)),
                        lSidecarPreciousPath);
                }

                lSidecarMutate(lSidecarCore);
                return LSidecarFile.LSidecarFileSave(
                    lSidecarPreciousPath,
                    LSidecarParse.LSidecarCoreFormat(lSidecarCore, lSidecarExistingJson));
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

    public static void LSidecarLibrarianAttach()
    {
        LLibrarian.LLibrarianCoreReader = LSidecarCoreRead;
        LLibrarian.LLibrarianKeyframesSeam = LSidecarKeyframesRead;
        LLibrarian.LLibrarianWaveformReader = LSidecarWaveformRead;
        LLibrarian.LLibrarianEditReader = LSidecarEditRead;
        LLibrarian.LLibrarianAudioReader = LSidecarAudioRead;
        LLibrarian.LLibrarianSplitReader = LSidecarSplitRead;
        LLibrarian.LLibrarianFixReader = LSidecarFixRead;
        LLibrarian.LLibrarianDiagnosisReader = LSidecarDiagnosisRead;
        LLibrarian.LLibrarianLoudnessReader = LSidecarLoudnessRead;
        LLibrarian.LLibrarianDurationReader = LSidecarDurationRead;
        LLibrarian.LLibrarianDurationResolver = LSidecarDurationResolve;
        LLibrarian.LLibrarianEditWriter = LSidecarEditSave;
        LLibrarian.LLibrarianAudioWriter = LSidecarAudioSave;
        LLibrarian.LLibrarianSplitWriter = LSidecarSplitSave;
        LLibrarian.LLibrarianFixWriter = LSidecarFixSave;
        LLibrarian.LLibrarianDiagnosisWriter = LSidecarDiagnosisSave;
        LLibrarian.LLibrarianLoudnessWriter = LSidecarLoudnessSave;
        LLibrarian.LLibrarianWaveformWriter = LSidecarWaveformSave;
        LLibrarian.LLibrarianFileChecker = LSidecarFileCheck;
        LLibrarian.LLibrarianSourceResolver = LSidecarSourceResolve;
        LLibrarian.LLibrarianSourceMatcher = LSidecarSourceMatch;
    }

    public static void LSidecarSegmentAttach()
    {
        LSegment.LSegmentLoadSeam = LSidecarSectionsRead;
        LSegment.LSegmentSaveSeam = LSidecarSectionsSave;
    }

    public static LSidecarSourceResult? LSidecarSourceResolve(string lSidecarPath) =>
        LSidecarRead(lSidecarPath) is { } lSidecar
            ? LSidecarSource.LSidecarSourceResolve(lSidecarPath, lSidecar)
            : null;

    public static bool LSidecarSourceMatch(string lSidecarMediaPath, string lSidecarPath) =>
        LSidecarRead(lSidecarPath) is { } lSidecar
        && LSidecarSource.LSidecarSourceMatch(lSidecarMediaPath, lSidecar.LSidecarSource);

    public static IReadOnlyList<LSidecarSectionRecord> LSidecarSectionsRead(string lSidecarSourcePath)
    {
        try
        {
            if (LLibrarian.LLibrarianLoad(lSidecarSourcePath) is { } lSidecarCore)
            {
                return lSidecarCore.LSidecarSections;
            }
        }
        catch (Exception lSidecarException)
        {
            LTraceLog.LTraceErrorRecord("Sidecar sections could not be restored", lSidecarException);
        }

        return Array.Empty<LSidecarSectionRecord>();
    }
}
