using System.IO;
using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public static partial class LSplit
{
    /// <summary>
    /// Turn a split description into work items and put them on the backend schedule.
    /// One item per usable section, all sharing a batch id. Returns how many were added.
    /// </summary>
    public static int LSplitInterpret(
        LWorkPriority lWorkPriority,
        LSplitWorkDescription lSplitWorkDescription)
    {
        string? lSplitSourcePath = lSplitWorkDescription.LSplitSourcePath;
        if (string.IsNullOrWhiteSpace(lSplitSourcePath) || lSplitWorkDescription.LSplitSections.Count == 0)
        {
            return 0;
        }

        LWorkOutput lSplitOutput = lSplitWorkDescription.LSplitOutput;
        string lSplitSourceStem = Path.GetFileNameWithoutExtension(lSplitSourcePath);
        string lSplitFolder = LSplitFolderRead(lSplitOutput, lSplitSourcePath);
        DateTimeOffset lSplitStamp = DateTimeOffset.Now;
        Guid lSplitBatchId = Guid.NewGuid();

        var lSplitTakenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lSplitWorkItems = new List<LWorkItem>();

        for (int lSplitIndex = 0; lSplitIndex < lSplitWorkDescription.LSplitSections.Count; lSplitIndex++)
        {
            LSplitSectionDescription lSplitSection = lSplitWorkDescription.LSplitSections[lSplitIndex];

            // A zero-length or inverted section would produce an empty file.
            if (lSplitSection.LSplitSectionEnd <= lSplitSection.LSplitSectionStart)
            {
                continue;
            }

            string lSplitOutputName = LSplitNameCreate(
                lSplitOutput,
                lSplitSourceStem,
                lSplitSection.LSplitSectionName,
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

        return LSchedule.LScheduleCurrent.LScheduleAdd(lSplitWorkItems);
    }

    /// <summary>
    /// Destination folder for the batch. "Custom folder" uses the chosen folder;
    /// anything else (or a custom folder that was never picked) falls back to the
    /// source file's own folder.
    /// </summary>
    private static string LSplitFolderRead(LWorkOutput lSplitOutput, string lSplitSourcePath)
    {
        if (string.Equals(lSplitOutput.LWorkOutputLocation, "Custom folder", StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(lSplitOutput.LWorkOutputLocationFolder))
        {
            return lSplitOutput.LWorkOutputLocationFolder;
        }

        return Path.GetDirectoryName(lSplitSourcePath) ?? string.Empty;
    }

    /// <summary>
    /// Resolve one section's output file name from the export name pattern, expanding
    /// the tokens the Name row offers. Sections share a pattern, so a pattern without
    /// {SectionNumber} gets the section name appended to keep outputs apart, and a
    /// numeric tail is added if two still collide.
    /// </summary>
    private static string LSplitNameCreate(
        LWorkOutput lSplitOutput,
        string lSplitSourceStem,
        string lSplitSectionName,
        int lSplitIndex,
        DateTimeOffset lSplitStamp,
        HashSet<string> lSplitTakenNames)
    {
        string lSplitPattern = string.IsNullOrWhiteSpace(lSplitOutput.LWorkOutputNamePattern)
            ? "{OriginalName}"
            : lSplitOutput.LWorkOutputNamePattern;

        bool lSplitHasNumber = lSplitPattern.Contains("{SectionNumber}", StringComparison.OrdinalIgnoreCase);

        string lSplitStem = lSplitPattern
            .Replace("{OriginalName}", lSplitSourceStem, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionNumber}", (lSplitIndex + 1).ToString("D2"), StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", lSplitStamp.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", lSplitStamp.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        // Without {SectionNumber} every section would resolve to the same name.
        if (!lSplitHasNumber)
        {
            string lSplitSuffix = string.IsNullOrWhiteSpace(lSplitSectionName)
                ? $"Section {lSplitIndex + 1}"
                : lSplitSectionName;
            lSplitStem = $"{lSplitStem}_{lSplitSuffix}";
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
