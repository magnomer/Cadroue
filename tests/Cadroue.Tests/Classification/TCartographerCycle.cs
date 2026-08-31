using System;

using Xunit;

namespace Cadroue.Tests;

public sealed class TCartographerCycle
{
    [Fact]
    public void ProcessingCycle_TerminatesBackEdgeToFinish()
    {
        var specs = new[]
        {
            TCartographerTabSpec.TCartographerProcessCreate(target: 1),
            TCartographerTabSpec.TCartographerProcessCreate(target: 0)
        };

        TCartographerPlanView plan = TCartographer.TCartographerPlanCreate(specs, entryIndex: 0);

        Assert.Equal(2, plan.TCartographerStages.Count);
        Assert.Equal(plan.TCartographerStageRead(1).TCartographerStageId, plan.TCartographerStageRead(0).TCartographerNextStage);
        Assert.Equal(TCartographer.TCartographerFinish, plan.TCartographerStageRead(1).TCartographerNextStage);
    }

    [Fact]
    public void FunnelCycle_TerminatesBackEdgeToFinish()
    {
        var specs = new[]
        {
            TCartographerTabSpec.TCartographerFunnelCreate(1),
            TCartographerTabSpec.TCartographerFunnelCreate(0)
        };

        TCartographerPlanView plan = TCartographer.TCartographerPlanCreate(specs, entryIndex: 0);

        Assert.Equal(2, plan.TCartographerStages.Count);
        Assert.Equal(plan.TCartographerStageRead(1).TCartographerStageId, Assert.Single(plan.TCartographerStageRead(0).TCartographerFunnelTargets));
        Assert.Equal(TCartographer.TCartographerFinish, Assert.Single(plan.TCartographerStageRead(1).TCartographerFunnelTargets));
    }

    [Fact]
    public void Diamond_SharesJoinStageWithoutFalseCycle()
    {
        var specs = new[]
        {
            TCartographerTabSpec.TCartographerFunnelCreate(1, 2),
            TCartographerTabSpec.TCartographerProcessCreate(target: 3),
            TCartographerTabSpec.TCartographerProcessCreate(target: 3),
            TCartographerTabSpec.TCartographerProcessCreate(target: -1)
        };

        TCartographerPlanView plan = TCartographer.TCartographerPlanCreate(specs, entryIndex: 0);

        Assert.Equal(4, plan.TCartographerStages.Count);
        Guid join = plan.TCartographerStageRead(3).TCartographerStageId;
        Assert.Equal(join, plan.TCartographerStageRead(1).TCartographerNextStage);
        Assert.Equal(join, plan.TCartographerStageRead(2).TCartographerNextStage);
        Assert.NotEqual(TCartographer.TCartographerFinish, join);
        Assert.Equal(Guid.Empty, plan.TCartographerStageRead(3).TCartographerNextStage);
    }
}
