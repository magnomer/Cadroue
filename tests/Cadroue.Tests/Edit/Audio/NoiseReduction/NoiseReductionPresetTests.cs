using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class NoiseReductionPresetTests
{
    [Fact]
    public void MediumPreset_ExactSettings_AreMatched()
    {
        Assert.Equal("Medium", TInterface.GrainMatch(12, -50, 6, 0.5, -38, LGrain.LGrainWhite));
    }

    [Fact]
    public void MediumPreset_SettingsWithinTolerance_AreMatched()
    {
        Assert.Equal("Medium", TInterface.GrainMatch(12.04, -50, 6, 0.5, -38, LGrain.LGrainWhite));
    }

    [Fact]
    public void MediumPreset_SettingsOutsideTolerance_AreNotMatched()
    {
        Assert.Null(TInterface.GrainMatch(12.1, -50, 6, 0.5, -38, LGrain.LGrainWhite));
    }

    [Fact]
    public void ShellacPreset_ExactSettings_AreMatched()
    {
        Assert.Equal("Shellac", TInterface.GrainMatch(12, -50, 8, 0.5, -35, LGrain.LGrainShellac));
    }

    [Fact]
    public void CustomSettings_DoNotMatchPreset()
    {
        Assert.Null(TInterface.GrainMatch(1, -70, 1, 0.1, -10, LGrain.LGrainWhite));
    }

    [Fact]
    public void KnownPresetToken_ReturnsPreset()
    {
        LGrainPreset? preset = TInterface.GrainRead("Medium");
        Assert.NotNull(preset);
        Assert.Equal(12, preset!.LGrainReduction);
    }

    [Fact]
    public void UnknownPresetToken_ReturnsNull()
    {
        Assert.Null(TInterface.GrainRead("Nope"));
    }

    [Fact]
    public void VinylToken_ParsesAsVinyl()
    {
        Assert.Equal(LGrain.LGrainVinyl, TInterface.GrainParse("Vinyl"));
    }

    [Fact]
    public void ShellacToken_ParsesAsShellac()
    {
        Assert.Equal(LGrain.LGrainShellac, TInterface.GrainParse("Shellac"));
    }

    [Fact]
    public void EmptyToken_ParsesAsWhite()
    {
        Assert.Equal(LGrain.LGrainWhite, TInterface.GrainParse(""));
    }

    [Fact]
    public void ShellacValue_FormatsAsShellacToken()
    {
        Assert.Equal("Shellac", TInterface.GrainFormat(LGrain.LGrainShellac));
    }

    [Theory]
    [InlineData(LGrain.LGrainWhite)]
    [InlineData(LGrain.LGrainVinyl)]
    [InlineData(LGrain.LGrainShellac)]
    public void StoredGrainValue_FormatAndParse_RoundTrips(LGrain type)
    {
        Assert.Equal(type, TInterface.GrainParse(TInterface.GrainFormat(type)));
    }
}
