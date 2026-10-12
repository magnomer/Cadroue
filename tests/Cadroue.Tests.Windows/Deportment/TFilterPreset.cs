using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TFilterPreset
{
    [Fact]
    public void Choice_FollowsOwnTable_SelectByIndex()
    {
        QFilter high = TInterface.TFilterCreate(true);
        QFilter low = TInterface.TFilterCreate(false);

        Assert.Equal(
            LPassband.LPassbandHighPresets.Count + 1, TInterface.TFilterChoiceRead(high).QInspectorChoiceNames.Count);
        Assert.Equal(2, TInterface.TFilterChoiceRead(high).QInspectorChoiceIndex);
        Assert.Equal(0, TInterface.TFilterChoiceRead(low).QInspectorChoiceIndex);

        TInterface.TFilterChoiceSelect(high, 0);
        Assert.Equal("Rumble", TInterface.TFilterTokenRead(high));
        Assert.Equal(30, TInterface.TFilterStepRead(high).LWorkPassFrequency);
        TInterface.TFilterChoiceSelect(low, 4);
        Assert.Equal("Telephone", TInterface.TFilterTokenRead(low));
    }

    [Fact]
    public void Poles_IndexAndResonance()
    {
        QFilter filter = TInterface.TFilterCreate(true);
        Assert.Equal(1, filter.QFilterPolesIndex);
        Assert.True(filter.QFilterResonanceActive);
        Assert.Equal(2, filter.QFilterPoles.Count);

        TInterface.TFilterPolesSelect(filter, 0);
        Assert.Equal(1, TInterface.TFilterStepRead(filter).LWorkPassPoles);
        Assert.Equal(0, filter.QFilterPolesIndex);
        Assert.False(filter.QFilterResonanceActive);
        TInterface.TFilterPolesSelect(filter, 9);
        Assert.Equal(0, filter.QFilterPolesIndex);
    }
}
