using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;
using Cadroue.UIDeportment;

namespace Cadroue.Tests;

internal static partial class TInterface
{
    internal static LProcessing TProcessingCreate() => new();
    internal static void TProcessingAttach(LProcessing processing, Action handler) =>
        processing.LProcessingChange += handler;
    internal static bool TProcessingActiveCheck(LProcessing processing, string step) =>
        processing.LProcessingActiveCheck(step);
    internal static bool TProcessingEnabledCheck(LProcessing processing, string step) =>
        processing.LProcessingEnabledCheck(step);
    internal static void TProcessingOrderAttach(LProcessing processing, Action handler) =>
        processing.LProcessingOrderChange += handler;
    internal static void TProcessingStepAttach(LProcessing processing, Action<string> handler) =>
        processing.LProcessingStepChange += handler;
    internal static void TProcessingStepAdd(LProcessing processing, string step) => processing.LProcessingStepAdd(step);
    internal static void TProcessingOrderedSet(LProcessing processing, bool ordered) =>
        processing.LProcessingOrderedSet(ordered);
    internal static void TProcessingEnabledSet(LProcessing processing, string step, bool enabled) =>
        processing.LProcessingEnabledSet(step, enabled);
    internal static void TProcessingEnabledSet(LProcessing processing, string step, bool enabled, string? notice) =>
        processing.LProcessingEnabledSet(step, enabled, notice);
    internal static string? TProcessingNoticeRead(LProcessing processing, string step) =>
        processing.LProcessingNoticeRead(step);
    internal static bool TProcessingStepSelect(LProcessing processing, string step) =>
        processing.LProcessingStepSelect(step);
    internal static bool TProcessingStepMove(LProcessing processing, int delta) =>
        processing.LProcessingStepMove(delta);
    internal static bool TProcessingIndexMove(LProcessing processing, int from, int to) =>
        processing.LProcessingIndexMove(from, to);
    internal static void TProcessingDragStart(LProcessing processing, string step, double x, double y) =>
        processing.LProcessingDragStart(step, x, y);
    internal static void TProcessingDragMove(
        LProcessing processing,
        double x,
        double y,
        bool pressed,
        IReadOnlyList<double> tops,
        IReadOnlyList<double> heights) =>
        processing.LProcessingDragMove(x, y, pressed, 4, 4, tops, heights);
    internal static void TProcessingDragClear(LProcessing processing) => processing.LProcessingDragClear();
    internal static void TProcessingRowAdd(LProcessing processing, string key) =>
        processing.LProcessingRowAdd(new LProcessingRow(key, string.Empty, key));
    internal static double TProcessingOpacityRead(LProcessing processing, string step) =>
        processing.LProcessingOpacityRead(step);
    internal static string TProcessingNumberRead(LProcessing processing, string step) =>
        processing.LProcessingNumberRead(step);
    internal static void TProcessingKeyHandle(LProcessing processing, string step, bool activate) =>
        processing.LProcessingKeyHandle(step, activate);
    internal static void TProcessingCancelAttach(LProcessing processing, Action handler) =>
        processing.LProcessingDragCancel += handler;
    internal static int TProcessingIndexResolve(double y, IReadOnlyList<double> tops, IReadOnlyList<double> heights) =>
        LProcessing.LProcessingIndexResolve(y, tops, heights);

    internal static LClinic TClinicCreate() => new();
    internal static void TClinicPlanAttach(LClinic clinic, Action handler) => clinic.LClinicPlanChange += handler;
    internal static void TClinicStepSet(LClinic clinic, string? step) => clinic.LClinicStepSet(step);
    internal static void TClinicActiveSet(LClinic clinic, bool active) => clinic.LClinicActiveSet(active);
    internal static void TClinicSalvageSet(LClinic clinic, LWorkFixSalvage salvage) =>
        clinic.LClinicSalvageSet(salvage);
    internal static void TSalvageActiveSet(LClinic clinic, bool active) => clinic.LSalvageActiveSet(active);
    internal static void TSalvagePersistentSet(LClinic clinic, bool persistent) =>
        clinic.LSalvagePersistentSet(persistent);
    internal static void TSalvageModeSet(LClinic clinic, LSalvageMode mode) => clinic.LSalvageModeSet(mode);
    internal static void TSalvageBasisSet(LClinic clinic, LSalvageBasis basis) => clinic.LSalvageBasisSet(basis);
    internal static LWorkFixSalvage TClinicSalvageCreate(
        bool active, LSalvageMode mode, LSalvageBasis basis, bool persistent) =>
        new(active, mode, basis, persistent);
    internal static LWorkFix TClinicPlanRead(LClinic clinic) => clinic.LClinicPlanRead();
    internal static void TClinicPlanApply(LClinic clinic, LWorkFix plan) => clinic.LClinicPlanApply(plan);
    internal static bool TClinicRepairCheck(LClinic clinic) => clinic.LClinicRepairCheck();
    internal static void TClinicChangeAttach(LClinic clinic, Action handler) => clinic.LClinicChange += handler;
    internal static void TClinicSourceSet(LClinic clinic, string? path) => clinic.LClinicSourceSet(path);
    internal static void TClinicResultSet(LClinic clinic, string path, LFlawKind kind, LCheckupOutcome outcome) =>
        clinic.LClinicResultSet(path, kind, new LCheckupResult(path, kind, outcome));
    internal static void TClinicProgressSet(LClinic clinic, string path, double value) =>
        clinic.LClinicProgressSet(path, value);
    internal static void TClinicResultsRemove(LClinic clinic, params string[] paths) =>
        clinic.LClinicResultsRemove(paths);
    internal static LCheckupOutcome TClinicOutcomeRead(LClinic clinic) => clinic.LClinicResultRead().LCheckupOutcome;
    internal static double TClinicProgressRead(LClinic clinic) => clinic.LClinicProgressRead();
    internal static string TClinicSimpleRead(LClinic clinic) => clinic.LClinicSimpleRead();
    internal static string TClinicTitleRead(LClinic clinic) => clinic.LClinicTitleRead();

    internal static void TCurvePressHandle(LCurve curve, double x, double y, double size) =>
        curve.LCurvePressHandle(x, y, size);
    internal static bool TCurveMoveHandle(LCurve curve, double x, double y, double size) =>
        curve.LCurveMoveHandle(x, y, size);
    internal static bool TCurveReleaseHandle(LCurve curve) => curve.LCurveReleaseHandle();
    internal static bool TCurveDragRead(LCurve curve) => curve.LCurveDragActive;
    internal static void TCurvePointCommit(LCurve curve, string input, string output) =>
        curve.LCurvePointCommit(input, output);
    internal static double TCurveInputRead(LCurve curve) => curve.LCurveInputPercent;
    internal static void TCurveHistogramSet(LCurve curve, LHistogramCounts? histogram) =>
        curve.LCurveHistogramSet(histogram);
    internal static IReadOnlyList<LCurveLine> TCurveGridRead(LCurve curve, double size) =>
        curve.LCurveCanvas.LCurveGridRead(size);
    internal static IReadOnlyList<LCurvePoint> TCurveTrackRead(LCurve curve, double size) =>
        curve.LCurveCanvas.LCurveTrackRead(size);
    internal static IReadOnlyList<LCurveDot> TCurveDotsRead(LCurve curve, double size) =>
        curve.LCurveCanvas.LCurveDotsRead(size);
    internal static IReadOnlyList<LCurvePoint> TCurveHistogramRead(LCurve curve, double size) =>
        curve.LCurveCanvas.LCurveHistogramRead(size);

    internal static void TWhitebalanceToolAttach(LWhitebalance whitebalance, Action<bool, LNeutralTarget> handler) =>
        whitebalance.LWhitebalanceToolChange += handler;
    internal static void TWhitebalanceToolToggle(LWhitebalance whitebalance, LNeutralTarget target, bool state) =>
        whitebalance.LWhitebalanceToolToggle(target, state);
    internal static bool TWhitebalanceGreyRead(LWhitebalance whitebalance) => whitebalance.LWhitebalanceGreyArmed;
    internal static bool TWhitebalanceWhiteRead(LWhitebalance whitebalance) => whitebalance.LWhitebalanceWhiteArmed;
    internal static string TWhitebalanceGuideRead(LWhitebalance whitebalance) => whitebalance.LWhitebalanceGuideRead();
    internal static void TWhitebalanceStatusSet(LWhitebalance whitebalance, string status) =>
        whitebalance.LWhitebalanceStatusSet(status);
    internal static LWhitebalanceReadout TWhitebalanceReadoutRead(LWhitebalance whitebalance) =>
        whitebalance.LWhitebalanceReadoutRead();
    internal static void TWhitebalanceMethodSelect(LWhitebalance whitebalance, int index) =>
        whitebalance.LWhitebalanceMethodSelect(index);
    internal static int TWhitebalanceIndexRead(LWhitebalance whitebalance) => whitebalance.LWhitebalanceMethodIndex;
    internal static void TWhitebalanceWheelHandle(
        LWhitebalance whitebalance, bool pressed, double x, double y, double size) =>
        whitebalance.LWhitebalanceWheelHandle(pressed, x, y, size);
    internal static LNeutralDot TWhitebalanceDotRead(LWhitebalance whitebalance, double size, double dot) =>
        whitebalance.LWhitebalanceDotRead(size, dot);

    internal static void TBlankWheelHandle(LBlank blank, bool pressed, double x, double y, double size) =>
        blank.LBlankWheelHandle(pressed, x, y, size);
    internal static LNeutralDot TBlankDotRead(LBlank blank, double size, double dot) => blank.LBlankDotRead(size, dot);
    internal static LNeutralBitmap TNeutralBitmapResolve(int size, double value) =>
        LNeutral.LNeutralBitmapResolve(size, value);

    internal static IReadOnlyList<LSensorPlan> TSensorPlansRead(LSensor sensor) => sensor.LSensorPlans;
    internal static bool TSensorEnabledRead(LSensor sensor, LDetectorKind kind) => sensor.LSensorEnabledRead(kind);
    internal static double TSensorValueRead(LSensor sensor, LDetectorKind kind, int row) =>
        sensor.LSensorValueRead(kind, row);
    internal static double TSensorDefaultRead(LSensor sensor, LDetectorKind kind, int row) =>
        sensor.LSensorDefaultRead(kind, row);
    internal static string TSensorUnitRead(LSensor sensor, LDetectorKind kind, int row) =>
        sensor.LSensorUnitRead(kind, row);
    internal static void TSensorValueSet(LSensor sensor, LDetectorKind kind, int row, double value) =>
        sensor.LSensorValueSet(kind, row, value);
    internal static void TSensorModeSelect(LSensor sensor, int index) => sensor.LSensorModeSelect(index);
    internal static void TSensorSpeedSelect(LSensor sensor, int index) => sensor.LSensorSpeedSelect(index);
    internal static void TSensorMetricSelect(LSensor sensor, int index) => sensor.LSensorMetricSelect(index);
    internal static LInspectorChoice TSensorChoiceRead(LSensor sensor, LDetectorKind kind) =>
        sensor.LSensorChoiceRead(kind);
    internal static void TSensorChoiceSelect(LSensor sensor, LDetectorKind kind, int index) =>
        sensor.LSensorChoiceSelect(kind, index);
    internal static void TSensorRunningSet(LSensor sensor, bool running) => sensor.LSensorRunningSet(running);
    internal static void TSensorRunHandle(LSensor sensor) => sensor.LSensorRunHandle();
    internal static void TSensorRunAttach(LSensor sensor, Action run, Action stop)
    {
        sensor.LSensorRunApply += run;
        sensor.LSensorStopApply += stop;
    }

    internal static IReadOnlyList<LInspectorRow> TLoudnessRowsRead(LLoudness loudness, bool dynamic) =>
        loudness.LLoudnessRowsRead(dynamic);
    internal static double TLoudnessValueRead(LLoudness loudness, bool dynamic, int index) =>
        loudness.LLoudnessValueRead(dynamic, index);
    internal static double TLoudnessDefaultRead(LLoudness loudness, bool dynamic, int index) =>
        loudness.LLoudnessDefaultRead(dynamic, index);
    internal static void TLoudnessValueSet(LLoudness loudness, bool dynamic, int index, double value) =>
        loudness.LLoudnessValueSet(dynamic, index, value);
    internal static void TLoudnessModeSelect(LLoudness loudness, int index) => loudness.LLoudnessModeSelect(index);
    internal static LInspectorChoice TLoudnessChoiceRead(LLoudness loudness, bool dynamic) =>
        loudness.LLoudnessChoiceRead(dynamic);
    internal static void TLoudnessChoiceSelect(LLoudness loudness, bool dynamic, int index) =>
        loudness.LLoudnessChoiceSelect(dynamic, index);

    internal static double TNoiseValueRead(LNoise noise, int index) => noise.LNoiseValueRead(index);
    internal static double TNoiseDefaultRead(LNoise noise, int index) => noise.LNoiseDefaultRead(index);
    internal static void TNoiseTypeSelect(LNoise noise, int index) => noise.LNoiseTypeSelect(index);
    internal static LInspectorChoice TNoiseChoiceRead(LNoise noise) => noise.LNoiseChoiceRead();
    internal static void TNoiseChoiceSelect(LNoise noise, int index) => noise.LNoiseChoiceSelect(index);

    internal static LInspectorChoice TFilterChoiceRead(LFilter filter) => filter.LFilterChoiceRead();
    internal static void TFilterChoiceSelect(LFilter filter, int index) => filter.LFilterChoiceSelect(index);
    internal static void TFilterPolesSelect(LFilter filter, int index) => filter.LFilterPolesSelect(index);

    internal static void TEqualizerRowsAttach(LEqualizer equalizer, Action handler) =>
        equalizer.LEqualizerRowsChange += handler;
    internal static string TEqualizerFrequencyCommit(LEqualizer equalizer, int index, string text) =>
        equalizer.LEqualizerFrequencyCommit(index, text);
    internal static string TEqualizerFrequencyRead(LEqualizer equalizer, int index) =>
        equalizer.LEqualizerFrequencyRead(index);
    internal static double TEqualizerGainRead(LEqualizer equalizer, int index) => equalizer.LEqualizerGainRead(index);
    internal static void TEqualizerGainSet(LEqualizer equalizer, int index, double gain) =>
        equalizer.LEqualizerGainSet(index, gain);
    internal static LInspectorChoice TEqualizerChoiceRead(LEqualizer equalizer) => equalizer.LEqualizerChoiceRead();
    internal static void TEqualizerChoiceSelect(LEqualizer equalizer, int index) =>
        equalizer.LEqualizerChoiceSelect(index);

    internal static LDetectorBound TDetectorThresholdRead(LDetectorKind kind) => LDetector.LDetectorThresholdRead(kind);
    internal static LInspectorChoice TInspectorChoiceRead(
        IReadOnlyList<string> tokens, Func<string, string> keyRead, string? token, string? match) =>
        LInspectorPlan.LInspectorChoiceRead(tokens, keyRead, token, match);
    internal static string? TInspectorChoiceResolve(
        IReadOnlyList<string> tokens, int index, string? token, string? match) =>
        LInspectorPlan.LInspectorChoiceResolve(tokens, index, token, match);

    internal static LViewer TViewerCreate() => new();
    internal static void TViewerMediaAttach(LViewer viewer, Action<LCargo> handler) =>
        viewer.LViewerMediaChange += handler;
    internal static void TViewerPlayingAttach(LViewer viewer, Action<bool> handler) =>
        viewer.LViewerPlayingChange += handler;
    internal static void TViewerBypassAttach(LViewer viewer, Action<bool> handler) =>
        viewer.LViewerBypassChange += handler;
    internal static void TViewerEngineAttach(LViewer viewer, Action handler) => viewer.LViewerEngineChange += handler;
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
        neutral.LViewerToolChange += handler;
    internal static void TViewerNeutralAttach(LViewerNeutral neutral, Action<LNeutralSample> handler) =>
        neutral.LViewerNeutralChange += handler;
    internal static void TViewerEstimateAttach(LViewerNeutral neutral, Action<LNeutralWheel> handler) =>
        neutral.LViewerEstimateChange += handler;
    internal static void TViewerFocusAttach(LViewerNeutral neutral, Action handler) =>
        neutral.LViewerFocusApply += handler;
    internal static LCrop TCropRead(LViewer viewer) => viewer.LCrop;
    internal static LCropDrag TCropDragRead(LViewer viewer) => viewer.LCropDrag;
    internal static void TCropAttach(LCrop crop, Action handler) => crop.LCropApply += handler;
    internal static void TCropVideoAttach(LCrop crop, Action handler) => crop.LCropVideoChange += handler;
    internal static void TCropCaptureAttach(LCropDrag drag, Action<bool> handler) => drag.LCropCaptureApply += handler;
    internal static void TCropSizeHandle(LCrop crop, double width, double height) =>
        crop.LCropSizeHandle(width, height);
    internal static void TCropActiveSet(LCrop crop, bool active) => crop.LCropActiveSet(active);
    internal static void TCropToolSet(LCrop crop, bool armed) => crop.LCropToolSet(armed);
    internal static void TCropRectSet(LCrop crop, LCropbox? rect) => crop.LCropRectSet(rect);
    internal static void TCropHide(LCrop crop) => crop.LCropHide();
    internal static LCropBox TCropBoxRead(LCrop crop) => crop.LCropBoxRead();
    internal static IReadOnlyList<LCropHandle> TCropHandlesRead(LCrop crop) => crop.LCropHandlesRead();
    internal static LCropShade TCropShadeRead(LCrop crop) => crop.LCropShadeRead();
    internal static LCropbox TCropVideoRead(LCrop crop) => crop.LCropVideoRead();
    internal static LCropbox? TCropPixelRead(LCrop crop) => crop.LCropPixelRead();
    internal static bool TCropGripHandle(LCropDrag drag, int index, double x, double y) =>
        drag.LCropGripHandle(index, x, y);
    internal static bool TCropBodyHandle(LCropDrag drag, double x, double y) => drag.LCropBodyHandle(x, y);
    internal static bool TCropPressHandle(LCropDrag drag, double x, double y) => drag.LCropPressHandle(x, y);
    internal static bool TCropMoveHandle(LCropDrag drag, bool pressed, double x, double y) =>
        drag.LCropMoveHandle(pressed, x, y);
    internal static bool TCropReleaseHandle(LCropDrag drag, double x, double y) => drag.LCropReleaseHandle(x, y);
    internal static LCargo TCargoCreate(string path, LMediaInfo? info, bool preview) =>
        new(path, info, info is not null, preview, null, null);
    internal static LMediaInfo TViewerInfoCreate(TimeSpan duration, int width, int height) =>
        new(duration, width, height, 25, "h264", false, "", 0, 0);
    internal static LMediaInfo TViewerAudioCreate(TimeSpan duration) =>
        new(duration, 0, 0, 0, "", true, "aac", 48000, 2);
    internal static LPreviewState TPreviewCropCreate(double x, double y, double width, double height) =>
        LPreviewState.LPreviewDefaultCreate().LCropboxChange(new LCropbox(x, y, width, height));

    internal static LPlayer TPlayerCreate() => new();
    internal static bool TPlayerAccurateSet(LPlayer player) => player.LPlayerAccurateSet();
    internal static void TPlayerAccurateReset(LPlayer player) => player.LPlayerAccurateReset();
    internal static void TPlayerRendererSet(LPlayer player, bool pending) => player.LPlayerRendererSet(pending);
    internal static bool TPlayerSeekCommit(LPlayer player, int milliseconds) => player.LPlayerSeekCommit(milliseconds);
    internal static Task TPlayerOpenStart(LPlayer player, string path) => player.LPlayerOpenStart(path);
    internal static bool TPlayerFilterApply(LPlayer player, string filter) => player.LPlayerFilterApply(filter);
    internal static void TPlayerEngineSet(LPlayer player, LPlayerSeam? seam) => player.LPlayerEngineSet(seam);
    internal static LPlayerSeam TPlayerSeamCreate(
        Action<string>? open = null,
        Action<string>? filter = null,
        Action<TimeSpan>? seek = null,
        Func<TimeSpan>? time = null,
        Func<bool>? ended = null,
        Action? dispose = null,
        Action<LPreviewApplication>? preview = null) => new(
            open ?? (_ => { }),
            () => { },
            () => { },
            () => { },
            seek ?? (_ => { }),
            _ => { },
            filter ?? (_ => { }),
            _ => { },
            preview ?? (_ => { }),
            () => { },
            time ?? (() => TimeSpan.Zero),
            ended ?? (() => false),
            (_, _) => { },
            dispose ?? (() => { }));
    internal static void TPlayerAppliedReset(LPlayer player) => player.LPlayerAppliedReset();
    internal static void TPlayerEndSet(LPlayer player, TimeSpan? end) => player.LPlayerEndSet(end);

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
        media.LViewerPlayerCreate += handler;
    internal static void TViewerHostAttach(LViewerRenderer renderer, Action handler) =>
        renderer.LViewerHostApply += handler;
    internal static Task TViewerLoadStart(LViewerMedia media, string path, TimeSpan position, bool? playing) =>
        media.LViewerLoadStart(new LViewerIntent(path, position, playing));
    internal static Task TViewerFlyleafApply(
        LViewerMedia media, string path, LMediaInfo? info, string? error, int serial) =>
        media.LViewerFlyleafApply(path, info, error, serial);
    internal static bool TViewerLoadCancel(LViewerMedia media) => media.LViewerLoadCancel();
    internal static bool TViewerMediaClose(LViewerMedia media, bool force) => media.LViewerMediaClose(force);
    internal static void TViewerCommandApply(LViewerMedia media, bool active) => media.LViewerCommandApply(active);
    internal static void TViewerClose(LViewerMedia media) => media.LViewerClose();
    internal static void TViewerClockAttach(LViewerPlayback playback, Action start, Action stop)
    {
        playback.LViewerClockStart += start;
        playback.LViewerClockStop += stop;
    }
    internal static void TViewerTickAttach(LViewerPlayback playback, Action<TimeSpan> handler) =>
        playback.LViewerClockTick += handler;
    internal static void TViewerLoupeAttach(LViewerPlayback playback, Action play, Action pause, Action<TimeSpan> seek)
    {
        playback.LViewerLoupePlay += play;
        playback.LViewerLoupePause += pause;
        playback.LViewerLoupeSeek += seek;
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
    internal static void TPlayerSeek(LPlayer player, TimeSpan position) => player.LPlayerSeek(position);
    internal static TimeSpan TPlayerTimeRead(LPlayer player) => player.LPlayerTimeRead();
    internal static bool TPlayerEndedRead(LPlayer player) => player.LPlayerEndedRead();
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
    internal static void TPlayerPreviewApply(LPlayer player, LPreviewState state) =>
        player.LPlayerPreviewApply(state, "test");
    internal static void TPlayerAudioApply(LPlayer player, string audio) => player.LPlayerAudioApply(audio);
}
