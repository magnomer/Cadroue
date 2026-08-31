using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TPreviewColor
{
    [Fact]
    public void Apply_DefaultColor_ResolvesToNeutral()
    {
        LPreviewState state = TInterface.TPreviewDefaultCreate();

        var result = new TPreview().TPreviewApply(state);

        Assert.Equal(0, result.TPreviewBrightness);
        Assert.Equal(0, result.TPreviewContrast);
        Assert.Equal(0, result.TPreviewSaturation);
        Assert.Equal(0, result.TPreviewHue);
        Assert.Equal(0u, result.TPreviewRotation);
    }

    [Fact]
    public void Apply_ContrastTwo_ClampsToCeiling()
    {
        LPreviewState state = TInterface.TPreviewColorChange(
            TInterface.TPreviewDefaultCreate(),
            TInterface.TColorCreate(0, 2, 1, 0));

        var result = new TPreview().TPreviewApply(state);

        Assert.Equal(100, result.TPreviewContrast);
    }

    [Fact]
    public void ColorResolve_InactiveSteps_ResolvesToNeutral()
    {
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkBrightnessCreate(false, 80),
            TInterface.TWorkContrastCreate(false, 150)
        });

        var result = TInterface.TPreviewColorResolve(video);

        Assert.Equal(TInterface.TColorCreate(0, 1, 1, 0), result);
    }

    [Fact]
    public void ColorResolve_ActiveBrightness_ScalesByFactor()
    {
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkBrightnessCreate(true, 80)
        });

        var result = TInterface.TPreviewColorResolve(video);

        Assert.Equal(1, result.LColorBrightness, 10);
    }

    [Fact]
    public void ColorResolve_ActiveContrast_PassesFfmpegValueThrough()
    {
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkContrastCreate(true, 150)
        });

        var result = TInterface.TPreviewColorResolve(video);

        Assert.Equal(1.5, result.LColorContrast, 10);
    }

    [Fact]
    public void ColorResolve_ActiveSaturation_PassesFfmpegValueThrough()
    {
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkSaturationCreate(true, 150)
        });

        var result = TInterface.TPreviewColorResolve(video);

        Assert.Equal(1.5, result.LColorSaturation, 10);
    }

    [Fact]
    public void ColorResolve_InactiveSaturation_ResolvesToNeutral()
    {
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkSaturationCreate(false, 150)
        });

        var result = TInterface.TPreviewColorResolve(video);

        Assert.Equal(1, result.LColorSaturation, 10);
    }

    [Fact]
    public void MpvFilterResolve_BrightnessContrast_EmitEqInGraph()
    {
        LPreviewState state = TInterface.TPreviewColorChange(
            TInterface.TPreviewDefaultCreate(),
            TInterface.TColorCreate(0.5, 1.5, 1, 0));

        Assert.Equal(
            "lavfi=[eq=brightness=0.2:contrast=1.5]",
            TInterface.TPreviewFilterResolve(state));
    }

    [Fact]
    public void MpvFilterResolve_ActiveGamma_EmitsEqGamma()
    {
        LColor color = TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkGammaCreate(true, 100)
        }));
        LPreviewState state = TInterface.TPreviewColorChange(
            TInterface.TPreviewDefaultCreate(), color);

        Assert.Equal(10, color.LColorGamma);
        Assert.Equal("lavfi=[eq=gamma=10]", TInterface.TPreviewFilterResolve(state));
    }

    [Fact]
    public void ColorResolve_AdvancedGamma_CarriesEveryExportValue()
    {
        LColor result = TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkGammaCreate(true, 50, -50, 25, 75, 25)
        }));

        Assert.Equal(Math.Pow(10, 0.5), result.LColorGamma);
        Assert.Equal(Math.Pow(10, -0.5), result.LColorGammaRed);
        Assert.Equal(Math.Pow(10, 0.25), result.LColorGammaGreen);
        Assert.Equal(Math.Pow(10, 0.75), result.LColorGammaBlue);
        Assert.Equal(25, result.LColorHighlightProtection);
    }

    [Theory]
    [InlineData(0.5, 0.5, 0.5, 0.5, 0, "lavfi=[eq=gamma=0.5:gamma_r=0.5:gamma_g=0.5:gamma_b=0.5]")]
    [InlineData(2, 2, 2, 2, 100, "lavfi=[eq=gamma=2:gamma_r=2:gamma_g=2:gamma_b=2:gamma_weight=0]")]
    public void MpvFilterResolve_AdvancedGamma_EmitsEqChannels(
        double global, double red, double green, double blue, double protection, string expected)
    {
        LPreviewState state = TInterface.TPreviewColorChange(
            TInterface.TPreviewDefaultCreate(),
            TInterface.TColorGammaCreate(global, red, green, blue, protection));

        Assert.Equal(expected, TInterface.TPreviewFilterResolve(state));
    }

    [Theory]
    [InlineData(25, "0.75")]
    [InlineData(75, "0.25")]
    public void MpvFilterResolve_HighlightProtection_EmitsEqGammaWeight(
        double protection, string expectedWeight)
    {
        LPreviewState state = TInterface.TPreviewColorChange(
            TInterface.TPreviewDefaultCreate(),
            TInterface.TColorGammaCreate(1.2, 1, 1, 1, protection));

        Assert.Equal(
            $"lavfi=[eq=gamma=1.2:gamma_weight={expectedWeight}]",
            TInterface.TPreviewFilterResolve(state));
    }

    [Fact]
    public void MpvFilterResolve_GammaWithGeometry_EmitsEqAfterGeometry()
    {
        LPreviewState state = TInterface.TPreviewRotateChange(
            TInterface.TPreviewColorChange(
                TInterface.TPreviewDefaultCreate(),
                TInterface.TColorGammaCreate(1.5)),
            TInterface.TRotateFlipCreate(LRotateKind.LRotate90, true, false));

        Assert.Equal(
            "lavfi=[hflip,transpose=1,eq=gamma=1.5]",
            TInterface.TPreviewFilterResolve(state));
    }

    [Fact]
    public void MpvFilterResolve_Cropbox_NeverEmitsCropFilter()
    {
        LPreviewState state = TInterface.TPreviewRotateChange(
            TInterface.TPreviewColorChange(
                TInterface.TPreviewDefaultCreate(),
                TInterface.TColorGammaCreate(1.5)),
            TInterface.TRotateFlipCreate(LRotateKind.LRotate90, true, false));
        state = TInterface.TPreviewCropboxChange(state, TInterface.TCropboxCreate(10, 20, 300, 200));

        Assert.DoesNotContain("crop=", TInterface.TPreviewFilterResolve(state));
    }

    [Fact]
    public void MpvFilterResolve_AdvancedGammaWithGeometry_AppendsEqLast()
    {
        LPreviewState state = TInterface.TPreviewRotateChange(
            TInterface.TPreviewColorChange(
                TInterface.TPreviewDefaultCreate(),
                TInterface.TColorGammaCreate(1.2, 0.9, 1.1, 1.3, 25)),
            TInterface.TRotateFlipCreate(LRotateKind.LRotate90, true, true));

        Assert.Equal(
            "lavfi=[hflip,vflip,transpose=1,"
            + "eq=gamma=1.2:gamma_r=0.9:gamma_g=1.1:gamma_b=1.3:gamma_weight=0.75]",
            TInterface.TPreviewFilterResolve(state));
    }

    [Fact]
    public void MpvFilterResolve_RepeatedAdvancedValue_HasStableCacheIdentity()
    {
        LPreviewState state = TInterface.TPreviewColorChange(
            TInterface.TPreviewDefaultCreate(),
            TInterface.TColorGammaCreate(1.2, 0.9, 1.1, 1.3, 25));

        string first = TInterface.TPreviewFilterResolve(state);
        string second = TInterface.TPreviewFilterResolve(state);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Apply_GammaState_FlyleafDtoHasNoGammaChannel()
    {
        LPreviewState state = TInterface.TPreviewColorChange(
            TInterface.TPreviewDefaultCreate(),
            TInterface.TColorGammaCreate(1.5));

        _ = new TPreview().TPreviewApply(state);

        Assert.DoesNotContain(
            typeof(LPreviewApplication).GetProperties(),
            property => property.Name.Contains("Gamma", StringComparison.OrdinalIgnoreCase));
    }

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

    [Theory]
    [InlineData(true, 1.5, 1.5)]
    [InlineData(false, 1.5, 0)]
    public void PreviewColorResolve_CarriesActiveExposure(bool active, double value, double expected)
    {
        LColor color = TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate([
            TInterface.TWorkExposureCreate(active, value)
        ]));

        Assert.Equal(expected, color.LColorExposure);
    }

    [Fact]
    public void PreviewColorResolve_AbsentExposureIsZero()
    {
        LColor color = TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate([]));

        Assert.Equal(0, color.LColorExposure);
    }

    [Fact]
    public void MpvFilterResolve_ActiveExposureEmitsStandaloneFilter()
    {
        LColor color = TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate([
            TInterface.TWorkExposureCreate(true, 1.5)
        ]));
        LPreviewState state = TInterface.TPreviewColorChange(TInterface.TPreviewDefaultCreate(), color);

        Assert.Equal("lavfi=[exposure=exposure=1.5]", TInterface.TPreviewFilterResolve(state));
    }

    [Fact]
    public void MpvFilterResolve_InactiveExposureEmitsNoFilter()
    {
        LColor color = TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate([
            TInterface.TWorkExposureCreate(false, 1.5)
        ]));
        LPreviewState state = TInterface.TPreviewColorChange(TInterface.TPreviewDefaultCreate(), color);

        Assert.Empty(TInterface.TPreviewFilterResolve(state));
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
