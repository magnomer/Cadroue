using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LContourTests
{
    [Fact]
    public void BandGrid_HasTenBands()
    {
        Assert.Equal(10, LContourCatalog.LContourBandGrid.Length);
    }

    [Theory]
    [InlineData("Flat")]
    [InlineData("Bass boost")]
    [InlineData("Bright")]
    [InlineData("Warm")]
    [InlineData("Loudness")]
    [InlineData("Vocal")]
    [InlineData("De-ess")]
    [InlineData("Podcast")]
    [InlineData("Telephone")]
    public void GainsRead_KnownToken_HasTenGains(string token)
    {
        double[]? gains = LContourCatalog.LContourGainsRead(token);
        Assert.NotNull(gains);
        Assert.Equal(10, gains!.Length);
    }

    [Fact]
    public void GainsRead_UnknownToken_ReturnsNull()
    {
        Assert.Null(LContourCatalog.LContourGainsRead("Custom"));
    }

    [Fact]
    public void TokensRead_ReturnsNinePresets()
    {
        Assert.Equal(9, LContourCatalog.LContourTokensRead().Count);
    }

    [Fact]
    public void Match_ExactBassBoost_ReturnsTrue()
    {
        double[] gains = { 6, 5, 3, 1, 0, 0, 0, 0, 0, 0 };
        Assert.True(LContourCatalog.LContourMatch(
            LContourCatalog.LContourBandGrid, gains, LContourCatalog.LContourGainsRead("Bass boost")!));
    }

    [Fact]
    public void Match_WithinTolerance_ReturnsTrue()
    {
        double[] freqs = { 31.4, 62, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };
        double[] gains = { 6.04, 5, 3, 1, 0, 0, 0, 0, 0, 0 };
        Assert.True(LContourCatalog.LContourMatch(
            freqs, gains, LContourCatalog.LContourGainsRead("Bass boost")!));
    }

    [Fact]
    public void Match_GainOutsideTolerance_ReturnsFalse()
    {
        double[] gains = { 6.1, 5, 3, 1, 0, 0, 0, 0, 0, 0 };
        Assert.False(LContourCatalog.LContourMatch(
            LContourCatalog.LContourBandGrid, gains, LContourCatalog.LContourGainsRead("Bass boost")!));
    }

    [Fact]
    public void Match_WrongCount_ReturnsFalse()
    {
        double[] freqs = { 31, 62 };
        double[] gains = { 0, 0 };
        Assert.False(LContourCatalog.LContourMatch(freqs, gains, LContourCatalog.LContourGainsRead("Flat")!));
    }

    [Fact]
    public void PresetFind_Zeros_ReturnsFlat()
    {
        double[] gains = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        Assert.Equal("Flat", LContourCatalog.LContourPresetFind(LContourCatalog.LContourBandGrid, gains));
    }

    [Fact]
    public void PresetFind_Deviated_ReturnsNull()
    {
        double[] gains = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        Assert.Null(LContourCatalog.LContourPresetFind(LContourCatalog.LContourBandGrid, gains));
    }
}
