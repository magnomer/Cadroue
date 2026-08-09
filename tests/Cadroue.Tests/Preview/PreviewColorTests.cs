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

        Assert.Equal(0.5, result.LColorBrightness, 10);
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
}
