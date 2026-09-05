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
}
