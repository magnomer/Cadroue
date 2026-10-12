using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TCompassButtons
{
    private static (QCompass TCompass, QFlow TFlow, QViewer TViewer) TCompassBuild(bool sections)
    {
        QFlow flow = TInterface.TFlowCreate();
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        return (TInterface.TCompassCreate(flow, viewer, sections), flow, viewer);
    }

    [Fact]
    public void GroupsRead_HidesSectionGroups_UnlessShown()
    {
        (QCompass plain, _, _) = TCompassBuild(false);
        (QCompass split, _, _) = TCompassBuild(true);

        Assert.Equal(3, TInterface.TCompassGroupsRead(plain).Count);
        Assert.Empty(TInterface.TCompassSectionRead(plain));
        Assert.Equal(5, TInterface.TCompassGroupsRead(split).Count);
        Assert.Equal([2, 3], TInterface.TCompassSectionRead(split));
        Assert.Equal(
            ["ZoomIn", "ZoomOut", "Play", "SectionAdd", "SectionDelete", "SectionStart", "SectionSplit", "SectionEnd",
                "KeyframePrevious", "KeyframeNearest", "KeyframeNext"],
            TInterface.TCompassGroupsRead(split)
                .SelectMany(g => g.QCompassGroupButtons)
                .Select(b => b.QCompassButtonKey));
    }

    [Fact]
    public void PlayRead_SwapsFaceByPlaying()
    {
        QCompassButton play = TInterface.TCompassPlayRead(false);
        QCompassButton pause = TInterface.TCompassPlayRead(true);

        Assert.Equal(QCompass.QCompassPlayKey, play.QCompassButtonKey);
        Assert.Equal(QCompass.QCompassPlayKey, pause.QCompassButtonKey);
        Assert.Equal("PCompassPlay.svg", play.QCompassButtonIcon);
        Assert.Equal("PCompassPause.svg", pause.QCompassButtonIcon);
        Assert.Equal(QCompass.QCompassAccentPositive, play.QCompassButtonAccent);
        Assert.Equal(QCompass.QCompassAccentNone, pause.QCompassButtonAccent);
    }

    [Fact]
    public void Run_PlayKey_TogglesByViewer_OtherKeysReachFlow()
    {
        (QCompass compass, QFlow flow, QViewer viewer) = TCompassBuild(false);
        int plays = 0;
        int pauses = 0;
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowPlayAttach(flow, () => plays++, () => pauses++, _ => { }, _ => { });

        TInterface.TCompassRun(compass, QCompass.QCompassPlayKey);
        Assert.Equal((1, 0), (plays, pauses));

        TInterfaceViewer.TViewerPlaybackUpdate(viewer, true, null);
        TInterface.TCompassRun(compass, QCompass.QCompassPlayKey);
        Assert.Equal((1, 1), (plays, pauses));

        bool waveform = compass.QCompassWaveformActive;
        TInterface.TCompassWaveformToggle(compass);
        Assert.Equal(!waveform, compass.QCompassWaveformActive);
    }

    [Fact]
    public void VolumeSet_ClampsAndSkipsEqual()
    {
        (QCompass compass, _, QViewer viewer) = TCompassBuild(false);
        TInterfaceViewer.TViewerCommandSet(viewer, true);
        List<double> applied = [];
        TInterface.TCompassVolumeAttach(viewer, applied.Add);
        double current = compass.QCompassVolume;

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
