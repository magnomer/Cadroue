using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TCropDrag
{
    private const string TCropSource = @"C:\media\clip.mp4";

    private static (LViewer, LCrop, LCropDrag) TCropBuild()
    {
        LViewer viewer = TInterface.TViewerCreate();
        LCrop crop = TInterface.TCropRead(viewer);
        LCargo cargo = TInterface.TCargoCreate(
            TCropSource, TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080), true);
        TInterface.TViewerMediaCommit(viewer, cargo, false);
        TInterface.TCropSizeHandle(crop, 800, 450);
        TInterface.TCropToolSet(crop, true);
        return (viewer, crop, TInterface.TCropDragRead(viewer));
    }

    private static (double, double, double, double) TCropBoxResolve(LCrop crop)
    {
        LCropBox box = TInterface.TCropBoxRead(crop);
        return (box.LCropBoxX, box.LCropBoxY, box.LCropBoxWidth, box.LCropBoxHeight);
    }

    [Fact]
    public void Draw_PressMoveRelease_CommitsSourcePixels()
    {
        (LViewer viewer, LCrop crop, LCropDrag drag) = TCropBuild();
        var captures = new List<bool>();
        int commits = 0;
        TInterface.TCropCaptureAttach(drag, captures.Add);
        TInterface.TCropVideoAttach(crop, () => commits++);

        Assert.True(TInterface.TCropPressHandle(drag, 100, 100));
        Assert.True(drag.LCropPointActive);
        Assert.True(TInterface.TCropMoveHandle(drag, true, 300, 200));
        Assert.Equal((100, 100, 200, 100), TCropBoxResolve(crop));
        Assert.False(TInterface.TCropMoveHandle(drag, false, 400, 400));
        Assert.True(TInterface.TCropReleaseHandle(drag, 300, 200));

        Assert.False(drag.LCropPointActive);
        Assert.Equal([true, false], captures);
        Assert.Equal(1, commits);
        Assert.Equal(TInterface.TCropboxCreate(240, 240, 480, 240), viewer.LViewerPreview.LCropbox);
    }

    [Fact]
    public void Draw_WithRatio_ClampsToVideoAndKeepsShape()
    {
        (_, LCrop crop, LCropDrag drag) = TCropBuild();
        TInterface.TCropRatioSet(crop, 1, 1);

        Assert.True(TInterface.TCropPressHandle(drag, 0, 0));
        Assert.True(TInterface.TCropMoveHandle(drag, true, 100, 50));

        Assert.Equal((0, 0, 50, 50), TCropBoxResolve(crop));
    }

    [Fact]
    public void Grip_BottomRight_ResizesFromOrigin()
    {
        (_, LCrop crop, LCropDrag drag) = TCropBuild();
        TInterface.TCropRectSet(crop, TInterface.TCropboxCreate(240, 240, 480, 240));

        Assert.True(TInterface.TCropGripHandle(drag, 4, 300, 200));
        Assert.True(drag.LCropPress);
        Assert.Equal((1, 1), (crop.LCropEdgeX, crop.LCropEdgeY));
        Assert.True(TInterface.TCropMoveHandle(drag, true, 340, 240));
        Assert.Equal((100, 100, 240, 140), TCropBoxResolve(crop));
        Assert.True(TInterface.TCropReleaseHandle(drag, 340, 240));

        Assert.False(drag.LCropPress);
        Assert.Equal(TInterface.TCropboxCreate(240, 240, 576, 336), TInterface.TCropPixelRead(crop));
    }

    [Fact]
    public void Body_Drag_MovesWholeBoxInsideVideo()
    {
        (_, LCrop crop, LCropDrag drag) = TCropBuild();
        TInterface.TCropRectSet(crop, TInterface.TCropboxCreate(240, 240, 480, 240));

        Assert.True(TInterface.TCropBodyHandle(drag, 150, 150));
        Assert.True(TInterface.TCropMoveHandle(drag, true, 170, 160));
        Assert.Equal((120, 110, 200, 100), TCropBoxResolve(crop));
        Assert.True(TInterface.TCropMoveHandle(drag, true, 2000, 2000));
        Assert.Equal((600, 350, 200, 100), TCropBoxResolve(crop));
    }

    [Fact]
    public void Press_NotEditable_IsIgnored()
    {
        (LViewer viewer, LCrop crop, LCropDrag drag) = TCropBuild();
        TInterface.TCropRectSet(crop, TInterface.TCropboxCreate(240, 240, 480, 240));

        TInterface.TCropLockSet(crop, true);
        Assert.False(TInterface.TCropPressHandle(drag, 10, 10));
        Assert.False(TInterface.TCropBodyHandle(drag, 150, 150));
        Assert.False(TInterface.TCropGripHandle(drag, 0, 100, 100));

        TInterface.TCropLockSet(crop, false);
        TInterface.TCropToolSet(crop, false);
        Assert.False(TInterface.TCropPressHandle(drag, 10, 10));
        Assert.Equal(LViewerTool.LViewerToolNone, TInterface.TViewerNeutralRead(viewer).LViewerTool);
    }

    [Fact]
    public void Lock_DuringDrag_ClearsTransientsAndReleasesCapture()
    {
        (_, LCrop crop, LCropDrag drag) = TCropBuild();
        var captures = new List<bool>();
        TInterface.TCropCaptureAttach(drag, captures.Add);
        TInterface.TCropRectSet(crop, TInterface.TCropboxCreate(240, 240, 480, 240));
        TInterface.TCropBodyHandle(drag, 150, 150);

        TInterface.TCropLockSet(crop, true);

        Assert.False(drag.LCropPress);
        Assert.Equal([true, false], captures);
    }
}
