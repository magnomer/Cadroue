using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

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
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);

        TInterfaceInspector.TInspectorPresetSelect(inspector, index);

        Assert.True(inspector.QInspectorCrop.QInspectorRatioFixed);
        Assert.Equal(index, inspector.QInspectorCrop.QInspectorPresetIndex);
        Assert.False(inspector.QInspectorCrop.QInspectorCustomShown);
        Assert.Equal(width, TInterfaceInspector.TInspectorRatioFormat(inspector, true, string.Empty));
        Assert.Equal(height, TInterfaceInspector.TInspectorRatioFormat(inspector, false, string.Empty));
    }

    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(1080, 1920)]
    [InlineData(1440, 1080)]
    [InlineData(720, 1280)]
    public void PresetSelect_FitsInsideSource(double sourceWidth, double sourceHeight)
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, sourceWidth, sourceHeight);

        TInterfaceInspector.TInspectorPresetSelect(inspector, 5);

        LWorkCrop crop = TInterfaceInspector.TInspectorCropRead(inspector);
        double width = sourceWidth - crop.LWorkCropLeft - crop.LWorkCropRight;
        double height = sourceHeight - crop.LWorkCropTop - crop.LWorkCropBottom;
        Assert.True(width > 0 && height > 0);
        Assert.True(width <= sourceWidth && height <= sourceHeight);
        Assert.Equal(width, height);
        Assert.False(inspector.QInspectorCrop.QInspectorNoticeShown);
    }

    [Fact]
    public void PresetSelect_NoSource_LeavesRatioFree()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();

        TInterfaceInspector.TInspectorPresetSelect(inspector, 1);

        Assert.False(inspector.QInspectorCrop.QInspectorRatioFixed);
        Assert.Equal(0, inspector.QInspectorCrop.QInspectorPresetIndex);
        Assert.True(inspector.QInspectorCrop.QInspectorCustomShown);
    }

    [Fact]
    public void PresetSelect_Custom_AfterPreset_ResetsRatio()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        TInterfaceInspector.TInspectorPresetSelect(inspector, 1);

        TInterfaceInspector.TInspectorPresetSelect(inspector, 0);

        Assert.False(inspector.QInspectorCrop.QInspectorRatioFixed);
        Assert.Equal("0", TInterfaceInspector.TInspectorRatioFormat(inspector, true, "16"));
        Assert.Equal(string.Empty, TInterfaceInspector.TInspectorRatioFormat(inspector, false, string.Empty));
    }

    [Fact]
    public void RatioCommit_EchoAfterPreset_KeepsPreset()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        TInterfaceInspector.TInspectorLenientSet(inspector, true);
        TInterfaceInspector.TInspectorPresetSelect(inspector, 1);
        TInterfaceInspector.TInspectorFixedSet(inspector, true);
        TInterfaceInspector.TInspectorLenientSet(inspector, true);
        LWorkCrop before = TInterfaceInspector.TInspectorCropRead(inspector);

        TInterfaceInspector.TInspectorRatioCommit(inspector, "16", "0", false);
        TInterfaceInspector.TInspectorEdgeCommit(inspector, 0, "7", false);

        Assert.Equal(1, inspector.QInspectorCrop.QInspectorPresetIndex);
        Assert.True(inspector.QInspectorCrop.QInspectorRatioLenient);
        Assert.Equal("9", TInterfaceInspector.TInspectorRatioFormat(inspector, false, "0"));
        Assert.Equal(before, TInterfaceInspector.TInspectorCropRead(inspector));
    }

    [Fact]
    public void RatioCommit_CustomText_KeepsIndexZero_FixedShowsMismatch()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);

        TInterfaceInspector.TInspectorRatioCommit(inspector, "5", "4");
        TInterfaceInspector.TInspectorFixedSet(inspector, true);

        Assert.Equal(0, inspector.QInspectorCrop.QInspectorPresetIndex);
        Assert.True(inspector.QInspectorCrop.QInspectorCustomShown);
        Assert.True(inspector.QInspectorCrop.QInspectorNoticeShown);
        Assert.NotEqual(string.Empty, TInterfaceInspector.TInspectorNoticeRead(inspector));
    }

    [Fact]
    public void LenientSet_WithinTolerance_HidesNotice_LenientNeedsFixed()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        TInterfaceInspector.TInspectorRatioCommit(inspector, "16", "9");

        TInterfaceInspector.TInspectorLenientSet(inspector, true);
        Assert.False(inspector.QInspectorCrop.QInspectorRatioLenient);

        TInterfaceInspector.TInspectorFixedSet(inspector, true);
        TInterfaceInspector.TInspectorLenientSet(inspector, true);
        TInterfaceInspector.TInspectorEdgeCommit(inspector, 0, "2");

        Assert.True(inspector.QInspectorCrop.QInspectorRatioLenient);
        Assert.False(inspector.QInspectorCrop.QInspectorNoticeShown);
    }

    [Fact]
    public void RatioFormat_FreeRatio_ShowsNormalizedCropSize()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        Assert.Equal(string.Empty, TInterfaceInspector.TInspectorRatioFormat(inspector, true, string.Empty));

        TInterfaceInspector.TInspectorEdgeCommit(inspector, 0, "320");

        Assert.Equal("40", TInterfaceInspector.TInspectorRatioFormat(inspector, true, string.Empty));
        Assert.Equal("27", TInterfaceInspector.TInspectorRatioFormat(inspector, false, string.Empty));
        Assert.Equal("40", TInterfaceInspector.TInspectorRatioFormat(inspector, true, "40"));
        Assert.Equal("1600 × 1080", TInterfaceInspector.TInspectorResolutionRead(inspector));
    }

    [Fact]
    public void ApplySet_Off_ResetsRatio_OnKeepsIt()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        TInterfaceInspector.TInspectorApplySet(inspector, true);
        TInterfaceInspector.TInspectorPresetSelect(inspector, 1);

        TInterfaceInspector.TInspectorApplySet(inspector, true);
        Assert.True(inspector.QInspectorCrop.QInspectorRatioFixed);

        TInterfaceInspector.TInspectorApplySet(inspector, false);

        Assert.False(inspector.QInspectorCrop.QInspectorActive);
        Assert.False(inspector.QInspectorCrop.QInspectorRatioFixed);
    }
}
