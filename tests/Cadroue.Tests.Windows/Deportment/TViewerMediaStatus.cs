using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Logging")]
public sealed class TViewerMediaStatus
{
    [Fact]
    public void MediaCommit_SetsSource_StopsPlayback_ClearsEnd()
    {
        LViewer viewer = TInterfaceViewer.TViewerCreate();
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(90), 1920, 1080);
        TInterfaceViewer.TViewerPlaybackUpdate(viewer, true, TimeSpan.FromSeconds(5));
        TInterfaceViewer.TViewerEndSet(viewer, true);

        TInterfaceViewer.TViewerMediaCommit(viewer, TInterfaceViewer.TCargoCreate("clip.mp4", info, true), false);

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
        LViewer viewer = TInterfaceViewer.TViewerCreate();
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(90), 1920, 1080);

        Assert.False(TInterfaceViewer.TViewerSourceMatch(viewer, @"C:\clip.cad"));

        TInterfaceViewer.TViewerRequestSet(viewer, @"C:\clip.cad");
        Assert.False(TInterfaceViewer.TViewerSourceMatch(viewer, @"C:\clip.cad"));

        TInterfaceViewer.TViewerIntentSet(viewer, @"C:\clip.mp4", TimeSpan.Zero, null);
        Assert.True(TInterfaceViewer.TViewerSourceMatch(viewer, @"C:\CLIP.CAD"));
        Assert.False(TInterfaceViewer.TViewerSourceMatch(viewer, @"C:\clip.mp4"));

        TInterfaceViewer.TViewerIntentReset(viewer);
        TInterfaceViewer.TViewerMediaCommit(viewer, TInterfaceViewer.TCargoCreate(@"C:\clip.mp4", null, false), false);
        Assert.False(TInterfaceViewer.TViewerSourceMatch(viewer, @"C:\clip.cad"));

        TInterfaceViewer.TViewerMediaCommit(viewer, TInterfaceViewer.TCargoCreate(@"C:\clip.mp4", info, true), false);
        Assert.True(TInterfaceViewer.TViewerSourceMatch(viewer, @"C:\clip.cad"));

        TInterfaceViewer.TViewerMediaClose(viewer);
        Assert.False(TInterfaceViewer.TViewerSourceMatch(viewer, @"C:\clip.cad"));
    }

    [Fact]
    public void MediaCommit_CropPersistent_KeepsCropbox()
    {
        LViewer viewer = TInterfaceViewer.TViewerCreate();
        TInterfaceViewer.TViewerPreviewSet(viewer, TInterfaceViewer.TPreviewCropCreate(10, 20, 300, 200));
        LCargo cargo = TInterfaceViewer.TCargoCreate("clip.mp4", null, false);

        TInterfaceViewer.TViewerMediaCommit(viewer, cargo, true);
        Assert.NotNull(viewer.LViewerPreview.LCropbox);

        TInterfaceViewer.TViewerMediaCommit(viewer, cargo, false);
        Assert.Null(viewer.LViewerPreview.LCropbox);
    }

    [Fact]
    public void MediaRaise_ThrowingSubscriber_RecordsAndContinues()
    {
        LViewer viewer = TInterfaceViewer.TViewerCreate();
        LCargo? seen = null;
        TInterfaceViewer.TViewerMediaAttach(viewer, _ => throw new InvalidOperationException("boom"));
        TInterfaceViewer.TViewerMediaAttach(viewer, cargo => seen = cargo);
        LCargo cargo = TInterfaceViewer.TCargoCreate("clip.mp4", null, false);
        using var logging = new TTrace();

        TInterfaceViewer.TViewerMediaRaise(viewer, cargo);

        Assert.Same(cargo, seen);
        TTraceEntry entry = Assert.Single(logging.TTraceEntries);
        Assert.Equal("Viewer media notice handler failed", entry.TTraceSummary);
        Assert.Contains("boom", entry.TTraceDetail);
    }

    [Fact]
    public void Bypass_SilencesFilter_NoticesOnce()
    {
        LViewer viewer = TInterfaceViewer.TViewerCreate();
        int notices = 0;
        TInterfaceViewer.TViewerBypassAttach(viewer, _ => notices++);
        TInterfaceViewer.TViewerFilterSet(viewer, "volume=2");

        Assert.Equal("volume=2", TInterfaceViewer.TViewerAudioResolve(viewer));

        TInterfaceViewer.TViewerBypassSet(viewer, true);
        TInterfaceViewer.TViewerBypassSet(viewer, true);

        Assert.Equal(string.Empty, TInterfaceViewer.TViewerAudioResolve(viewer));
        Assert.Equal(1, notices);

        TInterfaceViewer.TViewerFilterSet(viewer, null);
        TInterfaceViewer.TViewerBypassSet(viewer, false);

        Assert.Equal(string.Empty, TInterfaceViewer.TViewerAudioResolve(viewer));
        Assert.Equal(2, notices);
    }

    [Fact]
    public void Playing_NoticeOnlyOnFlip_PositionKept()
    {
        LViewer viewer = TInterfaceViewer.TViewerCreate();
        List<bool> flips = [];
        TInterfaceViewer.TViewerPlayingAttach(viewer, flips.Add);

        TInterfaceViewer.TViewerPlaybackUpdate(viewer, true, TimeSpan.FromSeconds(1));
        TInterfaceViewer.TViewerPlaybackUpdate(viewer, null, TimeSpan.FromSeconds(2));
        TInterfaceViewer.TViewerPlaybackUpdate(viewer, true, null);
        TInterfaceViewer.TViewerPlaybackUpdate(viewer, false, null);

        Assert.Equal([true, false], flips);
        Assert.Equal(TimeSpan.FromSeconds(2), viewer.LViewerPosition);
    }

    [Fact]
    public void Engine_NoticeOnlyOnChange()
    {
        LViewer viewer = TInterfaceViewer.TViewerCreate();
        int notices = 0;
        TInterfaceViewer.TViewerEngineAttach(viewer, () => notices++);

        TInterfaceViewer.TViewerEngineSet(viewer, LPreviewEngine.LPreviewEngineFlyleaf);
        TInterfaceViewer.TViewerEngineSet(viewer, LPreviewEngine.LPreviewEngineMpv);
        TInterfaceViewer.TViewerEngineSet(viewer, LPreviewEngine.LPreviewEngineMpv);

        Assert.Equal(1, notices);
        Assert.Equal(LPreviewEngine.LPreviewEngineMpv, viewer.LViewerEngine);
    }

    [Fact]
    public void AudioAllow_DefaultsOff_Toggles()
    {
        LViewer viewer = TInterfaceViewer.TViewerCreate();

        Assert.False(viewer.LViewerAudioAllowed);

        TInterfaceViewer.TViewerAllowSet(viewer, true);

        Assert.True(viewer.LViewerAudioAllowed);
    }
}
