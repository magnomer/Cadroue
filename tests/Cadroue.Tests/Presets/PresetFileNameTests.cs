using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class PresetFileNameTests
{
    [Fact]
    public void InvalidCharacters_ReplacedWithUnderscore()
    {
        using TPresets presets = new();
        Assert.Equal("a_b_c_d", presets.FileName("a/b:c?d"));
    }

    [Fact]
    public void TrimsSurroundingSpaces()
    {
        using TPresets presets = new();
        Assert.Equal("Clean", presets.FileName("  Clean  "));
    }

    [Fact]
    public void CleanName_ReturnedUnchanged()
    {
        using TPresets presets = new();
        Assert.Equal("Clean Name", presets.FileName("Clean Name"));
    }
}
