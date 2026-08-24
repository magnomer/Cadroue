using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests;

public sealed class TabPreferenceTests
{
    [Fact]
    public void EmptyDefaultTabs_ArePreserved()
    {
        LPreferenceState lPreferenceState = TInterface.PreferenceDefaultCreate();
        lPreferenceState.LPreferenceStartupTabs.Clear();

        TInterface.PreferenceNormalize(lPreferenceState);

        Assert.Empty(lPreferenceState.LPreferenceStartupTabs);
    }

    [Fact]
    public void MissingDefaultTabs_UseSplitFallback()
    {
        LPreferenceState lPreferenceState = TInterface.PreferenceDefaultCreate();
        lPreferenceState.LPreferenceStartupTabs = null!;

        TInterface.PreferenceNormalize(lPreferenceState);

        Assert.Equal(new[] { "Split" }, lPreferenceState.LPreferenceStartupTabs);
    }

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
