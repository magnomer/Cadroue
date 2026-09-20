using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TCompassButtons
{
    private static (LCompass TCompass, LFlow TFlow, LViewer TViewer) TCompassBuild(bool sections)
    {
        LFlow flow = TInterface.TFlowCreate();
        LViewer viewer = TInterface.TViewerCreate();
        return (TInterface.TCompassCreate(flow, viewer, sections), flow, viewer);
    }

    [Fact]
    public void GroupsRead_HidesSectionGroups_UnlessShown()
    {
        (LCompass plain, _, _) = TCompassBuild(false);
        (LCompass split, _, _) = TCompassBuild(true);

        Assert.Equal(3, TInterface.TCompassGroupsRead(plain).Count);
        Assert.Empty(TInterface.TCompassSectionRead(plain));
        Assert.Equal(5, TInterface.TCompassGroupsRead(split).Count);
        Assert.Equal([2, 3], TInterface.TCompassSectionRead(split));
        Assert.Equal(
            ["ZoomIn", "ZoomOut", "Play", "SectionAdd", "SectionDelete", "SectionStart", "SectionSplit", "SectionEnd",
                "KeyframePrevious", "KeyframeNearest", "KeyframeNext"],
            TInterface.TCompassGroupsRead(split)
                .SelectMany(g => g.LCompassGroupButtons)
                .Select(b => b.LCompassButtonKey));
    }

    [Fact]
    public void PlayRead_SwapsFaceByPlaying()
    {
        LCompassButton play = TInterface.TCompassPlayRead(false);
        LCompassButton pause = TInterface.TCompassPlayRead(true);

        Assert.Equal(LCompass.LCompassPlayKey, play.LCompassButtonKey);
        Assert.Equal(LCompass.LCompassPlayKey, pause.LCompassButtonKey);
        Assert.Equal("PCompassPlay.svg", play.LCompassButtonIcon);
        Assert.Equal("PCompassPause.svg", pause.LCompassButtonIcon);
        Assert.Equal(LCompass.LCompassAccentPositive, play.LCompassButtonAccent);
        Assert.Equal(LCompass.LCompassAccentNone, pause.LCompassButtonAccent);
    }

    [Fact]
    public void Run_PlayKey_TogglesByViewer_OtherKeysReachFlow()
    {
        (LCompass compass, LFlow flow, LViewer viewer) = TCompassBuild(false);
        int plays = 0;
        int pauses = 0;
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowPlayAttach(flow, () => plays++, () => pauses++, _ => { }, _ => { });

        TInterface.TCompassRun(compass, LCompass.LCompassPlayKey);
        Assert.Equal((1, 0), (plays, pauses));

        TInterface.TViewerPlaybackUpdate(viewer, true, null);
        TInterface.TCompassRun(compass, LCompass.LCompassPlayKey);
        Assert.Equal((1, 1), (plays, pauses));

        bool waveform = compass.LCompassWaveformActive;
        TInterface.TCompassWaveformToggle(compass);
        Assert.Equal(!waveform, compass.LCompassWaveformActive);
    }

    [Fact]
    public void VolumeSet_ClampsAndSkipsEqual()
    {
        (LCompass compass, _, LViewer viewer) = TCompassBuild(false);
        TInterface.TViewerCommandSet(viewer, true);
        List<double> applied = [];
        TInterface.TCompassVolumeAttach(viewer, applied.Add);
        double current = compass.LCompassVolume;

        TInterface.TCompassVolumeSet(compass, current);
        Assert.Empty(applied);

        TInterface.TCompassVolumeSet(compass, 500);
        TInterface.TCompassVolumeSet(compass, -5);
        Assert.Equal(new double[] { 100, 0 }.Where(v => v != current), applied);
    }

    [Fact]
    public void VolumeFormat_FillAndSeparators_ArePure()
    {
        Assert.Equal("43", TInterface.TCompassVolumeFormat(42.6));
        Assert.Equal("100", TInterface.TCompassVolumeFormat(140));
        Assert.Equal(0, TInterface.TCompassFillResolve(0, 50));
        Assert.Equal(66, TInterface.TCompassFillResolve(132, 50));
        Assert.Equal([0, 1, 1, 0, 1], TInterface.TCompassSeparatorsResolve(0, 0, 0, 40, 40));
    }
}
