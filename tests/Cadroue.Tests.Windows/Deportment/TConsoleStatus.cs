using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TConsoleStatus
{
    [Fact]
    public void Runs_AreOnePlainRun_WithoutAnAccent()
    {
        IReadOnlyList<QConsoleSpan> runs = TInterface.TConsoleRunsResolve("Paused, 1 of 3 done", null);
        Assert.Single(runs);
        Assert.Equal("Paused, 1 of 3 done", runs[0].QConsoleRunText);
        Assert.False(runs[0].QConsoleRunAccent);
    }

    [Fact]
    public void Runs_SplitAroundTheAccent()
    {
        IReadOnlyList<QConsoleSpan> runs = TInterface.TConsoleRunsResolve("State Paused, 1 of 3 done", "Paused");
        Assert.Equal(3, runs.Count);
        Assert.Equal("State ", runs[0].QConsoleRunText);
        Assert.Equal("Paused", runs[1].QConsoleRunText);
        Assert.True(runs[1].QConsoleRunAccent);
        Assert.Equal(", 1 of 3 done", runs[2].QConsoleRunText);
    }

    [Fact]
    public void Runs_OmitEmptyEdges()
    {
        IReadOnlyList<QConsoleSpan> runs = TInterface.TConsoleRunsResolve("Paused", "Paused");
        Assert.Single(runs);
        Assert.True(runs[0].QConsoleRunAccent);

        Assert.Single(TInterface.TConsoleRunsResolve("Paused", "Running"));
    }

    [Fact]
    public void Progress_RaisesGlideOnlyForwardAboveZero()
    {
        QConsole console = TInterface.TConsoleCreate();
        var applied = new List<(double, bool)>();
        TInterface.TConsoleProgressAttach(console, (value, glide) => applied.Add((value, glide)));

        Assert.True(TInterface.TConsoleProgressSet(console, 0.5));
        Assert.False(TInterface.TConsoleProgressSet(console, 0.5));
        Assert.True(TInterface.TConsoleProgressSet(console, 2));
        Assert.True(TInterface.TConsoleProgressSet(console, 0.25));
        Assert.True(TInterface.TConsoleProgressSet(console, 0));

        Assert.Equal([(0.5, true), (1, true), (0.25, false), (0, false)], applied);
        Assert.Equal(0, console.QConsoleProgress);
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
