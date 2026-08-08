using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LGroupSelectionTests
{
    [Fact]
    public void NameModeRequest_ChangesOwnerStateAndNotifiesSubscriber()
    {
        var lGroupSelection = new LGroupSelection();
        int lGroupChanges = 0;
        lGroupSelection.LGroupSelectionChange += () => lGroupChanges++;

        lGroupSelection.LGroupNameModeRequest(LSeriesNameMode.LSeriesNameFirst);

        Assert.Equal(LSeriesNameMode.LSeriesNameFirst, lGroupSelection.LGroupNameMode);
        Assert.Equal(1, lGroupChanges);
    }

    [Fact]
    public void Resolve_UsesOwnerNamingMode()
    {
        var lGroupSelection = new LGroupSelection(
            lGroupAuto: true,
            lGroupStrict: true,
            lGroupNameMode: LSeriesNameMode.LSeriesNameFirst);

        IReadOnlyList<LSeriesGroup> lGroups =
            lGroupSelection.LGroupResolve(new[] { "A (1).mp4", "A (2).mp4" });

        Assert.Equal("A (1)", Assert.Single(lGroups).Name);
    }

    [Fact]
    public void SameRequest_DoesNotNotifySubscriber()
    {
        var lGroupSelection = new LGroupSelection();
        int lGroupChanges = 0;
        lGroupSelection.LGroupSelectionChange += () => lGroupChanges++;

        lGroupSelection.LGroupNameModeRequest(LSeriesNameMode.LSeriesNameRemove);

        Assert.Equal(0, lGroupChanges);
    }
}
