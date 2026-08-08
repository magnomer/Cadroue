using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("Preset")]
public sealed class LPresetCollection { }

[Collection("Preset")]
public sealed class LPresetSelectionRenameTests
{
    private static void Seed(params string[] lNames)
    {
        LPreset.LPresetNames.Clear();
        foreach (string lName in lNames)
        {
            LPreset.LPresetNames.Add(lName);
        }

        LPresetSelection.LPresetLoadSeam = lName => new LPresetRecord { LPresetName = lName };
        LPresetSelection.LPresetRenameSeam = (lOld, lNew, lRecord) => true;
    }

    [Fact]
    public void Rename_BlankNewName_ReturnsFalse()
    {
        Seed("Current");
        var lSelection = new LPresetSelection("Current");
        Assert.False(lSelection.LPresetSelectionRename("Current", "   "));
    }

    [Fact]
    public void Rename_UnchangedName_ReturnsFalse()
    {
        Seed("Current");
        var lSelection = new LPresetSelection("Current");
        Assert.False(lSelection.LPresetSelectionRename("Current", "Current"));
    }

    [Fact]
    public void Rename_NativeOldName_ReturnsFalse()
    {
        Seed(LPreset.LPresetAudioDefault);
        var lSelection = new LPresetSelection("Current");
        Assert.False(lSelection.LPresetSelectionRename(LPreset.LPresetAudioDefault, "Renamed"));
    }

    [Fact]
    public void Rename_NewNameExists_ReturnsFalse()
    {
        Seed("Current", "Taken");
        var lSelection = new LPresetSelection("Current");
        Assert.False(lSelection.LPresetSelectionRename("Current", "Taken"));
    }

    [Fact]
    public void Rename_CurrentPreset_UpdatesSelectionName()
    {
        Seed("Current");
        var lSelection = new LPresetSelection("Current");
        Assert.True(lSelection.LPresetSelectionRename("Current", "Renamed"));
        Assert.Equal("Renamed", lSelection.LPresetSelectionName);
    }

    [Fact]
    public void Rename_NonCurrentPreset_KeepsSelectionName()
    {
        Seed("Current", "Other");
        var lSelection = new LPresetSelection("Current");
        Assert.True(lSelection.LPresetSelectionRename("Other", "Renamed"));
        Assert.Equal("Current", lSelection.LPresetSelectionName);
    }
}
