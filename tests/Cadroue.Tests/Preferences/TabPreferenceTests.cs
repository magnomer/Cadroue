using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests;

public sealed class TabPreferenceTests
{
    [Fact]
    public void VerticalTabs_DefaultToHorizontal()
    {
        Assert.False(LPreferenceState.LPreferenceDefaultCreate().LPreferenceVerticalTabs);
    }

    [Fact]
    public void VerticalTabs_Clone_PreservesSelection()
    {
        LPreferenceState lPreferenceState = LPreferenceState.LPreferenceDefaultCreate();
        lPreferenceState.LPreferenceVerticalTabs = true;

        LPreferenceState lPreferenceClone = lPreferenceState.LPreferenceClone();

        Assert.True(lPreferenceClone.LPreferenceVerticalTabs);
    }

    [Fact]
    public void VerticalTabs_Changed_AreReportedInDifference()
    {
        LPreferenceState lPreferenceBefore = LPreferenceState.LPreferenceDefaultCreate();
        LPreferenceState lPreferenceAfter = lPreferenceBefore.LPreferenceClone();
        lPreferenceAfter.LPreferenceVerticalTabs = true;

        Assert.Contains(
            "Vertical tabs: False -> True",
            lPreferenceAfter.LPreferenceDifferenceRead(lPreferenceBefore));
    }
}
