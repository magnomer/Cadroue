using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TClinicSalvage
{
    [Fact]
    public void FixedBasis_NeedsARepair_FallsBackToSource()
    {
        QClinic clinic = TInterface.TClinicCreate();
        int plans = 0;
        TInterface.TClinicPlanAttach(clinic, () => plans++);

        TInterface.TClinicSalvageSet(clinic, TInterface.TClinicSalvageCreate(
            true, LSalvageMode.LSalvageModeSeparate, LSalvageBasis.LSalvageBasisFixed, false));

        Assert.False(TInterface.TClinicRepairCheck(clinic));
        Assert.Equal(LSalvageBasis.LSalvageBasisSource, clinic.QClinicSalvage.LWorkSalvageBasis);
        Assert.Equal(LSalvageMode.LSalvageModeSeparate, clinic.QClinicSalvage.LWorkSalvageMode);
        Assert.Equal(1, plans);

        TInterface.TClinicStepSet(clinic, "Container");
        TInterface.TClinicActiveSet(clinic, true);
        TInterface.TClinicSalvageSet(
            clinic, clinic.QClinicSalvage with { LWorkSalvageBasis = LSalvageBasis.LSalvageBasisFixed });

        Assert.Equal(LSalvageBasis.LSalvageBasisFixed, clinic.QClinicSalvage.LWorkSalvageBasis);
        Assert.Equal(3, plans);

        TInterface.TClinicActiveSet(clinic, false);

        Assert.Equal(LSalvageBasis.LSalvageBasisSource, clinic.QClinicSalvage.LWorkSalvageBasis);
    }

    [Fact]
    public void SalvageStep_ShowsSalvage_WithoutAKind()
    {
        QClinic clinic = TInterface.TClinicCreate();

        TInterface.TClinicStepSet(clinic, QClinic.QClinicSalvageStep);

        Assert.True(clinic.QClinicSalvageShown);
        Assert.Null(clinic.QClinicKind);

        TInterface.TClinicActiveSet(clinic, true);

        Assert.False(TInterface.TClinicRepairCheck(clinic));
    }

    [Fact]
    public void PlanRoundTrip_KeepsStepsAndSalvage_NoPlanNotice()
    {
        QClinic clinic = TInterface.TClinicCreate();
        int plans = 0;
        TInterface.TClinicPlanAttach(clinic, () => plans++);
        TInterface.TClinicStepSet(clinic, "Timing");
        TInterface.TClinicActiveSet(clinic, true);
        LWorkFix plan = TInterface.TClinicPlanRead(clinic);

        QClinic restored = TInterface.TClinicCreate();
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
        QClinic clinic = TInterface.TClinicCreate();
        int plans = 0;
        TInterface.TClinicPlanAttach(clinic, () => plans++);

        TInterface.TSalvageActiveSet(clinic, true);
        TInterface.TSalvageModeSet(clinic, LSalvageMode.LSalvageModeSeparate);
        TInterface.TSalvagePersistentSet(clinic, true);
        TInterface.TSalvageBasisSet(clinic, LSalvageBasis.LSalvageBasisFixed);

        Assert.True(clinic.QClinicSalvageActive);
        Assert.True(clinic.QClinicSalvageSeparate);
        Assert.False(clinic.QClinicSalvageRejoin);
        Assert.True(clinic.QClinicSalvagePersistent);
        Assert.True(clinic.QClinicSalvageSource);
        Assert.Equal(3, plans);

        TInterface.TClinicStepSet(clinic, "Container");
        TInterface.TClinicActiveSet(clinic, true);
        TInterface.TSalvageBasisSet(clinic, LSalvageBasis.LSalvageBasisFixed);

        Assert.True(clinic.QClinicSalvageFixed);
        Assert.Equal(5, plans);
    }

    [Fact]
    public void FaceReads_FollowStepAndSalvage()
    {
        QClinic clinic = TInterface.TClinicCreate();

        Assert.False(clinic.QClinicKnown);
        Assert.False(clinic.QClinicStepShown);
        Assert.False(clinic.QClinicPersistentAllowed);
        Assert.False(clinic.QClinicResultShown);
        Assert.Equal(string.Empty, TInterface.TClinicSimpleRead(clinic));

        TInterface.TClinicStepSet(clinic, QClinic.QClinicSalvageStep);
        Assert.True(clinic.QClinicKnown);
        Assert.False(clinic.QClinicStepShown);
        Assert.False(clinic.QClinicPersistentAllowed);
        Assert.False(clinic.QClinicResultShown);

        TInterface.TClinicStepSet(clinic, "Timing");
        TInterface.TClinicActiveSet(clinic, true);
        Assert.True(clinic.QClinicStepShown);
        Assert.True(clinic.QClinicPersistentAllowed);
        Assert.True(clinic.QClinicRepairChecked);
        Assert.False(clinic.QClinicPersistentChecked);
        Assert.True(clinic.QClinicResultShown);
        Assert.False(clinic.QClinicScanShown);
        Assert.Equal(0, clinic.QClinicProgressValue);
        Assert.Equal("Processing.Step.Timing", TInterface.TClinicTitleRead(clinic));
    }
}
