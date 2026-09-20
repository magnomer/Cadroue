using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TInspectorCropOrientation
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 90)]
    [InlineData(2, 180)]
    [InlineData(3, 270)]
    public void RotateSelect_Index_MapsToDegrees(int index, int rotation)
    {
        LInspector inspector = TInterface.TInspectorCreate();

        TInterface.TInspectorRotateSelect(inspector, index);

        Assert.Equal(rotation, inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop.LWorkCropRotation);
        Assert.Equal(index, inspector.LInspectorCrop.LInspectorRotateIndex);
    }

    [Fact]
    public void RotateSelect_Negative_IsIgnored()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorRotateSelect(inspector, 2);

        TInterface.TInspectorRotateSelect(inspector, -1);

        Assert.Equal(2, inspector.LInspectorCrop.LInspectorRotateIndex);
    }

    [Fact]
    public void FlipSet_EachAxisIndependent()
    {
        LInspector inspector = TInterface.TInspectorCreate();

        TInterface.TInspectorFlipSet(inspector, true, true);
        Assert.True(inspector.LInspectorCrop.LInspectorFlipHorizontal);
        Assert.False(inspector.LInspectorCrop.LInspectorFlipVertical);

        TInterface.TInspectorFlipSet(inspector, false, true);
        TInterface.TInspectorFlipSet(inspector, true, false);

        Assert.False(inspector.LInspectorCrop.LInspectorFlipHorizontal);
        Assert.True(inspector.LInspectorCrop.LInspectorFlipVertical);
    }

    [Fact]
    public void RotateSelect_WithEdges_RemapsEdgesLikeGeometry()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);
        LWorkCrop before = TInterface.TWorkCropCreate(10, 20, 30, 40, 0, false, false);
        TInterface.TInspectorCropApply(inspector, before, true);

        TInterface.TInspectorRotateSelect(inspector, 1);

        Assert.Equal(
            TInterface.TCropboxOrientationResolve(before, 90, false, false),
            inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop);
    }

    [Fact]
    public void OrientationSet_SameValues_RaisesNothing()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        int notices = 0;
        TInterface.TCropboxStateAttach(inspector.LInspectorCrop.LInspectorCropbox, () => notices++);

        TInterface.TInspectorRotateSelect(inspector, 0);
        TInterface.TInspectorFlipSet(inspector, true, false);

        Assert.Equal(0, notices);
    }
}
