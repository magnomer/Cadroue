using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TCropAnchor
{
    [Theory]
    [InlineData(-1, -1, -1, 1, 1)]
    [InlineData(0, -1, 1, 0, 1)]
    [InlineData(1, 0, 0, -1, 0)]
    [InlineData(1, 1, -1, -1, -1)]
    public void Grip_DrivesFromEdge_AnchorsOpposite(int edgeX, int edgeY, int drive, int anchorX, int anchorY)
    {
        LCrop crop = TInterface.TCropCreate();

        TInterface.TCropGripSet(crop, edgeX, edgeY);

        Assert.Equal(edgeX, crop.LCropEdgeX);
        Assert.Equal(edgeY, crop.LCropEdgeY);
        Assert.Equal(drive, crop.LCropDrive);
        Assert.Equal(anchorX, crop.LCropAnchorX);
        Assert.Equal(anchorY, crop.LCropAnchorY);
        Assert.False(TInterface.TCropMoveCheck(crop));
    }

    [Fact]
    public void Body_ClearsEdges_MovesWholeBox()
    {
        LCrop crop = TInterface.TCropCreate();
        TInterface.TCropGripSet(crop, 1, 0);

        TInterface.TCropBodySet(crop);

        Assert.True(TInterface.TCropMoveCheck(crop));
        Assert.Equal(-1, crop.LCropDrive);
        Assert.Equal(-1, crop.LCropAnchorX);
        Assert.Equal(-1, crop.LCropAnchorY);
    }

    [Fact]
    public void Draw_ResetsAnchor_KeepsEdges()
    {
        LCrop crop = TInterface.TCropCreate();
        TInterface.TCropGripSet(crop, 0, 1);

        TInterface.TCropDrawSet(crop);

        Assert.Equal(0, crop.LCropEdgeX);
        Assert.Equal(1, crop.LCropEdgeY);
        Assert.Equal(-1, crop.LCropDrive);
        Assert.Equal(-1, crop.LCropAnchorX);
    }

    [Fact]
    public void Ratio_InvalidBecomesFree()
    {
        LCrop crop = TInterface.TCropCreate();

        TInterface.TCropRatioSet(crop, 16, 9);
        Assert.Equal(16, crop.LCropRatioWidth);
        Assert.Equal(9, crop.LCropRatioHeight);

        TInterface.TCropRatioSet(crop, 16, 0);
        Assert.Equal(0, crop.LCropRatioWidth);
        Assert.Equal(0, crop.LCropRatioHeight);
    }

    [Fact]
    public void Lock_DefaultsOpen_ActiveDefaultsOn()
    {
        LCrop crop = TInterface.TCropCreate();

        Assert.True(crop.LCropActive);
        Assert.False(crop.LCropLocked);
        Assert.False(crop.LCropPersistent);

        TInterface.TCropLockSet(crop, true);

        Assert.True(crop.LCropLocked);
    }

    [Fact]
    public void Loupe_PlayingClearsEnded_ResumeNeedsBoth()
    {
        LSLoupe loupe = TInterface.TLoupeCreate();
        List<bool> notices = [];
        TInterface.TLoupePlayingAttach(loupe, notices.Add);

        TInterface.TLoupePlayingSet(loupe, true);
        TInterface.TLoupeEndSet(loupe, true);
        TInterface.TLoupePlayingSet(loupe, false);

        Assert.False(TInterface.TLoupeResumeCheck(loupe));
        Assert.True(loupe.LSLoupeEnded);

        TInterface.TLoupePlayingSet(loupe, true);

        Assert.False(loupe.LSLoupeEnded);
        Assert.True(TInterface.TLoupeResumeCheck(loupe));
        Assert.Equal([true, false, true], notices);

        TInterface.TLoupeClose(loupe);

        Assert.True(loupe.LSLoupeClosed);
    }
}
