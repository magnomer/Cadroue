using System.Text.Json;
using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests;

public sealed class PreferencePersistenceTests
{
    [Fact]
    public void CollapseCompletedBatch_DefaultsToDisabled()
    {
        Assert.False(TInterface.PreferenceDefaultCreate().LPreferenceCollapseDone);
    }

    [Fact]
    public void LegacyJson_WithoutVerticalTabs_DefaultsToHorizontal()
    {
        LPreferenceState lPreferenceState = JsonSerializer.Deserialize<LPreferenceState>("{\"LPreferenceLanguage\":\"en\"}")!;
        TInterface.PreferenceNormalize(lPreferenceState);

        Assert.False(lPreferenceState.LPreferenceVerticalTabs);
    }

    [Fact]
    public void JsonRoundTrip_PreservesVerticalTabs()
    {
        LPreferenceState lPreferenceState = TInterface.PreferenceDefaultCreate();
        lPreferenceState.LPreferenceVerticalTabs = true;

        string lPreferenceJson = JsonSerializer.Serialize(lPreferenceState);
        LPreferenceState lPreferenceRestored = JsonSerializer.Deserialize<LPreferenceState>(lPreferenceJson)!;

        Assert.True(lPreferenceRestored.LPreferenceVerticalTabs);
    }

    [Fact]
    public void JsonRoundTrip_PreservesCollapseCompletedBatch()
    {
        LPreferenceState lPreferenceState = TInterface.PreferenceDefaultCreate();
        lPreferenceState.LPreferenceCollapseDone = true;

        string lPreferenceJson = JsonSerializer.Serialize(lPreferenceState);
        LPreferenceState lPreferenceRestored = JsonSerializer.Deserialize<LPreferenceState>(lPreferenceJson)!;

        Assert.True(lPreferenceRestored.LPreferenceCollapseDone);
    }

    [Fact]
    public void CloneAndDifference_PreserveCollapseCompletedBatch()
    {
        LPreferenceState lPreferenceBefore = TInterface.PreferenceDefaultCreate();
        LPreferenceState lPreferenceAfter = TInterface.PreferenceClone(lPreferenceBefore);
        lPreferenceAfter.LPreferenceCollapseDone = true;

        LPreferenceState lPreferenceClone = TInterface.PreferenceClone(lPreferenceAfter);

        Assert.True(lPreferenceClone.LPreferenceCollapseDone);
        Assert.Contains(
            "Collapse completed batch: False -> True",
            TInterface.PreferenceDifferenceRead(lPreferenceAfter, lPreferenceBefore));
    }

    [Fact]
    public void NewNativePresetGroup_DefaultsToFolded()
    {
        LPreferenceState lPreferenceState = TInterface.PreferenceDefaultCreate();

        Assert.True(TInterface.PreferencePresetGroupFoldedRead(lPreferenceState, "General"));
    }

    [Fact]
    public void JsonRoundTrip_PreservesExpandedPresetGroup()
    {
        LPreferenceState lPreferenceState = TInterface.PreferenceDefaultCreate();
        TInterface.PreferencePresetGroupFoldedSet(lPreferenceState, "General", false);

        string lPreferenceJson = JsonSerializer.Serialize(lPreferenceState);
        LPreferenceState lPreferenceRestored = JsonSerializer.Deserialize<LPreferenceState>(lPreferenceJson)!;
        TInterface.PreferenceNormalize(lPreferenceRestored);

        Assert.False(TInterface.PreferencePresetGroupFoldedRead(lPreferenceRestored, "general"));
    }

    [Fact]
    public void LegacyJson_WithoutPresetGroups_DefaultsNativeGroupToFolded()
    {
        LPreferenceState lPreferenceState = JsonSerializer.Deserialize<LPreferenceState>("{\"LPreferenceLanguage\":\"en\"}")!;
        TInterface.PreferenceNormalize(lPreferenceState);

        Assert.True(TInterface.PreferencePresetGroupFoldedRead(lPreferenceState, "Hardware"));
    }
}
