namespace Cadroue.ShellEngine;

public interface LFixPort
{
    void LFixRun(
        string lPresetName,
        IReadOnlyList<(string LFixPath, Guid LFixBatch)> lSources,
        bool lUrgent,
        Guid lRelayTarget,
        Guid lSourceTab);

    // Each plan member below hands lPlanApply a fresh plan record to fill; the answered record holds that plan.
    LClinicPort LFixPlanCreate(Action<LClinicPort> lPlanApply);

    // The plan is saved to the source unless it is off and the source has none saved, then its persistent part to
    // every path.
    LClinicPort LFixStateSave(Action<LClinicPort> lPlanApply, string lSourcePath, IReadOnlyList<string> lPaths);

    // The persistent part of the plan is merged into the plan saved for every path, when it holds a step.
    void LFixPersistentSave(Action<LClinicPort> lPlanApply, IReadOnlyList<string> lPaths);

    // The plan saved for the source under the persistent part of the plan; the record answers the merged plan.
    LClinicPort LFixPlanRestore(Action<LClinicPort> lPlanApply, string lSourcePath);
}
