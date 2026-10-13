using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Engine;

[CollectionDefinition("Preset", DisableParallelization = true)]
public sealed class TPresetCollection { }

[Collection("Preset")]
public sealed class TPresetFileName
{
    [Fact]
    public void InvalidCharacters_ReplacedWithUnderscore()
    {
        using TPreset presets = new();
        Assert.Equal("a_b_c_d", presets.TPresetFileRead("a/b:c?d"));
    }

    [Fact]
    public void TrimsSurroundingSpaces()
    {
        using TPreset presets = new();
        Assert.Equal("Clean", presets.TPresetFileRead("  Clean  "));
    }

    [Fact]
    public void CleanName_ReturnedUnchanged()
    {
        using TPreset presets = new();
        Assert.Equal("Clean Name", presets.TPresetFileRead("Clean Name"));
    }
}
