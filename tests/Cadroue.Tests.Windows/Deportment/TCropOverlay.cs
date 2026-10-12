using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TCropOverlay
{
    private const string TCropSource = @"C:\media\clip.mp4";

    private static (QViewer, QCrop) TCropBuild(LRotateKind rotate, double areaWidth, double areaHeight)
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        QCrop crop = TInterfaceCrop.TCropRead(viewer);
        LCargo cargo = TInterfaceViewer.TCargoCreate(
            TCropSource, TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080), true);
        TInterfaceViewer.TViewerMediaCommit(viewer, cargo, false);
        TInterfaceViewer.TViewerRotateSet(viewer, Interface.TInterface.TRotateFlipCreate(rotate, false, false));
        TInterfaceCrop.TCropSizeHandle(crop, areaWidth, areaHeight);
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
        (_, QCrop crop) = TCropBuild(rotate, areaWidth, areaHeight);

        LCropbox video = TInterfaceCrop.TCropVideoRead(crop);

        Assert.Equal(x, video.LCropboxX, 6);
        Assert.Equal(y, video.LCropboxY, 6);
        Assert.Equal(width, video.LCropboxWidth, 6);
        Assert.Equal(height, video.LCropboxHeight, 6);
    }

    [Fact]
    public void RectSet_PlacesBoxHandlesAndShade_InOverlayPixels()
    {
        (QViewer viewer, QCrop crop) = TCropBuild(LRotateKind.LRotateNone, 800, 450);
        int applies = 0;
        TInterfaceCrop.TCropAttach(crop, () => applies++);

        TInterfaceCrop.TCropRectSet(crop, Interface.TInterface.TCropboxCreate(480, 270, 960, 540));

        QCropBox box = TInterfaceCrop.TCropBoxRead(crop);
        Assert.True(box.QCropBoxShown);
        Assert.Equal((200, 112.5, 400, 225), (box.QCropBoxX, box.QCropBoxY, box.QCropBoxWidth, box.QCropBoxHeight));
        Assert.Equal(1, applies);

        IReadOnlyList<QCropGrip> handles = TInterfaceCrop.TCropHandlesRead(crop);
        Assert.Equal(QCrop.QCropHandleCount, handles.Count);
        Assert.All(handles, handle => Assert.True(handle.QCropHandleShown));
        Assert.Equal((195, 107.5), (handles[0].QCropHandleX, handles[0].QCropHandleY));
        Assert.Equal((395, 107.5), (handles[1].QCropHandleX, handles[1].QCropHandleY));
        Assert.Equal((595, 332.5), (handles[4].QCropHandleX, handles[4].QCropHandleY));
        Assert.Equal((195, 220), (handles[7].QCropHandleX, handles[7].QCropHandleY));

        QCropShade shade = TInterfaceCrop.TCropShadeRead(crop);
        Assert.True(shade.QCropShadeShown);
        Assert.Equal(800, shade.QCropShadeVideo.LCropboxWidth);
        Assert.Equal(400, shade.QCropShadeBox.LCropboxWidth);
        Assert.Equal(Interface.TInterface.TCropboxCreate(480, 270, 960, 540), viewer.QViewerPreview.LCropbox);
        Assert.Equal(Interface.TInterface.TCropboxCreate(480, 270, 960, 540), TInterfaceCrop.TCropPixelRead(crop));
    }

    [Fact]
    public void RectSet_Rotated_BoxFollowsDisplayTransform()
    {
        (_, QCrop crop) = TCropBuild(LRotateKind.LRotate90, 800, 450);

        TInterfaceCrop.TCropRectSet(crop, Interface.TInterface.TCropboxCreate(0, 0, 540, 960));

        QCropBox box = TInterfaceCrop.TCropBoxRead(crop);
        Assert.Equal(273.4375, box.QCropBoxX, 6);
        Assert.Equal(0, box.QCropBoxY, 6);
        Assert.Equal(126.5625, box.QCropBoxWidth, 6);
        Assert.Equal(225, box.QCropBoxHeight, 6);
    }

    [Fact]
    public void RectSet_OutsideSource_ClampsToFullFrame()
    {
        (QViewer viewer, QCrop crop) = TCropBuild(LRotateKind.LRotateNone, 800, 450);

        TInterfaceCrop.TCropRectSet(crop, Interface.TInterface.TCropboxCreate(1000, 600, 2000, 2000));

        Assert.Equal(Interface.TInterface.TCropboxCreate(0, 0, 1920, 1080), viewer.QViewerPreview.LCropbox);
        QCropBox box = TInterfaceCrop.TCropBoxRead(crop);
        Assert.Equal((0, 0, 800, 450), (box.QCropBoxX, box.QCropBoxY, box.QCropBoxWidth, box.QCropBoxHeight));
    }

    [Fact]
    public void RectSet_Null_HidesBoxAndClearsPreview()
    {
        (QViewer viewer, QCrop crop) = TCropBuild(LRotateKind.LRotateNone, 800, 450);
        TInterfaceCrop.TCropRectSet(crop, Interface.TInterface.TCropboxCreate(0, 0, 960, 540));

        TInterfaceCrop.TCropRectSet(crop, null);

        Assert.False(TInterfaceCrop.TCropBoxRead(crop).QCropBoxShown);
        Assert.False(TInterfaceCrop.TCropShadeRead(crop).QCropShadeShown);
        Assert.Null(viewer.QViewerPreview.LCropbox);
        Assert.Null(TInterfaceCrop.TCropPixelRead(crop));
        Assert.All(TInterfaceCrop.TCropHandlesRead(crop), handle => Assert.False(handle.QCropHandleShown));
    }

    [Fact]
    public void SizeHandle_Resize_RestoresBoxFromPreview()
    {
        (_, QCrop crop) = TCropBuild(LRotateKind.LRotateNone, 800, 450);
        TInterfaceCrop.TCropRectSet(crop, Interface.TInterface.TCropboxCreate(0, 0, 960, 540));

        TInterfaceCrop.TCropSizeHandle(crop, 400, 225);

        QCropBox box = TInterfaceCrop.TCropBoxRead(crop);
        Assert.Equal((200, 112.5), (box.QCropBoxWidth, box.QCropBoxHeight));
    }

    [Fact]
    public void ActiveAndLock_GateEditableAndCursor()
    {
        (QViewer viewer, QCrop crop) = TCropBuild(LRotateKind.LRotateNone, 800, 450);
        QViewerNeutral neutral = TInterfaceViewer.TViewerNeutralRead(viewer);
        TInterfaceCrop.TCropRectSet(crop, Interface.TInterface.TCropboxCreate(0, 0, 960, 540));

        Assert.True(crop.QCropEditable);
        Assert.False(crop.QCropCrossShown);

        TInterfaceCrop.TCropToolSet(crop, true);
        Assert.Equal(QViewerTool.QViewerToolCrop, neutral.QViewerTool);
        Assert.True(crop.QCropCrossShown);

        TInterfaceCrop.TCropActiveSet(crop, false);
        Assert.Equal(QViewerTool.QViewerToolNone, neutral.QViewerTool);
        Assert.False(crop.QCropEditable);
        Assert.False(TInterfaceCrop.TCropBoxRead(crop).QCropBoxShown);

        TInterfaceCrop.TCropActiveSet(crop, true);
        TInterfaceCrop.TCropLockSet(crop, true);
        Assert.False(crop.QCropEditable);
        Assert.True(TInterfaceCrop.TCropBoxRead(crop).QCropBoxShown);
        Assert.All(TInterfaceCrop.TCropHandlesRead(crop), handle => Assert.False(handle.QCropHandleShown));
    }
}
