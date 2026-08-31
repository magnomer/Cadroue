using System.Collections.Generic;
using System.Linq;

using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TSeriesGroup
{
    [Fact]
    public void UnnumberedFiles_FormSeparateGroups()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            TInterface.TSeriesResolve(new[] { "Alpha.mp4", "Beta.mp4" }, true);

        Assert.Equal(2, lSeriesGroups.Count);
        Assert.Equal("Alpha", lSeriesGroups[0].LSeriesName);
        Assert.Single(lSeriesGroups[0].LSeriesPaths);
        Assert.Equal("Beta", lSeriesGroups[1].LSeriesName);
        Assert.Single(lSeriesGroups[1].LSeriesPaths);
    }

    [Fact]
    public void LooseGrouping_CombinesConsecutiveFiles()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            TInterface.TSeriesResolve(new[] { "A (1).mp4", "A (2).mp4" }, false);

        LSeriesGroup lSeriesGroup = Assert.Single(lSeriesGroups);
        Assert.Equal("A", lSeriesGroup.LSeriesName);
        Assert.Equal(new[] { "A (1).mp4", "A (2).mp4" }, lSeriesGroup.LSeriesPaths);
    }

    [Fact]
    public void LooseGrouping_CombinesFilesAcrossNumberGaps()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            TInterface.TSeriesResolve(new[] { "A (1).mp4", "A (3).mp4" }, false);

        LSeriesGroup lSeriesGroup = Assert.Single(lSeriesGroups);
        Assert.Equal("A", lSeriesGroup.LSeriesName);
        Assert.Equal(2, lSeriesGroup.LSeriesPaths.Count);
    }

    [Fact]
    public void StrictGrouping_SplitsFilesAcrossNumberGaps()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            TInterface.TSeriesResolve(new[] { "A (1).mp4", "A (3).mp4" }, true);

        Assert.Equal(2, lSeriesGroups.Count);
        Assert.Equal("A (1)", lSeriesGroups[0].LSeriesName);
        Assert.Equal("A (3)", lSeriesGroups[1].LSeriesName);
        Assert.Single(lSeriesGroups[0].LSeriesPaths);
        Assert.Single(lSeriesGroups[1].LSeriesPaths);
    }

    [Fact]
    public void StrictGrouping_KeepsConsecutiveFilesTogether()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            TInterface.TSeriesResolve(new[] { "A (1).mp4", "A (2).mp4" }, true);

        LSeriesGroup lSeriesGroup = Assert.Single(lSeriesGroups);
        Assert.Equal("A", lSeriesGroup.LSeriesName);
        Assert.Equal(new[] { "A (1).mp4", "A (2).mp4" }, lSeriesGroup.LSeriesPaths);
    }

    [Fact]
    public void DifferentBaseNames_FormSeparateGroups()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            TInterface.TSeriesResolve(new[] { "A (1).mp4", "B (1).mp4" }, true);

        Assert.Equal(2, lSeriesGroups.Count);
        Assert.Contains(lSeriesGroups, lSeriesGroup => lSeriesGroup.LSeriesPaths.Single() == "A (1).mp4");
        Assert.Contains(lSeriesGroups, lSeriesGroup => lSeriesGroup.LSeriesPaths.Single() == "B (1).mp4");
    }
}
