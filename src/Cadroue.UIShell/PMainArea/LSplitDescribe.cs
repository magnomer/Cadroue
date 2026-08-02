using System.IO;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public static partial class LSplit
{
    public static int LSplitDescribe(
        LWorkPriority lWorkPriority,
        string? lSplitSourcePath,
        IReadOnlyList<LSplitSectionDescription> lSplitSections,
        LPreset lExportSpecificState,
        Guid lSplitRelayTarget = default,
        Guid lSplitRelaySource = default,
        Guid lSplitBatchId = default)
    {
        LSplitWorkDescription lSplitWorkDescription = new(
            lSplitSourcePath,
            lSplitSections,
            lExportSpecificState.LPresetOutputCreate());

        string lSplitTab = PControlBar.LTabset.LTabsetTitleRead(lSplitRelaySource);
        IReadOnlyList<LWorkItem> lSplitWorkItems = Cadroue.Application.LSplit.LSplitItemsCreate(
            lWorkPriority,
            lSplitWorkDescription,
            lSplitTab,
            lSplitMessage => LTraceLog.LTraceInfoRecord(lSplitMessage),
            lSplitMessage => LTraceLog.LTraceErrorRecord(lSplitMessage),
            lSplitBatchId);
        if (lSplitWorkItems.Count == 0)
        {
            return 0;
        }

        int lSplitAdded = PProgram.LScheduleCurrent.LScheduleAdd(
            lSplitWorkItems, lSplitRelayTarget, lSplitRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Split queued {lSplitAdded} of {lSplitWorkItems.Count} job(s) at {lWorkPriority} " +
            $"from '{Path.GetFileName(lSplitSourcePath)}'");
        return lSplitAdded;
    }

    public static async Task<int> LSplitAllDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<LWorkSource> lSplitSources,
        LPreset lExportSpecificState,
        Guid lSplitRelayTarget = default,
        Guid lSplitRelaySource = default)
    {
        string[] lSplitSourcePaths = lSplitSources
            .Select(lSplitSource => lSplitSource.LWorkSourcePath)
            .ToArray();
        var lSplitRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkSource lSplitSource in lSplitSources)
        {
            lSplitRelays[lSplitSource.LWorkSourcePath] = lSplitSource.LWorkSourceBatch;
        }

        IReadOnlyList<LSplitPlanRecord> lSplitPlans =
            await Task.Run(() => LSplitPlanCreate(lSplitSourcePaths)).ConfigureAwait(true);

        int lSplitAdded = 0;
        foreach (LSplitPlanRecord lSplitPlan in lSplitPlans)
        {
            lSplitRelays.TryGetValue(lSplitPlan.LSplitSourcePath, out Guid lSplitBatch);
            lSplitAdded += LSplitDescribe(
                lWorkPriority,
                lSplitPlan.LSplitSourcePath,
                lSplitPlan.LSplitPlanSections,
                lExportSpecificState,
                lSplitRelayTarget,
                lSplitRelaySource,
                lSplitBatch);
        }

        return lSplitAdded;
    }

    private static IReadOnlyList<LSplitPlanRecord> LSplitPlanCreate(IReadOnlyList<string> lSplitSourcePaths)
    {
        var lSplitPlans = new List<LSplitPlanRecord>();
        foreach (string lSplitSourcePath in lSplitSourcePaths)
        {
            IReadOnlyList<LSplitSectionDescription> lSplitSections = LSplitPlanRead(lSplitSourcePath);
            if (lSplitSections.Count > 0)
            {
                lSplitPlans.Add(new LSplitPlanRecord(lSplitSourcePath, lSplitSections));
            }
        }

        return lSplitPlans;
    }

    public static IReadOnlyList<LSplitSectionDescription> LSplitPlanRead(string lSplitSourcePath)
    {
        try
        {
            string lSplitSidecarPath = Cadroue.Media.LSidecarStore.LSidecarPathRead(lSplitSourcePath);
            if (Cadroue.Media.LSidecarStore.LSidecarRead(lSplitSidecarPath) is not { } lSplitSidecar)
            {
                return Array.Empty<LSplitSectionDescription>();
            }

            return lSplitSidecar.LSidecarSections
                .Where(lSplitRecord => lSplitRecord.LSidecarEndMilliseconds > lSplitRecord.LSidecarStartMilliseconds)
                .Select(lSplitRecord => new LSplitSectionDescription(
                    TimeSpan.FromMilliseconds(lSplitRecord.LSidecarStartMilliseconds),
                    TimeSpan.FromMilliseconds(lSplitRecord.LSidecarEndMilliseconds),
                    lSplitRecord.LSidecarName,
                    lSplitRecord.LSidecarPrefix,
                    lSplitRecord.LSidecarSuffix,
                    lSplitRecord.LSidecarHidden))
                .ToArray();
        }
        catch (Exception lSplitException)
        {
            LTraceLog.LTraceErrorRecord($"Split plan could not be read for '{lSplitSourcePath}'", lSplitException);
            return Array.Empty<LSplitSectionDescription>();
        }
    }
}
