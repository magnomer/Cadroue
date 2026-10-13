using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Logging")]
public sealed class TViewerPlayback
{
    private static (QViewer TViewer, QViewerPlayback TPlayback, List<string> TViewerCalls, List<string> TViewerClock)
        TPlaybackBuild(
        bool ready = true,
        bool ended = false,
        TimeSpan? time = null)
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        TInterfaceViewer.TViewerCommandSet(viewer, true);
        List<string> calls = [];
        List<string> clock = [];
        QViewerPlayback playback = TInterfaceViewer.TViewerPlaybackRead(viewer);
        TInterfaceViewer.TViewerClockAttach(playback, () => clock.Add("start"), () => clock.Add("stop"));
        if (ready)
        {
            TInterfacePlayer.TPlayerEngineSet(viewer.QPlayer, TInterfacePlayer.TPlayerSeamCreate(
                seek: position => calls.Add($"seek {position.TotalSeconds}"),
                time: () => time ?? TimeSpan.FromSeconds(4),
                ended: () => ended));
        }

        return (viewer, playback, calls, clock);
    }

    [Fact]
    public void Play_StartsClock_AndMarksPlaying()
    {
        (QViewer viewer, QViewerPlayback playback, _, List<string> clock) = TPlaybackBuild();

        TInterfaceViewer.TViewerPlay(playback);

        Assert.True(viewer.QViewerPlaying);
        Assert.Equal(TimeSpan.FromSeconds(4), viewer.QViewerPosition);
        Assert.Equal(["start"], clock);

        TInterfaceViewer.TViewerPause(playback);

        Assert.False(viewer.QViewerPlaying);
        Assert.Equal(["start", "stop"], clock);
    }

    [Fact]
    public void Play_AfterEnd_SeeksToStart()
    {
        (QViewer viewer, QViewerPlayback playback, List<string> calls, _) = TPlaybackBuild();
        TInterfaceViewer.TViewerEndSet(viewer, true);

        TInterfaceViewer.TViewerPlay(playback);

        Assert.Equal(["seek 0"], calls);
        Assert.False(viewer.QViewerEndReached);
    }

    [Fact]
    public void Play_WithPendingIntent_OnlyRewritesIntent()
    {
        (QViewer viewer, QViewerPlayback playback, List<string> calls, List<string> clock) = TPlaybackBuild();
        TInterfaceViewer.TViewerIntentSet(viewer, "clip.mp4", TimeSpan.FromSeconds(2), null);

        TInterfaceViewer.TViewerPlay(playback);
        Assert.True(viewer.QViewerWorkspace.CWorkspaceIntent!.CWorkspaceIntentPlaying);
        TInterfaceViewer.TViewerPause(playback);
        Assert.False(viewer.QViewerWorkspace.CWorkspaceIntent!.CWorkspaceIntentPlaying);
        TInterfaceViewer.TViewerSeek(playback, TimeSpan.FromSeconds(9));
        Assert.Equal(TimeSpan.FromSeconds(9), viewer.QViewerWorkspace.CWorkspaceIntent!.CWorkspaceIntentPosition);
        Assert.Equal(TimeSpan.FromSeconds(9), viewer.QViewerPosition);

        Assert.Empty(calls);
        Assert.Empty(clock);
    }

    [Fact]
    public void Play_CommandInactive_OrNoEngine_DoesNothing()
    {
        (QViewer viewer, QViewerPlayback playback, _, List<string> clock) = TPlaybackBuild(ready: false);

        TInterfaceViewer.TViewerPlay(playback);
        Assert.False(viewer.QViewerPlaying);

        TInterfaceViewer.TViewerCommandSet(viewer, false);
        TInterfaceViewer.TViewerPlay(playback);

        Assert.False(viewer.QViewerPlaying);
        Assert.Empty(clock);
    }

    [Fact]
    public void Loupe_ForwardsTransport_AndSyncsSeek()
    {
        (QViewer viewer, QViewerPlayback playback, List<string> calls, _) = TPlaybackBuild();
        List<string> loupe = [];
        TInterfaceViewer.TViewerLoupeAttach(
            playback,
            () => loupe.Add("play"),
            () => loupe.Add("pause"),
            position => loupe.Add($"seek {position.TotalSeconds}"));
        TInterfaceViewer.TViewerLoupeSet(viewer, true);

        TInterfaceViewer.TViewerPlay(playback);
        TInterfaceViewer.TViewerPause(playback);
        TInterfaceViewer.TViewerSeek(playback, TimeSpan.FromSeconds(7));

        Assert.Equal(["play", "pause", "seek 7"], loupe);
        Assert.Empty(calls);
        Assert.Equal(TimeSpan.FromSeconds(7), viewer.QViewerPosition);
    }

    [Fact]
    public void Seek_AccurateOnFlyleaf_MarksRequest_AndClearsEnd()
    {
        (QViewer viewer, QViewerPlayback playback, List<string> calls, _) = TPlaybackBuild();
        TInterfaceViewer.TViewerEndSet(viewer, true);

        TInterfaceViewer.TViewerSeek(playback, TimeSpan.FromSeconds(3));

        Assert.Equal(["seek 3"], calls);
        Assert.True(viewer.QPlayer.QPlayerAccurateActive);
        Assert.False(viewer.QViewerEndReached);
        Assert.Equal(TimeSpan.FromSeconds(3), viewer.QViewerPosition);
    }

    [Fact]
    public void Tick_AtEnd_StopsClock_AndMarksEnd()
    {
        (QViewer viewer, QViewerPlayback playback, _, List<string> clock) = TPlaybackBuild(ended: true);
        List<TimeSpan> ticks = [];
        TInterfaceViewer.TViewerTickAttach(playback, ticks.Add);
        TInterfaceViewer.TViewerPlay(playback);

        TInterfaceViewer.TViewerTick(playback);

        Assert.True(viewer.QViewerEndReached);
        Assert.False(viewer.QViewerPlaying);
        Assert.Equal(["start", "stop"], clock);
        Assert.Empty(ticks);
    }

    [Fact]
    public void Tick_WhilePlaying_RaisesPosition()
    {
        (QViewer viewer, QViewerPlayback playback, _, _) = TPlaybackBuild(time: TimeSpan.FromSeconds(11));
        List<TimeSpan> ticks = [];
        TInterfaceViewer.TViewerTickAttach(playback, ticks.Add);
        TInterfaceViewer.TViewerPlay(playback);

        TInterfaceViewer.TViewerTick(playback);

        Assert.Equal([TimeSpan.FromSeconds(11)], ticks);
        Assert.Equal(TimeSpan.FromSeconds(11), viewer.QViewerPosition);
    }

    [Fact]
    public void Suspend_RemembersPlaying_Resume_RestartsIt()
    {
        (QViewer viewer, QViewerPlayback playback, _, List<string> clock) = TPlaybackBuild();
        TInterfaceViewer.TViewerPlay(playback);

        TInterfaceViewer.TViewerSuspend(playback);

        Assert.True(viewer.QViewerResumeInactive);
        Assert.Equal(["start", "stop"], clock);

        TInterfaceViewer.TViewerResume(playback);

        Assert.False(viewer.QViewerResumeInactive);
        Assert.True(viewer.QViewerPlaying);
        Assert.Equal(["start", "stop", "start"], clock);
    }

    [Fact]
    public void VolumeSet_ClampsAndRaises_OnlyWhenCommanded()
    {
        (QViewer viewer, QViewerPlayback playback, _, _) = TPlaybackBuild();
        List<double> volumes = [];
        TInterface.TCompassVolumeAttach(viewer, volumes.Add);

        TInterfaceViewer.TViewerVolumeSet(playback, 140);
        TInterfaceViewer.TViewerCommandSet(viewer, false);
        TInterfaceViewer.TViewerVolumeSet(playback, 20);

        Assert.Equal([100], volumes);
        Assert.Equal(100, viewer.QViewerAudio.QViewerVolume);
    }
}
