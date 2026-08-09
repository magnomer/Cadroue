using System.Collections.Generic;
using System.Linq;

using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class FileGroupingTests
{
    [Fact]
    public void UnnumberedFiles_FormSeparateGroups()
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
    public void LooseGrouping_CombinesConsecutiveFiles()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            LSeries.LSeriesResolve(new[] { "A (1).mp4", "A (2).mp4" }, false);

        LSeriesGroup lSeriesGroup = Assert.Single(lSeriesGroups);
        Assert.Equal("A", lSeriesGroup.Name);
        Assert.Equal(new[] { "A (1).mp4", "A (2).mp4" }, lSeriesGroup.Paths);
    }

    [Fact]
    public void LooseGrouping_CombinesFilesAcrossNumberGaps()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            LSeries.LSeriesResolve(new[] { "A (1).mp4", "A (3).mp4" }, false);

        LSeriesGroup lSeriesGroup = Assert.Single(lSeriesGroups);
        Assert.Equal("A", lSeriesGroup.Name);
        Assert.Equal(2, lSeriesGroup.Paths.Count);
    }

    [Fact]
    public void StrictGrouping_SplitsFilesAcrossNumberGaps()
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
    public void StrictGrouping_KeepsConsecutiveFilesTogether()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            LSeries.LSeriesResolve(new[] { "A (1).mp4", "A (2).mp4" }, true);

        LSeriesGroup lSeriesGroup = Assert.Single(lSeriesGroups);
        Assert.Equal("A", lSeriesGroup.Name);
        Assert.Equal(new[] { "A (1).mp4", "A (2).mp4" }, lSeriesGroup.Paths);
    }

    [Fact]
    public void DifferentBaseNames_FormSeparateGroups()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            LSeries.LSeriesResolve(new[] { "A (1).mp4", "B (1).mp4" }, true);

        Assert.Equal(2, lSeriesGroups.Count);
        Assert.Contains(lSeriesGroups, lSeriesGroup => lSeriesGroup.Paths.Single() == "A (1).mp4");
        Assert.Contains(lSeriesGroups, lSeriesGroup => lSeriesGroup.Paths.Single() == "B (1).mp4");
    }
}
