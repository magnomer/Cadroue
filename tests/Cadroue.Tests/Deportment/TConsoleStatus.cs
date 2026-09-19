using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TConsoleStatus
{
    [Fact]
    public void Runs_AreOnePlainRun_WithoutAnAccent()
    {
        IReadOnlyList<LConsoleRun> runs = TInterface.TConsoleRunsResolve("Paused, 1 of 3 done", null);
        Assert.Single(runs);
        Assert.Equal("Paused, 1 of 3 done", runs[0].LConsoleRunText);
        Assert.False(runs[0].LConsoleRunAccent);
    }

    [Fact]
    public void Runs_SplitAroundTheAccent()
    {
        IReadOnlyList<LConsoleRun> runs = TInterface.TConsoleRunsResolve("State Paused, 1 of 3 done", "Paused");
        Assert.Equal(3, runs.Count);
        Assert.Equal("State ", runs[0].LConsoleRunText);
        Assert.Equal("Paused", runs[1].LConsoleRunText);
        Assert.True(runs[1].LConsoleRunAccent);
        Assert.Equal(", 1 of 3 done", runs[2].LConsoleRunText);
    }

    [Fact]
    public void Runs_OmitEmptyEdges()
    {
        IReadOnlyList<LConsoleRun> runs = TInterface.TConsoleRunsResolve("Paused", "Paused");
        Assert.Single(runs);
        Assert.True(runs[0].LConsoleRunAccent);

        Assert.Single(TInterface.TConsoleRunsResolve("Paused", "Running"));
    }

    [Fact]
    public void Progress_RaisesGlideOnlyForwardAboveZero()
    {
        LConsole console = TInterface.TConsoleCreate();
        var applied = new List<(double, bool)>();
        TInterface.TConsoleProgressAttach(console, (value, glide) => applied.Add((value, glide)));

        Assert.True(TInterface.TConsoleProgressSet(console, 0.5));
        Assert.False(TInterface.TConsoleProgressSet(console, 0.5));
        Assert.True(TInterface.TConsoleProgressSet(console, 2));
        Assert.True(TInterface.TConsoleProgressSet(console, 0.25));
        Assert.True(TInterface.TConsoleProgressSet(console, 0));

        Assert.Equal([(0.5, true), (1, true), (0.25, false), (0, false)], applied);
        Assert.Equal(0, console.LConsoleProgress);
    }

    [Fact]
    public void Spin_RaisesOnlyOnChange()
    {
        LConsole console = TInterface.TConsoleCreate();
        var applied = new List<bool>();
        TInterface.TConsoleSpinAttach(console, applied.Add);

        Assert.True(TInterface.TConsoleSpinSet(console, true));
        Assert.False(TInterface.TConsoleSpinSet(console, true));
        Assert.True(TInterface.TConsoleSpinSet(console, false));
        Assert.Equal([true, false], applied);
    }

    [Fact]
    public void Removal_IsSilent_WhenEveryItemWasRemoved()
    {
        var outcomes = new Dictionary<Guid, LScheduleRemoval>
        {
            [Guid.NewGuid()] = LScheduleRemoval.LScheduleRemovalRemoved,
        };
        Assert.Null(TInterface.TConsoleRemovalFormat(outcomes));
    }

    [Fact]
    public void Removal_ReportsHeldAndBlocked()
    {
        var outcomes = new Dictionary<Guid, LScheduleRemoval>
        {
            [Guid.NewGuid()] = LScheduleRemoval.LScheduleRemovalRemoved,
            [Guid.NewGuid()] = LScheduleRemoval.LScheduleRemovalHeld,
            [Guid.NewGuid()] = LScheduleRemoval.LScheduleRemovalBlocked,
        };
        Assert.NotNull(TInterface.TConsoleRemovalFormat(outcomes));
    }
}
