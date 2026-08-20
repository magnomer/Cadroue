using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class PresetRenameTests
{
    [Fact]
    public void BlankNewName_ReturnsFalse()
    {
        using TPresets presets = new();
        presets.SeedNames("Current");
        Assert.False(presets.RenameSelection("Current", "Current", "   ").Ok);
    }

    [Fact]
    public void UnchangedName_ReturnsFalse()
    {
        using TPresets presets = new();
        presets.SeedNames("Current");
        Assert.False(presets.RenameSelection("Current", "Current", "Current").Ok);
    }

    [Fact]
    public void NativeOldName_ReturnsFalse()
    {
        using TPresets presets = new();
        presets.SeedNames(presets.NativeDefaultName);
        Assert.False(presets.RenameSelection("Current", presets.NativeDefaultName, "Renamed").Ok);
    }

    [Fact]
    public void NewNameExists_ReturnsFalse()
    {
        using TPresets presets = new();
        presets.SeedNames("Current", "Taken");
        Assert.False(presets.RenameSelection("Current", "Current", "Taken").Ok);
    }

    [Fact]
    public void CurrentPreset_UpdatesSelectionName()
    {
        using TPresets presets = new();
        presets.SeedNames("Current");
        (bool ok, string selectionName) = presets.RenameSelection("Current", "Current", "Renamed");
        Assert.True(ok);
        Assert.Equal("Renamed", selectionName);
    }

    [Fact]
    public void NonCurrentPreset_KeepsSelectionName()
    {
        using TPresets presets = new();
        presets.SeedNames("Current", "Other");
        (bool ok, string selectionName) = presets.RenameSelection("Current", "Other", "Renamed");
        Assert.True(ok);
        Assert.Equal("Current", selectionName);
    }

    [Fact]
    public void CurrentPreset_SelectionNameNewBeforeRenameSeam()
    {
        using TPresets presets = new();
        presets.SeedNames("Current");
        Assert.Equal("Renamed", presets.RenameSelectionNameDuringSeam("Current", "Current", "Renamed"));
    }
}
