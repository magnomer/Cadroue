using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class GroupNamingModeTests
{
    [Fact]
    public void DifferentNameMode_ChangesOwnerStateAndNotifiesSubscriber()
    {
        LGroupSelection lGroupSelection = TInterface.GroupSelectionCreate();
        int lGroupChanges = 0;
        lGroupSelection.LGroupSelectionChange += () => lGroupChanges++;

        TInterface.GroupNameModeRequest(lGroupSelection, LSeriesNameMode.LSeriesNameFirst);

        Assert.Equal(LSeriesNameMode.LSeriesNameFirst, lGroupSelection.LGroupNameMode);
        Assert.Equal(1, lGroupChanges);
    }

    [Fact]
    public void SameNameMode_DoesNotNotifySubscriber()
    {
        LGroupSelection lGroupSelection = TInterface.GroupSelectionCreate();
        int lGroupChanges = 0;
        lGroupSelection.LGroupSelectionChange += () => lGroupChanges++;

        TInterface.GroupNameModeRequest(lGroupSelection, LSeriesNameMode.LSeriesNameBase);

        Assert.Equal(0, lGroupChanges);
    }
}
