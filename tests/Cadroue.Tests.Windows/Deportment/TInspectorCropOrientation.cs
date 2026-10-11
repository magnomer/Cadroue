using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TInspectorCropOrientation
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 90)]
    [InlineData(2, 180)]
    [InlineData(3, 270)]
    public void RotateSelect_Index_MapsToDegrees(int index, int rotation)
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();

        TInterfaceInspector.TInspectorRotateSelect(inspector, index);

        Assert.Equal(rotation, inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop.LWorkCropRotation);
        Assert.Equal(index, inspector.LInspectorCrop.LInspectorRotateIndex);
    }

    [Fact]
    public void RotateSelect_Negative_IsIgnored()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorRotateSelect(inspector, 2);

        TInterfaceInspector.TInspectorRotateSelect(inspector, -1);

        Assert.Equal(2, inspector.LInspectorCrop.LInspectorRotateIndex);
    }

    [Fact]
    public void FlipSet_EachAxisIndependent()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();

        TInterfaceInspector.TInspectorFlipSet(inspector, true, true);
        Assert.True(inspector.LInspectorCrop.LInspectorFlipHorizontal);
        Assert.False(inspector.LInspectorCrop.LInspectorFlipVertical);

        TInterfaceInspector.TInspectorFlipSet(inspector, false, true);
        TInterfaceInspector.TInspectorFlipSet(inspector, true, false);

        Assert.False(inspector.LInspectorCrop.LInspectorFlipHorizontal);
        Assert.True(inspector.LInspectorCrop.LInspectorFlipVertical);
    }

    [Fact]
    public void RotateSelect_WithEdges_RemapsEdgesLikeGeometry()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        LWorkCrop before = Interface.TInterface.TWorkCropCreate(10, 20, 30, 40, 0, false, false);
        TInterfaceInspector.TInspectorCropApply(inspector, before, true);

        TInterfaceInspector.TInspectorRotateSelect(inspector, 1);

        Assert.Equal(
            Interface.TInterface.TCropboxOrientationResolve(before, 90, false, false),
            inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop);
    }

    [Fact]
    public void OrientationSet_SameValues_RaisesNothing()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
        int notices = 0;
        TInterfaceCrop.TCropboxStateAttach(inspector.LInspectorCrop.LInspectorCropbox, () => notices++);

        TInterfaceInspector.TInspectorRotateSelect(inspector, 0);
        TInterfaceInspector.TInspectorFlipSet(inspector, true, false);

        Assert.Equal(0, notices);
    }
}
