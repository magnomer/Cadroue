using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TCropboxOrientation
{
    [Fact]
    public void HorizontalFlip_SwapsLeftAndRight()
    {
        LWorkCrop lCrop = TInterface.TWorkCropCreate(500, 100, 200, 500, 0, false, false);

        LWorkCrop lMapped = TInterface.TCropboxOrientationResolve(lCrop, 0, true, false);

        Assert.Equal(200, lMapped.LWorkCropLeft);
        Assert.Equal(100, lMapped.LWorkCropTop);
        Assert.Equal(500, lMapped.LWorkCropRight);
        Assert.Equal(500, lMapped.LWorkCropBottom);
    }

    [Fact]
    public void VerticalFlip_SwapsTopAndBottom()
    {
        LWorkCrop lCrop = TInterface.TWorkCropCreate(10, 20, 30, 40, 0, false, false);

        LWorkCrop lMapped = TInterface.TCropboxOrientationResolve(lCrop, 0, false, true);

        Assert.Equal(10, lMapped.LWorkCropLeft);
        Assert.Equal(40, lMapped.LWorkCropTop);
        Assert.Equal(30, lMapped.LWorkCropRight);
        Assert.Equal(20, lMapped.LWorkCropBottom);
    }

    [Fact]
    public void Rotate90_MapsEdgesBottomLeftTopRight()
    {
        LWorkCrop lCrop = TInterface.TWorkCropCreate(10, 20, 30, 40, 0, false, false);

        LWorkCrop lMapped = TInterface.TCropboxOrientationResolve(lCrop, 90, false, false);

        Assert.Equal(40, lMapped.LWorkCropLeft);
        Assert.Equal(10, lMapped.LWorkCropTop);
        Assert.Equal(20, lMapped.LWorkCropRight);
        Assert.Equal(30, lMapped.LWorkCropBottom);
    }

    [Fact]
    public void Rotate180_MapsEdgesRightBottomLeftTop()
    {
        LWorkCrop lCrop = TInterface.TWorkCropCreate(10, 20, 30, 40, 0, false, false);

        LWorkCrop lMapped = TInterface.TCropboxOrientationResolve(lCrop, 180, false, false);

        Assert.Equal(30, lMapped.LWorkCropLeft);
        Assert.Equal(40, lMapped.LWorkCropTop);
        Assert.Equal(10, lMapped.LWorkCropRight);
        Assert.Equal(20, lMapped.LWorkCropBottom);
    }

    [Fact]
    public void Rotate270_MapsEdgesTopRightBottomLeft()
    {
        LWorkCrop lCrop = TInterface.TWorkCropCreate(10, 20, 30, 40, 0, false, false);

        LWorkCrop lMapped = TInterface.TCropboxOrientationResolve(lCrop, 270, false, false);

        Assert.Equal(20, lMapped.LWorkCropLeft);
        Assert.Equal(30, lMapped.LWorkCropTop);
        Assert.Equal(40, lMapped.LWorkCropRight);
        Assert.Equal(10, lMapped.LWorkCropBottom);
    }

    [Fact]
    public void OrientationResolve_CarriesNewOrientationFlags()
    {
        LWorkCrop lCrop = TInterface.TWorkCropCreate(10, 20, 30, 40, 0, false, false);

        LWorkCrop lMapped = TInterface.TCropboxOrientationResolve(lCrop, 90, true, false);

        Assert.Equal(90, lMapped.LWorkCropRotation);
        Assert.True(lMapped.LWorkFlipHorizontal);
        Assert.False(lMapped.LWorkFlipVertical);
    }

    [Fact]
    public void OrientationResolve_InverseRestoresOriginalEdges()
    {
        LWorkCrop lCrop = TInterface.TWorkCropCreate(11, 22, 33, 44, 0, false, false);

        LWorkCrop lRotated = TInterface.TCropboxOrientationResolve(lCrop, 90, false, false);
        LWorkCrop lRestored = TInterface.TCropboxOrientationResolve(lRotated, 0, false, false);

        Assert.Equal(lCrop.LWorkCropLeft, lRestored.LWorkCropLeft);
        Assert.Equal(lCrop.LWorkCropTop, lRestored.LWorkCropTop);
        Assert.Equal(lCrop.LWorkCropRight, lRestored.LWorkCropRight);
        Assert.Equal(lCrop.LWorkCropBottom, lRestored.LWorkCropBottom);
    }

    [Fact]
    public void OrientationResolve_FlipThenRotateRemainsReversible()
    {
        LWorkCrop lCrop = TInterface.TWorkCropCreate(11, 22, 33, 44, 90, true, false);

        LWorkCrop lChanged = TInterface.TCropboxOrientationResolve(lCrop, 270, false, true);
        LWorkCrop lRestored = TInterface.TCropboxOrientationResolve(lChanged, 90, true, false);

        Assert.Equal(lCrop.LWorkCropLeft, lRestored.LWorkCropLeft);
        Assert.Equal(lCrop.LWorkCropTop, lRestored.LWorkCropTop);
        Assert.Equal(lCrop.LWorkCropRight, lRestored.LWorkCropRight);
        Assert.Equal(lCrop.LWorkCropBottom, lRestored.LWorkCropBottom);
    }
}
