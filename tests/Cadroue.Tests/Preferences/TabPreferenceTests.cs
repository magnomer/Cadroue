using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests;

public sealed class TabPreferenceTests
{
    [Fact]
    public void VerticalTabs_DefaultToHorizontal()
    {
        Assert.False(TInterface.PreferenceDefaultCreate().LPreferenceVerticalTabs);
    }

    [Fact]
    public void VerticalTabs_Clone_PreservesSelection()
    {
        LPreferenceState lPreferenceState = TInterface.PreferenceDefaultCreate();
        lPreferenceState.LPreferenceVerticalTabs = true;

        LPreferenceState lPreferenceClone = TInterface.PreferenceClone(lPreferenceState);

        Assert.True(lPreferenceClone.LPreferenceVerticalTabs);
    }

    [Fact]
    public void VerticalTabs_Changed_AreReportedInDifference()
    {
        LPreferenceState lPreferenceBefore = TInterface.PreferenceDefaultCreate();
        LPreferenceState lPreferenceAfter = TInterface.PreferenceClone(lPreferenceBefore);
        lPreferenceAfter.LPreferenceVerticalTabs = true;

        Assert.Contains(
            "Vertical tabs: False -> True",
            TInterface.PreferenceDifferenceRead(lPreferenceAfter, lPreferenceBefore));
    }
}
