using System.Collections.Generic;
using System.Linq;

using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LSeriesTests
{
    [Fact]
    public void Resolve_UnnumberedFiles_EachAlone()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            LSeries.LSeriesResolve(new[] { "Alpha.mp4", "Beta.mp4" }, true);

        Assert.Equal(2, lSeriesGroups.Count);
        Assert.Equal("Alpha", lSeriesGroups[0].Name);
        Assert.Single(lSeriesGroups[0].Paths);
        Assert.Equal("Beta", lSeriesGroups[1].Name);
        Assert.Single(lSeriesGroups[1].Paths);
    }

    [Fact]
    public void Resolve_Loose_LumpsConsecutiveIntoOneNamedByBase()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            LSeries.LSeriesResolve(new[] { "A (1).mp4", "A (2).mp4" }, false);

        LSeriesGroup lSeriesGroup = Assert.Single(lSeriesGroups);
        Assert.Equal("A", lSeriesGroup.Name);
        Assert.Equal(new[] { "A (1).mp4", "A (2).mp4" }, lSeriesGroup.Paths);
    }

    [Fact]
    public void Resolve_First_UsesFirstNumericallySortedFileName()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            LSeries.LSeriesResolve(
                new[] { "A (2).mp4", "A (1).mp4" },
                true,
                LSeriesNameMode.LSeriesNameFirst);

        LSeriesGroup lSeriesGroup = Assert.Single(lSeriesGroups);
        Assert.Equal("A (1)", lSeriesGroup.Name);
        Assert.Equal(new[] { "A (1).mp4", "A (2).mp4" }, lSeriesGroup.Paths);
    }

    [Fact]
    public void Resolve_First_LooseGroupUsesFirstNameAcrossNumberGaps()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            LSeries.LSeriesResolve(
                new[] { "A (3).mp4", "A (1).mp4" },
                false,
                LSeriesNameMode.LSeriesNameFirst);

        LSeriesGroup lSeriesGroup = Assert.Single(lSeriesGroups);
        Assert.Equal("A (1)", lSeriesGroup.Name);
        Assert.Equal(new[] { "A (1).mp4", "A (3).mp4" }, lSeriesGroup.Paths);
    }

    [Fact]
    public void Resolve_Remove_RemainsDefaultAndUsesBaseName()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            LSeries.LSeriesResolve(new[] { "A (1).mp4", "A (2).mp4" }, true);

        Assert.Equal("A", Assert.Single(lSeriesGroups).Name);
    }

    [Fact]
    public void Resolve_Loose_LumpsGappedNumbersIntoOneNamedByBase()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            LSeries.LSeriesResolve(new[] { "A (1).mp4", "A (3).mp4" }, false);

        LSeriesGroup lSeriesGroup = Assert.Single(lSeriesGroups);
        Assert.Equal("A", lSeriesGroup.Name);
        Assert.Equal(2, lSeriesGroup.Paths.Count);
    }

    [Fact]
    public void Resolve_Strict_SplitsNonConsecutiveIntoRuns()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            LSeries.LSeriesResolve(new[] { "A (1).mp4", "A (3).mp4" }, true);

        Assert.Equal(2, lSeriesGroups.Count);
        Assert.Equal("A (1)", lSeriesGroups[0].Name);
        Assert.Equal("A (3)", lSeriesGroups[1].Name);
        Assert.Single(lSeriesGroups[0].Paths);
        Assert.Single(lSeriesGroups[1].Paths);
    }

    [Fact]
    public void Resolve_Strict_KeepsConsecutiveAsOneNamedByBase()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            LSeries.LSeriesResolve(new[] { "A (1).mp4", "A (2).mp4" }, true);

        LSeriesGroup lSeriesGroup = Assert.Single(lSeriesGroups);
        Assert.Equal("A", lSeriesGroup.Name);
        Assert.Equal(new[] { "A (1).mp4", "A (2).mp4" }, lSeriesGroup.Paths);
    }

    [Fact]
    public void Resolve_MixedBases_StaySeparate()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            LSeries.LSeriesResolve(new[] { "A (1).mp4", "B (1).mp4" }, true);

        Assert.Equal(2, lSeriesGroups.Count);
        Assert.Contains(lSeriesGroups, lSeriesGroup => lSeriesGroup.Paths.Single() == "A (1).mp4");
        Assert.Contains(lSeriesGroups, lSeriesGroup => lSeriesGroup.Paths.Single() == "B (1).mp4");
    }
}
