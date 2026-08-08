using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

public sealed class LPresetNameTests
{
    private static void Seed(params string[] lNames)
    {
        LPreset.LPresetNames.Clear();
        foreach (string lName in lNames)
        {
            LPreset.LPresetNames.Add(lName);
        }
    }

    [Fact]
    public void NameUniqueCreate_FreeBase_ReturnsUnchanged()
    {
        Seed("Other");
        Assert.Equal("Preset", LPreset.LPresetNameCreate("Preset"));
    }

    [Fact]
    public void NameUniqueCreate_ExistingBase_ReturnsSecond()
    {
        Seed("Preset");
        Assert.Equal("Preset 2", LPreset.LPresetNameCreate("Preset"));
    }

    [Fact]
    public void NameUniqueCreate_BaseAndSecondExist_ReturnsThird()
    {
        Seed("Preset", "Preset 2");
        Assert.Equal("Preset 3", LPreset.LPresetNameCreate("Preset"));
    }

    [Fact]
    public void NameUniqueCreate_ComparisonIsCaseInsensitive()
    {
        Seed("preset");
        Assert.Equal("Preset 2", LPreset.LPresetNameCreate("Preset"));
    }
}
