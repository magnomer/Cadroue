using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Logging")]
public sealed class TViewerLoading
{
    private static (LViewer TViewer, LViewerMedia TMedia, List<string> TViewerOpened) TViewerBuild(
        bool openFails = false)
    {
        LViewer viewer = TInterfaceViewer.TViewerCreate();
        TInterfaceViewer.TViewerCommandSet(viewer, true);
        LViewerMedia media = TInterfaceViewer.TViewerMediaRead(viewer);
        List<string> opened = [];
        TInterfaceViewer.TViewerPlayerAttach(media, () => TInterfacePlayer.TPlayerEngineSet(
            viewer.LPlayer,
            TInterfacePlayer.TPlayerSeamCreate(open: path =>
            {
                opened.Add(path);
                if (openFails)
                {
                    throw new InvalidOperationException("engine refused");
                }
            })));
        return (viewer, media, opened);
    }

    [Fact]
    public async Task LoadStart_MissingFile_CommitsFailureCargo()
    {
        (LViewer viewer, LViewerMedia media, List<string> opened) = TViewerBuild();
        List<LCargo> cargos = [];
        TInterfaceViewer.TViewerMediaAttach(viewer, cargos.Add);
        string path = Path.Combine(Path.GetTempPath(), $"cadroue-missing-{Guid.NewGuid():N}.mp4");

        await TInterfaceViewer.TViewerLoadStart(media, path, TimeSpan.Zero, null);

        Assert.Empty(opened);
        Assert.Single(cargos);
        Assert.Null(cargos[0].LCargoMediaInfo);
        Assert.False(cargos[0].LCargoPreviewAvailable);
        Assert.NotNull(cargos[0].LCargoFfmpegError);
        Assert.Null(viewer.LViewerIntent);
        Assert.False(viewer.LPlayer.LPlayerReady);
    }

    [Fact]
    public async Task LoadStart_CommandInactive_DoesNothing()
    {
        (LViewer viewer, LViewerMedia media, _) = TViewerBuild();
        TInterfaceViewer.TViewerCommandSet(viewer, false);
        int serial = viewer.LViewerLoadSerial;

        await TInterfaceViewer.TViewerLoadStart(media, "clip.mp4", TimeSpan.Zero, null);

        Assert.Equal(serial, viewer.LViewerLoadSerial);
        Assert.Null(viewer.LViewerIntent);
    }

    [Fact]
    public async Task FlyleafApply_OpensCreatedEngine_AndCommitsPreview()
    {
        (LViewer viewer, LViewerMedia media, List<string> opened) = TViewerBuild();
        List<LCargo> cargos = [];
        TInterfaceViewer.TViewerMediaAttach(viewer, cargos.Add);
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(30), 1280, 720);
        TInterfaceViewer.TViewerIntentSet(viewer, "clip.mp4", TimeSpan.Zero, false);
        int serial = TInterfaceViewer.TViewerSerialChange(viewer);

        await TInterfaceViewer.TViewerFlyleafApply(media, "clip.mp4", info, null, serial);

        Assert.Equal(["clip.mp4"], opened);
        Assert.True(viewer.LPlayer.LPlayerReady);
        Assert.Single(cargos);
        Assert.True(cargos[0].LCargoPreviewAvailable);
        Assert.Same(info, viewer.LViewerMediaInfo);
        Assert.Equal("clip.mp4", viewer.LViewerSourcePath);
        Assert.Null(viewer.LViewerIntent);
        Assert.False(viewer.LViewerPlaying);
        Assert.True(viewer.LViewerHostVisible);
        Assert.True(viewer.LPlayer.LPlayerRendererPending);
    }

    [Fact]
    public async Task FlyleafApply_OpenThrows_CommitsPreviewError_NoEngine()
    {
        (LViewer viewer, LViewerMedia media, _) = TViewerBuild(openFails: true);
        List<LCargo> cargos = [];
        TInterfaceViewer.TViewerMediaAttach(viewer, cargos.Add);
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(30), 1280, 720);
        TInterfaceViewer.TViewerIntentSet(viewer, "clip.mp4", TimeSpan.Zero, null);
        int serial = TInterfaceViewer.TViewerSerialChange(viewer);

        await TInterfaceViewer.TViewerFlyleafApply(media, "clip.mp4", info, null, serial);

        Assert.False(viewer.LPlayer.LPlayerReady);
        Assert.Single(cargos);
        Assert.False(cargos[0].LCargoPreviewAvailable);
        Assert.Equal("engine refused", cargos[0].LCargoPreviewError);
        Assert.False(viewer.LViewerHostVisible);
        Assert.Same(info, viewer.LViewerMediaInfo);
    }

    [Fact]
    public async Task FlyleafApply_StaleSerial_DiscardsWithoutCommit()
    {
        (LViewer viewer, LViewerMedia media, List<string> opened) = TViewerBuild();
        List<LCargo> cargos = [];
        TInterfaceViewer.TViewerMediaAttach(viewer, cargos.Add);
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(30), 1280, 720);
        int serial = TInterfaceViewer.TViewerSerialChange(viewer);
        TInterfaceViewer.TViewerSerialChange(viewer);

        await TInterfaceViewer.TViewerFlyleafApply(media, "clip.mp4", info, null, serial);

        Assert.Single(opened);
        Assert.Empty(cargos);
        Assert.False(viewer.LPlayer.LPlayerReady);
        Assert.Null(viewer.LViewerMediaInfo);
    }

    [Fact]
    public async Task FlyleafApply_AudioOnlyRefused_WhenTabForbids()
    {
        (LViewer viewer, LViewerMedia media, List<string> opened) = TViewerBuild();
        List<LCargo> cargos = [];
        TInterfaceViewer.TViewerMediaAttach(viewer, cargos.Add);
        LMediaInfo info = TInterfaceViewer.TViewerAudioCreate(TimeSpan.FromSeconds(30));
        int serial = TInterfaceViewer.TViewerSerialChange(viewer);

        await TInterfaceViewer.TViewerFlyleafApply(media, "song.mp3", info, null, serial);

        Assert.Empty(opened);
        Assert.Single(cargos);
        Assert.Null(cargos[0].LCargoMediaInfo);
        Assert.NotNull(cargos[0].LCargoPreviewError);
    }

    [Fact]
    public async Task MediaClose_Force_DisposesEngine_RaisesEmptyCargo()
    {
        (LViewer viewer, LViewerMedia media, _) = TViewerBuild();
        List<LCargo> cargos = [];
        int cropResets = 0;
        TInterfaceViewer.TViewerMediaAttach(viewer, cargos.Add);
        TInterfaceCrop.TCropAttach(TInterfaceCrop.TCropRead(viewer), () => cropResets++);
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(30), 1280, 720);
        TInterfaceViewer.TViewerIntentSet(viewer, "clip.mp4", TimeSpan.Zero, false);
        await TInterfaceViewer.TViewerFlyleafApply(
            media, "clip.mp4", info, null, TInterfaceViewer.TViewerSerialChange(viewer));
        TInterfaceViewer.TViewerCommandSet(viewer, false);

        Assert.False(TInterfaceViewer.TViewerMediaClose(media, false));
        Assert.True(TInterfaceViewer.TViewerMediaClose(media, true));

        Assert.Equal(2, cargos.Count);
        Assert.Equal(string.Empty, cargos[1].LCargoSourcePath);
        Assert.Null(viewer.LViewerSourcePath);
        Assert.False(viewer.LPlayer.LPlayerReady);
        Assert.False(viewer.LViewerHostVisible);
        Assert.Equal(2, cropResets);
        Assert.False(TInterfaceViewer.TViewerMediaClose(media, true));
    }

    [Fact]
    public void LoadCancel_ClearsIntent_AdvancesSerial_OnlyWhenPending()
    {
        (LViewer viewer, LViewerMedia media, _) = TViewerBuild();

        Assert.False(TInterfaceViewer.TViewerLoadCancel(media));

        TInterfaceViewer.TViewerIntentSet(viewer, "clip.mp4", TimeSpan.Zero, null);
        int serial = viewer.LViewerLoadSerial;

        Assert.True(TInterfaceViewer.TViewerLoadCancel(media));
        Assert.Null(viewer.LViewerIntent);
        Assert.Equal(serial + 1, viewer.LViewerLoadSerial);
    }

    [Fact]
    public async Task CommandApply_Off_SuspendsPlaying_On_ResumesIt()
    {
        (LViewer viewer, LViewerMedia media, _) = TViewerBuild();
        List<string> clock = [];
        TInterfaceViewer.TViewerClockAttach(
            TInterfaceViewer.TViewerPlaybackRead(viewer), () => clock.Add("start"), () => clock.Add("stop"));
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(30), 1280, 720);
        TInterfaceViewer.TViewerIntentSet(viewer, "clip.mp4", TimeSpan.Zero, true);
        await TInterfaceViewer.TViewerFlyleafApply(
            media, "clip.mp4", info, null, TInterfaceViewer.TViewerSerialChange(viewer));
        Assert.True(viewer.LViewerPlaying);

        TInterfaceViewer.TViewerCommandApply(media, false);

        Assert.False(viewer.LViewerCommandActive);
        Assert.True(viewer.LViewerResumeInactive);
        Assert.Equal("stop", clock[^1]);

        TInterfaceViewer.TViewerCommandApply(media, true);

        Assert.True(viewer.LViewerCommandActive);
        Assert.False(viewer.LViewerResumeInactive);
        Assert.True(viewer.LViewerPlaying);
        Assert.Equal("start", clock[^1]);
    }

    [Fact]
    public void Close_Unloads_AndIgnoresLaterCalls()
    {
        (LViewer viewer, LViewerMedia media, _) = TViewerBuild();
        int serial = viewer.LViewerLoadSerial;

        TInterfaceViewer.TViewerClose(media);
        TInterfaceViewer.TViewerClose(media);

        Assert.True(viewer.LViewerUnloaded);
        Assert.Equal(serial + 1, viewer.LViewerLoadSerial);
        Assert.False(TInterfaceViewer.TViewerMediaClose(media, true));
    }
}
