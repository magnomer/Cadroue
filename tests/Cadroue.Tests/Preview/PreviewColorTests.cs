using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class PreviewColorTests
{
    [Fact]
    public void Apply_DefaultColor_ResolvesToNeutral()
    {
        LPreviewState state = TInterface.PreviewDefaultCreate();

        var result = new TPreview().ApplyState(state);

        Assert.Equal(0, result.Brightness);
        Assert.Equal(0, result.Contrast);
        Assert.Equal(0, result.Saturation);
        Assert.Equal(0, result.Hue);
        Assert.Equal(0u, result.Rotation);
    }

    [Fact]
    public void Apply_ContrastTwo_ClampsToCeiling()
    {
        LPreviewState state = TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(),
            TInterface.ColorCreate(0, 2, 1, 0));

        var result = new TPreview().ApplyState(state);

        Assert.Equal(100, result.Contrast);
    }

    [Fact]
    public void ColorResolve_InactiveSteps_ResolvesToNeutral()
    {
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkBrightnessCreate(false, 80),
            TInterface.WorkContrastCreate(false, 150)
        });

        var result = TInterface.PreviewColorResolve(video);

        Assert.Equal(TInterface.ColorCreate(0, 1, 1, 0), result);
    }

    [Fact]
    public void ColorResolve_ActiveBrightness_ScalesByFactor()
    {
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkBrightnessCreate(true, 80)
        });

        var result = TInterface.PreviewColorResolve(video);

        Assert.Equal(1, result.LColorBrightness, 10);
    }

    [Fact]
    public void ColorResolve_ActiveContrast_PassesFfmpegValueThrough()
    {
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkContrastCreate(true, 150)
        });

        var result = TInterface.PreviewColorResolve(video);

        Assert.Equal(1.5, result.LColorContrast, 10);
    }

    [Fact]
    public void MpvEqualizerResolve_CompensatesForNativeMpvContrastSemantics()
    {
        LPreviewState state = TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(),
            TInterface.PreviewColorResolve(TInterface.WorkVideoCreate(new[]
            {
                TInterface.WorkBrightnessCreate(true, 100),
                TInterface.WorkContrastCreate(true, 150)
            })));

        LPreviewMpvEqualizer result = TInterface.PreviewMpvEqualizerResolve(state);

        Assert.Equal(25, result.LPreviewMpvBrightness);
        Assert.Equal(50, result.LPreviewMpvContrast);
        Assert.Equal(-33, result.LPreviewMpvSaturation);
        Assert.Equal(0, result.LPreviewMpvHue);
        Assert.Equal(1, result.LPreviewMpvGammaFactor);
    }

    [Fact]
    public void MpvEqualizerResolve_ContrastOnly_PreservesFfmpegMidpointAndChroma()
    {
        LPreviewState state = TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(),
            TInterface.ColorCreate(0, 1.5, 1, 0));

        LPreviewMpvEqualizer result = TInterface.PreviewMpvEqualizerResolve(state);

        Assert.Equal(-25, result.LPreviewMpvBrightness);
        Assert.Equal(50, result.LPreviewMpvContrast);
        Assert.Equal(-33, result.LPreviewMpvSaturation);
    }

    [Fact]
    public void MpvFilterResolve_ColorOnly_DoesNotDuplicateNativeEqualizer()
    {
        LPreviewState state = TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(),
            TInterface.ColorCreate(0.5, 1.5, 1, 0));

        string result = TInterface.PreviewMpvFilterResolve(state);

        Assert.Empty(result);
    }

    [Fact]
    public void MpvEqualizerResolve_Gamma_UsesNativeGammaFactor()
    {
        LPreviewState state = TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(),
            TInterface.ColorGammaCreate(1.5));

        Assert.Equal(1.5, TInterface.PreviewMpvEqualizerResolve(state).LPreviewMpvGammaFactor);
    }

    [Fact]
    public void MpvEqualizerResolve_ActiveGammaWithinNativeRange_UsesFfmpegFactor()
    {
        LColor color = TInterface.PreviewColorResolve(TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkGammaCreate(true, -100)
        }));
        LPreviewState state = TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(), color);

        Assert.Equal(0.1, TInterface.PreviewMpvEqualizerResolve(state).LPreviewMpvGammaFactor);
        Assert.Empty(TInterface.PreviewMpvFilterResolve(state));
    }

    [Fact]
    public void MpvEqualizerResolve_ActiveGammaAboveNativeRange_UsesLutFilter()
    {
        LColor color = TInterface.PreviewColorResolve(TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkGammaCreate(true, 100)
        }));
        LPreviewState state = TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(), color);

        Assert.Equal(10, color.LColorGamma);
        Assert.Equal(1, TInterface.PreviewMpvEqualizerResolve(state).LPreviewMpvGammaFactor);
        Assert.Contains("lutyuv=y=", TInterface.PreviewMpvFilterResolve(state));
    }

    [Fact]
    public void ColorResolve_AdvancedGamma_CarriesEveryExportValue()
    {
        LColor result = TInterface.PreviewColorResolve(TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkGammaCreate(true, 50, -50, 25, 75, 25)
        }));

        Assert.Equal(Math.Pow(10, 0.5), result.LColorGamma);
        Assert.Equal(Math.Pow(10, -0.5), result.LColorGammaRed);
        Assert.Equal(Math.Pow(10, 0.25), result.LColorGammaGreen);
        Assert.Equal(Math.Pow(10, 0.75), result.LColorGammaBlue);
        Assert.Equal(25, result.LColorGammaHighlightProtection);
    }

    [Theory]
    [InlineData(0.5, 0.5, 0.5, 0.5, 0, "lavfi=[lutyuv=y='val*0+maxval*pow(val/maxval\\,4)*1':u='val*0+maxval*pow(val/maxval\\,1)*1':v='val*0+maxval*pow(val/maxval\\,1)*1']")]
    [InlineData(2, 2, 2, 2, 100, "lavfi=[lutyuv=y='val*1+maxval*pow(val/maxval\\,0.25)*0':u='val*1+maxval*pow(val/maxval\\,1)*0':v='val*1+maxval*pow(val/maxval\\,1)*0']")]
    public void MpvFilterResolve_AdvancedGamma_UsesCompactInvariantBounds(
        double global, double red, double green, double blue, double protection, string expected)
    {
        LPreviewState state = TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(),
            TInterface.ColorGammaCreate(global, red, green, blue, protection));

        Assert.Equal(1, TInterface.PreviewMpvEqualizerResolve(state).LPreviewMpvGammaFactor);
        Assert.Equal(expected, TInterface.PreviewMpvFilterResolve(state));
    }

    [Theory]
    [InlineData(25, 0.75)]
    [InlineData(75, 0.25)]
    public void MpvFilterResolve_HighlightProtection_InvertsGammaWeight(
        double protection, double expectedWeight)
    {
        LPreviewState state = TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(),
            TInterface.ColorGammaCreate(1.2, 1, 1, 1, protection));

        string expectedLinearWeight = (1 - expectedWeight)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains($"val*{expectedLinearWeight}+", TInterface.PreviewMpvFilterResolve(state));
        Assert.Contains($"*{expectedWeight.ToString(System.Globalization.CultureInfo.InvariantCulture)}'", TInterface.PreviewMpvFilterResolve(state));
    }

    [Fact]
    public void MpvFilterResolve_GammaWithGeometry_PreservesExportOrder()
    {
        LPreviewState state = TInterface.PreviewRotateFlipChange(
            TInterface.PreviewColorChange(
                TInterface.PreviewDefaultCreate(),
                TInterface.ColorGammaCreate(1.5)),
            TInterface.RotateFlipCreate(LRotateKind.LRotate90, true, false));

        Assert.Equal(
            "lavfi=[hflip,transpose=1]",
            TInterface.PreviewMpvFilterResolve(state));
    }

    [Fact]
    public void MpvFilterResolve_Cropbox_NeverEmitsCropFilter()
    {
        LPreviewState state = TInterface.PreviewRotateFlipChange(
            TInterface.PreviewColorChange(
                TInterface.PreviewDefaultCreate(),
                TInterface.ColorGammaCreate(1.5)),
            TInterface.RotateFlipCreate(LRotateKind.LRotate90, true, false));
        state = TInterface.PreviewCropboxChange(state, TInterface.CropboxCreate(10, 20, 300, 200));

        Assert.DoesNotContain("crop=", TInterface.PreviewMpvFilterResolve(state));
    }

    [Fact]
    public void MpvFilterResolve_AdvancedGammaWithGeometry_AppendsLutLast()
    {
        LPreviewState state = TInterface.PreviewRotateFlipChange(
            TInterface.PreviewColorChange(
                TInterface.PreviewDefaultCreate(),
                TInterface.ColorGammaCreate(1.2, 0.9, 1.1, 1.3, 25)),
            TInterface.RotateFlipCreate(LRotateKind.LRotate90, true, true));

        Assert.Equal(
            "lavfi=[hflip,vflip,transpose=1,"
            + "lutyuv=y='val*0.25+maxval*pow(val/maxval\\,0.757576)*0.75'"
            + ":u='val*0.25+maxval*pow(val/maxval\\,0.919866)*0.75'"
            + ":v='val*0.25+maxval*pow(val/maxval\\,1.105542)*0.75']",
            TInterface.PreviewMpvFilterResolve(state));
    }

    [Fact]
    public void MpvGammaTransition_AdvancedToNative_RemovesLutAndRestoresGlobalFactor()
    {
        LPreviewState advanced = TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(),
            TInterface.ColorGammaCreate(1.5, 1.1, 1, 1, 0));
        LPreviewState native = TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(),
            TInterface.ColorGammaCreate(1.5));

        Assert.Contains("lutyuv=", TInterface.PreviewMpvFilterResolve(advanced));
        Assert.Equal(1, TInterface.PreviewMpvEqualizerResolve(advanced).LPreviewMpvGammaFactor);
        Assert.Empty(TInterface.PreviewMpvFilterResolve(native));
        Assert.Equal(1.5, TInterface.PreviewMpvEqualizerResolve(native).LPreviewMpvGammaFactor);
    }

    [Fact]
    public void MpvFilterResolve_RepeatedAdvancedValue_HasStableCacheIdentity()
    {
        LPreviewState state = TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(),
            TInterface.ColorGammaCreate(1.2, 0.9, 1.1, 1.3, 25));

        string first = TInterface.PreviewMpvFilterResolve(state);
        string second = TInterface.PreviewMpvFilterResolve(state);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Apply_GammaState_FlyleafDtoHasNoGammaChannel()
    {
        LPreviewState state = TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(),
            TInterface.ColorGammaCreate(1.5));

        _ = new TPreview().ApplyState(state);

        Assert.DoesNotContain(
            typeof(LPreviewApplication).GetProperties(),
            property => property.Name.Contains("Gamma", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ColorResolve_ActiveWhitebalanceCarriesSettingsAndInactiveIsNeutral()
    {
        LColor active = TInterface.PreviewColorResolve(TInterface.WorkVideoCreate([
            TInterface.WorkWhitebalanceCreate(true, LWhitebalanceMethod.LWhitebalanceMethodAverage, 137.5)
        ]));
        LColor inactive = TInterface.PreviewColorResolve(TInterface.WorkVideoCreate([
            TInterface.WorkWhitebalanceCreate(false, LWhitebalanceMethod.LWhitebalanceMethodMinmax, 250)
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
        LColor color = TInterface.PreviewColorResolve(TInterface.WorkVideoCreate([
            TInterface.WorkWhitebalanceCreate(true, method, 137.5)
        ]));
        LPreviewState state = TInterface.PreviewColorChange(TInterface.PreviewDefaultCreate(), color);

        Assert.Equal(
            $"lavfi=[colorcorrect=analyze={token}:saturation=1.375]",
            TInterface.PreviewMpvFilterResolve(state));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(100, "1")]
    [InlineData(123.4567, "1.235")]
    [InlineData(300, "3")]
    public void MpvFilterResolve_WhitebalanceUsesCompactInvariantSaturation(
        double saturation, string expected)
    {
        LColor color = TInterface.PreviewColorResolve(TInterface.WorkVideoCreate([
            TInterface.WorkWhitebalanceCreate(
                true, LWhitebalanceMethod.LWhitebalanceMethodMedian, saturation)
        ]));
        LPreviewState state = TInterface.PreviewColorChange(TInterface.PreviewDefaultCreate(), color);

        Assert.EndsWith($":saturation={expected}]", TInterface.PreviewMpvFilterResolve(state));
    }

    [Fact]
    public void MpvFilterResolve_GeometryGammaWhitebalancePreservesExportOrder()
    {
        LColor color = TInterface.PreviewColorResolve(TInterface.WorkVideoCreate([
            TInterface.WorkGammaCreate(true, 20, 10, 0, 0, 25),
            TInterface.WorkWhitebalanceCreate(
                true, LWhitebalanceMethod.LWhitebalanceMethodMinmax, 125)
        ]));
        LPreviewState state = TInterface.PreviewRotateFlipChange(
            TInterface.PreviewColorChange(TInterface.PreviewDefaultCreate(), color),
            TInterface.RotateFlipCreate(LRotateKind.LRotate90, true, false));

        string filter = TInterface.PreviewMpvFilterResolve(state);
        Assert.DoesNotContain("crop=", filter);
        Assert.True(filter.IndexOf("lutyuv=", StringComparison.Ordinal) < filter.IndexOf("colorcorrect=", StringComparison.Ordinal));
    }

    [Fact]
    public void WhitebalanceTransition_MpvToFlyleafToMpv_RemovesAndRestoresFilter()
    {
        LWorkVideoStep whitebalance = TInterface.WorkWhitebalanceCreate(
            true, LWhitebalanceMethod.LWhitebalanceMethodAverage, 175);
        LWorkVideo mpv = TInterface.EditVideoCreate([whitebalance], true);
        LWorkVideo flyleaf = TInterface.EditVideoCreate([whitebalance], false);

        string firstMpv = TInterface.PreviewMpvFilterResolve(TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(), TInterface.PreviewColorResolve(mpv)));
        string flyleafFilter = TInterface.PreviewMpvFilterResolve(TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(), TInterface.PreviewColorResolve(flyleaf)));
        string secondMpv = TInterface.PreviewMpvFilterResolve(TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(), TInterface.PreviewColorResolve(mpv)));

        Assert.Equal("lavfi=[colorcorrect=analyze=average:saturation=1.75]", firstMpv);
        Assert.Empty(flyleafFilter);
        Assert.Equal(firstMpv, secondMpv);
        Assert.Equal(175, TInterface.WorkWhitebalanceRead(whitebalance).LWorkWhitebalanceSaturation);
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
        LColor color = TInterface.PreviewColorResolve(TInterface.WorkVideoCreate([
            TInterface.WorkWhitebalanceManualCreate(true, 100, red, green, blue, 0, 0, 0)
        ]));
        LPreviewState state = TInterface.PreviewColorChange(TInterface.PreviewDefaultCreate(), color);

        Assert.Equal($"lavfi=[{expected}]", TInterface.PreviewMpvFilterResolve(state));
    }

    [Theory]
    [InlineData(0, "colorchannelmixer=rr=1:gg=1:bb=1,eq=saturation=0")]
    [InlineData(100, "colorchannelmixer=rr=1:gg=1:bb=1")]
    [InlineData(300, "colorchannelmixer=rr=1:gg=1:bb=1,eq=saturation=3")]
    public void MpvFilterResolve_ManualWhitebalanceSaturationIsSeparateFilterWhenRequired(
        double saturation, string expected)
    {
        LColor color = TInterface.PreviewColorResolve(TInterface.WorkVideoCreate([
            TInterface.WorkWhitebalanceManualCreate(true, saturation, 1, 1, 1, 0, 0, 0)
        ]));
        LPreviewState state = TInterface.PreviewColorChange(TInterface.PreviewDefaultCreate(), color);

        Assert.Equal($"lavfi=[{expected}]", TInterface.PreviewMpvFilterResolve(state));
    }

    [Fact]
    public void MpvFilterResolve_GammaManualPreservesExportOrder()
    {
        LColor color = TInterface.PreviewColorResolve(TInterface.WorkVideoCreate([
            TInterface.WorkGammaCreate(true, 20, 10, 0, 0, 25),
            TInterface.WorkWhitebalanceManualCreate(true, 200, 1.2, 1, 0.8, 0, 0, 0)
        ]));
        LPreviewState state = TInterface.PreviewCropboxChange(
            TInterface.PreviewColorChange(TInterface.PreviewDefaultCreate(), color),
            TInterface.CropboxCreate(10, 20, 300, 200));

        string filter = TInterface.PreviewMpvFilterResolve(state);
        Assert.DoesNotContain("crop=", filter);
        Assert.True(filter.IndexOf("lutyuv=", StringComparison.Ordinal)
            < filter.IndexOf("colorchannelmixer=", StringComparison.Ordinal));
        Assert.True(filter.IndexOf("colorchannelmixer=", StringComparison.Ordinal)
            < filter.IndexOf("eq=saturation=", StringComparison.Ordinal));
    }

    [Fact]
    public void ManualWhitebalanceTransition_MpvToFlyleafToMpv_RemovesAndRestoresFilter()
    {
        LWorkVideoStep whitebalance =
            TInterface.WorkWhitebalanceManualCreate(true, 100, 1.2, 1, 0.8, 12, 34, 56);
        LWorkVideo mpv = TInterface.EditVideoCreate([whitebalance], true);
        LWorkVideo flyleaf = TInterface.EditVideoCreate([whitebalance], false);

        string firstMpv = TInterface.PreviewMpvFilterResolve(TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(), TInterface.PreviewColorResolve(mpv)));
        string flyleafFilter = TInterface.PreviewMpvFilterResolve(TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(), TInterface.PreviewColorResolve(flyleaf)));
        string secondMpv = TInterface.PreviewMpvFilterResolve(TInterface.PreviewColorChange(
            TInterface.PreviewDefaultCreate(), TInterface.PreviewColorResolve(mpv)));

        Assert.Equal("lavfi=[colorchannelmixer=rr=1.2:gg=1:bb=0.8]", firstMpv);
        Assert.Empty(flyleafFilter);
        Assert.Equal(firstMpv, secondMpv);
    }
}
