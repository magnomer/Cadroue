using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The fix dispatch: the preset's encoding is read by its name, the source records and the priority are built here;
// which sources run, and whether any run at all, is the gate's. The fix plan is built here from what the clinic
// writes into an empty plan record, and saved to or read from each source's sidecar.
public sealed class LFixOutlet : LFixPort
{
    public void LFixRun(
        string lPresetName,
        IReadOnlyList<(string LFixPath, Guid LFixBatch)> lSources,
        bool lUrgent,
        Guid lRelayTarget,
        Guid lSourceTab) =>
        LMessenger.LMessengerFixDescribe(
            lUrgent ? LWorkPriority.LWorkPriorityHigh : LWorkPriority.LWorkPriorityNormal,
            lSources.Select(lSource => new LWorkSource(lSource.LFixPath, lSource.LFixBatch)).ToArray(),
            LPresetSelection.LPresetEncodingRead(lPresetName),
            lRelayTarget,
            lSourceTab);

    public LClinicPort LFixPlanCreate(Action<LClinicPort> lPlanApply) => new LClinicOutlet(LFixWorkCreate(lPlanApply));

    public LClinicPort LFixStateSave(
        Action<LClinicPort> lPlanApply, string lSourcePath, IReadOnlyList<string> lPaths)
    {
        LWorkFix lPlan = LFixWorkCreate(lPlanApply);
        if (lPlan.LWorkFixActive || LFix.LFixPlanRead(lSourcePath, LLibrarian.LLibrarianFixLoad) is not null)
        {
            LFix.LFixPlanSave(lSourcePath, lPlan, LLibrarian.LLibrarianFixSave);
            LFixPersistentSave(lPlan, lPaths);
        }

        return new LClinicOutlet(lPlan);
    }

    public void LFixPersistentSave(Action<LClinicPort> lPlanApply, IReadOnlyList<string> lPaths) =>
        LFixPersistentSave(LFixWorkCreate(lPlanApply), lPaths);

    public LClinicPort LFixPlanRestore(Action<LClinicPort> lPlanApply, string lSourcePath)
    {
        LWorkFix? lSaved = LFix.LFixPlanRead(lSourcePath, LLibrarian.LLibrarianFixLoad);
        LWorkFix lPersistent = LFix.LFixPersistentResolve(LFixWorkCreate(lPlanApply));
        return new LClinicOutlet(LFix.LFixPlanResolve(lSaved, lPersistent));
    }

    private static void LFixPersistentSave(LWorkFix lPlan, IReadOnlyList<string> lPaths)
    {
        LWorkFix lPersistent = LFix.LFixPersistentResolve(lPlan);
        if (!lPersistent.LWorkFixSteps.Any())
        {
            return;
        }

        foreach (string lPath in lPaths)
        {
            LWorkFix? lSaved = LFix.LFixPlanRead(lPath, LLibrarian.LLibrarianFixLoad);
            LFix.LFixPlanSave(lPath, LFix.LFixPlanResolve(lSaved, lPersistent), LLibrarian.LLibrarianFixSave);
        }
    }

    private static LWorkFix LFixWorkCreate(Action<LClinicPort> lPlanApply)
    {
        ArgumentNullException.ThrowIfNull(lPlanApply);
        LClinicOutlet lPlan = new();
        lPlanApply(lPlan);
        return lPlan.LClinicPlan;
    }
}
