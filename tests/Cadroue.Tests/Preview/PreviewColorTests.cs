using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class PreviewColorTests
{
    [Fact]
    public void Apply_DefaultColor_ResolvesToNeutral()
    {
        var state = LPreviewState.LPreviewDefaultCreate();

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
        var state = LPreviewState.LPreviewDefaultCreate()
            .LColorChange(new LColor(0, 2, 1, 0));

        var result = new TPreview().ApplyState(state);

        Assert.Equal(100, result.Contrast);
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
