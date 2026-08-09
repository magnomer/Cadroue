using System.IO;
using Cadroue.Core;

namespace Cadroue.Application;

public static partial class LMerge
{
    public static IReadOnlyList<LWorkItem> LMergeItemsCreate(
        LWorkPriority lWorkPriority,
        IReadOnlyList<LWorkGroup> lMergeGroups,
        LEncoding lMergeOutput,
        string lMergeTab,
        Action<string> lInfoLog,
        Action<string> lErrorLog,
        IReadOnlyDictionary<string, Guid>? lMergeRelays = null)
    {
        DateTimeOffset lMergeStamp = DateTimeOffset.Now;
        Guid lMergeLooseBatch = Guid.NewGuid();
        var lMergeTakenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lMergeItems = new List<LWorkItem>();

        foreach (LWorkGroup lMergeGroup in lMergeGroups)
        {
            string[] lMergeSources = lMergeGroup.LWorkGroupPaths.Where(File.Exists).ToArray();
            if (lMergeSources.Length < 1)
            {
                continue;
            }

            string lMergeBasePath = lMergeSources[0];
            string lMergeFolder = lMergeOutput.LEncodingFolderRead(lMergeBasePath);
            string lMergeName = LMergeNameCreate(lMergeOutput, lMergeBasePath, lMergeGroup.LWorkGroupName, lMergeStamp, lMergeTakenNames);
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
            lErrorLog("Merge not queued: no group holds an existing file");
            return Array.Empty<LWorkItem>();
        }

        foreach (LWorkItem lMergeItem in lMergeItems)
        {
            lMergeItem.LWorkTab = lMergeTab;
            lInfoLog(
                $"Merge built job '{lMergeItem.LWorkOutputName}': {lMergeItem.LWorkMergeSources.Count} file(s) [batch {lMergeItem.LWorkBatchId:N}]");
        }

        return lMergeItems;
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
        LEncoding lMergeOutput,
        string lMergeBasePath,
        string lMergeGroupName,
        DateTimeOffset lMergeStamp,
        HashSet<string> lMergeTakenNames)
    {
        string lMergePattern = string.IsNullOrWhiteSpace(lMergeOutput.LEncodingNamePattern)
            ? "{OriginalName}"
            : lMergeOutput.LEncodingNamePattern;

        string lMergeStemName = lMergePattern
            .Replace("{Prefix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Suffix}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{OriginalName}", lMergeGroupName, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionNumber}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{SectionName}", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", lMergeStamp.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", lMergeStamp.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        lMergeStemName = LEncoding.LEncodingShorten(lMergeStemName);

        string lMergeBaseName = LMergeNameNormalize(lMergeStemName);
        string lMergeUniqueName = lMergeBaseName;
        int lMergeAttempt = 2;
        while (!lMergeTakenNames.Add(lMergeUniqueName))
        {
            lMergeUniqueName = $"{lMergeBaseName}_{lMergeAttempt}";
            lMergeAttempt++;
        }

        string lMergeExtension = lMergeOutput.LEncodingExtensionResolve(lMergeBasePath);
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
