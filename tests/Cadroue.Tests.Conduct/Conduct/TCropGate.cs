using System;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TCropGate
{
    private readonly TCropPort tCropPort = new();
    private readonly CCrop tCropGate;

    public TCropGate()
    {
        tCropGate = TAtelier.TAtelierCreate().CAtelierInspectorCreate().CInspectorCrop;
        tCropGate.CCropAccept(tCropPort);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 1)]
    [InlineData(180, 2)]
    [InlineData(270, 3)]
    [InlineData(45, 0)]
    public void CropRotateIndex_Rotation_ShowsItsChoice(int rotation, int index)
    {
        tCropPort.TCropRotation = rotation;

        Assert.Equal(index, tCropGate.CCropRotateIndex);
    }

    [Fact]
    public void CropFlip_ReadsBothAxes()
    {
        tCropPort.TCropHorizontal = true;

        Assert.True(tCropGate.CCropFlipHorizontal);
        Assert.False(tCropGate.CCropFlipVertical);
    }

    [Fact]
    public void CropRotateSelect_Choice_AsksItsRotation()
    {
        tCropGate.CCropRotateSelect(3);
        tCropGate.CCropRotateSelect(1);

        Assert.Equal(["rotate 270", "rotate 90"], tCropPort.TCropCalls);
    }

    [Fact]
    public void CropRotateSelect_NoChoice_AsksNothing()
    {
        tCropGate.CCropRotateSelect(-1);

        Assert.Empty(tCropPort.TCropCalls);
    }

    [Fact]
    public void CropRotateSelect_PastTheChoices_Throws() =>
        Assert.Throws<IndexOutOfRangeException>(() => tCropGate.CCropRotateSelect(4));

    [Fact]
    public void CropFlipSet_HandsAxisAndState()
    {
        tCropGate.CCropFlipSet(true, true);
        tCropGate.CCropFlipSet(false, false);

        Assert.Equal(["flip horizontal True", "flip vertical False"], tCropPort.TCropCalls);
    }

    [Fact]
    public void CropAccept_NoPort_Throws() =>
        Assert.Throws<ArgumentNullException>(() => tCropGate.CCropAccept(null!));

    [Fact]
    public void CropGate_BeforeAccept_Throws()
    {
        CCrop lGate = TAtelier.TAtelierCreate().CAtelierInspectorCreate().CInspectorCrop;

        Assert.Throws<InvalidOperationException>(() => lGate.CCropRotateIndex);
        Assert.Throws<InvalidOperationException>(() => lGate.CCropFlipSet(true, true));
    }

    [Fact]
    public void CropChanged_RecordChange_IsTold()
    {
        int lTold = 0;
        tCropGate.CCropChanged += () => lTold++;

        tCropPort.TCropRaise();

        Assert.Equal(1, lTold);
    }

    [Theory]
    [InlineData(true, 16, 9, 1)]
    [InlineData(true, 21, 9, 6)]
    [InlineData(true, 5, 2, 0)]
    [InlineData(false, 16, 9, 0)]
    public void CropPresetIndex_Ratio_ShowsItsChoice(bool ratioFixed, int width, int height, int index)
    {
        tCropPort.TCropState = (false, false, ratioFixed, false, width, height);

        Assert.Equal(index, tCropGate.CCropPresetIndex);
        Assert.Equal(index == 0, tCropGate.CCropCustomShown);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 20)]
    [InlineData(2, 30)]
    [InlineData(3, 40)]
    [InlineData(7, 40)]
    public void CropEdgeRead_Side_ReadsItsEdge(int side, int edge)
    {
        tCropPort.TCropEdges = (10, 20, 30, 40);

        Assert.Equal(edge, tCropGate.CCropEdgeRead(side));
    }

    [Fact]
    public void CropExtent_SourceLessEdges()
    {
        tCropPort.TCropEdges = (10, 20, 30, 40);

        Assert.Equal(1880, tCropGate.CCropWidth);
        Assert.Equal(1020, tCropGate.CCropHeight);
    }

    [Fact]
    public void CropResolution_NormalEdges_RoundsTheSize()
    {
        tCropPort.TCropSource = (true, 1000.6, 500.4);
        tCropPort.TCropNormal = (0, 0, 0, 0);

        Assert.Equal((1001d, 500d), tCropGate.CCropResolution);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1000)]
    public void CropResolution_NoSourceOrNoSize_IsNone(bool present, int edge)
    {
        tCropPort.TCropSource = (present, 1000, 500);
        tCropPort.TCropNormal = (edge, 0, 0, 0);

        Assert.Null(tCropGate.CCropResolution);
    }

    [Fact]
    public void CropRatioRead_HeldRatio_ShowsIt()
    {
        tCropPort.TCropState = (false, false, true, false, 16, 9);

        Assert.Equal(16, tCropGate.CCropRatioRead(true));
        Assert.Equal(9, tCropGate.CCropRatioRead(false));
        Assert.Empty(tCropPort.TCropCalls);
    }

    [Fact]
    public void CropRatioRead_NoRatio_ShowsTheDrawnRatio()
    {
        tCropPort.TCropEdges = (0, 0, 0, 0);

        Assert.Equal(192, tCropGate.CCropRatioRead(true));
        Assert.Equal(["normalize 1920x1080"], tCropPort.TCropCalls);
    }

    [Fact]
    public void CropRatioRead_NoRect_ShowsZero()
    {
        tCropPort.TCropRect = false;

        Assert.Equal(0, tCropGate.CCropRatioRead(true));
        Assert.Empty(tCropPort.TCropCalls);
    }

    [Fact]
    public void CropEdgeCommit_Typed_LocksAndClampsTheSide()
    {
        tCropGate.CCropEdgeCommit(2, "-5", true);
        tCropGate.CCropEdgeCommit(1, " ", true);
        tCropGate.CCropEdgeCommit(0, "12", false);

        Assert.Equal(["edge 2 0 True", "edge 1 0 False"], tCropPort.TCropCalls);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public void CropApplySet_TurnedOff_DropsTheRatio(bool active, bool apply, bool reset)
    {
        tCropPort.TCropState = (active, false, false, false, 0, 0);

        tCropGate.CCropApplySet(apply);

        Assert.Equal([$"apply {apply} {reset}"], tCropPort.TCropCalls);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void CropReset_Persistent_KeepsTheCrop(bool persistent, bool reset)
    {
        tCropPort.TCropState = (true, persistent, false, false, 0, 0);

        Assert.Equal(reset, tCropGate.CCropReset());
        Assert.Equal(reset ? ["reset"] : [], tCropPort.TCropCalls);
    }

    [Fact]
    public void CropRatioCommit_Typed_KeepsTheSwitches()
    {
        tCropPort.TCropState = (false, false, true, true, 4, 3);

        tCropGate.CCropRatioCommit("16", "x", true);
        tCropGate.CCropRatioCommit("1", "1", false);

        Assert.Equal(["ratio True True 16:0 False"], tCropPort.TCropCalls);
    }

    [Fact]
    public void CropFixedAndLenient_FreeRatio_IsNeverLenient()
    {
        tCropPort.TCropState = (false, false, true, true, 4, 3);

        tCropGate.CCropFixedSet(false);
        tCropGate.CCropLenientSet(true);

        Assert.Equal(["ratio False False 4:3 True", "ratio True True 4:3 True"], tCropPort.TCropCalls);
    }

    [Theory]
    [InlineData(16, 9, "ratio True True 16:9 False")]
    [InlineData(0, 9, "ratio False False 0:9 False")]
    public void CropRatioApply_RatioWithoutParts_IsFree(int width, int height, string call)
    {
        tCropGate.CCropRatioApply(true, true, width, height);

        Assert.Equal([call], tCropPort.TCropCalls);
    }

    [Theory]
    [InlineData(-1, true, 16, 9, true, null)]
    [InlineData(0, true, 16, 9, true, "ratio reset")]
    [InlineData(0, true, 5, 2, true, null)]
    [InlineData(1, true, 16, 9, true, null)]
    [InlineData(1, false, 16, 9, true, "preset 16:9")]
    [InlineData(3, false, 0, 0, false, null)]
    [InlineData(3, false, 0, 0, true, "preset 4:3")]
    public void CropPresetSelect_Choice_AsksItsEdit(
        int index, bool ratioFixed, int width, int height, bool present, string? call)
    {
        tCropPort.TCropState = (false, false, ratioFixed, false, width, height);
        tCropPort.TCropSource = (present, 1920, 1080);

        tCropGate.CCropPresetSelect(index);

        Assert.Equal(call is null ? [] : [call], tCropPort.TCropCalls);
    }
}
