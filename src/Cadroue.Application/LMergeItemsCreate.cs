using Cadroue.Core;

namespace Cadroue.Application;

public static class LMerge
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
        Guid lMergeLooseBatch = LGate.LGateBatchCreate();
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
            string lMergeFolder = LEncodingPath.LEncodingFolderRead(lMergeOutput, lMergeBasePath);
            string lMergeName = LMergeName.LMergeNameCreate(
                lMergeOutput,
                lMergeBasePath,
                lMergeGroup.LWorkGroupName,
                lMergeStamp,
                lMergeTakenNames);
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
                $"Merge built job '{lMergeItem.LWorkOutputName}': {lMergeItem.LWorkMergeSources.Count} file(s) " +
                $"[batch {lMergeItem.LWorkBatchId:N}]");
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
}
