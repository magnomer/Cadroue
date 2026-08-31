using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TPresetNaming
{
    [Fact]
    public void FreeBase_ReturnsUnchanged()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Other");
        Assert.Equal("Preset", presets.TPresetNameCreate("Preset"));
    }

    [Fact]
    public void ExistingBase_ReturnsSecond()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Preset");
        Assert.Equal("Preset 2", presets.TPresetNameCreate("Preset"));
    }

    [Fact]
    public void BaseAndSecondExist_ReturnsThird()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Preset", "Preset 2");
        Assert.Equal("Preset 3", presets.TPresetNameCreate("Preset"));
    }

    [Fact]
    public void Comparison_IsCaseInsensitive()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("preset");
        Assert.Equal("Preset 2", presets.TPresetNameCreate("Preset"));
    }
}
