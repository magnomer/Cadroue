using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TCropAnchor
{
    [Theory]
    [InlineData(-1, -1, -1, 1, 1)]
    [InlineData(0, -1, 1, 0, 1)]
    [InlineData(1, 0, 0, -1, 0)]
    [InlineData(1, 1, -1, -1, -1)]
    public void Grip_DrivesFromEdge_AnchorsOpposite(int edgeX, int edgeY, int drive, int anchorX, int anchorY)
    {
        QCrop crop = TInterfaceCrop.TCropCreate();

        TInterfaceCrop.TCropGripSet(crop, edgeX, edgeY);

        Assert.Equal(edgeX, crop.QCropEdgeX);
        Assert.Equal(edgeY, crop.QCropEdgeY);
        Assert.Equal(drive, crop.QCropDrive);
        Assert.Equal(anchorX, crop.QCropAnchorX);
        Assert.Equal(anchorY, crop.QCropAnchorY);
        Assert.False(TInterfaceCrop.TCropMoveCheck(crop));
    }

    [Fact]
    public void Body_ClearsEdges_MovesWholeBox()
    {
        QCrop crop = TInterfaceCrop.TCropCreate();
        TInterfaceCrop.TCropGripSet(crop, 1, 0);

        TInterfaceCrop.TCropBodySet(crop);

        Assert.True(TInterfaceCrop.TCropMoveCheck(crop));
        Assert.Equal(-1, crop.QCropDrive);
        Assert.Equal(-1, crop.QCropAnchorX);
        Assert.Equal(-1, crop.QCropAnchorY);
    }

    [Fact]
    public void Draw_ResetsAnchor_KeepsEdges()
    {
        QCrop crop = TInterfaceCrop.TCropCreate();
        TInterfaceCrop.TCropGripSet(crop, 0, 1);

        TInterfaceCrop.TCropDrawSet(crop);

        Assert.Equal(0, crop.QCropEdgeX);
        Assert.Equal(1, crop.QCropEdgeY);
        Assert.Equal(-1, crop.QCropDrive);
        Assert.Equal(-1, crop.QCropAnchorX);
    }

    [Fact]
    public void Ratio_InvalidBecomesFree()
    {
        QCrop crop = TInterfaceCrop.TCropCreate();

        TInterfaceCrop.TCropRatioSet(crop, 16, 9);
        Assert.Equal(16, crop.QCropRatioWidth);
        Assert.Equal(9, crop.QCropRatioHeight);

        TInterfaceCrop.TCropRatioSet(crop, 16, 0);
        Assert.Equal(0, crop.QCropRatioWidth);
        Assert.Equal(0, crop.QCropRatioHeight);
    }

    [Fact]
    public void Lock_DefaultsOpen_ActiveDefaultsOn()
    {
        QCrop crop = TInterfaceCrop.TCropCreate();

        Assert.True(crop.QCropActive);
        Assert.False(crop.QCropLocked);
        Assert.False(crop.QCropPersistent);

        TInterfaceCrop.TCropLockSet(crop, true);

        Assert.True(crop.QCropLocked);
    }

    [Fact]
    public void Loupe_PlayingClearsEnded_ResumeNeedsBoth()
    {
        QSLoupe loupe = TInterface.TLoupeCreate();
        List<bool> notices = [];
        TInterface.TLoupePlayingAttach(loupe, notices.Add);

        TInterface.TLoupePlayingSet(loupe, true);
        TInterface.TLoupeEndSet(loupe, true);
        TInterface.TLoupePlayingSet(loupe, false);

        Assert.False(TInterface.TLoupeResumeCheck(loupe));
        Assert.True(loupe.QSLoupeEnded);

        TInterface.TLoupePlayingSet(loupe, true);

        Assert.False(loupe.QSLoupeEnded);
        Assert.True(TInterface.TLoupeResumeCheck(loupe));
        Assert.Equal([true, false, true], notices);

        TInterface.TLoupeClose(loupe);

        Assert.True(loupe.QSLoupeClosed);
    }
}
