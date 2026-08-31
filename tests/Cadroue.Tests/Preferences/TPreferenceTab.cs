using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests;

public sealed class TPreferenceTab
{
    [Fact]
    public void EmptyDefaultTabs_ArePreserved()
    {
        LPreferenceState lPreferenceState = TInterface.TPreferenceDefaultCreate();
        lPreferenceState.LPreferenceStartupTabs.Clear();

        TInterface.TPreferenceNormalize(lPreferenceState);

        Assert.Empty(lPreferenceState.LPreferenceStartupTabs);
    }

    [Fact]
    public void MissingDefaultTabs_UseSplitFallback()
    {
        LPreferenceState lPreferenceState = TInterface.TPreferenceDefaultCreate();
        lPreferenceState.LPreferenceStartupTabs = null!;

        TInterface.TPreferenceNormalize(lPreferenceState);

        Assert.Equal(new[] { "Split" }, lPreferenceState.LPreferenceStartupTabs);
    }

    [Fact]
    public void VerticalTabs_DefaultToHorizontal()
    {
        Assert.False(TInterface.TPreferenceDefaultCreate().LPreferenceVerticalTabs);
    }

    [Fact]
    public void VerticalTabs_Clone_PreservesSelection()
    {
        LPreferenceState lPreferenceState = TInterface.TPreferenceDefaultCreate();
        lPreferenceState.LPreferenceVerticalTabs = true;

        LPreferenceState lPreferenceClone = TInterface.TPreferenceClone(lPreferenceState);

        Assert.True(lPreferenceClone.LPreferenceVerticalTabs);
    }

    [Fact]
    public void VerticalTabs_Changed_AreReportedInDifference()
    {
        LPreferenceState lPreferenceBefore = TInterface.TPreferenceDefaultCreate();
        LPreferenceState lPreferenceAfter = TInterface.TPreferenceClone(lPreferenceBefore);
        lPreferenceAfter.LPreferenceVerticalTabs = true;

        Assert.Contains(
            "Vertical tabs: False -> True",
            TInterface.TPreferenceDifferenceRead(lPreferenceAfter, lPreferenceBefore));
    }
}
