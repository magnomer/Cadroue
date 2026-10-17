using Cadroue.Conduct;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TFilterPreset
{
    [Fact]
    public void Choice_FollowsOwnTable_SelectByIndex()
    {
        CFilter high = TInterface.TFilterCreate(true);
        CFilter low = TInterface.TFilterCreate(false);

        Assert.Equal(LPassband.LPassbandHighPresets.Count + 1, TInterface.TFilterNamesRead(high).Count);
        Assert.Equal(2, TInterface.TFilterChosenRead(high));
        Assert.Equal(0, TInterface.TFilterChosenRead(low));

        TInterface.TFilterChoiceSelect(high, 0);
        Assert.Equal("Rumble", TInterface.TFilterTokenRead(high));
        Assert.Equal(30, TInterface.TFilterValueRead(high, 0));
        TInterface.TFilterChoiceSelect(low, 4);
        Assert.Equal("Telephone", TInterface.TFilterTokenRead(low));
    }

    [Fact]
    public void Poles_IndexAndResonance()
    {
        CFilter filter = TInterface.TFilterCreate(true);
        Assert.Equal(1, TInterface.TFilterPoleRead(filter));
        Assert.True(filter.CFilterResonanceActive);
        Assert.Equal(2, filter.CFilterPoleChoices.Count);

        TInterface.TFilterPolesSelect(filter, 0);
        Assert.Equal(1, filter.CFilterPoles);
        Assert.Equal(0, TInterface.TFilterPoleRead(filter));
        Assert.False(filter.CFilterResonanceActive);
        TInterface.TFilterPolesSelect(filter, 9);
        Assert.Equal(0, TInterface.TFilterPoleRead(filter));
    }
}
