using System.Text.Json;
using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests;

public sealed class TPreferencePersistence
{
    [Fact]
    public void CollapseCompletedBatch_DefaultsToDisabled()
    {
        Assert.False(TInterface.TPreferenceDefaultCreate().LPreferenceCollapseDone);
    }

    [Fact]
    public void LegacyJson_WithoutVerticalTabs_DefaultsToHorizontal()
    {
        LPreferenceState lPreferenceState = JsonSerializer.Deserialize<LPreferenceState>("{\"LPreferenceLanguage\":\"en\"}")!;
        TInterface.TPreferenceNormalize(lPreferenceState);

        Assert.False(lPreferenceState.LPreferenceVerticalTabs);
    }

    [Fact]
    public void JsonRoundTrip_PreservesVerticalTabs()
    {
        LPreferenceState lPreferenceState = TInterface.TPreferenceDefaultCreate();
        lPreferenceState.LPreferenceVerticalTabs = true;

        string lPreferenceJson = JsonSerializer.Serialize(lPreferenceState);
        LPreferenceState lPreferenceRestored = JsonSerializer.Deserialize<LPreferenceState>(lPreferenceJson)!;

        Assert.True(lPreferenceRestored.LPreferenceVerticalTabs);
    }

    [Fact]
    public void JsonRoundTrip_PreservesCollapseCompletedBatch()
    {
        LPreferenceState lPreferenceState = TInterface.TPreferenceDefaultCreate();
        lPreferenceState.LPreferenceCollapseDone = true;

        string lPreferenceJson = JsonSerializer.Serialize(lPreferenceState);
        LPreferenceState lPreferenceRestored = JsonSerializer.Deserialize<LPreferenceState>(lPreferenceJson)!;

        Assert.True(lPreferenceRestored.LPreferenceCollapseDone);
    }

    [Fact]
    public void CloneAndDifference_PreserveCollapseCompletedBatch()
    {
        LPreferenceState lPreferenceBefore = TInterface.TPreferenceDefaultCreate();
        LPreferenceState lPreferenceAfter = TInterface.TPreferenceClone(lPreferenceBefore);
        lPreferenceAfter.LPreferenceCollapseDone = true;

        LPreferenceState lPreferenceClone = TInterface.TPreferenceClone(lPreferenceAfter);

        Assert.True(lPreferenceClone.LPreferenceCollapseDone);
        Assert.Contains(
            "Collapse completed batch: False -> True",
            TInterface.TPreferenceDifferenceRead(lPreferenceAfter, lPreferenceBefore));
    }

    [Fact]
    public void NewNativePresetGroup_DefaultsToFolded()
    {
        LPreferenceState lPreferenceState = TInterface.TPreferenceDefaultCreate();

        Assert.True(TInterface.TPreferenceFoldedRead(lPreferenceState, "General"));
    }

    [Fact]
    public void JsonRoundTrip_PreservesExpandedPresetGroup()
    {
        LPreferenceState lPreferenceState = TInterface.TPreferenceDefaultCreate();
        TInterface.TPreferenceFoldedSet(lPreferenceState, "General", false);

        string lPreferenceJson = JsonSerializer.Serialize(lPreferenceState);
        LPreferenceState lPreferenceRestored = JsonSerializer.Deserialize<LPreferenceState>(lPreferenceJson)!;
        TInterface.TPreferenceNormalize(lPreferenceRestored);

        Assert.False(TInterface.TPreferenceFoldedRead(lPreferenceRestored, "general"));
    }

    [Fact]
    public void LegacyJson_WithoutPresetGroups_DefaultsNativeGroupToFolded()
    {
        LPreferenceState lPreferenceState = JsonSerializer.Deserialize<LPreferenceState>("{\"LPreferenceLanguage\":\"en\"}")!;
        TInterface.TPreferenceNormalize(lPreferenceState);

        Assert.True(TInterface.TPreferenceFoldedRead(lPreferenceState, "Hardware"));
    }
}
