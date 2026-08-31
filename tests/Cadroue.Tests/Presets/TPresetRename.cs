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
        Assert.False(presets.TPresetSelectionChange("Current", "Current", "   ").Ok);
    }

    [Fact]
    public void UnchangedName_ReturnsFalse()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Current");
        Assert.False(presets.TPresetSelectionChange("Current", "Current", "Current").Ok);
    }

    [Fact]
    public void NativeOldName_ReturnsFalse()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate(presets.TPresetNativeName);
        Assert.False(presets.TPresetSelectionChange("Current", presets.TPresetNativeName, "Renamed").Ok);
    }

    [Fact]
    public void NewNameExists_ReturnsFalse()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Current", "Taken");
        Assert.False(presets.TPresetSelectionChange("Current", "Current", "Taken").Ok);
    }

    [Fact]
    public void CurrentPreset_UpdatesSelectionName()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Current");
        (bool ok, string selectionName) = presets.TPresetSelectionChange("Current", "Current", "Renamed");
        Assert.True(ok);
        Assert.Equal("Renamed", selectionName);
    }

    [Fact]
    public void NonCurrentPreset_KeepsSelectionName()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Current", "Other");
        (bool ok, string selectionName) = presets.TPresetSelectionChange("Current", "Other", "Renamed");
        Assert.True(ok);
        Assert.Equal("Current", selectionName);
    }

    [Fact]
    public void CurrentPreset_SelectionNameNewBeforeRenameSeam()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Current");
        Assert.Equal("Renamed", presets.TPresetSeamChange("Current", "Current", "Renamed"));
    }
}
