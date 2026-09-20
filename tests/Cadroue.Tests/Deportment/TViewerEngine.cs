using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class TViewerEngine
{
    private static (LViewer TViewer, LViewerRenderer TRenderer) TViewerBuild()
    {
        LViewer viewer = TInterface.TViewerCreate();
        TInterface.TViewerCommandSet(viewer, true);
        return (viewer, TInterface.TViewerRendererRead(viewer));
    }

    [Fact]
    public void EngineRead_MarksTheActiveChoice_AndMpvAvailability()
    {
        (_, LViewerRenderer renderer) = TViewerBuild();

        LViewerEnginePlan plan = TInterface.TViewerEngineRead(renderer);

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
        string before = TInterface.TPreferenceEngineRead();
        string active = TInterface.TViewerEngineRead(renderer).LViewerChoiceFlyleaf.LViewerChoiceActive
            ? LRenderer.LRendererFlyleafToken
            : LRenderer.LRendererMpvToken;

        TInterface.TViewerEngineSelect(renderer, active);

        Assert.Equal(before, TInterface.TPreferenceEngineRead());
    }

    [Fact]
    public void EngineUpdate_NoChange_ReportsFalse_AndKeepsHost()
    {
        (LViewer viewer, LViewerRenderer renderer) = TViewerBuild();
        int hostApplies = 0;
        TInterface.TViewerHostAttach(renderer, () => hostApplies++);
        bool mpv = viewer.LViewerMpvActive;

        Assert.False(TInterface.TViewerEngineUpdate(renderer));
        Assert.Equal(mpv, viewer.LViewerMpvActive);
        Assert.Equal(0, hostApplies);
    }

    [Fact]
    public void OverlayHandle_ShownOnlyForVisibleSizedMpvHost()
    {
        (LViewer viewer, LViewerRenderer renderer) = TViewerBuild();

        Assert.False(TInterface.TViewerOverlayHandle(renderer, true, 320, 240));

        TInterface.TViewerMpvSet(viewer, true);
        TInterface.TViewerHostSet(viewer, true);

        Assert.True(viewer.LViewerMpvShown);
        Assert.False(viewer.LViewerFlyleafShown);
        Assert.True(TInterface.TViewerOverlayHandle(renderer, true, 320, 240));
        Assert.False(TInterface.TViewerOverlayHandle(renderer, false, 320, 240));
        Assert.False(TInterface.TViewerOverlayHandle(renderer, true, 0, 240));

        TInterface.TViewerHostSet(viewer, false);

        Assert.False(TInterface.TViewerOverlayHandle(renderer, true, 320, 240));
    }

    [Fact]
    public void SwitchRead_FollowsBypass_AndEngine()
    {
        (LViewer viewer, _) = TViewerBuild();
        LViewerSwitch filtered = TInterface.TViewerSwitchRead(viewer);
        TInterface.TViewerBypassSet(viewer, true);
        LViewerSwitch original = TInterface.TViewerSwitchRead(viewer);

        Assert.NotEqual(filtered.LViewerSwitchText, original.LViewerSwitchText);
        Assert.False(filtered.LViewerSwitchEnabled);

        TInterface.TViewerEngineSet(viewer, Cadroue.Core.LPreviewEngine.LPreviewEngineMpv);

        Assert.True(TInterface.TViewerSwitchRead(viewer).LViewerSwitchEnabled);
        Assert.True(viewer.LViewerAudioCapable);
    }

    [Fact]
    public void SurfaceMatch_ZeroForegroundNeverMatches()
    {
        (LViewer viewer, _) = TViewerBuild();

        Assert.False(TInterface.TViewerSurfaceMatch(viewer, 0, 0, 0));
        Assert.True(TInterface.TViewerSurfaceMatch(viewer, 7, 7, 0));
        Assert.True(TInterface.TViewerSurfaceMatch(viewer, 9, 7, 9));
        Assert.False(TInterface.TViewerSurfaceMatch(viewer, 5, 7, 9));
    }
}
