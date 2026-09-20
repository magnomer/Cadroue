using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TFilterPreset
{
    [Fact]
    public void Choice_FollowsOwnTable_SelectByIndex()
    {
        LFilter high = TInterface.TFilterCreate(true);
        LFilter low = TInterface.TFilterCreate(false);

        Assert.Equal(
            LPassband.LPassbandHighPresets.Count + 1, TInterface.TFilterChoiceRead(high).LInspectorChoiceNames.Count);
        Assert.Equal(2, TInterface.TFilterChoiceRead(high).LInspectorChoiceIndex);
        Assert.Equal(0, TInterface.TFilterChoiceRead(low).LInspectorChoiceIndex);

        TInterface.TFilterChoiceSelect(high, 0);
        Assert.Equal("Rumble", TInterface.TFilterTokenRead(high));
        Assert.Equal(30, TInterface.TFilterStepRead(high).LWorkPassFrequency);
        TInterface.TFilterChoiceSelect(low, 4);
        Assert.Equal("Telephone", TInterface.TFilterTokenRead(low));
    }

    [Fact]
    public void Poles_IndexAndResonance()
    {
        LFilter filter = TInterface.TFilterCreate(true);
        Assert.Equal(1, filter.LFilterPolesIndex);
        Assert.True(filter.LFilterResonanceActive);
        Assert.Equal(2, filter.LFilterPoles.Count);

        TInterface.TFilterPolesSelect(filter, 0);
        Assert.Equal(1, TInterface.TFilterStepRead(filter).LWorkPassPoles);
        Assert.Equal(0, filter.LFilterPolesIndex);
        Assert.False(filter.LFilterResonanceActive);
        TInterface.TFilterPolesSelect(filter, 9);
        Assert.Equal(0, filter.LFilterPolesIndex);
    }
}
