using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Logging")]
public sealed class TViewerEngine
{
    private static (QViewer TViewer, QViewerRenderer TRenderer) TViewerBuild()
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        TInterfaceViewer.TViewerCommandSet(viewer, true);
        return (viewer, TInterfaceViewer.TViewerRendererRead(viewer));
    }

    [Fact]
    public void EngineRead_MarksTheActiveChoice_AndMpvAvailability()
    {
        _ = TViewerBuild();

        QViewerEnginePlan plan = TInterfaceViewer.TViewerEngineRead();

        Assert.Equal(LRenderer.LRendererFlyleafToken, plan.QViewerChoiceFlyleaf.QViewerChoiceKey);
        Assert.Equal(LRenderer.LRendererMpvToken, plan.QViewerChoiceMpv.QViewerChoiceKey);
        Assert.True(plan.QViewerChoiceFlyleaf.QViewerChoiceEnabled);
        Assert.NotEqual(plan.QViewerChoiceFlyleaf.QViewerChoiceActive, plan.QViewerChoiceMpv.QViewerChoiceActive);
        Assert.NotEmpty(plan.QViewerChoiceFlyleaf.QViewerChoiceText);
        Assert.NotEmpty(plan.QViewerChoiceMpv.QViewerChoiceTip);
    }

    [Fact]
    public void EngineSelect_SameEngine_LeavesPreferenceAlone()
    {
        _ = TViewerBuild();
        string before = TInterfaceViewer.TPreferenceEngineRead();
        string active = TInterfaceViewer.TViewerEngineRead().QViewerChoiceFlyleaf.QViewerChoiceActive
            ? LRenderer.LRendererFlyleafToken
            : LRenderer.LRendererMpvToken;

        TInterfaceViewer.TViewerEngineSelect(active);

        Assert.Equal(before, TInterfaceViewer.TPreferenceEngineRead());
    }

    [Fact]
    public void EngineUpdate_NoChange_ReportsFalse_AndKeepsHost()
    {
        (QViewer viewer, QViewerRenderer renderer) = TViewerBuild();
        int hostApplies = 0;
        TInterfaceViewer.TViewerHostAttach(renderer, () => hostApplies++);
        bool mpv = viewer.QViewerTrace.CViewerMpvActive;

        Assert.False(TInterfaceViewer.TViewerEngineUpdate(renderer));
        Assert.Equal(mpv, viewer.QViewerTrace.CViewerMpvActive);
        Assert.Equal(0, hostApplies);
    }

    [Fact]
    public void OverlayHandle_ShownOnlyForVisibleSizedMpvHost()
    {
        (QViewer viewer, QViewerRenderer renderer) = TViewerBuild();

        Assert.False(TInterfaceViewer.TViewerOverlayHandle(renderer, true, 320, 240));

        TInterfaceViewer.TViewerMpvSet(viewer, true);
        TInterfaceViewer.TViewerHostSet(viewer, true);

        Assert.True(viewer.QViewerMpvShown);
        Assert.False(viewer.QViewerFlyleafShown);
        Assert.True(TInterfaceViewer.TViewerOverlayHandle(renderer, true, 320, 240));
        Assert.False(TInterfaceViewer.TViewerOverlayHandle(renderer, false, 320, 240));
        Assert.False(TInterfaceViewer.TViewerOverlayHandle(renderer, true, 0, 240));

        TInterfaceViewer.TViewerHostSet(viewer, false);

        Assert.False(TInterfaceViewer.TViewerOverlayHandle(renderer, true, 320, 240));
    }

    [Fact]
    public void SwitchRead_FollowsBypass_AndEngine()
    {
        (QViewer viewer, _) = TViewerBuild();
        QViewerSwitch filtered = TInterfaceViewer.TViewerSwitchRead(viewer);
        TInterfaceViewer.TViewerBypassSet(viewer, true);
        QViewerSwitch original = TInterfaceViewer.TViewerSwitchRead(viewer);

        Assert.NotEqual(filtered.QViewerSwitchText, original.QViewerSwitchText);
        Assert.False(filtered.QViewerSwitchEnabled);

        TInterfaceViewer.TViewerEngineSet(viewer, Cadroue.Core.LPreviewEngine.LPreviewEngineMpv);

        Assert.True(TInterfaceViewer.TViewerSwitchRead(viewer).QViewerSwitchEnabled);
        Assert.True(viewer.QViewerAudioCapable);
    }

    [Fact]
    public void SurfaceMatch_ZeroForegroundNeverMatches()
    {
        (QViewer viewer, _) = TViewerBuild();

        Assert.False(TInterfaceViewer.TViewerSurfaceMatch(viewer, 0, 0, 0));
        Assert.True(TInterfaceViewer.TViewerSurfaceMatch(viewer, 7, 7, 0));
        Assert.True(TInterfaceViewer.TViewerSurfaceMatch(viewer, 9, 7, 9));
        Assert.False(TInterfaceViewer.TViewerSurfaceMatch(viewer, 5, 7, 9));
    }
}
