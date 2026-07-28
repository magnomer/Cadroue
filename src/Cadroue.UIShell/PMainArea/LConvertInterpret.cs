using System.IO;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.UIShell.PMainArea;

public static partial class LConvert
{
    public static int LConvertInterpret(
        LWorkPriority lWorkPriority,
        LConvertWorkDescription lConvertWorkDescription)
    {
        IReadOnlyList<string> lConvertSourcePaths = lConvertWorkDescription.LConvertSourcePaths;
        if (lConvertSourcePaths.Count == 0)
        {
            LAppLog.LError("Convert not queued: the file list is empty");
            return 0;
        }

        LWorkOutput lConvertOutput = lConvertWorkDescription.LConvertOutput;
        var lConvertWorkItems = new List<LWorkItem>();
        int lConvertSkipped = 0;

        foreach (string lConvertSourcePath in lConvertSourcePaths)
        {
            LWorkMedia? lConvertMedia = null;
            if (lConvertWorkDescription.LConvertMedia is { } lConvertMap)
            {
                lConvertMap.TryGetValue(lConvertSourcePath, out lConvertMedia);
            }

            TimeSpan lConvertDuration = lConvertMedia?.LWorkMediaDuration
                ?? LConvertDurationRead(lConvertSourcePath);
            if (lConvertDuration <= TimeSpan.Zero)
            {
                LAppLog.LError($"Convert skipped '{Path.GetFileName(lConvertSourcePath)}': media duration is unknown");
                lConvertSkipped++;
                continue;
            }

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

        if (lConvertWorkItems.Count == 0)
        {
            LAppLog.LError("Convert not queued: no listed file could be read");
            return 0;
        }

        int lConvertAdded = LSchedule.LScheduleCurrent.LScheduleAdd(lConvertWorkItems);
        LAppLog.LInfo($"Convert queued {lConvertAdded} job(s) at {lWorkPriority} from {lConvertSourcePaths.Count} listed file(s)");

        if (lConvertSkipped > 0)
        {
            LAppLog.LError($"Convert skipped {lConvertSkipped} listed file(s): media duration is unknown");
        }

        return lConvertAdded;
    }

    private static TimeSpan LConvertDurationRead(string lConvertSourcePath)
    {
        try
        {
            return LMediaInfo.LMediaFfprobeRead(lConvertSourcePath).LMediaInfoDuration;
        }
        catch (Exception lConvertError)
        {
            LAppLog.LError($"Convert could not read '{Path.GetFileName(lConvertSourcePath)}': {lConvertError.Message}");
            return TimeSpan.Zero;
        }
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

        string lConvertBaseName = LConvertNameSanitize(lConvertStem);
        string lConvertFileName = LConvertNameFormat(lConvertOutput, lConvertBaseName);
        return LConvertSourceMatch(Path.Combine(lConvertFolder, lConvertFileName), lConvertSourcePath)
            ? LConvertNameFormat(lConvertOutput, $"{lConvertBaseName}_convert")
            : lConvertFileName;
    }

    private static string LConvertNameFormat(LWorkOutput lConvertOutput, string lConvertBaseName) =>
        string.IsNullOrWhiteSpace(lConvertOutput.LWorkOutputExtension)
            ? lConvertBaseName
            : $"{lConvertBaseName}.{lConvertOutput.LWorkOutputExtension}";

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

    private static string LConvertNameSanitize(string lConvertName)
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
