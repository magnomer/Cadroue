using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

public sealed class TBastion
{
    private static LWorkItem Work(Guid batch, LWorkState state)
    {
        return new LWorkItem(
            batch,
            LWorkKind.LWorkKindEdit,
            LWorkPriority.LWorkPriorityNormal,
            "source",
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            "name",
            "output",
            WorkCreationOutput.Create())
        {
            LWorkStateCurrent = state
        };
    }

    [Fact]
    public void RunningCohortIsProtected()
    {
        Guid batch = Guid.NewGuid();
        IReadOnlySet<Guid> cohorts = LBastion.LBastionCohortsRead(new[] { Work(batch, LWorkState.LWorkStateRunning) });

        Assert.Contains(batch, cohorts);
    }

    [Fact]
    public void PendingCohortIsNotProtected()
    {
        Guid batch = Guid.NewGuid();
        IReadOnlySet<Guid> cohorts = LBastion.LBastionCohortsRead(new[] { Work(batch, LWorkState.LWorkStatePending) });

        Assert.DoesNotContain(batch, cohorts);
    }

    [Theory]
    [InlineData(LWorkState.LWorkStateCancelled)]
    [InlineData(LWorkState.LWorkStateFailed)]
    [InlineData(LWorkState.LWorkStateDone)]
    public void InactiveCohortIsNotProtected(LWorkState state)
    {
        Guid batch = Guid.NewGuid();
        IReadOnlySet<Guid> cohorts = LBastion.LBastionCohortsRead(new[] { Work(batch, state) });

        Assert.DoesNotContain(batch, cohorts);
    }

    [Fact]
    public void RunningRootProtectsWholeCohort()
    {
        Guid batch = Guid.NewGuid();
        IReadOnlySet<Guid> cohorts = LBastion.LBastionCohortsRead(new[]
        {
            Work(batch, LWorkState.LWorkStateRunning),
            Work(batch, LWorkState.LWorkStatePending)
        });

        Assert.Contains(batch, cohorts);
        Assert.Single(cohorts);
    }

    [Fact]
    public void EmptyBatchIsIgnored()
    {
        IReadOnlySet<Guid> cohorts = LBastion.LBastionCohortsRead(new[] { Work(Guid.Empty, LWorkState.LWorkStateRunning) });

        Assert.Empty(cohorts);
    }
}
