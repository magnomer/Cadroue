using System.Text.Json;
using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests;

public sealed class PreferencePersistenceTests
{
    [Fact]
    public void LegacyJson_WithoutVerticalTabs_DefaultsToHorizontal()
    {
        LPreferenceState lPreferenceState = JsonSerializer.Deserialize<LPreferenceState>("{\"LPreferenceLanguage\":\"en\"}")!;
        lPreferenceState.LPreferenceNormalize();

        Assert.False(lPreferenceState.LPreferenceVerticalTabs);
    }

    [Fact]
    public void JsonRoundTrip_PreservesVerticalTabs()
    {
        LPreferenceState lPreferenceState = LPreferenceState.LPreferenceDefaultCreate();
        lPreferenceState.LPreferenceVerticalTabs = true;

        string lPreferenceJson = JsonSerializer.Serialize(lPreferenceState);
        LPreferenceState lPreferenceRestored = JsonSerializer.Deserialize<LPreferenceState>(lPreferenceJson)!;

        Assert.True(lPreferenceRestored.LPreferenceVerticalTabs);
    }
}
