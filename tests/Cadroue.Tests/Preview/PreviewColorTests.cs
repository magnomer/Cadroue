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
        state = TInterface.PreviewCropboxChange(state, TInterface.CropboxCreate(10, 20, 300, 200));

        Assert.Equal(
            "lavfi=[hflip,transpose=1,crop=300:200:10:20]",
            TInterface.PreviewMpvFilterResolve(state));
    }

    [Fact]
    public void MpvFilterResolve_AdvancedGammaWithGeometry_AppendsLutLast()
    {
        LPreviewState state = TInterface.PreviewRotateFlipChange(
            TInterface.PreviewColorChange(
                TInterface.PreviewDefaultCreate(),
                TInterface.ColorGammaCreate(1.2, 0.9, 1.1, 1.3, 25)),
            TInterface.RotateFlipCreate(LRotateKind.LRotate90, true, true));
        state = TInterface.PreviewCropboxChange(state, TInterface.CropboxCreate(10, 20, 300, 200));

        Assert.Equal(
            "lavfi=[hflip,vflip,transpose=1,crop=300:200:10:20,"
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
}
