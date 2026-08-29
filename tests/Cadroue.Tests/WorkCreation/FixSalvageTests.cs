using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class FixSalvageTests
{
    [Fact]
    public void Salvage_RoundTripsThroughSidecarRecord()
    {
        LWorkFix plan = TSalvage.PlanCreate(
            true, LSalvageMode.LSalvageModeSeparate, true, LSalvageBasis.LSalvageBasisFixed);

        LWorkFix restored = TSalvage.PersistentRoundTrip(plan);

        Assert.True(restored.LWorkFixSalvage.LWorkSalvageActive);
        Assert.Equal(LSalvageMode.LSalvageModeSeparate, restored.LWorkFixSalvage.LWorkSalvageMode);
        Assert.Equal(LSalvageBasis.LSalvageBasisFixed, restored.LWorkFixSalvage.LWorkSalvageBasis);
        Assert.True(restored.LWorkFixSalvage.LWorkSalvagePersistent);
    }

    [Fact]
    public void SalvageDefault_IsOffAndRejoin()
    {
        LWorkFix plan = TSalvage.DefaultCreate();

        Assert.False(plan.LWorkFixSalvage.LWorkSalvageActive);
        Assert.Equal(LSalvageMode.LSalvageModeRejoin, plan.LWorkFixSalvage.LWorkSalvageMode);
        Assert.Equal(LSalvageBasis.LSalvageBasisSource, plan.LWorkFixSalvage.LWorkSalvageBasis);
        Assert.False(plan.LWorkFixActive);
    }

    [Fact]
    public void SalvageActive_MakesPlanActive()
    {
        LWorkFix plan = TSalvage.PlanCreate(true, LSalvageMode.LSalvageModeRejoin, false);

        Assert.True(plan.LWorkFixActive);
    }

    [Fact]
    public void PersistentResolve_KeepsSalvageOnlyWhenPersistent()
    {
        LWorkFix transient = TSalvage.PlanCreate(true, LSalvageMode.LSalvageModeSeparate, false);

        Assert.False(TSalvage.PersistentResolve(transient).LWorkFixSalvage.LWorkSalvageActive);

        LWorkFix persistent = TSalvage.PlanCreate(true, LSalvageMode.LSalvageModeSeparate, true);

        LWorkFixSalvage kept = TSalvage.PersistentResolve(persistent).LWorkFixSalvage;
        Assert.True(kept.LWorkSalvageActive);
        Assert.Equal(LSalvageMode.LSalvageModeSeparate, kept.LWorkSalvageMode);
    }

    [Fact]
    public void PlanResolve_PrefersPersistentSalvageOverSaved()
    {
        LWorkFix saved = TSalvage.PlanCreate(true, LSalvageMode.LSalvageModeRejoin, false);
        LWorkFix persistent = TSalvage.PlanCreate(true, LSalvageMode.LSalvageModeSeparate, true);

        LWorkFixSalvage resolved = TSalvage.PlanResolve(saved, persistent).LWorkFixSalvage;

        Assert.Equal(LSalvageMode.LSalvageModeSeparate, resolved.LWorkSalvageMode);
    }
}
