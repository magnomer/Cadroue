using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class NoiseReductionPresetTests
{
    [Fact]
    public void MediumPreset_ExactSettings_AreMatched()
    {
        Assert.Equal("Medium", LGrainCatalog.LGrainMatch(12, -50, 6, 0.5, -38, LGrain.LGrainWhite));
    }

    [Fact]
    public void MediumPreset_SettingsWithinTolerance_AreMatched()
    {
        Assert.Equal("Medium", LGrainCatalog.LGrainMatch(12.04, -50, 6, 0.5, -38, LGrain.LGrainWhite));
    }

    [Fact]
    public void MediumPreset_SettingsOutsideTolerance_AreNotMatched()
    {
        Assert.Null(LGrainCatalog.LGrainMatch(12.1, -50, 6, 0.5, -38, LGrain.LGrainWhite));
    }

    [Fact]
    public void ShellacPreset_ExactSettings_AreMatched()
    {
        Assert.Equal("Shellac", LGrainCatalog.LGrainMatch(12, -50, 8, 0.5, -35, LGrain.LGrainShellac));
    }

    [Fact]
    public void CustomSettings_DoNotMatchPreset()
    {
        Assert.Null(LGrainCatalog.LGrainMatch(1, -70, 1, 0.1, -10, LGrain.LGrainWhite));
    }

    [Fact]
    public void KnownPresetToken_ReturnsPreset()
    {
        LGrainPreset? preset = LGrainCatalog.LGrainRead("Medium");
        Assert.NotNull(preset);
        Assert.Equal(12, preset!.LGrainReduction);
    }

    [Fact]
    public void UnknownPresetToken_ReturnsNull()
    {
        Assert.Null(LGrainCatalog.LGrainRead("Nope"));
    }

    [Fact]
    public void VinylToken_ParsesAsVinyl()
    {
        Assert.Equal(LGrain.LGrainVinyl, LGrainCatalog.LGrainParse("Vinyl"));
    }

    [Fact]
    public void ShellacToken_ParsesAsShellac()
    {
        Assert.Equal(LGrain.LGrainShellac, LGrainCatalog.LGrainParse("Shellac"));
    }

    [Fact]
    public void EmptyToken_ParsesAsWhite()
    {
        Assert.Equal(LGrain.LGrainWhite, LGrainCatalog.LGrainParse(""));
    }

    [Fact]
    public void ShellacValue_FormatsAsShellacToken()
    {
        Assert.Equal("Shellac", LGrainCatalog.LGrainFormat(LGrain.LGrainShellac));
    }

    [Theory]
    [InlineData(LGrain.LGrainWhite)]
    [InlineData(LGrain.LGrainVinyl)]
    [InlineData(LGrain.LGrainShellac)]
    public void StoredGrainValue_FormatAndParse_RoundTrips(LGrain type)
    {
        Assert.Equal(type, LGrainCatalog.LGrainParse(LGrainCatalog.LGrainFormat(type)));
    }
}
