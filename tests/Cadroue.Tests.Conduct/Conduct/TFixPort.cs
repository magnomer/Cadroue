using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;
using Cadroue.Tests.Interface;

namespace Cadroue.Tests;

// Records each ask; every plan member fills a fresh plan record from the clinic and answers it, except restore,
// which answers TFixRestored when a test sets one.
internal sealed class TFixPort : LFixPort
{
    public List<string> TFixCalls { get; } = [];

    public LClinicPort? TFixRestored { get; set; }

    public void LFixRun(
        string lPresetName,
        IReadOnlyList<(string LFixPath, Guid LFixBatch)> lSources,
        bool lUrgent,
        Guid lRelayTarget,
        Guid lSourceTab) =>
        TFixCalls.Add(
            $"{lPresetName}|"
            + string.Join(",", lSources.Select(lSource => $"{lSource.LFixPath}@{lSource.LFixBatch:N}"))
            + $"|{(lUrgent ? "urgent" : "normal")}|{lRelayTarget}|{lSourceTab}");

    public LClinicPort LFixPlanCreate(Action<LClinicPort> lPlanApply)
    {
        TFixCalls.Add("create");
        return TFixPlanCreate(lPlanApply);
    }

    public LClinicPort LFixStateSave(Action<LClinicPort> lPlanApply, string lSourcePath, IReadOnlyList<string> lPaths)
    {
        TFixCalls.Add($"save|{lSourcePath}|{string.Join(",", lPaths)}");
        return TFixPlanCreate(lPlanApply);
    }

    public void LFixPersistentSave(Action<LClinicPort> lPlanApply, IReadOnlyList<string> lPaths)
    {
        TFixCalls.Add($"persistent|{string.Join(",", lPaths)}");
        _ = TFixPlanCreate(lPlanApply);
    }

    public LClinicPort LFixPlanRestore(Action<LClinicPort> lPlanApply, string lSourcePath)
    {
        TFixCalls.Add($"restore|{lSourcePath}");
        LClinicPort lPlan = TFixPlanCreate(lPlanApply);
        return TFixRestored ?? lPlan;
    }

    private static LClinicPort TFixPlanCreate(Action<LClinicPort> lPlanApply)
    {
        LClinicPort lPlan = TInterface.TClinicOutletCreate();
        lPlanApply(lPlan);
        return lPlan;
    }
}
