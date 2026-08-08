using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LGrainTests
{
    [Fact]
    public void Match_MediumExact_ReturnsMedium()
    {
        Assert.Equal("Medium", LGrainCatalog.LGrainMatch(12, -50, 6, 0.5, -38, LGrain.LGrainWhite));
    }

    [Fact]
    public void Match_MediumWithinTolerance_ReturnsMedium()
    {
        Assert.Equal("Medium", LGrainCatalog.LGrainMatch(12.04, -50, 6, 0.5, -38, LGrain.LGrainWhite));
    }

    [Fact]
    public void Match_MediumOutsideTolerance_ReturnsNull()
    {
        Assert.Null(LGrainCatalog.LGrainMatch(12.1, -50, 6, 0.5, -38, LGrain.LGrainWhite));
    }

    [Fact]
    public void Match_ShellacExact_ReturnsShellac()
    {
        Assert.Equal("Shellac", LGrainCatalog.LGrainMatch(12, -50, 8, 0.5, -35, LGrain.LGrainShellac));
    }

    [Fact]
    public void Match_UnmatchedValues_ReturnsNull()
    {
        Assert.Null(LGrainCatalog.LGrainMatch(1, -70, 1, 0.1, -10, LGrain.LGrainWhite));
    }

    [Fact]
    public void Read_KnownToken_ReturnsPreset()
    {
        LGrainPreset? preset = LGrainCatalog.LGrainRead("Medium");
        Assert.NotNull(preset);
        Assert.Equal(12, preset!.LGrainReduction);
    }

    [Fact]
    public void Read_UnknownToken_ReturnsNull()
    {
        Assert.Null(LGrainCatalog.LGrainRead("Nope"));
    }

    [Fact]
    public void Parse_Vinyl_ReturnsVinyl()
    {
        Assert.Equal(LGrain.LGrainVinyl, LGrainCatalog.LGrainParse("Vinyl"));
    }

    [Fact]
    public void Parse_Shellac_ReturnsShellac()
    {
        Assert.Equal(LGrain.LGrainShellac, LGrainCatalog.LGrainParse("Shellac"));
    }

    [Fact]
    public void Parse_Empty_ReturnsWhite()
    {
        Assert.Equal(LGrain.LGrainWhite, LGrainCatalog.LGrainParse(""));
    }

    [Fact]
    public void Format_Shellac_ReturnsShellac()
    {
        Assert.Equal("Shellac", LGrainCatalog.LGrainFormat(LGrain.LGrainShellac));
    }

    [Theory]
    [InlineData(LGrain.LGrainWhite)]
    [InlineData(LGrain.LGrainVinyl)]
    [InlineData(LGrain.LGrainShellac)]
    public void ParseFormat_RoundTrip_ReturnsSame(LGrain type)
    {
        Assert.Equal(type, LGrainCatalog.LGrainParse(LGrainCatalog.LGrainFormat(type)));
    }
}
