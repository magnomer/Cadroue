using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TInspectorCropPreset
{
    [Theory]
    [InlineData(1, "16", "9")]
    [InlineData(2, "9", "16")]
    [InlineData(3, "4", "3")]
    [InlineData(4, "3", "4")]
    [InlineData(5, "1", "1")]
    [InlineData(6, "21", "9")]
    public void PresetSelect_Index_SetsRatioAndReadsBack(int index, string width, string height)
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);

        TInterface.TInspectorPresetSelect(inspector, index);

        Assert.True(inspector.LInspectorCrop.LInspectorRatioFixed);
        Assert.Equal(index, inspector.LInspectorCrop.LInspectorPresetIndex);
        Assert.False(inspector.LInspectorCrop.LInspectorCustomShown);
        Assert.Equal(width, TInterface.TInspectorRatioFormat(inspector, true, string.Empty));
        Assert.Equal(height, TInterface.TInspectorRatioFormat(inspector, false, string.Empty));
    }

    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(1080, 1920)]
    [InlineData(1440, 1080)]
    [InlineData(720, 1280)]
    public void PresetSelect_FitsInsideSource(double sourceWidth, double sourceHeight)
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, sourceWidth, sourceHeight);

        TInterface.TInspectorPresetSelect(inspector, 5);

        LWorkCrop crop = TInterface.TInspectorCropRead(inspector);
        double width = sourceWidth - crop.LWorkCropLeft - crop.LWorkCropRight;
        double height = sourceHeight - crop.LWorkCropTop - crop.LWorkCropBottom;
        Assert.True(width > 0 && height > 0);
        Assert.True(width <= sourceWidth && height <= sourceHeight);
        Assert.Equal(width, height);
        Assert.False(inspector.LInspectorCrop.LInspectorNoticeShown);
    }

    [Fact]
    public void PresetSelect_NoSource_LeavesRatioFree()
    {
        LInspector inspector = TInterface.TInspectorCreate();

        TInterface.TInspectorPresetSelect(inspector, 1);

        Assert.False(inspector.LInspectorCrop.LInspectorRatioFixed);
        Assert.Equal(0, inspector.LInspectorCrop.LInspectorPresetIndex);
        Assert.True(inspector.LInspectorCrop.LInspectorCustomShown);
    }

    [Fact]
    public void PresetSelect_Custom_AfterPreset_ResetsRatio()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);
        TInterface.TInspectorPresetSelect(inspector, 1);

        TInterface.TInspectorPresetSelect(inspector, 0);

        Assert.False(inspector.LInspectorCrop.LInspectorRatioFixed);
        Assert.Equal("0", TInterface.TInspectorRatioFormat(inspector, true, "16"));
        Assert.Equal(string.Empty, TInterface.TInspectorRatioFormat(inspector, false, string.Empty));
    }

    [Fact]
    public void RatioCommit_CustomText_KeepsIndexZero_FixedShowsMismatch()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);

        TInterface.TInspectorRatioCommit(inspector, "5", "4");
        TInterface.TInspectorFixedSet(inspector, true);

        Assert.Equal(0, inspector.LInspectorCrop.LInspectorPresetIndex);
        Assert.True(inspector.LInspectorCrop.LInspectorCustomShown);
        Assert.True(inspector.LInspectorCrop.LInspectorNoticeShown);
        Assert.NotEqual(string.Empty, TInterface.TInspectorNoticeRead(inspector));
    }

    [Fact]
    public void LenientSet_WithinTolerance_HidesNotice_LenientNeedsFixed()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);
        TInterface.TInspectorRatioCommit(inspector, "16", "9");

        TInterface.TInspectorLenientSet(inspector, true);
        Assert.False(inspector.LInspectorCrop.LInspectorRatioLenient);

        TInterface.TInspectorFixedSet(inspector, true);
        TInterface.TInspectorLenientSet(inspector, true);
        TInterface.TInspectorEdgeCommit(inspector, 0, "2");

        Assert.True(inspector.LInspectorCrop.LInspectorRatioLenient);
        Assert.False(inspector.LInspectorCrop.LInspectorNoticeShown);
    }

    [Fact]
    public void RatioFormat_FreeRatio_ShowsNormalizedCropSize()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);
        Assert.Equal(string.Empty, TInterface.TInspectorRatioFormat(inspector, true, string.Empty));

        TInterface.TInspectorEdgeCommit(inspector, 0, "320");

        Assert.Equal("40", TInterface.TInspectorRatioFormat(inspector, true, string.Empty));
        Assert.Equal("27", TInterface.TInspectorRatioFormat(inspector, false, string.Empty));
        Assert.Equal("40", TInterface.TInspectorRatioFormat(inspector, true, "40"));
        Assert.Equal("1600 × 1080", TInterface.TInspectorResolutionRead(inspector));
    }

    [Fact]
    public void ApplySet_Off_ResetsRatio_OnKeepsIt()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);
        TInterface.TInspectorApplySet(inspector, true);
        TInterface.TInspectorPresetSelect(inspector, 1);

        TInterface.TInspectorApplySet(inspector, true);
        Assert.True(inspector.LInspectorCrop.LInspectorRatioFixed);

        TInterface.TInspectorApplySet(inspector, false);

        Assert.False(inspector.LInspectorCrop.LInspectorActive);
        Assert.False(inspector.LInspectorCrop.LInspectorRatioFixed);
    }
}
