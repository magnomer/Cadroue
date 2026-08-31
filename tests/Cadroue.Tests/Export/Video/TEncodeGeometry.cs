using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

public sealed class TEncodeGeometry
{
    [Fact]
    public void VerticalFlipAndClockwiseRotation_FiltersMatchPreviewOrder()
    {
        LWorkCrop lCrop = TInterface.TWorkCropCreate(0, 0, 0, 0, 90, false, true);

        IReadOnlyList<string> lFilters = TInterface.TEncodeGeometryRead(lCrop);

        Assert.Equal(new[] { "vflip", "transpose=1" }, lFilters);
    }

    [Fact]
    public void FlipsRotationAndCrop_FiltersFollowGeometryOrder()
    {
        LWorkCrop lCrop = TInterface.TWorkCropCreate(2, 4, 6, 8, 270, true, true);

        IReadOnlyList<string> lFilters = TInterface.TEncodeGeometryRead(lCrop);

        Assert.Equal(
            new[]
            {
                "hflip",
                "vflip",
                "transpose=2",
                "crop=in_w-2-6:in_h-4-8:2:4"
            },
            lFilters);
    }
}
