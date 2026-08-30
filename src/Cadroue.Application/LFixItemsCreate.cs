using System.IO;
using Cadroue.Core;

namespace Cadroue.Application;

public static partial class LFix
{
    public static IReadOnlyList<LWorkItem> LFixItemsCreate(
        LWorkPriority lWorkPriority,
        LFixWorkDescription lFixWorkDescription,
        string lFixTab,
        Action<string> lErrorLog,
        Func<string, TimeSpan> lDurationRead)
    {
        IReadOnlyList<string> lFixSourcePaths = lFixWorkDescription.LFixSourcePaths;
        if (lFixSourcePaths.Count == 0)
        {
            lErrorLog("Fix not queued: the file list is empty");
            return Array.Empty<LWorkItem>();
        }

        LEncoding lFixOutput = lFixWorkDescription.LFixOutput;
        var lFixWorkItems = new List<LWorkItem>();
        Guid lFixLooseBatch = LGate.LGateBatchCreate();

        foreach (string lFixSourcePath in lFixSourcePaths)
        {
            Guid lFixBatch = lFixWorkDescription.LFixRelays is { } lFixRelayMap
                && lFixRelayMap.TryGetValue(lFixSourcePath, out Guid lFixRelay)
                && lFixRelay != Guid.Empty
                ? lFixRelay
                : lFixLooseBatch;

            LWorkMedia? lFixMedia = null;
            if (lFixWorkDescription.LFixMedia is { } lFixMap)
            {
                lFixMap.TryGetValue(lFixSourcePath, out lFixMedia);
            }

            TimeSpan lFixDuration = lFixMedia?.LWorkMediaDuration ?? lDurationRead(lFixSourcePath);

            string lFixFolder = lFixOutput.LEncodingFolderRead(lFixSourcePath);
            string lFixOutputName = LFixNameCreate(lFixOutput, lFixSourcePath, lFixFolder, lFixDuration);

            LWorkFix lFixPlanCurrent = LWorkFix.LWorkFixCreate();
            if (lFixWorkDescription.LFixPlans is { } lFixPlanMap
                && lFixPlanMap.TryGetValue(lFixSourcePath, out LWorkFix? lFixPlan)
                && lFixPlan is not null)
            {
                lFixPlanCurrent = lFixPlan;
            }

            lFixWorkItems.Add(new LWorkItem(
                lFixBatch,
                LWorkKind.LWorkKindFix,
                lWorkPriority,
                lFixSourcePath,
                TimeSpan.Zero,
                lFixDuration,
                lFixOutputName,
                Path.Combine(lFixFolder, lFixOutputName),
                lFixOutput)
            {
                LWorkSourceMedia = lFixMedia,
                LWorkTab = lFixTab,
                LWorkFixPlan = lFixPlanCurrent
            });
        }

        return lFixWorkItems;
    }


    private static string LFixNameCreate(LEncoding lFixOutput, string lFixSourcePath, string lFixFolder, TimeSpan lFixDuration)
    {
        string lFixSourceStem = Path.GetFileNameWithoutExtension(lFixSourcePath);
        string lFixPattern = string.IsNullOrWhiteSpace(lFixOutput.LEncodingNamePattern)
            ? "{OriginalName}"
            : lFixOutput.LEncodingNamePattern;

        DateTimeOffset lFixStamp = DateTimeOffset.Now;
        string lFixStem = lFixPattern
            .Replace("{Prefix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Suffix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{OriginalName}", lFixSourceStem, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionNumber}", "01", StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionName}", "Fix", StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionStart}", LEncoding.LEncodingTimeFormat(TimeSpan.Zero), StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionEnd}", LEncoding.LEncodingTimeFormat(lFixDuration), StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionDuration}", LEncoding.LEncodingTimeFormat(lFixDuration), StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", lFixStamp.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", lFixStamp.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        lFixStem = LEncoding.LEncodingShorten(lFixStem);

        string lFixBaseName = LFixNameNormalize(lFixStem);
        string lFixFileName = LFixNameFormat(lFixBaseName, lFixSourcePath);
        return LFixSourceMatch(Path.Combine(lFixFolder, lFixFileName), lFixSourcePath)
            ? LFixNameFormat($"{lFixBaseName}_fix", lFixSourcePath)
            : lFixFileName;
    }

    private static string LFixNameFormat(string lFixBaseName, string lFixSourcePath)
    {
        // Fix is a source-representation pass-through: the copy stage keeps the source
        // container and stream layout, so the destination extension must mirror the
        // source, never the export preset's container.
        string lFixExtension = Path.GetExtension(lFixSourcePath).TrimStart('.');
        return string.IsNullOrWhiteSpace(lFixExtension)
            ? lFixBaseName
            : $"{lFixBaseName}.{lFixExtension}";
    }

    private static bool LFixSourceMatch(string lFixOutputPath, string lFixSourcePath)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(lFixOutputPath),
                Path.GetFullPath(lFixSourcePath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception lFixError) when (lFixError is ArgumentException or IOException or NotSupportedException)
        {
            return string.Equals(lFixOutputPath, lFixSourcePath, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string LFixNameNormalize(string lFixName)
    {
        char[] lFixInvalidChars = Path.GetInvalidFileNameChars();
        var lFixBuilder = new System.Text.StringBuilder(lFixName.Length);
        foreach (char lFixChar in lFixName)
        {
            lFixBuilder.Append(Array.IndexOf(lFixInvalidChars, lFixChar) >= 0 ? '_' : lFixChar);
        }

        string lFixTrimmed = lFixBuilder.ToString().Trim();
        return lFixTrimmed.Length == 0 ? "output" : lFixTrimmed;
    }
}
