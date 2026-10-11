using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TInspectorCropEdge
{
    [Fact]
    public void EdgeCommit_Text_WritesClampedEdges()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);

        TInterfaceInspector.TInspectorEdgeCommit(inspector, 0, "10");
        TInterfaceInspector.TInspectorEdgeCommit(inspector, 1, "-5");
        TInterfaceInspector.TInspectorEdgeCommit(inspector, 2, "abc");
        TInterfaceInspector.TInspectorEdgeCommit(inspector, 3, "40");

        LWorkCrop crop = inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop;
        Assert.Equal(
            (10, 0, 0, 40), (crop.LWorkCropLeft, crop.LWorkCropTop, crop.LWorkCropRight, crop.LWorkCropBottom));
        Assert.Equal("1910 × 1040", TInterfaceInspector.TInspectorResolutionRead(inspector));
    }

    [Fact]
    public void EdgeFormat_EchoesEquivalentText()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        TInterfaceInspector.TInspectorEdgeCommit(inspector, 0, "12");

        Assert.Equal("12", TInterfaceInspector.TInspectorEdgeFormat(inspector, 0, "12"));
        Assert.Equal("12", TInterfaceInspector.TInspectorEdgeFormat(inspector, 0, "7"));
        Assert.Equal(string.Empty, TInterfaceInspector.TInspectorEdgeFormat(inspector, 1, string.Empty));
        Assert.Equal("0", TInterfaceInspector.TInspectorEdgeFormat(inspector, 1, "3"));
    }

    [Fact]
    public void EdgeCommit_FixedRatio_FollowsRatioAcrossAxis()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        TInterfaceInspector.TInspectorPresetSelect(inspector, 5);

        TInterfaceInspector.TInspectorEdgeCommit(inspector, 0, "500");

        LWorkCrop crop = inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop;
        double width = 1920 - crop.LWorkCropLeft - crop.LWorkCropRight;
        double height = 1080 - crop.LWorkCropTop - crop.LWorkCropBottom;
        Assert.Equal(width, height);
        Assert.True(inspector.LInspectorCrop.LInspectorRatioFixed);
    }

    [Fact]
    public void EdgeReset_ClearsEdges_KeepsRatioOff()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        TInterfaceInspector.TInspectorEdgeCommit(inspector, 2, "300");

        TInterfaceInspector.TInspectorEdgeReset(inspector);

        LWorkCrop cleared = Interface.TInterface.TWorkCropCreate();
        Assert.Equal(cleared, inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop);
        Assert.False(inspector.LInspectorCrop.LInspectorRatioFixed);
    }

    [Fact]
    public void CropSet_DrawnRect_BecomesEdges_NullClears()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);

        TInterfaceInspector.TInspectorCropSet(
            inspector, Interface.TInterface.TCropboxCreate(100, 50, 800, 600), -1, -1, -1);

        LWorkCrop crop = inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop;
        Assert.Equal(
            (100, 50, 1020, 430), (crop.LWorkCropLeft, crop.LWorkCropTop, crop.LWorkCropRight, crop.LWorkCropBottom));

        TInterfaceInspector.TInspectorCropSet(inspector, null, -1, -1, -1);

        LWorkCrop cleared = Interface.TInterface.TWorkCropCreate();
        Assert.Equal(cleared, inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop);
    }

    [Fact]
    public void CropSet_FixedRatio_SnapsDrawnRect()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        TInterfaceInspector.TInspectorPresetSelect(inspector, 5);

        TInterfaceInspector.TInspectorCropSet(
            inspector, Interface.TInterface.TCropboxCreate(0, 0, 800, 400), -1, -1, -1);

        LCropbox? rect = TInterfaceInspector.TInspectorRectRead(inspector);
        Assert.NotNull(rect);
        Assert.Equal(rect.LCropboxWidth, rect.LCropboxHeight);
    }

    [Fact]
    public void CropReset_PersistentSkips_OtherwiseClears()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        TInterfaceInspector.TInspectorEdgeCommit(inspector, 0, "20");
        TInterfaceInspector.TInspectorStepSet(inspector, "Crop");
        TInterfaceInspector.TInspectorApplySet(inspector, true);
        TInterfaceInspector.TInspectorToolSet(inspector, true);
        TInterfaceCrop.TCropboxPersistentSet(inspector.LInspectorCrop.LInspectorCropbox, true);

        TInterfaceInspector.TInspectorCropReset(inspector);
        Assert.Equal(20, inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop.LWorkCropLeft);
        Assert.True(inspector.LInspectorToolArmed);

        TInterfaceCrop.TCropboxPersistentSet(inspector.LInspectorCrop.LInspectorCropbox, false);
        TInterfaceInspector.TInspectorCropReset(inspector);

        LWorkCrop cleared = Interface.TInterface.TWorkCropCreate();
        Assert.Equal(cleared, inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop);
        Assert.False(inspector.LInspectorToolArmed);
    }
}
