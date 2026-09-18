using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class TViewerMediaStatus
{
    [Fact]
    public void MediaCommit_SetsSource_StopsPlayback_ClearsEnd()
    {
        LViewer viewer = TInterface.TViewerCreate();
        LMediaInfo info = TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(90), 1920, 1080);
        TInterface.TViewerPlaybackUpdate(viewer, true, TimeSpan.FromSeconds(5));
        TInterface.TViewerEndSet(viewer, true);

        TInterface.TViewerMediaCommit(viewer, TInterface.TCargoCreate("clip.mp4", info, true), false);

        Assert.Equal("clip.mp4", viewer.LViewerSourcePath);
        Assert.Same(info, viewer.LViewerMediaInfo);
        Assert.False(viewer.LViewerPlaying);
        Assert.Equal(TimeSpan.Zero, viewer.LViewerPosition);
        Assert.False(viewer.LViewerEndReached);
        Assert.Equal(TimeSpan.FromSeconds(90), viewer.LViewerDuration);
        Assert.True(viewer.LViewerVideoPresent);
    }

    [Fact]
    public void SourceMatch_RequestedPath_WhilePendingOrLoaded_NotAfterFailureOrClose()
    {
        LViewer viewer = TInterface.TViewerCreate();
        LMediaInfo info = TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(90), 1920, 1080);

        Assert.False(TInterface.TViewerSourceMatch(viewer, @"C:\clip.cad"));

        TInterface.TViewerRequestSet(viewer, @"C:\clip.cad");
        Assert.False(TInterface.TViewerSourceMatch(viewer, @"C:\clip.cad"));

        TInterface.TViewerIntentSet(viewer, @"C:\clip.mp4", TimeSpan.Zero, null);
        Assert.True(TInterface.TViewerSourceMatch(viewer, @"C:\CLIP.CAD"));
        Assert.False(TInterface.TViewerSourceMatch(viewer, @"C:\clip.mp4"));

        TInterface.TViewerIntentReset(viewer);
        TInterface.TViewerMediaCommit(viewer, TInterface.TCargoCreate(@"C:\clip.mp4", null, false), false);
        Assert.False(TInterface.TViewerSourceMatch(viewer, @"C:\clip.cad"));

        TInterface.TViewerMediaCommit(viewer, TInterface.TCargoCreate(@"C:\clip.mp4", info, true), false);
        Assert.True(TInterface.TViewerSourceMatch(viewer, @"C:\clip.cad"));

        TInterface.TViewerMediaClose(viewer);
        Assert.False(TInterface.TViewerSourceMatch(viewer, @"C:\clip.cad"));
    }

    [Fact]
    public void MediaCommit_CropPersistent_KeepsCropbox()
    {
        LViewer viewer = TInterface.TViewerCreate();
        TInterface.TViewerPreviewSet(viewer, TInterface.TPreviewCropCreate(10, 20, 300, 200));
        LCargo cargo = TInterface.TCargoCreate("clip.mp4", null, false);

        TInterface.TViewerMediaCommit(viewer, cargo, true);
        Assert.NotNull(viewer.LViewerPreview.LCropbox);

        TInterface.TViewerMediaCommit(viewer, cargo, false);
        Assert.Null(viewer.LViewerPreview.LCropbox);
    }

    [Fact]
    public void MediaRaise_ThrowingSubscriber_RecordsAndContinues()
    {
        LViewer viewer = TInterface.TViewerCreate();
        LCargo? seen = null;
        TInterface.TViewerMediaAttach(viewer, _ => throw new InvalidOperationException("boom"));
        TInterface.TViewerMediaAttach(viewer, cargo => seen = cargo);
        LCargo cargo = TInterface.TCargoCreate("clip.mp4", null, false);
        using var logging = new TTrace();

        TInterface.TViewerMediaRaise(viewer, cargo);

        Assert.Same(cargo, seen);
        TTraceEntry entry = Assert.Single(logging.TTraceEntries);
        Assert.Equal("Viewer media notice handler failed", entry.TTraceSummary);
        Assert.Contains("boom", entry.TTraceDetail);
    }

    [Fact]
    public void Bypass_SilencesFilter_NoticesOnce()
    {
        LViewer viewer = TInterface.TViewerCreate();
        int notices = 0;
        TInterface.TViewerBypassAttach(viewer, _ => notices++);
        TInterface.TViewerFilterSet(viewer, "volume=2");

        Assert.Equal("volume=2", TInterface.TViewerAudioResolve(viewer));

        TInterface.TViewerBypassSet(viewer, true);
        TInterface.TViewerBypassSet(viewer, true);

        Assert.Equal(string.Empty, TInterface.TViewerAudioResolve(viewer));
        Assert.Equal(1, notices);

        TInterface.TViewerFilterSet(viewer, null);
        TInterface.TViewerBypassSet(viewer, false);

        Assert.Equal(string.Empty, TInterface.TViewerAudioResolve(viewer));
        Assert.Equal(2, notices);
    }

    [Fact]
    public void Playing_NoticeOnlyOnFlip_PositionKept()
    {
        LViewer viewer = TInterface.TViewerCreate();
        List<bool> flips = [];
        TInterface.TViewerPlayingAttach(viewer, flips.Add);

        TInterface.TViewerPlaybackUpdate(viewer, true, TimeSpan.FromSeconds(1));
        TInterface.TViewerPlaybackUpdate(viewer, null, TimeSpan.FromSeconds(2));
        TInterface.TViewerPlaybackUpdate(viewer, true, null);
        TInterface.TViewerPlaybackUpdate(viewer, false, null);

        Assert.Equal([true, false], flips);
        Assert.Equal(TimeSpan.FromSeconds(2), viewer.LViewerPosition);
    }

    [Fact]
    public void Engine_NoticeOnlyOnChange()
    {
        LViewer viewer = TInterface.TViewerCreate();
        int notices = 0;
        TInterface.TViewerEngineAttach(viewer, () => notices++);

        TInterface.TViewerEngineSet(viewer, LPreviewEngine.LPreviewEngineFlyleaf);
        TInterface.TViewerEngineSet(viewer, LPreviewEngine.LPreviewEngineMpv);
        TInterface.TViewerEngineSet(viewer, LPreviewEngine.LPreviewEngineMpv);

        Assert.Equal(1, notices);
        Assert.Equal(LPreviewEngine.LPreviewEngineMpv, viewer.LViewerEngine);
    }

    [Fact]
    public void AudioAllow_DefaultsOff_Toggles()
    {
        LViewer viewer = TInterface.TViewerCreate();

        Assert.False(viewer.LViewerAudioAllowed);

        TInterface.TViewerAllowSet(viewer, true);

        Assert.True(viewer.LViewerAudioAllowed);
    }
}
