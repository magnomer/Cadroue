using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// One clinic record for the clinic gate: a fix plan or a checkup result, read and set as plain values. A flaw kind
// travels by its name from one explicit table; an unknown kind or name throws. The empty outlet holds the default
// plan, every kind with nothing on, and an untested result.
public sealed class LClinicOutlet : LClinicPort
{
    private static readonly IReadOnlyList<(LFlawKind LClinicKind, string LClinicName)> _lClinicKinds =
    [
        (LFlawKind.LFlawKindContainer, "Container"),
        (LFlawKind.LFlawKindTruncation, "Truncation"),
        (LFlawKind.LFlawKindTransport, "Transport"),
        (LFlawKind.LFlawKindMetadata, "Metadata"),
        (LFlawKind.LFlawKindIndex, "Index"),
        (LFlawKind.LFlawKindFraming, "Framing"),
        (LFlawKind.LFlawKindConfig, "Config"),
        (LFlawKind.LFlawKindTiming, "Timing"),
        (LFlawKind.LFlawKindSecondary, "Secondary"),
        (LFlawKind.LFlawKindCoded, "Coded"),
        (LFlawKind.LFlawKindFfvone, "Ffvone")
    ];

    private readonly LCheckupResult _lClinicResult;
    private LWorkFix _lClinicPlan;

    public LClinicOutlet()
        : this(LClinicPlanCreate())
    {
    }

    public LClinicOutlet(LWorkFix lPlan)
    {
        ArgumentNullException.ThrowIfNull(lPlan);
        _lClinicPlan = lPlan;
        _lClinicResult = new LCheckupResult(
            string.Empty, LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeUntested);
    }

    public LClinicOutlet(LCheckupResult lResult)
    {
        _lClinicPlan = LClinicPlanCreate();
        _lClinicResult = lResult;
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
                .Select(lStep => (LClinicNameRead(lStep.LWorkFixKind), lStep.LWorkFixRepair, lStep.LWorkFixPersistent))
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
            lPlanSteps.Add(new LWorkFixStep(LClinicKindRead(lKind), lRepair, lStepPersistent));
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

    public (string LClinicSource, string LClinicKind, bool LClinicScanning) LClinicResultRead() =>
        (_lClinicResult.LCheckupSource,
            LClinicNameRead(_lClinicResult.LCheckupKind),
            _lClinicResult.LCheckupOutcome == LCheckupOutcome.LCheckupOutcomeScanning);

    // The wording runs untested, scanning, clean, failed, then the defect, evidence and repair labels.
    public string LClinicResultFormat(IReadOnlyList<string> lWording)
    {
        ArgumentNullException.ThrowIfNull(lWording);
        return LCheckupFormatter.LCheckupBodyFormat(
            _lClinicResult,
            new LCheckupStrings(
                lWording[0], lWording[1], lWording[2], lWording[3], lWording[4], lWording[5], lWording[6]));
    }

    private static LWorkFix LClinicPlanCreate() =>
        new(_lClinicKinds.Select(lEntry => new LWorkFixStep(lEntry.LClinicKind, false, false)).ToList());

    private static string LClinicNameRead(LFlawKind lKind) =>
        _lClinicKinds.Where(lEntry => lEntry.LClinicKind == lKind).Select(lEntry => lEntry.LClinicName).Single();

    private static LFlawKind LClinicKindRead(string lName) =>
        _lClinicKinds.Where(lEntry => lEntry.LClinicName == lName).Select(lEntry => lEntry.LClinicKind).Single();
}
