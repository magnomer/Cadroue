using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// When a fix tab's clinic plan is saved, restored or read. CFix makes one over its own port: each plan gate takes the
// tab's clinic.
public sealed class CFixPlan
{
    private readonly LFixPort _cFixPlanPort;

    internal CFixPlan(LFixPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cFixPlanPort = port;
    }

    // The clinic's plan is saved to the open source, its persistent part to every unlocked item, unless saves are
    // held, no source is open or the source's item, found ignoring case, is locked. Each step and the salvage are
    // answered with whether the plan has them on.
    public IReadOnlyList<(string CFixStep, bool CFixActive)> CFixStateSave(
        CClinic clinic,
        CWorkspace workspace,
        IReadOnlyList<(string CFixPath, Guid CFixBatch, bool CFixLocked)> items)
    {
        ArgumentNullException.ThrowIfNull(clinic);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(items);
        LClinicPort plan = clinic.CClinicSaveSuspended
            || workspace.CWorkspaceSource is not { } source
            || CDocket.CDocketFind(items, source)?.CDocketLocked == true
                ? _cFixPlanPort.LFixPlanCreate(clinic.CClinicPlanApply)
                : _cFixPlanPort.LFixStateSave(
                    clinic.CClinicPlanApply,
                    source,
                    CDocket.CDocketUnlockedRead(items, null).Select(item => item.CDocketPath).ToArray());
        return CFixActiveCreate(plan);
    }

    // Before the workspace opens a new path, the plan of the source it leaves is saved; then the clinic follows.
    public void CFixSourceOpen(
        CClinic clinic,
        CWorkspace workspace,
        IReadOnlyList<(string CFixPath, Guid CFixBatch, bool CFixLocked)> items,
        string path)
    {
        _ = CFixStateSave(clinic, workspace, items);
        clinic.CClinicSourceSet(path);
    }

    // Added items take the persistent part of the plan, unless saves are held.
    public void CFixPersistentSave(CClinic clinic, IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(clinic);
        ArgumentNullException.ThrowIfNull(paths);
        if (clinic.CClinicSaveSuspended)
        {
            return;
        }

        _cFixPlanPort.LFixPersistentSave(clinic.CClinicPlanApply, paths);
    }

    // The clinic takes the plan saved for the source under its own persistent part; the steps are answered as by
    // CFixStateSave.
    public IReadOnlyList<(string CFixStep, bool CFixActive)> CFixPlanRestore(CClinic clinic, string sourcePath)
    {
        ArgumentNullException.ThrowIfNull(clinic);
        LClinicPort plan = _cFixPlanPort.LFixPlanRestore(clinic.CClinicPlanApply, sourcePath);
        clinic.CClinicPlanAccept(plan);
        return CFixActiveCreate(plan);
    }

    // A tab without a layout minimizes the clinic; a layout's persistent plan is taken as it comes. The steps are
    // answered as by CFixStateSave.
    public IReadOnlyList<(string CFixStep, bool CFixActive)> CFixLayoutApply(
        CClinic clinic, bool layoutMissing, LClinicPort? plan)
    {
        ArgumentNullException.ThrowIfNull(clinic);
        if (layoutMissing)
        {
            clinic.CClinicMinimizedSet(true);
        }
        else if (plan is not null)
        {
            clinic.CClinicPlanAccept(plan);
        }

        return CFixActiveCreate(_cFixPlanPort.LFixPlanCreate(clinic.CClinicPlanApply));
    }

    // A step is on when the plan repairs its kind; the salvage, when it is active.
    private static IReadOnlyList<(string CFixStep, bool CFixActive)> CFixActiveCreate(LClinicPort plan)
    {
        var answer = plan.LClinicPlanRead();
        return answer.LClinicSteps
            .Select(step => (step.LClinicKind, step.LClinicRepair))
            .Append((CClinic.CClinicSalvageStep, answer.LClinicActive))
            .ToArray();
    }
}
