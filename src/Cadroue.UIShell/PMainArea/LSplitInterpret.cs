using System.IO;
using Cadroue.Core;
using Cadroue.UIShell;

namespace Cadroue.UIShell.PMainArea;

public static partial class LSplit
{
    public static int LSplitInterpret(
        LWorkPriority lWorkPriority,
        LSplitWorkDescription lSplitWorkDescription)
    {
        string? lSplitSourcePath = lSplitWorkDescription.LSplitSourcePath;
        if (string.IsNullOrWhiteSpace(lSplitSourcePath))
        {
            LAppLog.LError("Split not queued: no source file is open");
            return 0;
        }

        if (lSplitWorkDescription.LSplitSections.Count == 0)
        {
            LAppLog.LError($"Split not queued for '{Path.GetFileName(lSplitSourcePath)}': no sections have been cut");
            return 0;
        }

        LWorkOutput lSplitOutput = lSplitWorkDescription.LSplitOutput;
        string lSplitSourceStem = Path.GetFileNameWithoutExtension(lSplitSourcePath);
        string lSplitFolder = lSplitOutput.LWorkFolderRead(lSplitSourcePath);
        DateTimeOffset lSplitStamp = DateTimeOffset.Now;
        Guid lSplitBatchId = Guid.NewGuid();

        var lSplitTakenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lSplitWorkItems = new List<LWorkItem>();

        for (int lSplitIndex = 0; lSplitIndex < lSplitWorkDescription.LSplitSections.Count; lSplitIndex++)
        {
            LSplitSectionDescription lSplitSection = lSplitWorkDescription.LSplitSections[lSplitIndex];

            if (lSplitSection.LSplitSectionEnd <= lSplitSection.LSplitSectionStart)
            {
                continue;
            }

            string lSplitOutputName = LSplitNameCreate(
                lSplitOutput,
                lSplitSourceStem,
                lSplitSection,
                lSplitIndex,
                lSplitStamp,
                lSplitTakenNames);

            lSplitWorkItems.Add(new LWorkItem(
                lSplitBatchId,
                LWorkKind.LWorkKindSplit,
                lWorkPriority,
                lSplitSourcePath,
                lSplitSection.LSplitSectionStart,
                lSplitSection.LSplitSectionEnd,
                lSplitOutputName,
                Path.Combine(lSplitFolder, lSplitOutputName),
                lSplitOutput));
        }

        int lSplitSkipped = lSplitWorkDescription.LSplitSections.Count - lSplitWorkItems.Count;
        if (lSplitSkipped > 0)
        {
            LAppLog.LError($"Split skipped {lSplitSkipped} section(s) of '{Path.GetFileName(lSplitSourcePath)}': empty or reversed range");
        }

        int lSplitAdded = LSchedule.LScheduleCurrent.LScheduleAdd(lSplitWorkItems);
        LAppLog.LInfo(
            $"Split queued {lSplitAdded} job(s) at {lWorkPriority} from '{Path.GetFileName(lSplitSourcePath)}' " +
            $"into '{lSplitFolder}' [batch {lSplitBatchId:N}]");
        foreach (LWorkItem lSplitItem in lSplitWorkItems)
        {
            LAppLog.LInfo(
                $"Split job '{lSplitItem.LWorkOutputName}': " +
                $"{lSplitItem.LWorkStart:hh\\:mm\\:ss\\.fff} to {lSplitItem.LWorkEnd:hh\\:mm\\:ss\\.fff} " +
                $"({lSplitItem.LWorkDuration:hh\\:mm\\:ss\\.fff})");
        }

        return lSplitAdded;
    }


    private static string LSplitNameCreate(
        LWorkOutput lSplitOutput,
        string lSplitSourceStem,
        LSplitSectionDescription lSplitSection,
        int lSplitIndex,
        DateTimeOffset lSplitStamp,
        HashSet<string> lSplitTakenNames)
    {
        string lSplitSectionName = lSplitSection.LSplitSectionName;
        string lSplitPattern = string.IsNullOrWhiteSpace(lSplitOutput.LWorkOutputNamePattern)
            ? "{OriginalName}"
            : lSplitOutput.LWorkOutputNamePattern;

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

        if (!lSplitHasNumber && !lSplitHasSectionName)
        {
            lSplitStem = $"{lSplitStem}_{lSplitResolvedSectionName}";
        }

        string lSplitBaseName = LSplitNameSanitize(lSplitStem);
        string lSplitUniqueName = lSplitBaseName;
        int lSplitAttempt = 2;
        while (!lSplitTakenNames.Add(lSplitUniqueName))
        {
            lSplitUniqueName = $"{lSplitBaseName}_{lSplitAttempt}";
            lSplitAttempt++;
        }

        return string.IsNullOrWhiteSpace(lSplitOutput.LWorkOutputExtension)
            ? lSplitUniqueName
            : $"{lSplitUniqueName}.{lSplitOutput.LWorkOutputExtension}";
    }

    private static string LSplitNameSanitize(string lSplitName)
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
