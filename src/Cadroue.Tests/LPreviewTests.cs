using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LPreviewTests
{
    private static LPreviewApplication Resolve(LPreviewState lPreviewState)
    {
        LPreviewApplication? captured = null;
        LPreview.LPreviewApplySeam = (_, application) => captured = application;
        LPreview.LPreviewApply(new object(), lPreviewState);
        Assert.NotNull(captured);
        return captured!;
    }

    [Fact]
    public void Apply_DefaultColor_ResolvesToNeutral()
    {
        var state = LPreviewState.LPreviewDefaultCreate();

        var result = Resolve(state);

        Assert.Equal(0, result.LPreviewBrightness);
        Assert.Equal(0, result.LPreviewContrast);
        Assert.Equal(0, result.LPreviewSaturation);
        Assert.Equal(0, result.LPreviewHue);
        Assert.Equal(0u, result.LPreviewRotation);
    }

    [Fact]
    public void Apply_ContrastTwo_ClampsToCeiling()
    {
        var state = LPreviewState.LPreviewDefaultCreate()
            .LColorChange(new LColor(0, 2, 1, 0));

        var result = Resolve(state);

        Assert.Equal(100, result.LPreviewContrast);
    }

    [Fact]
    public void Apply_Rotate270_ResolvesRotation()
    {
        var state = LPreviewState.LPreviewDefaultCreate()
            .LRotateFlipChange(new LRotateFlip(LRotateKind.LRotate270, false, false));

        var result = Resolve(state);

        Assert.Equal(270u, result.LPreviewRotation);
    }

    [Fact]
    public void ColorResolve_InactiveSteps_ResolvesToNeutral()
    {
        var video = new LWorkVideo(new[]
        {
            LWorkVideoStep.LWorkBrightnessCreate(false, 80),
            LWorkVideoStep.LWorkContrastCreate(false, 150)
        });

        var result = LPreview.LPreviewColorResolve(video);

        Assert.Equal(new LColor(0, 1, 1, 0), result);
    }

    [Fact]
    public void ColorResolve_ActiveBrightness_ScalesByFactor()
    {
        var video = new LWorkVideo(new[]
        {
            LWorkVideoStep.LWorkBrightnessCreate(true, 80)
        });

        var result = LPreview.LPreviewColorResolve(video);

        Assert.Equal(0.5, result.LColorBrightness, 10);
    }

    [Fact]
    public void ColorResolve_ActiveContrast_PassesFfmpegValueThrough()
    {
        var video = new LWorkVideo(new[]
        {
            LWorkVideoStep.LWorkContrastCreate(true, 150)
        });

        var result = LPreview.LPreviewColorResolve(video);

        Assert.Equal(1.5, result.LColorContrast, 10);
    }
}
