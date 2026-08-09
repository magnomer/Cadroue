using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class GroupNamingModeTests
{
    [Fact]
    public void DifferentNameMode_ChangesOwnerStateAndNotifiesSubscriber()
    {
        var lGroupSelection = new LGroupSelection();
        int lGroupChanges = 0;
        lGroupSelection.LGroupSelectionChange += () => lGroupChanges++;

        lGroupSelection.LGroupNameModeRequest(LSeriesNameMode.LSeriesNameFirst);

        Assert.Equal(LSeriesNameMode.LSeriesNameFirst, lGroupSelection.LGroupNameMode);
        Assert.Equal(1, lGroupChanges);
    }

    [Fact]
    public void SameNameMode_DoesNotNotifySubscriber()
    {
        var lGroupSelection = new LGroupSelection();
        int lGroupChanges = 0;
        lGroupSelection.LGroupSelectionChange += () => lGroupChanges++;

        lGroupSelection.LGroupNameModeRequest(LSeriesNameMode.LSeriesNameRemove);

        Assert.Equal(0, lGroupChanges);
    }
}
