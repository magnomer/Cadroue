using System;

using Xunit;

namespace Cadroue.Tests;

public sealed class RelayCycleTests
{
    [Fact]
    public void ProcessingCycle_TerminatesBackEdgeToFinish()
    {
        var specs = new[]
        {
            TCartographerTabSpec.Processing(target: 1),
            TCartographerTabSpec.Processing(target: 0)
        };

        TCartographerPlanView plan = TCartographer.PlanCreate(specs, entryIndex: 0);

        Assert.Equal(2, plan.Stages.Count);
        Assert.Equal(plan.Stage(1).StageId, plan.Stage(0).NextStage);
        Assert.Equal(TCartographer.FinishTarget, plan.Stage(1).NextStage);
    }

    [Fact]
    public void FunnelCycle_TerminatesBackEdgeToFinish()
    {
        var specs = new[]
        {
            TCartographerTabSpec.FunnelInto(1),
            TCartographerTabSpec.FunnelInto(0)
        };

        TCartographerPlanView plan = TCartographer.PlanCreate(specs, entryIndex: 0);

        Assert.Equal(2, plan.Stages.Count);
        Assert.Equal(plan.Stage(1).StageId, Assert.Single(plan.Stage(0).FunnelTargets));
        Assert.Equal(TCartographer.FinishTarget, Assert.Single(plan.Stage(1).FunnelTargets));
    }

    [Fact]
    public void Diamond_SharesJoinStageWithoutFalseCycle()
    {
        var specs = new[]
        {
            TCartographerTabSpec.FunnelInto(1, 2),
            TCartographerTabSpec.Processing(target: 3),
            TCartographerTabSpec.Processing(target: 3),
            TCartographerTabSpec.Processing(target: -1)
        };

        TCartographerPlanView plan = TCartographer.PlanCreate(specs, entryIndex: 0);

        Assert.Equal(4, plan.Stages.Count);
        Guid join = plan.Stage(3).StageId;
        Assert.Equal(join, plan.Stage(1).NextStage);
        Assert.Equal(join, plan.Stage(2).NextStage);
        Assert.NotEqual(TCartographer.FinishTarget, join);
        Assert.Equal(Guid.Empty, plan.Stage(3).NextStage);
    }
}
