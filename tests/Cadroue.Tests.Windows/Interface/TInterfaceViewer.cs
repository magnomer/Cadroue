using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static class TInterfaceViewer
{
    internal static QViewer TViewerCreate()
    {
        CAtelier atelier = TInterface.TAtelierCreate();
        return new(atelier.CAtelierUsher, atelier.CAtelierWorkspaceCreate());
    }
    internal static void TViewerMediaAttach(QViewer viewer, Action<LCargo> handler) =>
        viewer.QViewerMediaChanged += handler;
    internal static void TViewerPlayingAttach(QViewer viewer, Action<bool> handler) =>
        viewer.QViewerPlayingChanged += handler;
    internal static void TViewerBypassAttach(QViewer viewer, Action<bool> handler) =>
        viewer.QViewerAudio.QViewerBypassChanged += handler;
    internal static void TViewerEngineAttach(QViewer viewer, Action handler) => viewer.QViewerEngineChanged += handler;
    internal static int TViewerSerialChange(QViewer viewer) => viewer.QViewerSerialChange();
    internal static bool TViewerSerialCheck(QViewer viewer, int serial) => viewer.QViewerSerialCheck(serial);
    internal static void TViewerCommandSet(QViewer viewer, bool active) => viewer.QViewerCommandSet(active);
    internal static void TViewerUnloadSet(QViewer viewer) => viewer.QViewerUnloadSet();
    internal static void TViewerEndSet(QViewer viewer, bool reached) => viewer.QViewerEndSet(reached);
    internal static void TViewerAllowSet(QViewer viewer, bool allowed) => viewer.QViewerAllowSet(allowed);
    internal static void TViewerFilterSet(QViewer viewer, string? filter) =>
        viewer.QViewerAudio.QViewerFilterSet(filter);
    internal static void TViewerBypassSet(QViewer viewer, bool bypass) => viewer.QViewerAudio.QViewerBypassSet(bypass);
    internal static string TViewerAudioResolve(QViewer viewer) => viewer.QViewerAudio.QViewerAudioResolve();
    internal static void TViewerEngineSet(QViewer viewer, LPreviewEngine engine) => viewer.QViewerEngineSet(engine);
    internal static void TViewerIntentSet(QViewer viewer, string path, TimeSpan position, bool? playing)
    {
        viewer.QViewerWorkspace.CWorkspaceStart(path);
        viewer.QViewerWorkspace.CWorkspaceSeek(position);
        if (playing is bool play)
        {
            viewer.QViewerWorkspace.CWorkspacePlay(play);
        }
    }
    internal static void TViewerIntentReset(QViewer viewer) => viewer.QViewerWorkspace.CWorkspaceCancel();
    internal static void TViewerRequestSet(QViewer viewer, string path) => viewer.QViewerWorkspace.CWorkspaceRequestSet(path);
    // True when the workspace holds or is loading the path, so asking for it raises no open.
    // The viewer's command is off for the ask, so a raised open is refused before it changes anything.
    internal static bool TViewerSourceMatch(QViewer viewer, string path)
    {
        bool active = viewer.QViewerCommandActive;
        int heard = 0;
        void TViewerNoticeRecord(string _) => heard++;
        viewer.QViewerCommandSet(false);
        viewer.QViewerOpenNotice += TViewerNoticeRecord;
        viewer.QViewerWorkspace.CWorkspaceOpen(path);
        viewer.QViewerOpenNotice -= TViewerNoticeRecord;
        viewer.QViewerCommandSet(active);
        return heard == 0;
    }
    internal static void TViewerPreviewSet(QViewer viewer, LPreviewState preview) => viewer.QViewerPreviewSet(preview);
    internal static void TViewerRotateSet(QViewer viewer, LRotateFlip rotate) =>
        viewer.QViewerPreviewSet(viewer.QViewerPreview.LRotateFlipChange(rotate));
    internal static void TViewerPlaybackUpdate(QViewer viewer, bool? playing, TimeSpan? position) =>
        viewer.QViewerPlaybackUpdate(playing, position);
    internal static void TViewerMediaCommit(QViewer viewer, LCargo cargo, bool persistent) =>
        viewer.QViewerMediaCommit(cargo, persistent);
    internal static void TViewerMediaRaise(QViewer viewer, LCargo cargo) => viewer.QViewerMediaRaise(cargo);
    internal static void TViewerMediaClose(QViewer viewer) => viewer.QViewerMediaClose();
    internal static QViewerNeutral TViewerNeutralRead(QViewer viewer) => viewer.QViewerNeutral;
    internal static void TViewerToolSet(QViewerNeutral neutral, bool armed, LNeutralTarget target) =>
        neutral.QViewerToolSet(armed, target);
    internal static void TViewerToolApply(QViewerNeutral neutral, QViewerTool tool) => neutral.QViewerToolApply(tool);
    internal static void TViewerNeutralCancel(QViewerNeutral neutral) => neutral.QViewerNeutralCancel();
    internal static bool TViewerKeyHandle(QViewerNeutral neutral, string key) => neutral.QViewerKeyHandle(key);
    internal static void TViewerPressHandle(QViewerNeutral neutral, double x, double y) =>
        neutral.QViewerPressHandle(x, y);
    internal static void TViewerEstimateStart(QViewerNeutral neutral, LWhitebalanceMethod method) =>
        neutral.QViewerEstimateStart(method);
    internal static void TViewerToolAttach(QViewerNeutral neutral, Action<bool, LNeutralTarget> handler) =>
        neutral.QViewerToolChanged += handler;
    internal static void TViewerNeutralAttach(QViewerNeutral neutral, Action<LNeutralSample> handler) =>
        neutral.QViewerNeutralChanged += handler;
    internal static void TViewerEstimateAttach(QViewerNeutral neutral, Action<LNeutralWheel> handler) =>
        neutral.QViewerEstimateChanged += handler;
    internal static void TViewerFocusAttach(QViewerNeutral neutral, Action handler) =>
        neutral.QViewerFocusApplied += handler;

    internal static LCargo TCargoCreate(string path, LMediaInfo? info, bool preview) =>
        new(path, info, info is not null, preview, null, null);
    internal static LCargo TCargoErrorCreate(string path, string? ffmpeg, string? preview) =>
        new(path, null, false, false, ffmpeg, preview);
    internal static LMediaInfo TViewerInfoCreate(TimeSpan duration, int width, int height) =>
        new(duration, width, height, 25, "h264", false, "", 0, 0);
    internal static LMediaInfo TViewerAudioCreate(TimeSpan duration) =>
        new(duration, 0, 0, 0, "", true, "aac", 48000, 2);
    internal static LPreviewState TPreviewCropCreate(double x, double y, double width, double height) =>
        LPreviewState.LPreviewDefaultCreate().LCropboxChange(new LCropbox(x, y, width, height));

    internal static QViewerMedia TViewerMediaRead(QViewer viewer) => viewer.QViewerMedia;
    internal static QViewerPlayback TViewerPlaybackRead(QViewer viewer) => viewer.QViewerPlayback;
    internal static QViewerSource TViewerSourceRead(QViewer viewer) => viewer.QViewerSource;
    internal static QViewerRenderer TViewerRendererRead(QViewer viewer) => viewer.QViewerRenderer;
    internal static void TViewerLoupeSet(QViewer viewer, bool active) => viewer.QViewerLoupeSet(active);
    internal static void TViewerMpvSet(QViewer viewer, bool mpv) => viewer.QViewerMpvSet(mpv);
    internal static void TViewerHostSet(QViewer viewer, bool visible) => viewer.QViewerHostSet(visible);
    internal static QViewerSwitch TViewerSwitchRead(QViewer viewer) =>
        viewer.QViewerAudio.QViewerSwitchRead();
    internal static bool TViewerSurfaceMatch(QViewer viewer, nint foreground, nint surface, nint overlay) =>
        viewer.QViewerSurfaceMatch(foreground, surface, overlay);
    internal static void TViewerPlayerAttach(QViewerMedia media, Action handler) =>
        media.QViewerPlayerCreated += handler;
    internal static void TViewerHostAttach(QViewerRenderer renderer, Action handler) =>
        renderer.QViewerHostApplied += handler;
    internal static Task TViewerLoadStart(QViewerMedia media, string path) => media.QViewerLoadStart(path);
    internal static Task TViewerFlyleafApply(
        QViewerMedia media, string path, LMediaInfo? info, string? error, int serial) =>
        media.QViewerFlyleafApply(path, info, error, serial);
    internal static bool TViewerLoadCancel(QViewerMedia media) => media.QViewerLoadCancel();
    internal static bool TViewerMediaClose(QViewerMedia media, bool force) => media.QViewerMediaClose(force);
    internal static void TViewerCommandApply(QViewerMedia media, bool active) => media.QViewerCommandApply(active);
    internal static void TViewerClose(QViewerMedia media) => media.QViewerTeardown.QViewerClose();
    internal static void TViewerClockAttach(QViewerPlayback playback, Action start, Action stop)
    {
        playback.QViewerClockStarted += start;
        playback.QViewerClockStopped += stop;
    }
    internal static void TViewerTickAttach(QViewerPlayback playback, Action<TimeSpan> handler) =>
        playback.QViewerClockTicked += handler;
    internal static void TViewerLoupeAttach(QViewerPlayback playback, Action play, Action pause, Action<TimeSpan> seek)
    {
        playback.QViewerLoupePlayed += play;
        playback.QViewerLoupePaused += pause;
        playback.QViewerLoupeSought += seek;
    }
    internal static void TViewerPlay(QViewerPlayback playback) => playback.QViewerPlay();
    internal static void TViewerPause(QViewerPlayback playback) => playback.QViewerPause();
    internal static void TViewerSeek(QViewerPlayback playback, TimeSpan position) => playback.QViewerSeek(position);
    internal static void TViewerVolumeSet(QViewerPlayback playback, double volume) => playback.QViewerVolumeSet(volume);
    internal static void TViewerSuspend(QViewerPlayback playback) => playback.QViewerSuspend();
    internal static void TViewerResume(QViewerPlayback playback) => playback.QViewerResume();
    internal static void TViewerTick(QViewerPlayback playback) => playback.QViewerTick();
    internal static void TViewerSourceOpen(QViewerSource source, string path) => source.QViewerSourceOpen(path);
    internal static void TViewerPathHandle(QViewer viewer, string? path) => viewer.QViewerWorkspace.CWorkspaceOpen(path);
    internal static void TViewerAskAttach(QViewerSource source, Action<QViewerAsk, Action<bool>> handler) =>
        source.QViewerSourceAsk += handler;
    internal static void TViewerDropAttach(QViewerSource source, Action<IReadOnlyList<string>> handler) =>
        source.QViewerPathsDrop += handler;
    internal static QWindowDropEffect TViewerDropResolve(QViewerSource source, string[]? paths, bool copyable) =>
        source.QViewerDropResolve(paths, copyable);
    internal static QWindowDropEffect TViewerDropHandle(QViewerSource source, string[]? paths, bool copyable) =>
        source.QViewerDropHandle(paths, copyable);
    internal static QViewerEnginePlan TViewerEngineRead(QViewerRenderer renderer) => renderer.QViewerEngineRead();
    internal static string TPreferenceEngineRead() => LPreference.LPreferenceStateCurrent.LPreferencePreviewEngine;
    internal static void TViewerEngineSelect(QViewerRenderer renderer, string key) => renderer.QViewerEngineSelect(key);
    internal static bool TViewerEngineUpdate(QViewerRenderer renderer) => renderer.QViewerEngineUpdate();
    internal static bool TViewerOverlayHandle(QViewerRenderer renderer, bool visible, double width, double height) =>
        renderer.QViewerOverlayHandle(visible, width, height);

    internal static LSidecarSourceResult TSidecarSourceCreate(string path, LSidecarSourceKind kind) =>
        new(path, kind, false, Path.GetFileName(path));
    internal static void TViewerLibrarianAttach(
        Func<string, bool>? checker,
        Func<string, LSidecarSourceResult?>? resolver,
        Func<string, string, bool>? matcher)
    {
        LLibrarian.LLibrarianFileChecker = checker;
        LLibrarian.LLibrarianSourceResolver = resolver;
        LLibrarian.LLibrarianSourceMatcher = matcher;
    }
}
