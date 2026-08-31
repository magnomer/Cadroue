using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TSeriesMode
{
    [Fact]
    public void DifferentNameMode_ChangesOwnerStateAndNotifiesSubscriber()
    {
        LGroupSelection lGroupSelection = TInterface.TGroupSelectionCreate();
        int lGroupChanges = 0;
        lGroupSelection.LGroupSelectionChange += () => lGroupChanges++;

        TInterface.TGroupModeRead(lGroupSelection, LSeriesNameMode.LSeriesNameFirst);

        Assert.Equal(LSeriesNameMode.LSeriesNameFirst, lGroupSelection.LGroupNameMode);
        Assert.Equal(1, lGroupChanges);
    }

    [Fact]
    public void SameNameMode_DoesNotNotifySubscriber()
    {
        LGroupSelection lGroupSelection = TInterface.TGroupSelectionCreate();
        int lGroupChanges = 0;
        lGroupSelection.LGroupSelectionChange += () => lGroupChanges++;

        TInterface.TGroupModeRead(lGroupSelection, LSeriesNameMode.LSeriesNameBase);

        Assert.Equal(0, lGroupChanges);
    }
}
