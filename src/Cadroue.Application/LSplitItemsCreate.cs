using System.IO;
using Cadroue.Core;

namespace Cadroue.Application;

public static partial class LSplit
{
    public static IReadOnlyList<LWorkItem> LSplitItemsCreate(
        LWorkPriority lWorkPriority,
        LSplitWorkDescription lSplitWorkDescription,
        string lSplitTab,
        Action<string> lInfoLog,
        Action<string> lErrorLog,
        Guid lSplitBatchId = default)
    {
        string? lSplitSourcePath = lSplitWorkDescription.LSplitSourcePath;
        if (string.IsNullOrWhiteSpace(lSplitSourcePath))
        {
            lErrorLog("Split not queued: no source file is open");
            return Array.Empty<LWorkItem>();
        }

        if (lSplitWorkDescription.LSplitSections.Count == 0)
        {
            lErrorLog($"Split not queued for '{Path.GetFileName(lSplitSourcePath)}': no sections have been cut");
            return Array.Empty<LWorkItem>();
        }

        LEncoding lSplitOutput = lSplitWorkDescription.LSplitOutput;
        string lSplitSourceStem = Path.GetFileNameWithoutExtension(lSplitSourcePath);
        string lSplitFolder = lSplitOutput.LEncodingFolderRead(lSplitSourcePath);
        DateTimeOffset lSplitStamp = DateTimeOffset.Now;
        Guid lSplitBatch = lSplitBatchId != Guid.Empty ? lSplitBatchId : Guid.NewGuid();

        var lSplitTakenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lSplitWorkItems = new List<LWorkItem>();
        int lSplitHidden = 0;

        for (int lSplitIndex = 0; lSplitIndex < lSplitWorkDescription.LSplitSections.Count; lSplitIndex++)
        {
            LSplitSectionDescription lSplitSection = lSplitWorkDescription.LSplitSections[lSplitIndex];

            if (lSplitSection.LSplitSectionHidden)
            {
                lSplitHidden++;
                continue;
            }

            if (lSplitSection.LSplitSectionEnd <= lSplitSection.LSplitSectionStart)
            {
                continue;
            }

            string lSplitOutputName = LSplitNameCreate(
                lSplitOutput,
                lSplitSourcePath,
                lSplitSourceStem,
                lSplitSection,
                lSplitIndex,
                lSplitStamp,
                lSplitTakenNames);

            lSplitWorkItems.Add(new LWorkItem(
                lSplitBatch,
                LWorkKind.LWorkKindSplit,
                lWorkPriority,
                lSplitSourcePath,
                lSplitSection.LSplitSectionStart,
                lSplitSection.LSplitSectionEnd,
                lSplitOutputName,
                Path.Combine(lSplitFolder, lSplitOutputName),
                lSplitOutput));
        }

        int lSplitSkipped = lSplitWorkDescription.LSplitSections.Count - lSplitWorkItems.Count - lSplitHidden;
        if (lSplitSkipped > 0)
        {
            lErrorLog($"Split skipped {lSplitSkipped} section(s) of '{Path.GetFileName(lSplitSourcePath)}': empty or reversed range");
        }

        if (lSplitHidden > 0)
        {
            lInfoLog($"Split left {lSplitHidden} off section(s) of '{Path.GetFileName(lSplitSourcePath)}' out; their numbers are kept");
        }

        foreach (LWorkItem lSplitItem in lSplitWorkItems)
        {
            lSplitItem.LWorkTab = lSplitTab;
        }

        lInfoLog(
            $"Split built {lSplitWorkItems.Count} job(s) at {lWorkPriority} from '{Path.GetFileName(lSplitSourcePath)}' " +
            $"into '{lSplitFolder}' [batch {lSplitBatch:N}]");
        foreach (LWorkItem lSplitItem in lSplitWorkItems)
        {
            lInfoLog(
                $"Split job '{lSplitItem.LWorkOutputName}': " +
                $"{lSplitItem.LWorkOrigin:hh\\:mm\\:ss\\.fff} to {lSplitItem.LWorkEnd:hh\\:mm\\:ss\\.fff} " +
                $"({lSplitItem.LWorkDuration:hh\\:mm\\:ss\\.fff})");
        }

        return lSplitWorkItems;
    }


    private static string LSplitNameCreate(
        LEncoding lSplitOutput,
        string lSplitSourcePath,
        string lSplitSourceStem,
        LSplitSectionDescription lSplitSection,
        int lSplitIndex,
        DateTimeOffset lSplitStamp,
        HashSet<string> lSplitTakenNames)
    {
        string lSplitSectionName = lSplitSection.LSplitSectionName;
        string lSplitPattern = string.IsNullOrWhiteSpace(lSplitOutput.LEncodingNamePattern)
            ? "{OriginalName}"
            : lSplitOutput.LEncodingNamePattern;

        bool lSplitHasNumber = lSplitPattern.Contains("{SectionNumber}", StringComparison.OrdinalIgnoreCase);
        bool lSplitHasSectionName = lSplitPattern.Contains("{SectionName}", StringComparison.OrdinalIgnoreCase);

        string lSplitResolvedSectionName = string.IsNullOrWhiteSpace(lSplitSectionName)
            ? $"Section {lSplitIndex + 1}"
            : lSplitSectionName;

        string lSplitStem = lSplitPattern
            .Replace("{Prefix}", lSplitSection.LSplitSectionPrefix, StringComparison.OrdinalIgnoreCase)
            .Replace("{Suffix}", lSplitSection.LSplitSectionSuffix, StringComparison.OrdinalIgnoreCase)
            .Replace("{OriginalName}", lSplitSourceStem, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionNumber}", (lSplitIndex + 1).ToString("D2"), StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionName}", lSplitResolvedSectionName, StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", lSplitStamp.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", lSplitStamp.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        lSplitStem = LEncoding.LEncodingShorten(lSplitStem);

        if (!lSplitHasNumber && !lSplitHasSectionName)
        {
            lSplitStem = $"{lSplitStem}_{lSplitResolvedSectionName}";
        }

        string lSplitBaseName = LSplitNameNormalize(lSplitStem);
        string lSplitUniqueName = lSplitBaseName;
        int lSplitAttempt = 2;
        while (!lSplitTakenNames.Add(lSplitUniqueName))
        {
            lSplitUniqueName = $"{lSplitBaseName}_{lSplitAttempt}";
            lSplitAttempt++;
        }

        string lSplitExtension = lSplitOutput.LEncodingExtensionResolve(lSplitSourcePath);
        return string.IsNullOrWhiteSpace(lSplitExtension)
            ? lSplitUniqueName
            : $"{lSplitUniqueName}.{lSplitExtension}";
    }

    private static string LSplitNameNormalize(string lSplitName)
    {
        char[] lSplitInvalidChars = Path.GetInvalidFileNameChars();
        var lSplitBuilder = new System.Text.StringBuilder(lSplitName.Length);
        foreach (char lSplitChar in lSplitName)
        {
            lSplitBuilder.Append(Array.IndexOf(lSplitInvalidChars, lSplitChar) >= 0 ? '_' : lSplitChar);
        }

        string lSplitTrimmed = lSplitBuilder.ToString().Trim();
        return lSplitTrimmed.Length == 0 ? "output" : lSplitTrimmed;
    }
}
