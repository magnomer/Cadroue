using System.IO;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.UIShell.PMainArea;

public static partial class LConvert
{
    public static IReadOnlyList<LWorkItem> LConvertInterpret(
        LWorkPriority lWorkPriority,
        LConvertWorkDescription lConvertWorkDescription)
    {
        IReadOnlyList<string> lConvertSourcePaths = lConvertWorkDescription.LConvertSourcePaths;
        if (lConvertSourcePaths.Count == 0)
        {
            LTraceLog.LTraceErrorRecord("Convert not queued: the file list is empty");
            return Array.Empty<LWorkItem>();
        }

        LWorkOutput lConvertOutput = lConvertWorkDescription.LConvertOutput;
        var lConvertWorkItems = new List<LWorkItem>();

        foreach (string lConvertSourcePath in lConvertSourcePaths)
        {
            LWorkMedia? lConvertMedia = null;
            if (lConvertWorkDescription.LConvertMedia is { } lConvertMap)
            {
                lConvertMap.TryGetValue(lConvertSourcePath, out lConvertMedia);
            }

            TimeSpan lConvertDuration = lConvertMedia?.LWorkMediaDuration
                ?? LSidecarStore.LSidecarDurationRead(lConvertSourcePath);

            string lConvertFolder = lConvertOutput.LWorkFolderRead(lConvertSourcePath);
            string lConvertOutputName = LConvertNameCreate(lConvertOutput, lConvertSourcePath, lConvertFolder);

            lConvertWorkItems.Add(new LWorkItem(
                Guid.NewGuid(),
                LWorkKind.LWorkKindConvert,
                lWorkPriority,
                lConvertSourcePath,
                TimeSpan.Zero,
                lConvertDuration,
                lConvertOutputName,
                Path.Combine(lConvertFolder, lConvertOutputName),
                lConvertOutput)
            {
                LWorkSourceMedia = lConvertMedia
            });
        }

        return lConvertWorkItems;
    }

    private static string LConvertNameCreate(LWorkOutput lConvertOutput, string lConvertSourcePath, string lConvertFolder)
    {
        string lConvertSourceStem = Path.GetFileNameWithoutExtension(lConvertSourcePath);
        string lConvertPattern = string.IsNullOrWhiteSpace(lConvertOutput.LWorkOutputNamePattern)
            ? "{OriginalName}"
            : lConvertOutput.LWorkOutputNamePattern;

        DateTimeOffset lConvertStamp = DateTimeOffset.Now;
        string lConvertStem = lConvertPattern
            .Replace("{Prefix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Suffix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{OriginalName}", lConvertSourceStem, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionNumber}", "01", StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionName}", "Convert", StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", lConvertStamp.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", lConvertStamp.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        lConvertStem = LWorkOutput.LWorkOutputShorten(lConvertStem);

        string lConvertBaseName = LConvertNameNormalize(lConvertStem);
        string lConvertFileName = LConvertNameFormat(lConvertOutput, lConvertBaseName, lConvertSourcePath);
        return LConvertSourceMatch(Path.Combine(lConvertFolder, lConvertFileName), lConvertSourcePath)
            ? LConvertNameFormat(lConvertOutput, $"{lConvertBaseName}_convert", lConvertSourcePath)
            : lConvertFileName;
    }

    private static string LConvertNameFormat(LWorkOutput lConvertOutput, string lConvertBaseName, string lConvertSourcePath)
    {
        string lConvertExtension = lConvertOutput.LWorkExtensionResolve(lConvertSourcePath);
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
