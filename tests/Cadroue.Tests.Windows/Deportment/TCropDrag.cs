using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TCropDrag
{
    private const string TCropSource = @"C:\media\clip.mp4";

    private static (LViewer, LCrop, LCropDrag) TCropBuild()
    {
        LViewer viewer = TInterfaceViewer.TViewerCreate();
        LCrop crop = TInterfaceCrop.TCropRead(viewer);
        LCargo cargo = TInterfaceViewer.TCargoCreate(
            TCropSource, TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080), true);
        TInterfaceViewer.TViewerMediaCommit(viewer, cargo, false);
        TInterfaceCrop.TCropSizeHandle(crop, 800, 450);
        TInterfaceCrop.TCropToolSet(crop, true);
        return (viewer, crop, TInterfaceCrop.TCropDragRead(viewer));
    }

    private static (double, double, double, double) TCropBoxResolve(LCrop crop)
    {
        LCropBox box = TInterfaceCrop.TCropBoxRead(crop);
        return (box.LCropBoxX, box.LCropBoxY, box.LCropBoxWidth, box.LCropBoxHeight);
    }

    [Fact]
    public void Draw_PressMoveRelease_CommitsSourcePixels()
    {
        (LViewer viewer, LCrop crop, LCropDrag drag) = TCropBuild();
        var captures = new List<bool>();
        int commits = 0;
        TInterfaceCrop.TCropCaptureAttach(drag, captures.Add);
        TInterfaceCrop.TCropVideoAttach(crop, () => commits++);

        Assert.True(TInterfaceCrop.TCropPressHandle(drag, 100, 100));
        Assert.True(drag.LCropPointActive);
        Assert.True(TInterfaceCrop.TCropMoveHandle(drag, true, 300, 200));
        Assert.Equal((100, 100, 200, 100), TCropBoxResolve(crop));
        Assert.False(TInterfaceCrop.TCropMoveHandle(drag, false, 400, 400));
        Assert.True(TInterfaceCrop.TCropReleaseHandle(drag, 300, 200));

        Assert.False(drag.LCropPointActive);
        Assert.Equal([true, false], captures);
        Assert.Equal(1, commits);
        Assert.Equal(Interface.TInterface.TCropboxCreate(240, 240, 480, 240), viewer.LViewerPreview.LCropbox);
    }

    [Fact]
    public void Draw_WithRatio_ClampsToVideoAndKeepsShape()
    {
        (_, LCrop crop, LCropDrag drag) = TCropBuild();
        TInterfaceCrop.TCropRatioSet(crop, 1, 1);

        Assert.True(TInterfaceCrop.TCropPressHandle(drag, 0, 0));
        Assert.True(TInterfaceCrop.TCropMoveHandle(drag, true, 100, 50));

        Assert.Equal((0, 0, 50, 50), TCropBoxResolve(crop));
    }

    [Fact]
    public void Grip_BottomRight_ResizesFromOrigin()
    {
        (_, LCrop crop, LCropDrag drag) = TCropBuild();
        TInterfaceCrop.TCropRectSet(crop, Interface.TInterface.TCropboxCreate(240, 240, 480, 240));

        Assert.True(TInterfaceCrop.TCropGripHandle(drag, 4, 300, 200));
        Assert.True(drag.LCropPress);
        Assert.Equal((1, 1), (crop.LCropEdgeX, crop.LCropEdgeY));
        Assert.True(TInterfaceCrop.TCropMoveHandle(drag, true, 340, 240));
        Assert.Equal((100, 100, 240, 140), TCropBoxResolve(crop));
        Assert.True(TInterfaceCrop.TCropReleaseHandle(drag, 340, 240));

        Assert.False(drag.LCropPress);
        Assert.Equal(Interface.TInterface.TCropboxCreate(240, 240, 576, 336), TInterfaceCrop.TCropPixelRead(crop));
    }

    [Fact]
    public void Body_Drag_MovesWholeBoxInsideVideo()
    {
        (_, LCrop crop, LCropDrag drag) = TCropBuild();
        TInterfaceCrop.TCropRectSet(crop, Interface.TInterface.TCropboxCreate(240, 240, 480, 240));

        Assert.True(TInterfaceCrop.TCropBodyHandle(drag, 150, 150));
        Assert.True(TInterfaceCrop.TCropMoveHandle(drag, true, 170, 160));
        Assert.Equal((120, 110, 200, 100), TCropBoxResolve(crop));
        Assert.True(TInterfaceCrop.TCropMoveHandle(drag, true, 2000, 2000));
        Assert.Equal((600, 350, 200, 100), TCropBoxResolve(crop));
    }

    [Fact]
    public void Press_NotEditable_IsIgnored()
    {
        (LViewer viewer, LCrop crop, LCropDrag drag) = TCropBuild();
        TInterfaceCrop.TCropRectSet(crop, Interface.TInterface.TCropboxCreate(240, 240, 480, 240));

        TInterfaceCrop.TCropLockSet(crop, true);
        Assert.False(TInterfaceCrop.TCropPressHandle(drag, 10, 10));
        Assert.False(TInterfaceCrop.TCropBodyHandle(drag, 150, 150));
        Assert.False(TInterfaceCrop.TCropGripHandle(drag, 0, 100, 100));

        TInterfaceCrop.TCropLockSet(crop, false);
        TInterfaceCrop.TCropToolSet(crop, false);
        Assert.False(TInterfaceCrop.TCropPressHandle(drag, 10, 10));
        Assert.Equal(LViewerTool.LViewerToolNone, TInterfaceViewer.TViewerNeutralRead(viewer).LViewerTool);
    }

    [Fact]
    public void Lock_DuringDrag_ClearsTransientsAndReleasesCapture()
    {
        (_, LCrop crop, LCropDrag drag) = TCropBuild();
        var captures = new List<bool>();
        TInterfaceCrop.TCropCaptureAttach(drag, captures.Add);
        TInterfaceCrop.TCropRectSet(crop, Interface.TInterface.TCropboxCreate(240, 240, 480, 240));
        TInterfaceCrop.TCropBodyHandle(drag, 150, 150);

        TInterfaceCrop.TCropLockSet(crop, true);

        Assert.False(drag.LCropPress);
        Assert.Equal([true, false], captures);
    }
}
