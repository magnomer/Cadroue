using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
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

    [Fact]
    public void FileNameCreate_InvalidCharacters_ReplacedWithUnderscore()
    {
        Assert.Equal("a_b_c_d", LPreset.LPresetFileFormat("a/b:c?d"));
    }

    [Fact]
    public void FileNameCreate_TrimsSurroundingSpaces()
    {
        Assert.Equal("Clean", LPreset.LPresetFileFormat("  Clean  "));
    }

    [Fact]
    public void FileNameCreate_CleanName_ReturnedUnchanged()
    {
        Assert.Equal("Clean Name", LPreset.LPresetFileFormat("Clean Name"));
    }

    [Fact]
    public void ImportNameResolve_NonBlankStoredName_ReturnedTrimmed()
    {
        Assert.Equal("Stored", LPreset.LPresetNameResolve("  Stored  ", @"C:\dir\file.json"));
    }

    [Fact]
    public void ImportNameResolve_BlankStoredName_FallsBackToFileStem()
    {
        Assert.Equal("file", LPreset.LPresetNameResolve("   ", @"C:\dir\file.json"));
    }
}
