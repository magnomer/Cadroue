using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TPreviewExposure
{
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

}
