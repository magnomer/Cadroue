using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// One fix tab's clinic plan: each flaw kind's repair and keep switches, in the engine's order, and the salvage
// choices; the fixed basis needs a repair. CClinic makes one, and each setter answers whether the plan changed, so
// CClinic alone tells it.
internal sealed class CClinicPlan
{
    private readonly IReadOnlyList<string> _cClinicPlanKinds;
    private readonly Dictionary<string, (bool CClinicRepair, bool CClinicPersistent)> _cClinicPlanSteps =
        new(StringComparer.Ordinal);
    private bool _cClinicPlanActive;
    private bool _cClinicPlanSeparate;
    private bool _cClinicPlanFixed;
    private bool _cClinicPlanPersistent;

    internal CClinicPlan(LClinicPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var answer = plan.LClinicPlanRead();
        _cClinicPlanKinds = answer.LClinicSteps.Select(step => step.LClinicKind).ToArray();
        foreach ((string kind, bool repair, bool persistent) in answer.LClinicSteps)
        {
            _cClinicPlanSteps[kind] = (repair, persistent);
        }

        _cClinicPlanActive = answer.LClinicActive;
        _cClinicPlanSeparate = answer.LClinicSeparate;
        _cClinicPlanFixed = answer.LClinicFixed;
        _cClinicPlanPersistent = answer.LClinicPersistent;
    }

    // A kind's two switches; none for a kind no plan has named.
    internal (bool CClinicRepair, bool CClinicPersistent)? CClinicPlanRead(string kind) =>
        _cClinicPlanSteps.TryGetValue(kind, out (bool, bool) step) ? step : null;

    internal bool CClinicRepairCheck() => _cClinicPlanSteps.Values.Any(step => step.CClinicRepair);

    // The salvage panel's switches: the fixed basis needs a repair, and the description follows the basis.
    internal (bool CClinicActive,
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
            : _cClinicPlanFixed
                ? "Clinic.Salvage.Basis.Fixed.Description"
                : "Clinic.Salvage.Basis.Source.Description";
        return (_cClinicPlanActive,
            _cClinicPlanPersistent,
            _cClinicPlanSeparate,
            _cClinicPlanFixed,
            fixable,
            description);
    }

    // A repair change may drop the fixed basis.
    internal bool CClinicRepairSet(string kind, bool repair)
    {
        if (_cClinicPlanSteps[kind].CClinicRepair == repair)
        {
            return false;
        }

        _cClinicPlanSteps[kind] = _cClinicPlanSteps[kind] with { CClinicRepair = repair };
        _cClinicPlanFixed = CClinicFixedNormalize(_cClinicPlanFixed);
        return true;
    }

    internal bool CClinicPersistentSet(string kind, bool persistent)
    {
        if (_cClinicPlanSteps[kind].CClinicPersistent == persistent)
        {
            return false;
        }

        _cClinicPlanSteps[kind] = _cClinicPlanSteps[kind] with { CClinicPersistent = persistent };
        return true;
    }

    internal bool CSalvageActiveSet(bool active) => CClinicSalvageApply(
        active, _cClinicPlanSeparate, _cClinicPlanFixed, _cClinicPlanPersistent);

    internal bool CSalvagePersistentSet(bool persistent) => CClinicSalvageApply(
        _cClinicPlanActive, _cClinicPlanSeparate, _cClinicPlanFixed, persistent);

    internal bool CSalvageModeSet(bool separate) => CClinicSalvageApply(
        _cClinicPlanActive, separate, _cClinicPlanFixed, _cClinicPlanPersistent);

    internal bool CSalvageBasisSet(bool fixedBasis) => CClinicSalvageApply(
        _cClinicPlanActive, _cClinicPlanSeparate, fixedBasis, _cClinicPlanPersistent);

    // A plan sets the steps it lists and the salvage; the fixed basis falls back without a repair.
    internal void CClinicPlanAccept(LClinicPort plan)
    {
        var answer = plan.LClinicPlanRead();
        foreach ((string kind, bool repair, bool persistent) in answer.LClinicSteps)
        {
            _cClinicPlanSteps[kind] = (repair, persistent);
        }

        _cClinicPlanActive = answer.LClinicActive;
        _cClinicPlanSeparate = answer.LClinicSeparate;
        _cClinicPlanFixed = CClinicFixedNormalize(answer.LClinicFixed);
        _cClinicPlanPersistent = answer.LClinicPersistent;
    }

    // The engine builds the plan: every kind in the engine's order, then the salvage.
    internal void CClinicPlanApply(LClinicPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        plan.LClinicPlanSet(
            _cClinicPlanKinds
                .Select(kind =>
                    (kind, _cClinicPlanSteps[kind].CClinicRepair, _cClinicPlanSteps[kind].CClinicPersistent))
                .ToArray(),
            _cClinicPlanActive,
            _cClinicPlanSeparate,
            _cClinicPlanFixed,
            _cClinicPlanPersistent);
    }

    private bool CClinicSalvageApply(bool active, bool separate, bool fixedBasis, bool persistent)
    {
        bool fixedNormal = CClinicFixedNormalize(fixedBasis);
        if (_cClinicPlanActive == active
            && _cClinicPlanSeparate == separate
            && _cClinicPlanFixed == fixedNormal
            && _cClinicPlanPersistent == persistent)
        {
            return false;
        }

        _cClinicPlanActive = active;
        _cClinicPlanSeparate = separate;
        _cClinicPlanFixed = fixedNormal;
        _cClinicPlanPersistent = persistent;
        return true;
    }

    private bool CClinicFixedNormalize(bool fixedBasis) => fixedBasis && CClinicRepairCheck();
}
