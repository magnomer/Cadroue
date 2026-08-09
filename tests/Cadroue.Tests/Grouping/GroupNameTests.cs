using System.Collections.Generic;

using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class GroupNameTests
{
    [Fact]
    public void FirstNameMode_UsesLowestNumberedFile()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            TInterface.SeriesResolve(
                new[] { "A (2).mp4", "A (1).mp4" },
                true,
                LSeriesNameMode.LSeriesNameFirst);

        LSeriesGroup lSeriesGroup = Assert.Single(lSeriesGroups);
        Assert.Equal("A (1)", lSeriesGroup.Name);
        Assert.Equal(new[] { "A (1).mp4", "A (2).mp4" }, lSeriesGroup.Paths);
    }

    [Fact]
    public void FirstNameMode_UsesLowestNumberedFileAcrossGaps()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            TInterface.SeriesResolve(
                new[] { "A (3).mp4", "A (1).mp4" },
                false,
                LSeriesNameMode.LSeriesNameFirst);

        LSeriesGroup lSeriesGroup = Assert.Single(lSeriesGroups);
        Assert.Equal("A (1)", lSeriesGroup.Name);
        Assert.Equal(new[] { "A (1).mp4", "A (3).mp4" }, lSeriesGroup.Paths);
    }

    [Fact]
    public void RemoveNameMode_UsesBaseNameByDefault()
    {
        IReadOnlyList<LSeriesGroup> lSeriesGroups =
            TInterface.SeriesResolve(new[] { "A (1).mp4", "A (2).mp4" }, true);

        Assert.Equal("A", Assert.Single(lSeriesGroups).Name);
    }

    [Fact]
    public void OwnerNameMode_DeterminesResolvedGroupName()
    {
        LGroupSelection lGroupSelection = TInterface.GroupSelectionCreate(
            groupAuto: true,
            groupStrict: true,
            nameMode: LSeriesNameMode.LSeriesNameFirst);

        IReadOnlyList<LSeriesGroup> lGroups =
            TInterface.GroupResolve(lGroupSelection, new[] { "A (1).mp4", "A (2).mp4" });

        Assert.Equal("A (1)", Assert.Single(lGroups).Name);
    }
}
