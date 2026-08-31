using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TSalvageFix
{
    [Fact]
    public void Salvage_RoundTripsThroughSidecarRecord()
    {
        LWorkFix plan = TSalvage.TSalvagePlanCreate(
            true, LSalvageMode.LSalvageModeSeparate, true, LSalvageBasis.LSalvageBasisFixed);

        LWorkFix restored = TSalvage.TSalvagePersistMatch(plan);

        Assert.True(restored.LWorkFixSalvage.LWorkSalvageActive);
        Assert.Equal(LSalvageMode.LSalvageModeSeparate, restored.LWorkFixSalvage.LWorkSalvageMode);
        Assert.Equal(LSalvageBasis.LSalvageBasisFixed, restored.LWorkFixSalvage.LWorkSalvageBasis);
        Assert.True(restored.LWorkFixSalvage.LWorkSalvagePersistent);
    }

    [Fact]
    public void SalvageDefault_IsOffAndRejoin()
    {
        LWorkFix plan = TSalvage.TSalvageDefaultCreate();

        Assert.False(plan.LWorkFixSalvage.LWorkSalvageActive);
        Assert.Equal(LSalvageMode.LSalvageModeRejoin, plan.LWorkFixSalvage.LWorkSalvageMode);
        Assert.Equal(LSalvageBasis.LSalvageBasisSource, plan.LWorkFixSalvage.LWorkSalvageBasis);
        Assert.False(plan.LWorkFixActive);
    }

    [Fact]
    public void SalvageActive_MakesPlanActive()
    {
        LWorkFix plan = TSalvage.TSalvagePlanCreate(true, LSalvageMode.LSalvageModeRejoin, false);

        Assert.True(plan.LWorkFixActive);
    }

    [Fact]
    public void PersistentResolve_KeepsSalvageOnlyWhenPersistent()
    {
        LWorkFix transient = TSalvage.TSalvagePlanCreate(true, LSalvageMode.LSalvageModeSeparate, false);

        Assert.False(TSalvage.TSalvagePersistResolve(transient).LWorkFixSalvage.LWorkSalvageActive);

        LWorkFix persistent = TSalvage.TSalvagePlanCreate(true, LSalvageMode.LSalvageModeSeparate, true);

        LWorkFixSalvage kept = TSalvage.TSalvagePersistResolve(persistent).LWorkFixSalvage;
        Assert.True(kept.LWorkSalvageActive);
        Assert.Equal(LSalvageMode.LSalvageModeSeparate, kept.LWorkSalvageMode);
    }

    [Fact]
    public void PlanResolve_PrefersPersistentSalvageOverSaved()
    {
        LWorkFix saved = TSalvage.TSalvagePlanCreate(true, LSalvageMode.LSalvageModeRejoin, false);
        LWorkFix persistent = TSalvage.TSalvagePlanCreate(true, LSalvageMode.LSalvageModeSeparate, true);

        LWorkFixSalvage resolved = TSalvage.TSalvagePlanResolve(saved, persistent).LWorkFixSalvage;

        Assert.Equal(LSalvageMode.LSalvageModeSeparate, resolved.LWorkSalvageMode);
    }
}
