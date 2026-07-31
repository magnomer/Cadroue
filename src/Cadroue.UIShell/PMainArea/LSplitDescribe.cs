using Cadroue.Core;
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
        Guid lSplitRelaySource = default)
    {
        LSplitWorkDescription lSplitWorkDescription = new(
            lSplitSourcePath,
            lSplitSections,
            lExportSpecificState.LPresetOutputCreate());

        return LSplit.LSplitInterpret(lWorkPriority, lSplitWorkDescription, lSplitRelayTarget, lSplitRelaySource);
    }

    public static async Task<int> LSplitAllDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<string> lSplitSourcePaths,
        LPreset lExportSpecificState,
        Guid lSplitRelayTarget = default,
        Guid lSplitRelaySource = default)
    {
        IReadOnlyList<LSplitPlanRecord> lSplitPlans =
            await Task.Run(() => LSplitPlanCreate(lSplitSourcePaths)).ConfigureAwait(true);

        int lSplitAdded = 0;
        foreach (LSplitPlanRecord lSplitPlan in lSplitPlans)
        {
            lSplitAdded += LSplitDescribe(
                lWorkPriority,
                lSplitPlan.LSplitPlanSourcePath,
                lSplitPlan.LSplitPlanSections,
                lExportSpecificState,
                lSplitRelayTarget,
                lSplitRelaySource);
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
