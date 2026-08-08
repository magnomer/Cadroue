using System.Text.Json;
using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests;

public sealed class LPreferenceStateTests
{
    [Fact]
    public void LPreferenceVerticalTabsDefaultsToHorizontal()
    {
        Assert.False(LPreferenceState.LPreferenceDefaultCreate().LPreferenceVerticalTabs);
    }

    [Fact]
    public void LPreferenceCloneKeepsVerticalTabs()
    {
        LPreferenceState lPreferenceState = LPreferenceState.LPreferenceDefaultCreate();
        lPreferenceState.LPreferenceVerticalTabs = true;

        LPreferenceState lPreferenceClone = lPreferenceState.LPreferenceClone();

        Assert.True(lPreferenceClone.LPreferenceVerticalTabs);
    }

    [Fact]
    public void LPreferenceDifferenceReportsVerticalTabs()
    {
        LPreferenceState lPreferenceBefore = LPreferenceState.LPreferenceDefaultCreate();
        LPreferenceState lPreferenceAfter = lPreferenceBefore.LPreferenceClone();
        lPreferenceAfter.LPreferenceVerticalTabs = true;

        Assert.Contains(
            "Vertical tabs: False -> True",
            lPreferenceAfter.LPreferenceDifferenceRead(lPreferenceBefore));
    }

    [Fact]
    public void LPreferenceLegacyJsonWithoutVerticalTabsStaysHorizontal()
    {
        LPreferenceState lPreferenceState = JsonSerializer.Deserialize<LPreferenceState>("{\"LPreferenceLanguage\":\"en\"}")!;
        lPreferenceState.LPreferenceNormalize();

        Assert.False(lPreferenceState.LPreferenceVerticalTabs);
    }

    [Fact]
    public void LPreferenceJsonRoundTripKeepsVerticalTabs()
    {
        LPreferenceState lPreferenceState = LPreferenceState.LPreferenceDefaultCreate();
        lPreferenceState.LPreferenceVerticalTabs = true;

        string lPreferenceJson = JsonSerializer.Serialize(lPreferenceState);
        LPreferenceState lPreferenceRestored = JsonSerializer.Deserialize<LPreferenceState>(lPreferenceJson)!;

        Assert.True(lPreferenceRestored.LPreferenceVerticalTabs);
    }
}
