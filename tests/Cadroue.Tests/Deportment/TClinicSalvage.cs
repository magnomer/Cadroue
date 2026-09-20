using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TClinicSalvage
{
    [Fact]
    public void FixedBasis_NeedsARepair_FallsBackToSource()
    {
        LClinic clinic = TInterface.TClinicCreate();
        int plans = 0;
        TInterface.TClinicPlanAttach(clinic, () => plans++);

        TInterface.TClinicSalvageSet(clinic, TInterface.TClinicSalvageCreate(
            true, LSalvageMode.LSalvageModeSeparate, LSalvageBasis.LSalvageBasisFixed, false));

        Assert.False(TInterface.TClinicRepairCheck(clinic));
        Assert.Equal(LSalvageBasis.LSalvageBasisSource, clinic.LClinicSalvage.LWorkSalvageBasis);
        Assert.Equal(LSalvageMode.LSalvageModeSeparate, clinic.LClinicSalvage.LWorkSalvageMode);
        Assert.Equal(1, plans);

        TInterface.TClinicStepSet(clinic, "Container");
        TInterface.TClinicActiveSet(clinic, true);
        TInterface.TClinicSalvageSet(
            clinic, clinic.LClinicSalvage with { LWorkSalvageBasis = LSalvageBasis.LSalvageBasisFixed });

        Assert.Equal(LSalvageBasis.LSalvageBasisFixed, clinic.LClinicSalvage.LWorkSalvageBasis);
        Assert.Equal(3, plans);

        TInterface.TClinicActiveSet(clinic, false);

        Assert.Equal(LSalvageBasis.LSalvageBasisSource, clinic.LClinicSalvage.LWorkSalvageBasis);
    }

    [Fact]
    public void SalvageStep_ShowsSalvage_WithoutAKind()
    {
        LClinic clinic = TInterface.TClinicCreate();

        TInterface.TClinicStepSet(clinic, LClinic.LClinicSalvageStep);

        Assert.True(clinic.LClinicSalvageShown);
        Assert.Null(clinic.LClinicKind);

        TInterface.TClinicActiveSet(clinic, true);

        Assert.False(TInterface.TClinicRepairCheck(clinic));
    }

    [Fact]
    public void PlanRoundTrip_KeepsStepsAndSalvage_NoPlanNotice()
    {
        LClinic clinic = TInterface.TClinicCreate();
        int plans = 0;
        TInterface.TClinicPlanAttach(clinic, () => plans++);
        TInterface.TClinicStepSet(clinic, "Timing");
        TInterface.TClinicActiveSet(clinic, true);
        LWorkFix plan = TInterface.TClinicPlanRead(clinic);

        LClinic restored = TInterface.TClinicCreate();
        TInterface.TClinicPlanApply(restored, plan);

        LWorkFix read = TInterface.TClinicPlanRead(restored);
        Assert.True(TInterface.TClinicRepairCheck(restored));
        Assert.Equal(plan.LWorkFixSalvage, read.LWorkFixSalvage);
        Assert.Equal(plan.LWorkFixSteps, read.LWorkFixSteps);
        Assert.Contains(
            read.LWorkFixSteps, step => step.LWorkFixKind == LFlawKind.LFlawKindTiming && step.LWorkFixRepair);
        Assert.Equal(1, plans);
    }

    [Fact]
    public void FieldSetters_ChangeOneField_NormalizeBasis()
    {
        LClinic clinic = TInterface.TClinicCreate();
        int plans = 0;
        TInterface.TClinicPlanAttach(clinic, () => plans++);

        TInterface.TSalvageActiveSet(clinic, true);
        TInterface.TSalvageModeSet(clinic, LSalvageMode.LSalvageModeSeparate);
        TInterface.TSalvagePersistentSet(clinic, true);
        TInterface.TSalvageBasisSet(clinic, LSalvageBasis.LSalvageBasisFixed);

        Assert.True(clinic.LClinicSalvageActive);
        Assert.True(clinic.LClinicSalvageSeparate);
        Assert.False(clinic.LClinicSalvageRejoin);
        Assert.True(clinic.LClinicSalvagePersistent);
        Assert.True(clinic.LClinicSalvageSource);
        Assert.Equal(3, plans);

        TInterface.TClinicStepSet(clinic, "Container");
        TInterface.TClinicActiveSet(clinic, true);
        TInterface.TSalvageBasisSet(clinic, LSalvageBasis.LSalvageBasisFixed);

        Assert.True(clinic.LClinicSalvageFixed);
        Assert.Equal(5, plans);
    }

    [Fact]
    public void FaceReads_FollowStepAndSalvage()
    {
        LClinic clinic = TInterface.TClinicCreate();

        Assert.False(clinic.LClinicKnown);
        Assert.False(clinic.LClinicStepShown);
        Assert.False(clinic.LClinicPersistentAllowed);
        Assert.False(clinic.LClinicResultShown);
        Assert.Equal(string.Empty, TInterface.TClinicSimpleRead(clinic));

        TInterface.TClinicStepSet(clinic, LClinic.LClinicSalvageStep);
        Assert.True(clinic.LClinicKnown);
        Assert.False(clinic.LClinicStepShown);
        Assert.False(clinic.LClinicPersistentAllowed);
        Assert.False(clinic.LClinicResultShown);

        TInterface.TClinicStepSet(clinic, "Timing");
        TInterface.TClinicActiveSet(clinic, true);
        Assert.True(clinic.LClinicStepShown);
        Assert.True(clinic.LClinicPersistentAllowed);
        Assert.True(clinic.LClinicRepairChecked);
        Assert.False(clinic.LClinicPersistentChecked);
        Assert.True(clinic.LClinicResultShown);
        Assert.False(clinic.LClinicScanShown);
        Assert.Equal(0, clinic.LClinicProgressValue);
        Assert.Equal("Processing.Step.Timing", TInterface.TClinicTitleRead(clinic));
    }
}
