using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TPreviewWhitebalance
{
    [Fact]
    public void ColorResolve_ActiveWhitebalanceCarriesSettingsAndInactiveIsNeutral()
    {
        LColor active = TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate([
            TInterface.TWorkWhitebalanceCreate(true, LWhitebalanceMethod.LWhitebalanceMethodAverage, 137.5)
        ]));
        LColor inactive = TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate([
            TInterface.TWorkWhitebalanceCreate(false, LWhitebalanceMethod.LWhitebalanceMethodMinmax, 250)
        ]));

        Assert.NotNull(active.LColorWhitebalance);
        Assert.Equal(LWhitebalanceMethod.LWhitebalanceMethodAverage,
            active.LColorWhitebalance!.LWorkWhitebalanceMethod);
        Assert.Equal(137.5, active.LColorWhitebalance.LWorkWhitebalanceSaturation);
        Assert.Null(inactive.LColorWhitebalance);
    }

    [Theory]
    [InlineData(LWhitebalanceMethod.LWhitebalanceMethodAverage, "average")]
    [InlineData(LWhitebalanceMethod.LWhitebalanceMethodMinmax, "minmax")]
    [InlineData(LWhitebalanceMethod.LWhitebalanceMethodMedian, "median")]
    public void MpvFilterResolve_WhitebalanceFormatsEveryMethodToken(
        LWhitebalanceMethod method, string token)
    {
        LColor color = TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate([
            TInterface.TWorkWhitebalanceCreate(true, method, 137.5)
        ]));
        LPreviewState state = TInterface.TPreviewColorChange(TInterface.TPreviewDefaultCreate(), color);

        Assert.Equal(
            $"lavfi=[colorcorrect=analyze={token}:saturation=1.375]",
            TInterface.TPreviewFilterResolve(state));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(100, "1")]
    [InlineData(123.4567, "1.235")]
    [InlineData(300, "3")]
    public void MpvFilterResolve_WhitebalanceUsesCompactInvariantSaturation(
        double saturation, string expected)
    {
        LColor color = TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate([
            TInterface.TWorkWhitebalanceCreate(
                true, LWhitebalanceMethod.LWhitebalanceMethodMedian, saturation)
        ]));
        LPreviewState state = TInterface.TPreviewColorChange(TInterface.TPreviewDefaultCreate(), color);

        Assert.EndsWith($":saturation={expected}]", TInterface.TPreviewFilterResolve(state));
    }

    [Fact]
    public void MpvFilterResolve_GeometryWhitebalanceGammaCanonicalOrder()
    {
        LColor color = TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate([
            TInterface.TWorkGammaCreate(true, 20, 10, 0, 0, 25),
            TInterface.TWorkWhitebalanceCreate(
                true, LWhitebalanceMethod.LWhitebalanceMethodMinmax, 125)
        ]));
        LPreviewState state = TInterface.TPreviewRotateChange(
            TInterface.TPreviewColorChange(TInterface.TPreviewDefaultCreate(), color),
            TInterface.TRotateFlipCreate(LRotateKind.LRotate90, true, false));

        string filter = TInterface.TPreviewFilterResolve(state);
        Assert.DoesNotContain("crop=", filter);
        Assert.True(filter.IndexOf("colorcorrect=", StringComparison.Ordinal) < filter.IndexOf("eq=", StringComparison.Ordinal));
    }

    [Fact]
    public void WhitebalanceTransition_MpvToFlyleafToMpv_RemovesAndRestoresFilter()
    {
        LWorkVideoStep whitebalance = TInterface.TWorkWhitebalanceCreate(
            true, LWhitebalanceMethod.LWhitebalanceMethodAverage, 175);
        LWorkVideo mpv = TInterface.TEditVideoCreate([whitebalance], true);
        LWorkVideo flyleaf = TInterface.TEditVideoCreate([whitebalance], false);

        string firstMpv = TInterface.TPreviewFilterResolve(TInterface.TPreviewColorChange(
            TInterface.TPreviewDefaultCreate(), TInterface.TPreviewColorResolve(mpv)));
        string flyleafFilter = TInterface.TPreviewFilterResolve(TInterface.TPreviewColorChange(
            TInterface.TPreviewDefaultCreate(), TInterface.TPreviewColorResolve(flyleaf)));
        string secondMpv = TInterface.TPreviewFilterResolve(TInterface.TPreviewColorChange(
            TInterface.TPreviewDefaultCreate(), TInterface.TPreviewColorResolve(mpv)));

        Assert.Equal("lavfi=[colorcorrect=analyze=average:saturation=1.75]", firstMpv);
        Assert.Empty(flyleafFilter);
        Assert.Equal(firstMpv, secondMpv);
        Assert.Equal(175, TInterface.TWorkWhitebalanceRead(whitebalance).LWorkWhitebalanceSaturation);
    }

    [Theory]
    [InlineData(1, 1, 1, "colorchannelmixer=rr=1:gg=1:bb=1")]
    [InlineData(1.2, 1, 0.8, "colorchannelmixer=rr=1.2:gg=1:bb=0.8")]
    [InlineData(0.8, 1, 1.2, "colorchannelmixer=rr=0.8:gg=1:bb=1.2")]
    [InlineData(5, 1, -1, "colorchannelmixer=rr=2:gg=1:bb=0")]
    [InlineData(1.3755, 0.66667, 1, "colorchannelmixer=rr=1.376:gg=0.667:bb=1")]
    public void MpvFilterResolve_ManualWhitebalanceEmitsDiagonalMixer(
        double red, double green, double blue, string expected)
    {
        LColor color = TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate([
            TInterface.TWorkManualCreate(true, 100, red, green, blue, 0, 0, 0)
        ]));
        LPreviewState state = TInterface.TPreviewColorChange(TInterface.TPreviewDefaultCreate(), color);

        Assert.Equal($"lavfi=[{expected}]", TInterface.TPreviewFilterResolve(state));
    }

    [Theory]
    [InlineData(0, "colorchannelmixer=rr=1:gg=1:bb=1,eq=saturation=0")]
    [InlineData(100, "colorchannelmixer=rr=1:gg=1:bb=1")]
    [InlineData(300, "colorchannelmixer=rr=1:gg=1:bb=1,eq=saturation=3")]
    public void MpvFilterResolve_ManualWhitebalanceSaturationIsSeparateFilterWhenRequired(
        double saturation, string expected)
    {
        LColor color = TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate([
            TInterface.TWorkManualCreate(true, saturation, 1, 1, 1, 0, 0, 0)
        ]));
        LPreviewState state = TInterface.TPreviewColorChange(TInterface.TPreviewDefaultCreate(), color);

        Assert.Equal($"lavfi=[{expected}]", TInterface.TPreviewFilterResolve(state));
    }

    [Fact]
    public void MpvFilterResolve_ManualWhitebalanceGammaCanonicalOrder()
    {
        LColor color = TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate([
            TInterface.TWorkGammaCreate(true, 20, 10, 0, 0, 25),
            TInterface.TWorkManualCreate(true, 200, 1.2, 1, 0.8, 0, 0, 0)
        ]));
        LPreviewState state = TInterface.TPreviewCropboxChange(
            TInterface.TPreviewColorChange(TInterface.TPreviewDefaultCreate(), color),
            TInterface.TCropboxCreate(10, 20, 300, 200));

        string filter = TInterface.TPreviewFilterResolve(state);
        Assert.DoesNotContain("crop=", filter);
        Assert.True(filter.IndexOf("colorchannelmixer=", StringComparison.Ordinal)
            < filter.IndexOf("eq=saturation=", StringComparison.Ordinal));
        Assert.True(filter.IndexOf("eq=saturation=", StringComparison.Ordinal)
            < filter.IndexOf("eq=gamma=", StringComparison.Ordinal));
    }

    [Fact]
    public void ManualWhitebalanceTransition_MpvToFlyleafToMpv_RemovesAndRestoresFilter()
    {
        LWorkVideoStep whitebalance =
            TInterface.TWorkManualCreate(true, 100, 1.2, 1, 0.8, 12, 34, 56);
        LWorkVideo mpv = TInterface.TEditVideoCreate([whitebalance], true);
        LWorkVideo flyleaf = TInterface.TEditVideoCreate([whitebalance], false);

        string firstMpv = TInterface.TPreviewFilterResolve(TInterface.TPreviewColorChange(
            TInterface.TPreviewDefaultCreate(), TInterface.TPreviewColorResolve(mpv)));
        string flyleafFilter = TInterface.TPreviewFilterResolve(TInterface.TPreviewColorChange(
            TInterface.TPreviewDefaultCreate(), TInterface.TPreviewColorResolve(flyleaf)));
        string secondMpv = TInterface.TPreviewFilterResolve(TInterface.TPreviewColorChange(
            TInterface.TPreviewDefaultCreate(), TInterface.TPreviewColorResolve(mpv)));

        Assert.Equal("lavfi=[colorchannelmixer=rr=1.2:gg=1:bb=0.8]", firstMpv);
        Assert.Empty(flyleafFilter);
        Assert.Equal(firstMpv, secondMpv);
    }
}
