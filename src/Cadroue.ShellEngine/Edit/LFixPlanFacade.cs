using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The fix plan's saves for the plan gate: the clinic writes into a fresh plan record, the sidecars keep the plan, and
// each answer is a record holding the plan saved or restored.
public sealed class LFixPlanFacade : LFixPlanPort
{
    internal LFixPlanFacade()
    {
    }

    public LClinicPort LFixPlanCreate(Action<LClinicPort> lPlanApply) =>
        new LClinicOutlet(LClinicOutlet.LClinicOutletCreate(lPlanApply).LClinicPlan);

    public LClinicPort LFixStateSave(
        Action<LClinicPort> lPlanApply, string lSourcePath, IReadOnlyList<string> lPaths)
    {
        LWorkFix lPlan = LClinicOutlet.LClinicOutletCreate(lPlanApply).LClinicPlan;
        LFixSidecar.LFixStateSave(lPlan, lSourcePath, lPaths);
        return new LClinicOutlet(lPlan);
    }

    public void LFixPersistentSave(Action<LClinicPort> lPlanApply, IReadOnlyList<string> lPaths) =>
        LFixSidecar.LFixPersistentSave(LClinicOutlet.LClinicOutletCreate(lPlanApply).LClinicPlan, lPaths);

    public LClinicPort LFixPlanRestore(Action<LClinicPort> lPlanApply, string lSourcePath) =>
        new LClinicOutlet(LFixSidecar.LFixSidecarRestore(
            lSourcePath, () => LClinicOutlet.LClinicOutletCreate(lPlanApply).LClinicPlan));
}
