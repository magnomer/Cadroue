using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static class TInterfaceViewer
{
    internal static LViewer TViewerCreate() => new(TInterface.TAtelierCreate().CAtelierUsher);
    internal static void TViewerMediaAttach(LViewer viewer, Action<LCargo> handler) =>
        viewer.LViewerMediaChanged += handler;
    internal static void TViewerPlayingAttach(LViewer viewer, Action<bool> handler) =>
        viewer.LViewerPlayingChanged += handler;
    internal static void TViewerBypassAttach(LViewer viewer, Action<bool> handler) =>
        viewer.LViewerBypassChanged += handler;
    internal static void TViewerEngineAttach(LViewer viewer, Action handler) => viewer.LViewerEngineChanged += handler;
    internal static int TViewerSerialChange(LViewer viewer) => viewer.LViewerSerialChange();
    internal static bool TViewerSerialCheck(LViewer viewer, int serial) => viewer.LViewerSerialCheck(serial);
    internal static void TViewerCommandSet(LViewer viewer, bool active) => viewer.LViewerCommandSet(active);
    internal static void TViewerUnloadSet(LViewer viewer) => viewer.LViewerUnloadSet();
    internal static void TViewerEndSet(LViewer viewer, bool reached) => viewer.LViewerEndSet(reached);
    internal static void TViewerAllowSet(LViewer viewer, bool allowed) => viewer.LViewerAllowSet(allowed);
    internal static void TViewerFilterSet(LViewer viewer, string? filter) => viewer.LViewerFilterSet(filter);
    internal static void TViewerBypassSet(LViewer viewer, bool bypass) => viewer.LViewerBypassSet(bypass);
    internal static string TViewerAudioResolve(LViewer viewer) => viewer.LViewerAudioResolve();
    internal static void TViewerEngineSet(LViewer viewer, LPreviewEngine engine) => viewer.LViewerEngineSet(engine);
    internal static void TViewerIntentSet(LViewer viewer, string path, TimeSpan position, bool? playing) =>
        viewer.LViewerIntentSet(new LViewerIntent(path, position, playing));
    internal static void TViewerIntentReset(LViewer viewer) => viewer.LViewerIntentSet(null);
    internal static void TViewerRequestSet(LViewer viewer, string path) => viewer.LViewerRequestSet(path);
    internal static bool TViewerSourceMatch(LViewer viewer, string path) => viewer.LViewerSourceMatch(path);
    internal static void TViewerPreviewSet(LViewer viewer, LPreviewState preview) => viewer.LViewerPreviewSet(preview);
    internal static void TViewerRotateSet(LViewer viewer, LRotateFlip rotate) =>
        viewer.LViewerPreviewSet(viewer.LViewerPreview.LRotateFlipChange(rotate));
    internal static void TViewerPlaybackUpdate(LViewer viewer, bool? playing, TimeSpan? position) =>
        viewer.LViewerPlaybackUpdate(playing, position);
    internal static void TViewerMediaCommit(LViewer viewer, LCargo cargo, bool persistent) =>
        viewer.LViewerMediaCommit(cargo, persistent);
    internal static void TViewerMediaRaise(LViewer viewer, LCargo cargo) => viewer.LViewerMediaRaise(cargo);
    internal static void TViewerMediaClose(LViewer viewer) => viewer.LViewerMediaClose();
    internal static LViewerNeutral TViewerNeutralRead(LViewer viewer) => viewer.LViewerNeutral;
    internal static void TViewerToolSet(LViewerNeutral neutral, bool armed, LNeutralTarget target) =>
        neutral.LViewerToolSet(armed, target);
    internal static void TViewerToolApply(LViewerNeutral neutral, LViewerTool tool) => neutral.LViewerToolApply(tool);
    internal static void TViewerNeutralCancel(LViewerNeutral neutral) => neutral.LViewerNeutralCancel();
    internal static bool TViewerKeyHandle(LViewerNeutral neutral, string key) => neutral.LViewerKeyHandle(key);
    internal static void TViewerPressHandle(LViewerNeutral neutral, double x, double y) =>
        neutral.LViewerPressHandle(x, y);
    internal static void TViewerEstimateStart(LViewerNeutral neutral, LWhitebalanceMethod method) =>
        neutral.LViewerEstimateStart(method);
    internal static void TViewerToolAttach(LViewerNeutral neutral, Action<bool, LNeutralTarget> handler) =>
        neutral.LViewerToolChanged += handler;
    internal static void TViewerNeutralAttach(LViewerNeutral neutral, Action<LNeutralSample> handler) =>
        neutral.LViewerNeutralChanged += handler;
    internal static void TViewerEstimateAttach(LViewerNeutral neutral, Action<LNeutralWheel> handler) =>
        neutral.LViewerEstimateChanged += handler;
    internal static void TViewerFocusAttach(LViewerNeutral neutral, Action handler) =>
        neutral.LViewerFocusApplied += handler;

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

    internal static LViewerMedia TViewerMediaRead(LViewer viewer) => viewer.LViewerMedia;
    internal static LViewerPlayback TViewerPlaybackRead(LViewer viewer) => viewer.LViewerPlayback;
    internal static LViewerSource TViewerSourceRead(LViewer viewer) => viewer.LViewerSource;
    internal static LViewerRenderer TViewerRendererRead(LViewer viewer) => viewer.LViewerRenderer;
    internal static void TViewerLoupeSet(LViewer viewer, bool active) => viewer.LViewerLoupeSet(active);
    internal static void TViewerMpvSet(LViewer viewer, bool mpv) => viewer.LViewerMpvSet(mpv);
    internal static void TViewerHostSet(LViewer viewer, bool visible) => viewer.LViewerHostSet(visible);
    internal static LViewerSwitch TViewerSwitchRead(LViewer viewer) => viewer.LViewerSwitchRead();
    internal static bool TViewerSurfaceMatch(LViewer viewer, nint foreground, nint surface, nint overlay) =>
        viewer.LViewerSurfaceMatch(foreground, surface, overlay);
    internal static void TViewerPlayerAttach(LViewerMedia media, Action handler) =>
        media.LViewerPlayerCreated += handler;
    internal static void TViewerHostAttach(LViewerRenderer renderer, Action handler) =>
        renderer.LViewerHostApplied += handler;
    internal static Task TViewerLoadStart(LViewerMedia media, string path, TimeSpan position, bool? playing) =>
        media.LViewerLoadStart(new LViewerIntent(path, position, playing));
    internal static Task TViewerFlyleafApply(
        LViewerMedia media, string path, LMediaInfo? info, string? error, int serial) =>
        media.LViewerFlyleafApply(path, info, error, serial);
    internal static bool TViewerLoadCancel(LViewerMedia media) => media.LViewerLoadCancel();
    internal static bool TViewerMediaClose(LViewerMedia media, bool force) => media.LViewerMediaClose(force);
    internal static void TViewerCommandApply(LViewerMedia media, bool active) => media.LViewerCommandApply(active);
    internal static void TViewerClose(LViewerMedia media) => media.QViewerTeardown.LViewerClose();
    internal static void TViewerClockAttach(LViewerPlayback playback, Action start, Action stop)
    {
        playback.LViewerClockStarted += start;
        playback.LViewerClockStopped += stop;
    }
    internal static void TViewerTickAttach(LViewerPlayback playback, Action<TimeSpan> handler) =>
        playback.LViewerClockTicked += handler;
    internal static void TViewerLoupeAttach(LViewerPlayback playback, Action play, Action pause, Action<TimeSpan> seek)
    {
        playback.LViewerLoupePlayed += play;
        playback.LViewerLoupePaused += pause;
        playback.LViewerLoupeSought += seek;
    }
    internal static void TViewerPlay(LViewerPlayback playback) => playback.LViewerPlay();
    internal static void TViewerPause(LViewerPlayback playback) => playback.LViewerPause();
    internal static void TViewerSeek(LViewerPlayback playback, TimeSpan position) => playback.LViewerSeek(position);
    internal static void TViewerVolumeSet(LViewerPlayback playback, double volume) => playback.LViewerVolumeSet(volume);
    internal static void TViewerSuspend(LViewerPlayback playback) => playback.LViewerSuspend();
    internal static void TViewerResume(LViewerPlayback playback) => playback.LViewerResume();
    internal static void TViewerTick(LViewerPlayback playback) => playback.LViewerTick();
    internal static void TViewerSourceOpen(LViewerSource source, string path) => source.LViewerSourceOpen(path);
    internal static void TViewerPathHandle(LViewer viewer, string? path) => viewer.LViewerPathHandle(path);
    internal static void TViewerAskAttach(LViewerSource source, Action<LViewerAsk, Action<bool>> handler) =>
        source.LViewerSourceAsk += handler;
    internal static void TViewerDropAttach(LViewerSource source, Action<IReadOnlyList<string>> handler) =>
        source.LViewerPathsDrop += handler;
    internal static LWindowDropEffect TViewerDropResolve(LViewerSource source, string[]? paths, bool copyable) =>
        source.LViewerDropResolve(paths, copyable);
    internal static LWindowDropEffect TViewerDropHandle(LViewerSource source, string[]? paths, bool copyable) =>
        source.LViewerDropHandle(paths, copyable);
    internal static LViewerEnginePlan TViewerEngineRead(LViewerRenderer renderer) => renderer.LViewerEngineRead();
    internal static string TPreferenceEngineRead() => LPreference.LPreferenceStateCurrent.LPreferencePreviewEngine;
    internal static void TViewerEngineSelect(LViewerRenderer renderer, string key) => renderer.LViewerEngineSelect(key);
    internal static bool TViewerEngineUpdate(LViewerRenderer renderer) => renderer.LViewerEngineUpdate();
    internal static bool TViewerOverlayHandle(LViewerRenderer renderer, bool visible, double width, double height) =>
        renderer.LViewerOverlayHandle(visible, width, height);

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
