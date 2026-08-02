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
        Guid lConvertLooseBatch = Guid.NewGuid();

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
            string lConvertOutputName = LConvertNameCreate(lConvertOutput, lConvertSourcePath, lConvertFolder);

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

    private static string LConvertNameCreate(LEncoding lConvertOutput, string lConvertSourcePath, string lConvertFolder)
    {
        string lConvertSourceStem = Path.GetFileNameWithoutExtension(lConvertSourcePath);
        string lConvertPattern = string.IsNullOrWhiteSpace(lConvertOutput.LEncodingNamePattern)
            ? "{OriginalName}"
            : lConvertOutput.LEncodingNamePattern;

        DateTimeOffset lConvertStamp = DateTimeOffset.Now;
        string lConvertStem = lConvertPattern
            .Replace("{Prefix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Suffix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{OriginalName}", lConvertSourceStem, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionNumber}", "01", StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionName}", "Convert", StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", lConvertStamp.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", lConvertStamp.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        lConvertStem = LEncoding.LEncodingShorten(lConvertStem);

        string lConvertBaseName = LConvertNameNormalize(lConvertStem);
        string lConvertFileName = LConvertNameFormat(lConvertOutput, lConvertBaseName, lConvertSourcePath);
        return LConvertSourceMatch(Path.Combine(lConvertFolder, lConvertFileName), lConvertSourcePath)
            ? LConvertNameFormat(lConvertOutput, $"{lConvertBaseName}_convert", lConvertSourcePath)
            : lConvertFileName;
    }

    private static string LConvertNameFormat(LEncoding lConvertOutput, string lConvertBaseName, string lConvertSourcePath)
    {
        string lConvertExtension = lConvertOutput.LEncodingExtensionResolve(lConvertSourcePath);
        return string.IsNullOrWhiteSpace(lConvertExtension)
            ? lConvertBaseName
            : $"{lConvertBaseName}.{lConvertExtension}";
    }

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

    private static string LConvertNameNormalize(string lConvertName)
    {
        char[] lConvertInvalidChars = Path.GetInvalidFileNameChars();
        var lConvertBuilder = new System.Text.StringBuilder(lConvertName.Length);
        foreach (char lConvertChar in lConvertName)
        {
            lConvertBuilder.Append(Array.IndexOf(lConvertInvalidChars, lConvertChar) >= 0 ? '_' : lConvertChar);
        }

        string lConvertTrimmed = lConvertBuilder.ToString().Trim();
        return lConvertTrimmed.Length == 0 ? "output" : lConvertTrimmed;
    }
}
