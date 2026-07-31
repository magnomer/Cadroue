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
        Guid lMergeRelayTarget = default,
        Guid lMergeRelaySource = default,
        IReadOnlyDictionary<string, Guid>? lMergeRelays = null)
    {
        DateTimeOffset lMergeStamp = DateTimeOffset.Now;
        Guid lMergeLooseBatch = Guid.NewGuid();
        var lMergeTakenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lMergeItems = new List<LWorkItem>();

        foreach (PGroup.PGroupSelection lMergeGroup in lMergeGroups)
        {
            string[] lMergeSources = lMergeGroup.PGroupSelectionPaths.Where(File.Exists).ToArray();
            if (lMergeSources.Length == 0)
            {
                continue;
            }

            string lMergeBasePath = lMergeSources[0];
            string lMergeFolder = lMergeOutput.LWorkFolderRead(lMergeBasePath);
            string lMergeName = LMergeNameCreate(lMergeOutput, lMergeBasePath, lMergeGroup.PGroupSelectionName, lMergeStamp, lMergeTakenNames);
            Guid lMergeBatch = LMergeBatchResolve(lMergeSources, lMergeRelays, lMergeLooseBatch);

            lMergeItems.Add(new LWorkItem(
                lMergeBatch,
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
            LTraceLog.LTraceErrorRecord("Merge not queued: no group holds an existing file");
            return 0;
        }

        int lMergeAdded = LSchedule.LScheduleCurrent.LScheduleAdd(lMergeItems, lMergeRelayTarget, lMergeRelaySource);
        LTraceLog.LTraceInfoRecord($"Merge queued {lMergeAdded} group(s) at {lWorkPriority}");
        foreach (LWorkItem lMergeItem in lMergeItems)
        {
            LTraceLog.LTraceInfoRecord(
                $"Merge job '{lMergeItem.LWorkOutputName}': {lMergeItem.LWorkMergeSources.Count} file(s) [batch {lMergeItem.LWorkBatchId:N}]");
        }

        return lMergeAdded;
    }

    private static Guid LMergeBatchResolve(
        IReadOnlyList<string> lMergeSources,
        IReadOnlyDictionary<string, Guid>? lMergeRelays,
        Guid lMergeLooseBatch)
    {
        if (lMergeRelays is not null)
        {
            foreach (string lMergeSource in lMergeSources)
            {
                if (lMergeRelays.TryGetValue(lMergeSource, out Guid lMergeRelay) && lMergeRelay != Guid.Empty)
                {
                    return lMergeRelay;
                }
            }
        }

        return lMergeLooseBatch;
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
