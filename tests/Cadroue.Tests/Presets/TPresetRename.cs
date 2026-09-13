using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TPresetRename
{
    [Fact]
    public void BlankNewName_ReturnsFalse()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Current");
        Assert.False(presets.TPresetSelectionChange("Current", "Current", "   ").TPresetSelectionOk);
    }

    [Fact]
    public void UnchangedName_ReturnsFalse()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Current");
        Assert.False(presets.TPresetSelectionChange("Current", "Current", "Current").TPresetSelectionOk);
    }

    [Fact]
    public void NativeOldName_ReturnsFalse()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate(presets.TPresetNativeName);
        Assert.False(presets.TPresetSelectionChange(
            "Current",
            presets.TPresetNativeName,
            "Renamed").TPresetSelectionOk);
    }

    [Fact]
    public void NewNameExists_ReturnsFalse()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Current", "Taken");
        Assert.False(presets.TPresetSelectionChange("Current", "Current", "Taken").TPresetSelectionOk);
    }

    [Fact]
    public void CurrentPreset_UpdatesSelectionName()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Current");
        TPresetSelectionResult lResult = presets.TPresetSelectionChange("Current", "Current", "Renamed");
        Assert.True(lResult.TPresetSelectionOk);
        Assert.Equal("Renamed", lResult.TPresetSelectionName);
    }

    [Fact]
    public void NonCurrentPreset_KeepsSelectionName()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Current", "Other");
        TPresetSelectionResult lResult = presets.TPresetSelectionChange("Current", "Other", "Renamed");
        Assert.True(lResult.TPresetSelectionOk);
        Assert.Equal("Current", lResult.TPresetSelectionName);
    }

    [Fact]
    public void CurrentPreset_SelectionNameNewBeforeRenameSeam()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Current");
        Assert.Equal("Renamed", presets.TPresetSeamChange("Current", "Current", "Renamed"));
    }
}
