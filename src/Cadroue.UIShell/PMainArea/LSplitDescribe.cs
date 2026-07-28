using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public static partial class LSplit
{
    public static int LSplitDescribe(
        LWorkPriority lWorkPriority,
        string? lSplitSourcePath,
        IReadOnlyList<LSplitSectionDescription> lSplitSections,
        LExportSpecificState lExportSpecificState)
    {
        LSplitWorkDescription lSplitWorkDescription = new(
            lSplitSourcePath,
            lSplitSections,
            lExportSpecificState.LPresetOutputCreate());

        return LSplit.LSplitInterpret(lWorkPriority, lSplitWorkDescription);
    }

    public static async Task<int> LSplitAllDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<string> lSplitSourcePaths,
        LExportSpecificState lExportSpecificState)
    {
        IReadOnlyList<LSplitPlanRecord> lSplitPlans =
            await Task.Run(() => LSplitPlanCollect(lSplitSourcePaths)).ConfigureAwait(true);

        int lSplitAdded = 0;
        foreach (LSplitPlanRecord lSplitPlan in lSplitPlans)
        {
            lSplitAdded += LSplitDescribe(
                lWorkPriority,
                lSplitPlan.LSplitPlanSourcePath,
                lSplitPlan.LSplitPlanSections,
                lExportSpecificState);
        }

        return lSplitAdded;
    }

    private static IReadOnlyList<LSplitPlanRecord> LSplitPlanCollect(IReadOnlyList<string> lSplitSourcePaths)
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

            return lSplitSidecar.Sections
                .Where(lSplitRecord => lSplitRecord.EndMilliseconds > lSplitRecord.StartMilliseconds)
                .Select(lSplitRecord => new LSplitSectionDescription(
                    TimeSpan.FromMilliseconds(lSplitRecord.StartMilliseconds),
                    TimeSpan.FromMilliseconds(lSplitRecord.EndMilliseconds),
                    lSplitRecord.Name,
                    lSplitRecord.Prefix,
                    lSplitRecord.Suffix))
                .ToArray();
        }
        catch (Exception lSplitException)
        {
            LAppLog.LError($"Split plan could not be read for '{lSplitSourcePath}'", lSplitException);
            return Array.Empty<LSplitSectionDescription>();
        }
    }
}
