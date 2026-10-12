using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static partial class TInterface
{
    internal static LProcessing TProcessingCreate() => new();
    internal static void TProcessingAttach(LProcessing processing, Action handler) =>
        processing.LProcessingChanged += handler;
    internal static bool TProcessingActiveCheck(LProcessing processing, string step) =>
        processing.LProcessingActiveCheck(step);
    internal static bool TProcessingEnabledCheck(LProcessing processing, string step) =>
        processing.LProcessingEnabledCheck(step);
    internal static void TProcessingOrderAttach(LProcessing processing, Action handler) =>
        processing.LProcessingOrderChanged += handler;
    internal static void TProcessingStepAttach(LProcessing processing, Action<string> handler) =>
        processing.LProcessingStepChanged += handler;
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
        processing.LProcessingDragCanceled += handler;
    internal static int TProcessingIndexResolve(double y, IReadOnlyList<double> tops, IReadOnlyList<double> heights) =>
        LProcessing.LProcessingIndexResolve(y, tops, heights);

    internal static LClinic TClinicCreate() => new();
    internal static void TClinicPlanAttach(LClinic clinic, Action handler) => clinic.LClinicPlanChanged += handler;
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
    internal static void TClinicChangeAttach(LClinic clinic, Action handler) => clinic.LClinicChanged += handler;
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
        whitebalance.LWhitebalanceToolChanged += handler;
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
        sensor.LSensorRunApplied += run;
        sensor.LSensorStopApplied += stop;
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
        equalizer.LEqualizerRowsChanged += handler;
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

    internal static LInfo TInfoCreate(LViewer viewer) => new(viewer);
    internal static void TInfoAttach(LInfo info, Action handler) => info.LInfoChanged += handler;
    internal static void TInfoDetach(LInfo info) => info.LInfoDetach();
    internal static IReadOnlyList<LInfoRow> TInfoRowsRead(LInfo info) => info.LInfoRowsRead();

    internal static LSource TSourceCreate(LViewer viewer, bool audioAllowed) =>
        new(TAtelierCreate().CAtelierUsher, viewer, audioAllowed);
    internal static void TSourcePathAttach(LSource source, Action<string> handler) =>
        source.LSourcePathApplied += handler;
    internal static void TSourceRefuseAttach(LSource source, Action<string, string> handler) =>
        source.LSourceRefuse += handler;
    internal static string TSourceFilterRead(LSource source) => source.LSourceFilterRead();
    internal static bool TSourcePlaceholderCheck(string text) => LSource.LSourcePlaceholderCheck(text);
    internal static bool TSourceKeyRun(LSource source, string key, string text) => source.LSourceKeyRun(key, text);
    internal static bool TSourceOpen(LSource source, string path) => source.LSourceOpen(path);
    internal static void TSourceDialogOpen(LSource source, bool? confirmed, string path) =>
        source.LSourceDialogOpen(confirmed, path);

    internal static LSection TSectionCreate(LFlow flow) => new(flow);
    internal static void TSectionAttach(LSection section, Action change, Action select)
    {
        section.LSectionChanged += change;
        section.LSectionSelectApplied += select;
    }
    internal static void TSectionDragAttach(LSection section, Action<int> start, Action<int> end, Action<int, int> move)
    {
        section.LSectionDrag.LSectionDragStarted += start;
        section.LSectionDrag.LSectionDragEnd += end;
        section.LSectionDrag.LSectionRowMoved += move;
    }
    internal static void TSectionEnabledAttach(LSection section, Action<bool> handler) =>
        section.LSectionEnabledApplied += handler;
    internal static IReadOnlyList<LSectionRow> TSectionRowsRead(LSection section) => section.LSectionRowsRead();
    internal static string TSectionTitleRead(LSection section) => section.LSectionTitleRead();
    internal static bool TSectionPressHandle(LSection section, int index, int clicks, double x, double y) =>
        section.LSectionDrag.LSectionPressHandle(index, clicks, x, y, 0, 0);
    internal static bool TSectionMoveHandle(
        LSection section, double x, double y, double minimum, bool pressed, double[] tops, double[] heights) =>
        section.LSectionDrag.LSectionMoveHandle(x, y, minimum, minimum, pressed, tops, heights);
    internal static bool TSectionReleaseHandle(LSection section, bool shift, bool control) =>
        section.LSectionDrag.LSectionReleaseHandle(shift, control);
    internal static void TSectionLostHandle(LSection section) => section.LSectionDrag.LSectionLostHandle();
    internal static void TSectionToggleHandle(LSection section, int index) => section.LSectionToggleHandle(index);
    internal static bool TSectionSeekHandle(LSection section, int index, int clicks, bool end) =>
        section.LSectionSeekHandle(index, clicks, end);
    internal static bool TSectionRenameHandle(LSection section, int index, int clicks) =>
        section.LSectionRenameHandle(index, clicks);
    internal static void TSectionTextSet(LSection section, string name, string prefix, string suffix) =>
        section.LSectionTextSet(name, prefix, suffix);
    internal static void TSectionEditCommit(LSection section) => section.LSectionEditCommit();
    internal static void TSectionEditCancel(LSection section) => section.LSectionEditCancel();
    internal static bool TSectionKeyRun(LSection section, string key) => section.LSectionKeyRun(key);
    internal static void TSectionBlurHandle(LSection section, int serial, bool inside) =>
        section.LSectionBlurHandle(serial, inside);
    internal static bool TSectionStepCheck(string text) => LSection.LSectionStepCheck(text);
}
