using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TContourPreset
{
    [Fact]
    public void EqualizerPreset_BandGrid_HasTenBands()
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
    public void EqualizerPreset_KnownToken_HasTenGains(string token)
    {
        double[]? gains = TInterface.TContourGainsRead(token);
        Assert.NotNull(gains);
        Assert.Equal(10, gains!.Length);
    }

    [Fact]
    public void EqualizerPreset_UnknownToken_ReturnsNoGains()
    {
        Assert.Null(TInterface.TContourGainsRead("Custom"));
    }

    [Fact]
    public void EqualizerPreset_Catalog_HasNinePresets()
    {
        Assert.Equal(9, TInterface.TContourTokensRead().Count);
    }

    [Fact]
    public void EqualizerPreset_ExactBassBoostSettings_Match()
    {
        double[] gains = { 6, 5, 3, 1, 0, 0, 0, 0, 0, 0 };
        Assert.True(TInterface.TContourMatch(
            LContourCatalog.LContourBandGrid, gains, TInterface.TContourGainsRead("Bass boost")!));
    }

    [Fact]
    public void EqualizerPreset_SettingsWithinTolerance_Match()
    {
        double[] freqs = { 31.4, 62, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };
        double[] gains = { 6.04, 5, 3, 1, 0, 0, 0, 0, 0, 0 };
        Assert.True(TInterface.TContourMatch(
            freqs, gains, TInterface.TContourGainsRead("Bass boost")!));
    }

    [Fact]
    public void EqualizerPreset_GainBeyondTolerance_DoesNotMatch()
    {
        double[] gains = { 6.1, 5, 3, 1, 0, 0, 0, 0, 0, 0 };
        Assert.False(TInterface.TContourMatch(
            LContourCatalog.LContourBandGrid, gains, TInterface.TContourGainsRead("Bass boost")!));
    }

    [Fact]
    public void EqualizerPreset_WrongBandCount_DoesNotMatch()
    {
        double[] freqs = { 31, 62 };
        double[] gains = { 0, 0 };
        Assert.False(TInterface.TContourMatch(freqs, gains, TInterface.TContourGainsRead("Flat")!));
    }

    [Fact]
    public void EqualizerPreset_ZeroGains_FindFlatPreset()
    {
        double[] gains = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        Assert.Equal("Flat", TInterface.TContourPresetFind(LContourCatalog.LContourBandGrid, gains));
    }

    [Fact]
    public void EqualizerPreset_DeviatedGains_FindNoPreset()
    {
        double[] gains = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        Assert.Null(TInterface.TContourPresetFind(LContourCatalog.LContourBandGrid, gains));
    }
}
