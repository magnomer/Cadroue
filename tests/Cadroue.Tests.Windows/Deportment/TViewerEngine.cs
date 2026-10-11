using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Logging")]
public sealed class TViewerEngine
{
    private static (LViewer TViewer, LViewerRenderer TRenderer) TViewerBuild()
    {
        LViewer viewer = TInterfaceViewer.TViewerCreate();
        TInterfaceViewer.TViewerCommandSet(viewer, true);
        return (viewer, TInterfaceViewer.TViewerRendererRead(viewer));
    }

    [Fact]
    public void EngineRead_MarksTheActiveChoice_AndMpvAvailability()
    {
        (_, LViewerRenderer renderer) = TViewerBuild();

        LViewerEnginePlan plan = TInterfaceViewer.TViewerEngineRead(renderer);

        Assert.Equal(LRenderer.LRendererFlyleafToken, plan.LViewerChoiceFlyleaf.LViewerChoiceKey);
        Assert.Equal(LRenderer.LRendererMpvToken, plan.LViewerChoiceMpv.LViewerChoiceKey);
        Assert.True(plan.LViewerChoiceFlyleaf.LViewerChoiceEnabled);
        Assert.NotEqual(plan.LViewerChoiceFlyleaf.LViewerChoiceActive, plan.LViewerChoiceMpv.LViewerChoiceActive);
        Assert.NotEmpty(plan.LViewerChoiceFlyleaf.LViewerChoiceText);
        Assert.NotEmpty(plan.LViewerChoiceMpv.LViewerChoiceTip);
    }

    [Fact]
    public void EngineSelect_SameEngine_LeavesPreferenceAlone()
    {
        (_, LViewerRenderer renderer) = TViewerBuild();
        string before = TInterfaceViewer.TPreferenceEngineRead();
        string active = TInterfaceViewer.TViewerEngineRead(renderer).LViewerChoiceFlyleaf.LViewerChoiceActive
            ? LRenderer.LRendererFlyleafToken
            : LRenderer.LRendererMpvToken;

        TInterfaceViewer.TViewerEngineSelect(renderer, active);

        Assert.Equal(before, TInterfaceViewer.TPreferenceEngineRead());
    }

    [Fact]
    public void EngineUpdate_NoChange_ReportsFalse_AndKeepsHost()
    {
        (LViewer viewer, LViewerRenderer renderer) = TViewerBuild();
        int hostApplies = 0;
        TInterfaceViewer.TViewerHostAttach(renderer, () => hostApplies++);
        bool mpv = viewer.LViewerMpvActive;

        Assert.False(TInterfaceViewer.TViewerEngineUpdate(renderer));
        Assert.Equal(mpv, viewer.LViewerMpvActive);
        Assert.Equal(0, hostApplies);
    }

    [Fact]
    public void OverlayHandle_ShownOnlyForVisibleSizedMpvHost()
    {
        (LViewer viewer, LViewerRenderer renderer) = TViewerBuild();

        Assert.False(TInterfaceViewer.TViewerOverlayHandle(renderer, true, 320, 240));

        TInterfaceViewer.TViewerMpvSet(viewer, true);
        TInterfaceViewer.TViewerHostSet(viewer, true);

        Assert.True(viewer.LViewerMpvShown);
        Assert.False(viewer.LViewerFlyleafShown);
        Assert.True(TInterfaceViewer.TViewerOverlayHandle(renderer, true, 320, 240));
        Assert.False(TInterfaceViewer.TViewerOverlayHandle(renderer, false, 320, 240));
        Assert.False(TInterfaceViewer.TViewerOverlayHandle(renderer, true, 0, 240));

        TInterfaceViewer.TViewerHostSet(viewer, false);

        Assert.False(TInterfaceViewer.TViewerOverlayHandle(renderer, true, 320, 240));
    }

    [Fact]
    public void SwitchRead_FollowsBypass_AndEngine()
    {
        (LViewer viewer, _) = TViewerBuild();
        LViewerSwitch filtered = TInterfaceViewer.TViewerSwitchRead(viewer);
        TInterfaceViewer.TViewerBypassSet(viewer, true);
        LViewerSwitch original = TInterfaceViewer.TViewerSwitchRead(viewer);

        Assert.NotEqual(filtered.LViewerSwitchText, original.LViewerSwitchText);
        Assert.False(filtered.LViewerSwitchEnabled);

        TInterfaceViewer.TViewerEngineSet(viewer, Cadroue.Core.LPreviewEngine.LPreviewEngineMpv);

        Assert.True(TInterfaceViewer.TViewerSwitchRead(viewer).LViewerSwitchEnabled);
        Assert.True(viewer.LViewerAudioCapable);
    }

    [Fact]
    public void SurfaceMatch_ZeroForegroundNeverMatches()
    {
        (LViewer viewer, _) = TViewerBuild();

        Assert.False(TInterfaceViewer.TViewerSurfaceMatch(viewer, 0, 0, 0));
        Assert.True(TInterfaceViewer.TViewerSurfaceMatch(viewer, 7, 7, 0));
        Assert.True(TInterfaceViewer.TViewerSurfaceMatch(viewer, 9, 7, 9));
        Assert.False(TInterfaceViewer.TViewerSurfaceMatch(viewer, 5, 7, 9));
    }
}
