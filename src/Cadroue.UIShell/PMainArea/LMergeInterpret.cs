using System.IO;
using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public static partial class LMerge
{
    public static int LMergeInterpret(
        LWorkPriority lWorkPriority,
        IReadOnlyList<PGroup.PGroupSelection> lMergeGroups,
        LWorkOutput lMergeOutput,
        Guid lMergeRelayTarget = default)
    {
        DateTimeOffset lMergeStamp = DateTimeOffset.Now;
        Guid lMergeBatchId = Guid.NewGuid();
        var lMergeTakenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lMergeItems = new List<LWorkItem>();

        foreach (PGroup.PGroupSelection lMergeGroup in lMergeGroups)
        {
            string[] lMergeSources = lMergeGroup.PGroupSelectionPaths.Where(File.Exists).ToArray();
            if (lMergeSources.Length < 2)
            {
                continue;
            }

            string lMergeBasePath = lMergeSources[0];
            string lMergeFolder = lMergeOutput.LWorkFolderRead(lMergeBasePath);
            string lMergeName = LMergeNameCreate(lMergeOutput, lMergeBasePath, lMergeGroup.PGroupSelectionName, lMergeStamp, lMergeTakenNames);

            lMergeItems.Add(new LWorkItem(
                lMergeBatchId,
                LWorkKind.LWorkKindMerge,
                lWorkPriority,
                lMergeBasePath,
                TimeSpan.Zero,
                TimeSpan.Zero,
                lMergeName,
                Path.Combine(lMergeFolder, lMergeName),
                lMergeOutput,
                lWorkMergeSources: lMergeSources));
        }

        if (lMergeItems.Count == 0)
        {
            LTraceLog.LTraceErrorRecord("Merge not queued: no group holds two or more existing files");
            return 0;
        }

        int lMergeAdded = LSchedule.LScheduleCurrent.LScheduleAdd(lMergeItems, lMergeRelayTarget);
        LTraceLog.LTraceInfoRecord($"Merge queued {lMergeAdded} group(s) at {lWorkPriority} [batch {lMergeBatchId:N}]");
        foreach (LWorkItem lMergeItem in lMergeItems)
        {
            LTraceLog.LTraceInfoRecord($"Merge job '{lMergeItem.LWorkOutputName}': {lMergeItem.LWorkMergeSources.Count} file(s)");
        }

        return lMergeAdded;
    }

    private static string LMergeNameCreate(
        LWorkOutput lMergeOutput,
        string lMergeBasePath,
        string lMergeGroupName,
        DateTimeOffset lMergeStamp,
        HashSet<string> lMergeTakenNames)
    {
        string lMergePattern = string.IsNullOrWhiteSpace(lMergeOutput.LWorkOutputNamePattern)
            ? "{OriginalName}"
            : lMergeOutput.LWorkOutputNamePattern;

        string lMergeStemName = lMergePattern
            .Replace("{Prefix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Suffix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{OriginalName}", lMergeGroupName, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionNumber}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionName}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", lMergeStamp.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", lMergeStamp.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        lMergeStemName = LWorkOutput.LWorkOutputShorten(lMergeStemName);

        string lMergeBaseName = LMergeNameNormalize(lMergeStemName);
        string lMergeUniqueName = lMergeBaseName;
        int lMergeAttempt = 2;
        while (!lMergeTakenNames.Add(lMergeUniqueName))
        {
            lMergeUniqueName = $"{lMergeBaseName}_{lMergeAttempt}";
            lMergeAttempt++;
        }

        string lMergeExtension = lMergeOutput.LWorkExtensionResolve(lMergeBasePath);
        return string.IsNullOrWhiteSpace(lMergeExtension)
            ? lMergeUniqueName
            : $"{lMergeUniqueName}.{lMergeExtension}";
    }

    private static string LMergeNameNormalize(string lMergeName)
    {
        char[] lMergeInvalidChars = Path.GetInvalidFileNameChars();
        var lMergeBuilder = new System.Text.StringBuilder(lMergeName.Length);
        foreach (char lMergeChar in lMergeName)
        {
            lMergeBuilder.Append(Array.IndexOf(lMergeInvalidChars, lMergeChar) >= 0 ? '_' : lMergeChar);
        }

        string lMergeTrimmed = lMergeBuilder.ToString().Trim();
        return lMergeTrimmed.Length == 0 ? "merged" : lMergeTrimmed;
    }
}
