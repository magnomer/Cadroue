using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The fix runs' decisions: whether the preset lets a run go, then which docket items run; and when a tab's clinic
// plan is saved, restored or read. One per atelier: each plan gate takes the tab's clinic.
public sealed class CFix
{
    private readonly LFixPort _cFixPort;

    internal CFix(LFixPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cFixPort = port;
    }

    // The single run: the first item whose path is the current one, ignoring case, runs while unlocked. Without a valid
    // preset nothing runs and false is answered; otherwise the port is asked once, even with nothing picked.
    public bool CFixRun(
        bool presetValid,
        string presetName,
        string? path,
        IReadOnlyList<(string CFixPath, Guid CFixBatch, bool CFixLocked)> items,
        bool urgent,
        Guid relayTarget,
        Guid sourceTab)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (!presetValid)
        {
            return false;
        }

        _cFixPort.LFixRun(
            presetName,
            items
                .Where(item => path is not null
                    && string.Equals(item.CFixPath, path, StringComparison.OrdinalIgnoreCase))
                .Take(1)
                .Where(item => !item.CFixLocked)
                .Select(item => (item.CFixPath, item.CFixBatch))
                .ToArray(),
            urgent,
            relayTarget,
            sourceTab);
        return true;
    }

    // Every unlocked item runs, or, given paths, every unlocked item whose path is among them, in docket order, at
    // normal priority. Without a valid preset nothing runs and false is answered.
    public bool CFixAllRun(
        bool presetValid,
        string presetName,
        IReadOnlyList<(string CFixPath, Guid CFixBatch, bool CFixLocked)> items,
        IReadOnlyList<string>? paths,
        Guid relayTarget,
        Guid sourceTab)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (!presetValid)
        {
            return false;
        }

        _cFixPort.LFixRun(
            presetName,
            items
                .Where(item => !item.CFixLocked
                    && (paths is null || paths.Contains(item.CFixPath, StringComparer.OrdinalIgnoreCase)))
                .Select(item => (item.CFixPath, item.CFixBatch))
                .ToArray(),
            false,
            relayTarget,
            sourceTab);
        return true;
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
            || CFixLockCheck(items, source)
                ? _cFixPort.LFixPlanCreate(clinic.CClinicPlanApply)
                : _cFixPort.LFixStateSave(
                    clinic.CClinicPlanApply,
                    source,
                    items.Where(item => !item.CFixLocked).Select(item => item.CFixPath).ToArray());
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

        _cFixPort.LFixPersistentSave(clinic.CClinicPlanApply, paths);
    }

    // The clinic takes the plan saved for the source under its own persistent part; the steps are answered as by
    // CFixStateSave.
    public IReadOnlyList<(string CFixStep, bool CFixActive)> CFixPlanRestore(CClinic clinic, string sourcePath)
    {
        ArgumentNullException.ThrowIfNull(clinic);
        LClinicPort plan = _cFixPort.LFixPlanRestore(clinic.CClinicPlanApply, sourcePath);
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

        return CFixActiveCreate(_cFixPort.LFixPlanCreate(clinic.CClinicPlanApply));
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

    private static bool CFixLockCheck(
        IReadOnlyList<(string CFixPath, Guid CFixBatch, bool CFixLocked)> items, string path) =>
        items.FirstOrDefault(item => string.Equals(item.CFixPath, path, StringComparison.OrdinalIgnoreCase))
            .CFixLocked;
}
