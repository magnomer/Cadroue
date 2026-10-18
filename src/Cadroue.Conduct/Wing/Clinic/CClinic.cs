using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The clinic's decisions for one fix tab's panel: the shown source and step, what the panel shows for them, the
// minimized panel and the save hold, and the one notice of each change. The plan is CClinicPlan's and the checkup
// results CClinicCheckup's; this gate aims their changes at the shown step and tells them. Flaw kinds travel by name;
// the engine supplies them, in order, with the default plan.
public sealed class CClinic
{
    public const string CClinicSalvageStep = "Salvage";

    private readonly CClinicPlan _cClinicPlan;
    private readonly CClinicCheckup _cClinicCheckup = new();
    private string? _cClinicSource;
    private string? _cClinicStep;
    private string? _cClinicKind;
    private bool _cClinicMinimized;
    private int _cClinicSaveDepth;

    internal CClinic(LClinicPort plan) => _cClinicPlan = new CClinicPlan(plan);

    public event Action? CClinicChanged;
    public event Action? CClinicPlanChanged;
    public event Action<bool>? CClinicMinimizeChanged;

    public string? CClinicSource => _cClinicSource;

    public string? CClinicStep => _cClinicStep;

    public string? CClinicKind => _cClinicKind;

    public bool CClinicSalvageShown => _cClinicStep == CClinicSalvageStep;

    public bool CClinicStepShown => _cClinicKind is not null;

    public bool CClinicKnown => CClinicSalvageShown || CClinicStepShown;

    public bool CClinicPersistentAllowed => CClinicKnown && !CClinicSalvageShown;

    public bool CClinicRepairChecked =>
        _cClinicKind is { } kind && _cClinicPlan.CClinicPlanRead(kind) is { CClinicRepair: true };

    public bool CClinicPersistentChecked =>
        _cClinicKind is { } kind && _cClinicPlan.CClinicPlanRead(kind) is { CClinicPersistent: true };

    public bool CClinicResultShown => !CClinicSalvageShown && CClinicStepShown;

    public bool CClinicScanShown => CClinicResultShown && CClinicScanCheck();

    public double CClinicProgressValue => CClinicScanShown ? CClinicProgressRead() : 0;

    public bool CClinicMinimized => _cClinicMinimized;

    public bool CClinicSaveSuspended => _cClinicSaveDepth > 0;

    // The header names the shown step, or the clinic when none is known.
    public string CClinicTitleKey => CClinicKnown ? $"Processing.Step.{_cClinicStep}" : "Clinic.Header.Title";

    // A step's two explanations; none when no step is known.
    public string? CClinicSimpleKey => CClinicKnown ? $"Clinic.Step.{_cClinicStep}.Simple" : null;

    public string? CClinicTechnicalKey => CClinicKnown ? $"Clinic.Step.{_cClinicStep}.Technical" : null;

    public bool CClinicScanCheck() =>
        _cClinicCheckup.CClinicResultFind(_cClinicSource, _cClinicKind) is { CClinicScanning: true };

    public double CClinicProgressRead() => _cClinicCheckup.CClinicProgressRead(_cClinicSource);

    // The salvage panel in one answer: shown on the salvage step, with the plan's salvage switches.
    public (bool CClinicShown,
        bool CClinicActive,
        bool CClinicPersistent,
        bool CClinicSeparate,
        bool CClinicFixed,
        bool CClinicFixable,
        string CClinicDescription)
        CClinicSalvageRead()
    {
        var salvage = _cClinicPlan.CClinicSalvageRead();
        return (CClinicSalvageShown,
            salvage.CClinicActive,
            salvage.CClinicPersistent,
            salvage.CClinicSeparate,
            salvage.CClinicFixed,
            salvage.CClinicFixable,
            salvage.CClinicDescription);
    }

    // The wording runs untested, scanning, clean, failed, then the defect, evidence and repair labels; a step with
    // no result reads as untested.
    public string CClinicResultFormat(IReadOnlyList<string> wording)
    {
        ArgumentNullException.ThrowIfNull(wording);
        return _cClinicCheckup.CClinicResultFind(_cClinicSource, _cClinicKind) is { } stored
            ? stored.CClinicResult.LCheckupFormat(wording)
            : wording[0];
    }

    public void CClinicMinimizedSet(bool minimized)
    {
        if (_cClinicMinimized == minimized)
        {
            return;
        }

        _cClinicMinimized = minimized;
        CClinicMinimizeChanged?.Invoke(minimized);
    }

    public void CClinicSaveSuspend() => _cClinicSaveDepth++;

    public void CClinicSaveResume() => _cClinicSaveDepth = Math.Max(0, _cClinicSaveDepth - 1);

    public void CClinicSourceSet(string? sourcePath)
    {
        _cClinicSource = string.IsNullOrWhiteSpace(sourcePath) ? null : sourcePath;
        CClinicChanged?.Invoke();
    }

    public void CClinicStepSet(string? stepName)
    {
        _cClinicStep = stepName;
        _cClinicKind = stepName is not null && _cClinicPlan.CClinicPlanRead(stepName) is not null ? stepName : null;
        CClinicChanged?.Invoke();
    }

    public void CClinicRepairSet(bool repair) =>
        CClinicPlanRaise(_cClinicKind is { } kind && _cClinicPlan.CClinicRepairSet(kind, repair));

    public void CClinicPersistentSet(bool persistent) =>
        CClinicPlanRaise(_cClinicKind is { } kind && _cClinicPlan.CClinicPersistentSet(kind, persistent));

    public void CSalvageActiveSet(bool active) => CClinicPlanRaise(_cClinicPlan.CSalvageActiveSet(active));

    public void CSalvagePersistentSet(bool persistent) =>
        CClinicPlanRaise(_cClinicPlan.CSalvagePersistentSet(persistent));

    public void CSalvageModeSet(bool separate) => CClinicPlanRaise(_cClinicPlan.CSalvageModeSet(separate));

    public void CSalvageBasisSet(bool fixedBasis) => CClinicPlanRaise(_cClinicPlan.CSalvageBasisSet(fixedBasis));

    // An accepted plan is no user change, so saves hold until it is told.
    public void CClinicPlanAccept(LClinicPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        CClinicSaveSuspend();
        try
        {
            _cClinicPlan.CClinicPlanAccept(plan);
            CClinicChanged?.Invoke();
        }
        finally
        {
            CClinicSaveResume();
        }
    }

    public void CClinicPlanApply(LClinicPort plan) => _cClinicPlan.CClinicPlanApply(plan);

    // A result is told when it is the shown source's result of the shown kind.
    public void CClinicResultAccept(LCheckupPort result)
    {
        (string source, string kind) = _cClinicCheckup.CClinicResultAccept(result);
        if (CClinicSourceMatch(source) && _cClinicKind == kind)
        {
            CClinicChanged?.Invoke();
        }
    }

    public void CClinicResultsRemove(IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        bool shown = false;
        foreach (string path in paths)
        {
            shown |= _cClinicCheckup.CClinicResultRemove(path) && CClinicSourceMatch(path);
        }

        if (shown)
        {
            CClinicChanged?.Invoke();
        }
    }

    public void CClinicProgressSet(string path, double value)
    {
        _cClinicCheckup.CClinicProgressSet(path, value);
        if (CClinicSourceMatch(path))
        {
            CClinicChanged?.Invoke();
        }
    }

    // A user change to the plan repaints the panel and asks for a save.
    private void CClinicPlanRaise(bool changed)
    {
        if (!changed)
        {
            return;
        }

        CClinicChanged?.Invoke();
        CClinicPlanChanged?.Invoke();
    }

    private bool CClinicSourceMatch(string path) =>
        string.Equals(path, _cClinicSource, StringComparison.OrdinalIgnoreCase);
}
