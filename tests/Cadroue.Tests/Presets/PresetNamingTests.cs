using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class PresetNamingTests
{
    [Fact]
    public void FreeBase_ReturnsUnchanged()
    {
        using TPresets presets = new();
        presets.SeedNames("Other");
        Assert.Equal("Preset", presets.CreateUniqueName("Preset"));
    }

    [Fact]
    public void ExistingBase_ReturnsSecond()
    {
        using TPresets presets = new();
        presets.SeedNames("Preset");
        Assert.Equal("Preset 2", presets.CreateUniqueName("Preset"));
    }

    [Fact]
    public void BaseAndSecondExist_ReturnsThird()
    {
        using TPresets presets = new();
        presets.SeedNames("Preset", "Preset 2");
        Assert.Equal("Preset 3", presets.CreateUniqueName("Preset"));
    }

    [Fact]
    public void Comparison_IsCaseInsensitive()
    {
        using TPresets presets = new();
        presets.SeedNames("preset");
        Assert.Equal("Preset 2", presets.CreateUniqueName("Preset"));
    }
}
