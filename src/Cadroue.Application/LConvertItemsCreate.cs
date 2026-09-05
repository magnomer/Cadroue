using System.IO;
using Cadroue.Core;

namespace Cadroue.Application;

public static partial class LConvert
{
    public static IReadOnlyList<LWorkItem> LConvertItemsCreate(
        LWorkPriority lWorkPriority,
        LConvertWorkDescription lConvertWorkDescription,
        string lConvertTab,
        Action<string> lErrorLog,
        Func<string, TimeSpan> lDurationRead)
    {
        IReadOnlyList<string> lConvertSourcePaths = lConvertWorkDescription.LConvertSourcePaths;
        if (lConvertSourcePaths.Count == 0)
        {
            lErrorLog("Convert not queued: the file list is empty");
            return Array.Empty<LWorkItem>();
        }

        LEncoding lConvertOutput = lConvertWorkDescription.LConvertOutput;
        var lConvertWorkItems = new List<LWorkItem>();
        var lConvertTakenPaths = new HashSet<string>(StringComparer.Ordinal);
        DateTimeOffset lConvertStamp = DateTimeOffset.Now;
        Guid lConvertLooseBatch = LGate.LGateBatchCreate();

        foreach (string lConvertSourcePath in lConvertSourcePaths)
        {
            Guid lConvertBatch = lConvertWorkDescription.LConvertRelays is { } lConvertRelayMap
                && lConvertRelayMap.TryGetValue(lConvertSourcePath, out Guid lConvertRelay)
                && lConvertRelay != Guid.Empty
                ? lConvertRelay
                : lConvertLooseBatch;

            LWorkMedia? lConvertMedia = null;
            if (lConvertWorkDescription.LConvertMedia is { } lConvertMap)
            {
                lConvertMap.TryGetValue(lConvertSourcePath, out lConvertMedia);
            }

            TimeSpan lConvertDuration = lConvertMedia?.LWorkMediaDuration
                ?? lDurationRead(lConvertSourcePath);

            string lConvertFolder = lConvertOutput.LEncodingFolderRead(lConvertSourcePath);
            string lConvertOutputName = LConvertNameCreate(
                lConvertOutput, lConvertSourcePath, lConvertFolder, lConvertDuration, lConvertStamp, lConvertTakenPaths);

            lConvertWorkItems.Add(new LWorkItem(
                lConvertBatch,
                LWorkKind.LWorkKindConvert,
                lWorkPriority,
                lConvertSourcePath,
                TimeSpan.Zero,
                lConvertDuration,
                lConvertOutputName,
                Path.Combine(lConvertFolder, lConvertOutputName),
                lConvertOutput)
            {
                LWorkSourceMedia = lConvertMedia,
                LWorkTab = lConvertTab
            });
        }

        return lConvertWorkItems;
    }

    private static string LConvertNameCreate(
        LEncoding lConvertOutput,
        string lConvertSourcePath,
        string lConvertFolder,
        TimeSpan lConvertDuration,
        DateTimeOffset lConvertStamp,
        HashSet<string> lConvertTakenPaths)
    {
        string lConvertSourceStem = Path.GetFileNameWithoutExtension(lConvertSourcePath);
        string lConvertPattern = string.IsNullOrWhiteSpace(lConvertOutput.LEncodingNamePattern)
            ? "{OriginalName}"
            : lConvertOutput.LEncodingNamePattern;

        string lConvertStem = LEncoding.LEncodingNameFormat(
            lConvertPattern,
            lConvertMarker => LConvertMarkerRead(lConvertMarker, lConvertSourceStem, lConvertDuration, lConvertStamp));

        string lConvertBaseName = LEncoding.LEncodingNameNormalize(lConvertStem);
        string lConvertExtension = lConvertOutput.LEncodingExtensionResolve(lConvertSourcePath);
        if (LConvertSourceMatch(
                Path.Combine(lConvertFolder, LConvertNameFormat(lConvertBaseName, lConvertExtension)),
                lConvertSourcePath))
        {
            lConvertBaseName = $"{lConvertBaseName}_convert";
        }

        return LEncoding.LEncodingNameClaim(lConvertTakenPaths, lConvertFolder, lConvertBaseName, lConvertExtension);
    }

    private static string? LConvertMarkerRead(
        string lConvertMarker,
        string lConvertSourceStem,
        TimeSpan lConvertDuration,
        DateTimeOffset lConvertStamp)
    {
        if (lConvertMarker.Equals("Prefix", StringComparison.OrdinalIgnoreCase)
            || lConvertMarker.Equals("Suffix", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (lConvertMarker.Equals("OriginalName", StringComparison.OrdinalIgnoreCase))
        {
            return lConvertSourceStem;
        }

        if (lConvertMarker.Equals("SectionNumber", StringComparison.OrdinalIgnoreCase))
        {
            return "01";
        }

        if (lConvertMarker.Equals("SectionName", StringComparison.OrdinalIgnoreCase))
        {
            return "Convert";
        }

        if (lConvertMarker.Equals("SectionStart", StringComparison.OrdinalIgnoreCase))
        {
            return LEncoding.LEncodingTimeFormat(TimeSpan.Zero);
        }

        if (lConvertMarker.Equals("SectionEnd", StringComparison.OrdinalIgnoreCase)
            || lConvertMarker.Equals("SectionDuration", StringComparison.OrdinalIgnoreCase))
        {
            return LEncoding.LEncodingTimeFormat(lConvertDuration);
        }

        if (lConvertMarker.Equals("Date", StringComparison.OrdinalIgnoreCase))
        {
            return lConvertStamp.ToString("yyyy-MM-dd");
        }

        return lConvertMarker.Equals("Time", StringComparison.OrdinalIgnoreCase)
            ? lConvertStamp.ToString("HHmmss")
            : null;
    }

    // A duration-bearing name token cannot be filled from the sidecar cache alone: a file never opened in
    // the editor has no cached duration and would freeze "00-00-00.000" into its output name while the
    // later background measurement corrected only the item. Such a pattern is probed off the calling
    // thread before admission; every other pattern keeps the free cache read.
    public static async Task<Func<string, TimeSpan>> LConvertDurationResolve(
        LEncoding lConvertOutput, IReadOnlyList<string> lConvertSourcePaths)
    {
        if (!LConvertDurationCheck(lConvertOutput.LEncodingNamePattern ?? string.Empty))
        {
            return LLibrarian.LLibrarianDurationRead;
        }

        Dictionary<string, TimeSpan> lConvertDurations = await Task.Run(() =>
        {
            var lConvertMap = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase);
            foreach (string lConvertSourcePath in lConvertSourcePaths)
            {
                lConvertMap[lConvertSourcePath] = LLibrarian.LLibrarianDurationResolve(lConvertSourcePath);
            }

            return lConvertMap;
        }).ConfigureAwait(true);

        return lConvertSourcePath =>
            lConvertDurations.TryGetValue(lConvertSourcePath, out TimeSpan lConvertDuration)
                ? lConvertDuration
                : LLibrarian.LLibrarianDurationRead(lConvertSourcePath);
    }

    private static bool LConvertDurationCheck(string lConvertPattern) =>
        lConvertPattern.Contains("{SectionEnd}", StringComparison.OrdinalIgnoreCase)
        || lConvertPattern.Contains("{SectionDuration}", StringComparison.OrdinalIgnoreCase);

    private static string LConvertNameFormat(string lConvertBaseName, string lConvertExtension) =>
        string.IsNullOrWhiteSpace(lConvertExtension)
            ? lConvertBaseName
            : $"{lConvertBaseName}.{lConvertExtension}";

    private static bool LConvertSourceMatch(string lConvertOutputPath, string lConvertSourcePath)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(lConvertOutputPath),
                Path.GetFullPath(lConvertSourcePath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception lConvertError) when (lConvertError is ArgumentException or IOException or NotSupportedException)
        {
            return string.Equals(lConvertOutputPath, lConvertSourcePath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
