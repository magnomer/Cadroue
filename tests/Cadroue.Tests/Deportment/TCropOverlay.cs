using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TCropOverlay
{
    private const string TCropSource = @"C:\media\clip.mp4";

    private static (LViewer, LCrop) TCropBuild(LRotateKind rotate, double areaWidth, double areaHeight)
    {
        LViewer viewer = TInterface.TViewerCreate();
        LCrop crop = TInterface.TCropRead(viewer);
        LCargo cargo = TInterface.TCargoCreate(
            TCropSource, TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080), true);
        TInterface.TViewerMediaCommit(viewer, cargo, false);
        TInterface.TViewerRotateSet(viewer, TInterface.TRotateFlipCreate(rotate, false, false));
        TInterface.TCropSizeHandle(crop, areaWidth, areaHeight);
        return (viewer, crop);
    }

    [Theory]
    [InlineData(LRotateKind.LRotateNone, 800, 450, 0, 0, 800, 450)]
    [InlineData(LRotateKind.LRotate180, 800, 450, 0, 0, 800, 450)]
    [InlineData(LRotateKind.LRotate90, 800, 450, 273.4375, 0, 253.125, 450)]
    [InlineData(LRotateKind.LRotate270, 400, 800, 0, 44.444444444444457, 400, 711.1111111111111)]
    public void VideoRead_FitsRotatedSourceInsideArea(
        LRotateKind rotate, double areaWidth, double areaHeight, double x, double y, double width, double height)
    {
        (_, LCrop crop) = TCropBuild(rotate, areaWidth, areaHeight);

        LCropbox video = TInterface.TCropVideoRead(crop);

        Assert.Equal(x, video.LCropboxX, 6);
        Assert.Equal(y, video.LCropboxY, 6);
        Assert.Equal(width, video.LCropboxWidth, 6);
        Assert.Equal(height, video.LCropboxHeight, 6);
    }

    [Fact]
    public void RectSet_PlacesBoxHandlesAndShade_InOverlayPixels()
    {
        (LViewer viewer, LCrop crop) = TCropBuild(LRotateKind.LRotateNone, 800, 450);
        int applies = 0;
        TInterface.TCropAttach(crop, () => applies++);

        TInterface.TCropRectSet(crop, TInterface.TCropboxCreate(480, 270, 960, 540));

        LCropBox box = TInterface.TCropBoxRead(crop);
        Assert.True(box.LCropBoxShown);
        Assert.Equal((200, 112.5, 400, 225), (box.LCropBoxX, box.LCropBoxY, box.LCropBoxWidth, box.LCropBoxHeight));
        Assert.Equal(1, applies);

        IReadOnlyList<LCropHandle> handles = TInterface.TCropHandlesRead(crop);
        Assert.Equal(LCrop.LCropHandleCount, handles.Count);
        Assert.All(handles, handle => Assert.True(handle.LCropHandleShown));
        Assert.Equal((195, 107.5), (handles[0].LCropHandleX, handles[0].LCropHandleY));
        Assert.Equal((395, 107.5), (handles[1].LCropHandleX, handles[1].LCropHandleY));
        Assert.Equal((595, 332.5), (handles[4].LCropHandleX, handles[4].LCropHandleY));
        Assert.Equal((195, 220), (handles[7].LCropHandleX, handles[7].LCropHandleY));

        LCropShade shade = TInterface.TCropShadeRead(crop);
        Assert.True(shade.LCropShadeShown);
        Assert.Equal(800, shade.LCropShadeVideo.LCropboxWidth);
        Assert.Equal(400, shade.LCropShadeBox.LCropboxWidth);
        Assert.Equal(TInterface.TCropboxCreate(480, 270, 960, 540), viewer.LViewerPreview.LCropbox);
        Assert.Equal(TInterface.TCropboxCreate(480, 270, 960, 540), TInterface.TCropPixelRead(crop));
    }

    [Fact]
    public void RectSet_Rotated_BoxFollowsDisplayTransform()
    {
        (_, LCrop crop) = TCropBuild(LRotateKind.LRotate90, 800, 450);

        TInterface.TCropRectSet(crop, TInterface.TCropboxCreate(0, 0, 540, 960));

        LCropBox box = TInterface.TCropBoxRead(crop);
        Assert.Equal(273.4375, box.LCropBoxX, 6);
        Assert.Equal(0, box.LCropBoxY, 6);
        Assert.Equal(126.5625, box.LCropBoxWidth, 6);
        Assert.Equal(225, box.LCropBoxHeight, 6);
    }

    [Fact]
    public void RectSet_OutsideSource_ClampsToFullFrame()
    {
        (LViewer viewer, LCrop crop) = TCropBuild(LRotateKind.LRotateNone, 800, 450);

        TInterface.TCropRectSet(crop, TInterface.TCropboxCreate(1000, 600, 2000, 2000));

        Assert.Equal(TInterface.TCropboxCreate(0, 0, 1920, 1080), viewer.LViewerPreview.LCropbox);
        LCropBox box = TInterface.TCropBoxRead(crop);
        Assert.Equal((0, 0, 800, 450), (box.LCropBoxX, box.LCropBoxY, box.LCropBoxWidth, box.LCropBoxHeight));
    }

    [Fact]
    public void RectSet_Null_HidesBoxAndClearsPreview()
    {
        (LViewer viewer, LCrop crop) = TCropBuild(LRotateKind.LRotateNone, 800, 450);
        TInterface.TCropRectSet(crop, TInterface.TCropboxCreate(0, 0, 960, 540));

        TInterface.TCropRectSet(crop, null);

        Assert.False(TInterface.TCropBoxRead(crop).LCropBoxShown);
        Assert.False(TInterface.TCropShadeRead(crop).LCropShadeShown);
        Assert.Null(viewer.LViewerPreview.LCropbox);
        Assert.Null(TInterface.TCropPixelRead(crop));
        Assert.All(TInterface.TCropHandlesRead(crop), handle => Assert.False(handle.LCropHandleShown));
    }

    [Fact]
    public void SizeHandle_Resize_RestoresBoxFromPreview()
    {
        (_, LCrop crop) = TCropBuild(LRotateKind.LRotateNone, 800, 450);
        TInterface.TCropRectSet(crop, TInterface.TCropboxCreate(0, 0, 960, 540));

        TInterface.TCropSizeHandle(crop, 400, 225);

        LCropBox box = TInterface.TCropBoxRead(crop);
        Assert.Equal((200, 112.5), (box.LCropBoxWidth, box.LCropBoxHeight));
    }

    [Fact]
    public void ActiveAndLock_GateEditableAndCursor()
    {
        (LViewer viewer, LCrop crop) = TCropBuild(LRotateKind.LRotateNone, 800, 450);
        LViewerNeutral neutral = TInterface.TViewerNeutralRead(viewer);
        TInterface.TCropRectSet(crop, TInterface.TCropboxCreate(0, 0, 960, 540));

        Assert.True(crop.LCropEditable);
        Assert.False(crop.LCropCrossShown);

        TInterface.TCropToolSet(crop, true);
        Assert.Equal(LViewerTool.LViewerToolCrop, neutral.LViewerTool);
        Assert.True(crop.LCropCrossShown);

        TInterface.TCropActiveSet(crop, false);
        Assert.Equal(LViewerTool.LViewerToolNone, neutral.LViewerTool);
        Assert.False(crop.LCropEditable);
        Assert.False(TInterface.TCropBoxRead(crop).LCropBoxShown);

        TInterface.TCropActiveSet(crop, true);
        TInterface.TCropLockSet(crop, true);
        Assert.False(crop.LCropEditable);
        Assert.True(TInterface.TCropBoxRead(crop).LCropBoxShown);
        Assert.All(TInterface.TCropHandlesRead(crop), handle => Assert.False(handle.LCropHandleShown));
    }
}
