using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TPresetImportName
{
    [Fact]
    public void NonBlankStoredName_ReturnedTrimmed()
    {
        using TPreset presets = new();
        Assert.Equal("Stored", presets.TPresetImportRead("  Stored  ", @"C:\dir\file.json"));
    }

    [Fact]
    public void BlankStoredName_FallsBackToFileStem()
    {
        using TPreset presets = new();
        Assert.Equal("file", presets.TPresetImportRead("   ", @"C:\dir\file.json"));
    }
}
