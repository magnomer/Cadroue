using Cadroue.Core;

namespace Cadroue.ShellEngine;

// One fix plan record for the clinic gate, read and set as plain values. A flaw kind travels by its clinic name. The
// empty outlet holds the default plan, every kind with nothing on.
public sealed class LClinicOutlet : LClinicPort
{
    private LWorkFix _lClinicPlan;

    public LClinicOutlet()
        : this(LClinicPlanCreate())
    {
    }

    public LClinicOutlet(LWorkFix lPlan)
    {
        ArgumentNullException.ThrowIfNull(lPlan);
        _lClinicPlan = lPlan;
    }

    public LWorkFix LClinicPlan => _lClinicPlan;

    public (IReadOnlyList<(string LClinicKind, bool LClinicRepair, bool LClinicPersistent)> LClinicSteps,
        bool LClinicActive,
        bool LClinicSeparate,
        bool LClinicFixed,
        bool LClinicPersistent) LClinicPlanRead()
    {
        LWorkFixSalvage lSalvage = _lClinicPlan.LWorkFixSalvage;
        return (
            _lClinicPlan.LWorkFixSteps
                .Select(lStep => (
                    LClinicFlaw.LClinicNameRead(lStep.LWorkFixKind),
                    lStep.LWorkFixRepair,
                    lStep.LWorkFixPersistent))
                .ToList(),
            lSalvage.LWorkSalvageActive,
            lSalvage.LWorkSalvageMode == LSalvageMode.LSalvageModeSeparate,
            lSalvage.LWorkSalvageBasis == LSalvageBasis.LSalvageBasisFixed,
            lSalvage.LWorkSalvagePersistent);
    }

    public void LClinicPlanSet(
        IReadOnlyList<(string LClinicKind, bool LClinicRepair, bool LClinicPersistent)> lSteps,
        bool lActive,
        bool lSeparate,
        bool lFixed,
        bool lPersistent)
    {
        ArgumentNullException.ThrowIfNull(lSteps);
        var lPlanSteps = new List<LWorkFixStep>();
        foreach ((string lKind, bool lRepair, bool lStepPersistent) in lSteps)
        {
            lPlanSteps.Add(new LWorkFixStep(LClinicFlaw.LClinicKindRead(lKind), lRepair, lStepPersistent));
        }

        _lClinicPlan = new LWorkFix(lPlanSteps)
        {
            LWorkFixSalvage = new LWorkFixSalvage(
                lActive,
                lSeparate ? LSalvageMode.LSalvageModeSeparate : LSalvageMode.LSalvageModeRejoin,
                lFixed ? LSalvageBasis.LSalvageBasisFixed : LSalvageBasis.LSalvageBasisSource,
                lPersistent)
        };
    }

    private static LWorkFix LClinicPlanCreate() =>
        new(LClinicFlaw.LClinicFlawOrder.Select(lKind => new LWorkFixStep(lKind, false, false)).ToList());
}
