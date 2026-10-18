using Cadroue.Core;

namespace Cadroue.Application;

public static class LFix
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

            string lFixFolder = LEncodingPath.LEncodingFolderRead(lFixOutput, lFixSourcePath);
            string lFixOutputName = LFixName.LFixNameCreate(lFixOutput, lFixSourcePath, lFixFolder, lFixDuration);

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
}
