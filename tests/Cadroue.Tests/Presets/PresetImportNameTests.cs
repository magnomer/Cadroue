using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class PresetImportNameTests
{
    [Fact]
    public void NonBlankStoredName_ReturnedTrimmed()
    {
        using TPresets presets = new();
        Assert.Equal("Stored", presets.ImportName("  Stored  ", @"C:\dir\file.json"));
    }

    [Fact]
    public void BlankStoredName_FallsBackToFileStem()
    {
        using TPresets presets = new();
        Assert.Equal("file", presets.ImportName("   ", @"C:\dir\file.json"));
    }
}
