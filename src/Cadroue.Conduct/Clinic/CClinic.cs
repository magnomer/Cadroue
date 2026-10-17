using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The clinic's decisions for one fix tab: each flaw kind's repair and keep switches, the salvage choices, the shown
// source and step, every source's checkup results and progress, the minimized panel and the save hold. Flaw kinds
// travel by name; the engine supplies them, in order, with the default plan.
public sealed class CClinic
{
    public const string CClinicSalvageStep = "Salvage";

    private readonly IReadOnlyList<string> _cClinicKinds;
    private readonly Dictionary<string, (bool CClinicRepair, bool CClinicPersistent)> _cClinicSteps =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, (LClinicPort CClinicResult, bool CClinicScanning)>>
        _cClinicResults = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, double> _cClinicProgress = new(StringComparer.OrdinalIgnoreCase);
    private bool _cClinicSalvageActive;
    private bool _cClinicSalvageSeparate;
    private bool _cClinicSalvageFixed;
    private bool _cClinicSalvagePersistent;
    private string? _cClinicSource;
    private string? _cClinicStep;
    private string? _cClinicKind;
    private bool _cClinicMinimized;
    private int _cClinicSaveDepth;

    internal CClinic(LClinicPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var answer = plan.LClinicPlanRead();
        _cClinicKinds = answer.LClinicSteps.Select(step => step.LClinicKind).ToArray();
        foreach ((string kind, bool repair, bool persistent) in answer.LClinicSteps)
        {
            _cClinicSteps[kind] = (repair, persistent);
        }

        _cClinicSalvageActive = answer.LClinicActive;
        _cClinicSalvageSeparate = answer.LClinicSeparate;
        _cClinicSalvageFixed = answer.LClinicFixed;
        _cClinicSalvagePersistent = answer.LClinicPersistent;
    }

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

    public bool CClinicRepairChecked => _cClinicKind is { } kind && _cClinicSteps[kind].CClinicRepair;

    public bool CClinicPersistentChecked => _cClinicKind is { } kind && _cClinicSteps[kind].CClinicPersistent;

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

    public bool CClinicRepairCheck() => _cClinicSteps.Values.Any(step => step.CClinicRepair);

    public bool CClinicScanCheck() => CClinicResultFind() is { CClinicScanning: true };

    public double CClinicProgressRead() =>
        _cClinicSource is { } source && _cClinicProgress.TryGetValue(source, out double value) ? value : 0;

    // The salvage panel in one answer: the fixed basis needs a repair, and the description follows the basis.
    public (bool CClinicShown,
        bool CClinicActive,
        bool CClinicPersistent,
        bool CClinicSeparate,
        bool CClinicFixed,
        bool CClinicFixable,
        string CClinicDescription)
        CClinicSalvageRead()
    {
        bool fixable = CClinicRepairCheck();
        string description = !fixable
            ? "Clinic.Salvage.Basis.None"
            : _cClinicSalvageFixed
                ? "Clinic.Salvage.Basis.Fixed.Description"
                : "Clinic.Salvage.Basis.Source.Description";
        return (CClinicSalvageShown,
            _cClinicSalvageActive,
            _cClinicSalvagePersistent,
            _cClinicSalvageSeparate,
            _cClinicSalvageFixed,
            fixable,
            description);
    }

    // The wording runs untested, scanning, clean, failed, then the defect, evidence and repair labels; a step with
    // no result reads as untested.
    public string CClinicResultFormat(IReadOnlyList<string> wording)
    {
        ArgumentNullException.ThrowIfNull(wording);
        return CClinicResultFind() is { } stored ? stored.CClinicResult.LClinicResultFormat(wording) : wording[0];
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
        _cClinicKind = stepName is not null && _cClinicSteps.ContainsKey(stepName) ? stepName : null;
        CClinicChanged?.Invoke();
    }

    public void CClinicRepairSet(bool repair)
    {
        if (_cClinicKind is not { } kind || _cClinicSteps[kind].CClinicRepair == repair)
        {
            return;
        }

        _cClinicSteps[kind] = _cClinicSteps[kind] with { CClinicRepair = repair };
        _cClinicSalvageFixed = CClinicFixedNormalize(_cClinicSalvageFixed);
        CClinicChanged?.Invoke();
        CClinicPlanChanged?.Invoke();
    }

    public void CClinicPersistentSet(bool persistent)
    {
        if (_cClinicKind is not { } kind || _cClinicSteps[kind].CClinicPersistent == persistent)
        {
            return;
        }

        _cClinicSteps[kind] = _cClinicSteps[kind] with { CClinicPersistent = persistent };
        CClinicChanged?.Invoke();
        CClinicPlanChanged?.Invoke();
    }

    public void CSalvageActiveSet(bool active) => CClinicSalvageApply(
        active, _cClinicSalvageSeparate, _cClinicSalvageFixed, _cClinicSalvagePersistent);

    public void CSalvagePersistentSet(bool persistent) => CClinicSalvageApply(
        _cClinicSalvageActive, _cClinicSalvageSeparate, _cClinicSalvageFixed, persistent);

    public void CSalvageModeSet(bool separate) => CClinicSalvageApply(
        _cClinicSalvageActive, separate, _cClinicSalvageFixed, _cClinicSalvagePersistent);

    public void CSalvageBasisSet(bool fixedBasis) => CClinicSalvageApply(
        _cClinicSalvageActive, _cClinicSalvageSeparate, fixedBasis, _cClinicSalvagePersistent);

    // A plan sets the steps it lists and the salvage; the fixed basis falls back without a repair. An accepted plan
    // is no user change, so saves hold until it is told.
    public void CClinicPlanAccept(LClinicPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        CClinicSaveSuspend();
        try
        {
            var answer = plan.LClinicPlanRead();
            foreach ((string kind, bool repair, bool persistent) in answer.LClinicSteps)
            {
                _cClinicSteps[kind] = (repair, persistent);
            }

            _cClinicSalvageActive = answer.LClinicActive;
            _cClinicSalvageSeparate = answer.LClinicSeparate;
            _cClinicSalvageFixed = CClinicFixedNormalize(answer.LClinicFixed);
            _cClinicSalvagePersistent = answer.LClinicPersistent;
            CClinicChanged?.Invoke();
        }
        finally
        {
            CClinicSaveResume();
        }
    }

    // The engine builds the plan: every kind in the engine's order, then the salvage.
    public void CClinicPlanApply(LClinicPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.LClinicPlanSet(
            _cClinicKinds
                .Select(kind => (kind, _cClinicSteps[kind].CClinicRepair, _cClinicSteps[kind].CClinicPersistent))
                .ToArray(),
            _cClinicSalvageActive,
            _cClinicSalvageSeparate,
            _cClinicSalvageFixed,
            _cClinicSalvagePersistent);
    }

    // A result replaces its source's result of that kind; a scan restarts the progress, any other outcome ends it.
    public void CClinicResultAccept(LClinicPort result)
    {
        ArgumentNullException.ThrowIfNull(result);
        (string source, string kind, bool scanning) = result.LClinicResultRead();
        if (!_cClinicResults.TryGetValue(source, out Dictionary<string, (LClinicPort, bool)>? kinds))
        {
            kinds = new Dictionary<string, (LClinicPort, bool)>(StringComparer.Ordinal);
            _cClinicResults[source] = kinds;
        }

        kinds[kind] = (result, scanning);
        if (scanning)
        {
            _cClinicProgress[source] = 0;
        }
        else
        {
            _cClinicProgress.Remove(source);
        }

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
            shown |= _cClinicResults.Remove(path) && CClinicSourceMatch(path);
            _cClinicProgress.Remove(path);
        }

        if (shown)
        {
            CClinicChanged?.Invoke();
        }
    }

    public void CClinicProgressSet(string path, double value)
    {
        _cClinicProgress[path] = Math.Clamp(value, 0, 1);
        if (CClinicSourceMatch(path))
        {
            CClinicChanged?.Invoke();
        }
    }

    private void CClinicSalvageApply(bool active, bool separate, bool fixedBasis, bool persistent)
    {
        bool fixedNormal = CClinicFixedNormalize(fixedBasis);
        if (_cClinicSalvageActive == active
            && _cClinicSalvageSeparate == separate
            && _cClinicSalvageFixed == fixedNormal
            && _cClinicSalvagePersistent == persistent)
        {
            return;
        }

        _cClinicSalvageActive = active;
        _cClinicSalvageSeparate = separate;
        _cClinicSalvageFixed = fixedNormal;
        _cClinicSalvagePersistent = persistent;
        CClinicChanged?.Invoke();
        CClinicPlanChanged?.Invoke();
    }

    private bool CClinicFixedNormalize(bool fixedBasis) => fixedBasis && CClinicRepairCheck();

    private (LClinicPort CClinicResult, bool CClinicScanning)? CClinicResultFind() =>
        _cClinicKind is { } kind
        && _cClinicSource is { } source
        && _cClinicResults.TryGetValue(source, out Dictionary<string, (LClinicPort, bool)>? kinds)
        && kinds.TryGetValue(kind, out (LClinicPort, bool) stored)
            ? stored
            : null;

    private bool CClinicSourceMatch(string path) =>
        string.Equals(path, _cClinicSource, StringComparison.OrdinalIgnoreCase);
}
