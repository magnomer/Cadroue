using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TInspectorCropEdge
{
    [Fact]
    public void EdgeCommit_Text_WritesClampedEdges()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);

        TInterface.TInspectorEdgeCommit(inspector, 0, "10");
        TInterface.TInspectorEdgeCommit(inspector, 1, "-5");
        TInterface.TInspectorEdgeCommit(inspector, 2, "abc");
        TInterface.TInspectorEdgeCommit(inspector, 3, "40");

        LWorkCrop crop = inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop;
        Assert.Equal(
            (10, 0, 0, 40), (crop.LWorkCropLeft, crop.LWorkCropTop, crop.LWorkCropRight, crop.LWorkCropBottom));
        Assert.Equal("1910 × 1040", TInterface.TInspectorResolutionRead(inspector));
    }

    [Fact]
    public void EdgeFormat_EchoesEquivalentText()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);
        TInterface.TInspectorEdgeCommit(inspector, 0, "12");

        Assert.Equal("12", TInterface.TInspectorEdgeFormat(inspector, 0, "12"));
        Assert.Equal("12", TInterface.TInspectorEdgeFormat(inspector, 0, "7"));
        Assert.Equal(string.Empty, TInterface.TInspectorEdgeFormat(inspector, 1, string.Empty));
        Assert.Equal("0", TInterface.TInspectorEdgeFormat(inspector, 1, "3"));
    }

    [Fact]
    public void EdgeCommit_FixedRatio_FollowsRatioAcrossAxis()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);
        TInterface.TInspectorPresetSelect(inspector, 5);

        TInterface.TInspectorEdgeCommit(inspector, 0, "500");

        LWorkCrop crop = inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop;
        double width = 1920 - crop.LWorkCropLeft - crop.LWorkCropRight;
        double height = 1080 - crop.LWorkCropTop - crop.LWorkCropBottom;
        Assert.Equal(width, height);
        Assert.True(inspector.LInspectorCrop.LInspectorRatioFixed);
    }

    [Fact]
    public void EdgeReset_ClearsEdges_KeepsRatioOff()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);
        TInterface.TInspectorEdgeCommit(inspector, 2, "300");

        TInterface.TInspectorEdgeReset(inspector);

        Assert.Equal(TInterface.TWorkCropCreate(), inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop);
        Assert.False(inspector.LInspectorCrop.LInspectorRatioFixed);
    }

    [Fact]
    public void CropSet_DrawnRect_BecomesEdges_NullClears()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);

        TInterface.TInspectorCropSet(inspector, TInterface.TCropboxCreate(100, 50, 800, 600), -1, -1, -1);

        LWorkCrop crop = inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop;
        Assert.Equal(
            (100, 50, 1020, 430), (crop.LWorkCropLeft, crop.LWorkCropTop, crop.LWorkCropRight, crop.LWorkCropBottom));

        TInterface.TInspectorCropSet(inspector, null, -1, -1, -1);

        Assert.Equal(TInterface.TWorkCropCreate(), inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop);
    }

    [Fact]
    public void CropSet_FixedRatio_SnapsDrawnRect()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);
        TInterface.TInspectorPresetSelect(inspector, 5);

        TInterface.TInspectorCropSet(inspector, TInterface.TCropboxCreate(0, 0, 800, 400), -1, -1, -1);

        LCropbox? rect = TInterface.TInspectorRectRead(inspector);
        Assert.NotNull(rect);
        Assert.Equal(rect.LCropboxWidth, rect.LCropboxHeight);
    }

    [Fact]
    public void CropReset_PersistentSkips_OtherwiseClears()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);
        TInterface.TInspectorEdgeCommit(inspector, 0, "20");
        TInterface.TInspectorStepSet(inspector, "Crop");
        TInterface.TInspectorApplySet(inspector, true);
        TInterface.TInspectorToolSet(inspector, true);
        TInterface.TCropboxPersistentSet(inspector.LInspectorCrop.LInspectorCropbox, true);

        TInterface.TInspectorCropReset(inspector);
        Assert.Equal(20, inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop.LWorkCropLeft);
        Assert.True(inspector.LInspectorToolArmed);

        TInterface.TCropboxPersistentSet(inspector.LInspectorCrop.LInspectorCropbox, false);
        TInterface.TInspectorCropReset(inspector);

        Assert.Equal(TInterface.TWorkCropCreate(), inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop);
        Assert.False(inspector.LInspectorToolArmed);
    }
}
