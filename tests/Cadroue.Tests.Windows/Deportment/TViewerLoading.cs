using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Logging")]
public sealed class TViewerLoading
{
    private static (QViewer TViewer, QViewerMedia TMedia, List<string> TViewerOpened) TViewerBuild(
        bool openFails = false)
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        TInterfaceViewer.TViewerCommandSet(viewer, true);
        QViewerMedia media = TInterfaceViewer.TViewerMediaRead(viewer);
        List<string> opened = [];
        TInterfaceViewer.TViewerPlayerAttach(media, () => TInterfacePlayer.TPlayerEngineSet(
            viewer.QPlayer,
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
        (QViewer viewer, QViewerMedia media, List<string> opened) = TViewerBuild();
        List<LCargo> cargos = [];
        TInterfaceViewer.TViewerMediaAttach(viewer, cargos.Add);
        string path = Path.Combine(Path.GetTempPath(), $"cadroue-missing-{Guid.NewGuid():N}.mp4");

        await TInterfaceViewer.TViewerLoadStart(media, path);

        Assert.Empty(opened);
        Assert.Single(cargos);
        Assert.Null(cargos[0].LCargoMediaInfo);
        Assert.False(cargos[0].LCargoPreviewAvailable);
        Assert.NotNull(cargos[0].LCargoFfmpegError);
        Assert.Null(viewer.QViewerWorkspace.CWorkspaceIntent);
        Assert.False(viewer.QPlayer.QPlayerReady);
    }

    [Fact]
    public async Task LoadStart_CommandInactive_DoesNothing()
    {
        (QViewer viewer, QViewerMedia media, _) = TViewerBuild();
        TInterfaceViewer.TViewerCommandSet(viewer, false);
        int serial = viewer.QViewerTrace.CViewerSerial;

        await TInterfaceViewer.TViewerLoadStart(media, "clip.mp4");

        Assert.Equal(serial, viewer.QViewerTrace.CViewerSerial);
        Assert.Null(viewer.QViewerWorkspace.CWorkspaceIntent);
    }

    [Fact]
    public async Task FlyleafApply_OpensCreatedEngine_AndCommitsPreview()
    {
        (QViewer viewer, QViewerMedia media, List<string> opened) = TViewerBuild();
        List<LCargo> cargos = [];
        TInterfaceViewer.TViewerMediaAttach(viewer, cargos.Add);
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(30), 1280, 720);
        TInterfaceViewer.TViewerIntentSet(viewer, "clip.mp4", TimeSpan.Zero, false);
        int serial = TInterfaceViewer.TViewerSerialChange(viewer);

        await TInterfaceViewer.TViewerFlyleafApply(media, "clip.mp4", info, null, serial);

        Assert.Equal(["clip.mp4"], opened);
        Assert.True(viewer.QPlayer.QPlayerReady);
        Assert.Single(cargos);
        Assert.True(cargos[0].LCargoPreviewAvailable);
        Assert.Same(info, viewer.QViewerMediaInfo);
        Assert.Equal("clip.mp4", viewer.QViewerWorkspace.CWorkspaceSource);
        Assert.Null(viewer.QViewerWorkspace.CWorkspaceIntent);
        Assert.False(viewer.QViewerPlaying);
        Assert.True(viewer.QViewerTrace.CViewerHostVisible);
        Assert.True(viewer.QPlayer.QPlayerRendererPending);
    }

    [Fact]
    public async Task FlyleafApply_OpenThrows_CommitsPreviewError_NoEngine()
    {
        (QViewer viewer, QViewerMedia media, _) = TViewerBuild(openFails: true);
        List<LCargo> cargos = [];
        TInterfaceViewer.TViewerMediaAttach(viewer, cargos.Add);
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(30), 1280, 720);
        TInterfaceViewer.TViewerIntentSet(viewer, "clip.mp4", TimeSpan.Zero, null);
        int serial = TInterfaceViewer.TViewerSerialChange(viewer);

        await TInterfaceViewer.TViewerFlyleafApply(media, "clip.mp4", info, null, serial);

        Assert.False(viewer.QPlayer.QPlayerReady);
        Assert.Single(cargos);
        Assert.False(cargos[0].LCargoPreviewAvailable);
        Assert.Equal("engine refused", cargos[0].LCargoPreviewError);
        Assert.False(viewer.QViewerTrace.CViewerHostVisible);
        Assert.Same(info, viewer.QViewerMediaInfo);
    }

    [Fact]
    public async Task FlyleafApply_StaleSerial_DiscardsWithoutCommit()
    {
        (QViewer viewer, QViewerMedia media, List<string> opened) = TViewerBuild();
        List<LCargo> cargos = [];
        TInterfaceViewer.TViewerMediaAttach(viewer, cargos.Add);
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(30), 1280, 720);
        int serial = TInterfaceViewer.TViewerSerialChange(viewer);
        TInterfaceViewer.TViewerSerialChange(viewer);

        await TInterfaceViewer.TViewerFlyleafApply(media, "clip.mp4", info, null, serial);

        Assert.Single(opened);
        Assert.Empty(cargos);
        Assert.False(viewer.QPlayer.QPlayerReady);
        Assert.Null(viewer.QViewerMediaInfo);
    }

    [Fact]
    public async Task FlyleafApply_AudioOnlyRefused_WhenTabForbids()
    {
        (QViewer viewer, QViewerMedia media, List<string> opened) = TViewerBuild();
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
        (QViewer viewer, QViewerMedia media, _) = TViewerBuild();
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
        Assert.Null(viewer.QViewerWorkspace.CWorkspaceSource);
        Assert.False(viewer.QPlayer.QPlayerReady);
        Assert.False(viewer.QViewerTrace.CViewerHostVisible);
        Assert.Equal(2, cropResets);
        Assert.False(TInterfaceViewer.TViewerMediaClose(media, true));
    }

    [Fact]
    public void LoadCancel_ClearsIntent_AdvancesSerial_OnlyWhenPending()
    {
        (QViewer viewer, QViewerMedia media, _) = TViewerBuild();

        Assert.False(TInterfaceViewer.TViewerLoadCancel(media));

        TInterfaceViewer.TViewerIntentSet(viewer, "clip.mp4", TimeSpan.Zero, null);
        int serial = viewer.QViewerTrace.CViewerSerial;

        Assert.True(TInterfaceViewer.TViewerLoadCancel(media));
        Assert.Null(viewer.QViewerWorkspace.CWorkspaceIntent);
        Assert.Equal(serial + 1, viewer.QViewerTrace.CViewerSerial);
    }

    [Fact]
    public async Task CommandApply_Off_SuspendsPlaying_On_ResumesIt()
    {
        (QViewer viewer, QViewerMedia media, _) = TViewerBuild();
        List<string> clock = [];
        TInterfaceViewer.TViewerClockAttach(
            TInterfaceViewer.TViewerPlaybackRead(viewer), () => clock.Add("start"), () => clock.Add("stop"));
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(30), 1280, 720);
        TInterfaceViewer.TViewerIntentSet(viewer, "clip.mp4", TimeSpan.Zero, true);
        await TInterfaceViewer.TViewerFlyleafApply(
            media, "clip.mp4", info, null, TInterfaceViewer.TViewerSerialChange(viewer));
        Assert.True(viewer.QViewerPlaying);

        TInterfaceViewer.TViewerCommandApply(media, false);

        Assert.False(viewer.QViewerTrace.CViewerCommandActive);
        Assert.True(viewer.QViewerResumeInactive);
        Assert.Equal("stop", clock[^1]);

        TInterfaceViewer.TViewerCommandApply(media, true);

        Assert.True(viewer.QViewerTrace.CViewerCommandActive);
        Assert.False(viewer.QViewerResumeInactive);
        Assert.True(viewer.QViewerPlaying);
        Assert.Equal("start", clock[^1]);
    }

    [Fact]
    public void Close_Unloads_AndIgnoresLaterCalls()
    {
        (QViewer viewer, QViewerMedia media, _) = TViewerBuild();
        int serial = viewer.QViewerTrace.CViewerSerial;

        TInterfaceViewer.TViewerClose(media);
        TInterfaceViewer.TViewerClose(media);

        Assert.True(viewer.QViewerTrace.CViewerUnloaded);
        Assert.Equal(serial + 1, viewer.QViewerTrace.CViewerSerial);
        Assert.False(TInterfaceViewer.TViewerMediaClose(media, true));
    }
}
