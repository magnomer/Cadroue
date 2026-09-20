using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class TViewerPlayback
{
    private static (LViewer TViewer, LViewerPlayback TPlayback, List<string> TViewerCalls, List<string> TViewerClock)
        TPlaybackBuild(
        bool ready = true,
        bool ended = false,
        TimeSpan? time = null)
    {
        LViewer viewer = TInterface.TViewerCreate();
        TInterface.TViewerCommandSet(viewer, true);
        List<string> calls = [];
        List<string> clock = [];
        LViewerPlayback playback = TInterface.TViewerPlaybackRead(viewer);
        TInterface.TViewerClockAttach(playback, () => clock.Add("start"), () => clock.Add("stop"));
        if (ready)
        {
            TInterface.TPlayerEngineSet(viewer.LPlayer, TInterface.TPlayerSeamCreate(
                seek: position => calls.Add($"seek {position.TotalSeconds}"),
                time: () => time ?? TimeSpan.FromSeconds(4),
                ended: () => ended));
        }

        return (viewer, playback, calls, clock);
    }

    [Fact]
    public void Play_StartsClock_AndMarksPlaying()
    {
        (LViewer viewer, LViewerPlayback playback, _, List<string> clock) = TPlaybackBuild();

        TInterface.TViewerPlay(playback);

        Assert.True(viewer.LViewerPlaying);
        Assert.Equal(TimeSpan.FromSeconds(4), viewer.LViewerPosition);
        Assert.Equal(["start"], clock);

        TInterface.TViewerPause(playback);

        Assert.False(viewer.LViewerPlaying);
        Assert.Equal(["start", "stop"], clock);
    }

    [Fact]
    public void Play_AfterEnd_SeeksToStart()
    {
        (LViewer viewer, LViewerPlayback playback, List<string> calls, _) = TPlaybackBuild();
        TInterface.TViewerEndSet(viewer, true);

        TInterface.TViewerPlay(playback);

        Assert.Equal(["seek 0"], calls);
        Assert.False(viewer.LViewerEndReached);
    }

    [Fact]
    public void Play_WithPendingIntent_OnlyRewritesIntent()
    {
        (LViewer viewer, LViewerPlayback playback, List<string> calls, List<string> clock) = TPlaybackBuild();
        TInterface.TViewerIntentSet(viewer, "clip.mp4", TimeSpan.FromSeconds(2), null);

        TInterface.TViewerPlay(playback);
        Assert.True(viewer.LViewerIntent!.LViewerIntentPlaying);
        TInterface.TViewerPause(playback);
        Assert.False(viewer.LViewerIntent!.LViewerIntentPlaying);
        TInterface.TViewerSeek(playback, TimeSpan.FromSeconds(9));
        Assert.Equal(TimeSpan.FromSeconds(9), viewer.LViewerIntent!.LViewerIntentPosition);
        Assert.Equal(TimeSpan.FromSeconds(9), viewer.LViewerPosition);

        Assert.Empty(calls);
        Assert.Empty(clock);
    }

    [Fact]
    public void Play_CommandInactive_OrNoEngine_DoesNothing()
    {
        (LViewer viewer, LViewerPlayback playback, _, List<string> clock) = TPlaybackBuild(ready: false);

        TInterface.TViewerPlay(playback);
        Assert.False(viewer.LViewerPlaying);

        TInterface.TViewerCommandSet(viewer, false);
        TInterface.TViewerPlay(playback);

        Assert.False(viewer.LViewerPlaying);
        Assert.Empty(clock);
    }

    [Fact]
    public void Loupe_ForwardsTransport_AndSyncsSeek()
    {
        (LViewer viewer, LViewerPlayback playback, List<string> calls, _) = TPlaybackBuild();
        List<string> loupe = [];
        TInterface.TViewerLoupeAttach(
            playback,
            () => loupe.Add("play"),
            () => loupe.Add("pause"),
            position => loupe.Add($"seek {position.TotalSeconds}"));
        TInterface.TViewerLoupeSet(viewer, true);

        TInterface.TViewerPlay(playback);
        TInterface.TViewerPause(playback);
        TInterface.TViewerSeek(playback, TimeSpan.FromSeconds(7));

        Assert.Equal(["play", "pause", "seek 7"], loupe);
        Assert.Empty(calls);
        Assert.Equal(TimeSpan.FromSeconds(7), viewer.LViewerPosition);
    }

    [Fact]
    public void Seek_AccurateOnFlyleaf_MarksRequest_AndClearsEnd()
    {
        (LViewer viewer, LViewerPlayback playback, List<string> calls, _) = TPlaybackBuild();
        TInterface.TViewerEndSet(viewer, true);

        TInterface.TViewerSeek(playback, TimeSpan.FromSeconds(3));

        Assert.Equal(["seek 3"], calls);
        Assert.True(viewer.LPlayer.LPlayerAccurateActive);
        Assert.False(viewer.LViewerEndReached);
        Assert.Equal(TimeSpan.FromSeconds(3), viewer.LViewerPosition);
    }

    [Fact]
    public void Tick_AtEnd_StopsClock_AndMarksEnd()
    {
        (LViewer viewer, LViewerPlayback playback, _, List<string> clock) = TPlaybackBuild(ended: true);
        List<TimeSpan> ticks = [];
        TInterface.TViewerTickAttach(playback, ticks.Add);
        TInterface.TViewerPlay(playback);

        TInterface.TViewerTick(playback);

        Assert.True(viewer.LViewerEndReached);
        Assert.False(viewer.LViewerPlaying);
        Assert.Equal(["start", "stop"], clock);
        Assert.Empty(ticks);
    }

    [Fact]
    public void Tick_WhilePlaying_RaisesPosition()
    {
        (LViewer viewer, LViewerPlayback playback, _, _) = TPlaybackBuild(time: TimeSpan.FromSeconds(11));
        List<TimeSpan> ticks = [];
        TInterface.TViewerTickAttach(playback, ticks.Add);
        TInterface.TViewerPlay(playback);

        TInterface.TViewerTick(playback);

        Assert.Equal([TimeSpan.FromSeconds(11)], ticks);
        Assert.Equal(TimeSpan.FromSeconds(11), viewer.LViewerPosition);
    }

    [Fact]
    public void Suspend_RemembersPlaying_Resume_RestartsIt()
    {
        (LViewer viewer, LViewerPlayback playback, _, List<string> clock) = TPlaybackBuild();
        TInterface.TViewerPlay(playback);

        TInterface.TViewerSuspend(playback);

        Assert.True(viewer.LViewerResumeInactive);
        Assert.Equal(["start", "stop"], clock);

        TInterface.TViewerResume(playback);

        Assert.False(viewer.LViewerResumeInactive);
        Assert.True(viewer.LViewerPlaying);
        Assert.Equal(["start", "stop", "start"], clock);
    }

    [Fact]
    public void VolumeSet_ClampsAndRaises_OnlyWhenCommanded()
    {
        (LViewer viewer, LViewerPlayback playback, _, _) = TPlaybackBuild();
        List<double> volumes = [];
        TInterface.TCompassVolumeAttach(viewer, volumes.Add);

        TInterface.TViewerVolumeSet(playback, 140);
        TInterface.TViewerCommandSet(viewer, false);
        TInterface.TViewerVolumeSet(playback, 20);

        Assert.Equal([100], volumes);
        Assert.Equal(100, viewer.LViewerVolume);
    }
}
