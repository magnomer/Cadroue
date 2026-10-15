using System.Windows.Controls;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static partial class TInterface
{
    internal static QProcessing TProcessingCreate() => new();
    internal static void TProcessingAttach(QProcessing processing, Action handler) =>
        processing.QProcessingChanged += handler;
    internal static bool TProcessingActiveCheck(QProcessing processing, string step) =>
        processing.QProcessingActiveCheck(step);
    internal static bool TProcessingEnabledCheck(QProcessing processing, string step) =>
        processing.QProcessingEnabledCheck(step);
    internal static void TProcessingOrderAttach(QProcessing processing, Action handler) =>
        processing.QProcessingOrderChanged += handler;
    internal static void TProcessingStepAttach(QProcessing processing, Action<string> handler) =>
        processing.QProcessingStepChanged += handler;
    internal static void TProcessingStepAdd(QProcessing processing, string step) => processing.QProcessingStepAdd(step);
    internal static void TProcessingOrderedSet(QProcessing processing, bool ordered) =>
        processing.QProcessingOrderedSet(ordered);
    internal static void TProcessingEnabledSet(QProcessing processing, string step, bool enabled) =>
        processing.QProcessingEnabledSet(step, enabled);
    internal static void TProcessingEnabledSet(QProcessing processing, string step, bool enabled, string? notice) =>
        processing.QProcessingEnabledSet(step, enabled, notice);
    internal static string? TProcessingNoticeRead(QProcessing processing, string step) =>
        processing.QProcessingNoticeRead(step);
    internal static bool TProcessingStepSelect(QProcessing processing, string step) =>
        processing.QProcessingStepSelect(step);
    internal static bool TProcessingStepMove(QProcessing processing, int delta) =>
        processing.QProcessingStepMove(delta);
    internal static bool TProcessingIndexMove(QProcessing processing, int from, int to) =>
        processing.QProcessingIndexMove(from, to);
    internal static void TProcessingDragStart(QProcessing processing, string step, double x, double y) =>
        processing.QProcessingDragStart(step, x, y);
    internal static void TProcessingDragMove(
        QProcessing processing,
        double x,
        double y,
        bool pressed,
        IReadOnlyList<double> tops,
        IReadOnlyList<double> heights) =>
        processing.QProcessingDragMove(x, y, pressed, 4, 4, tops, heights);
    internal static void TProcessingDragClear(QProcessing processing) => processing.QProcessingDragClear();
    internal static void TProcessingRowAdd(QProcessing processing, string key) =>
        processing.QProcessingRowAdd(new QProcessingRow(key, string.Empty, key));
    internal static double TProcessingOpacityRead(QProcessing processing, string step) =>
        processing.QProcessingOpacityRead(step);
    internal static string TProcessingNumberRead(QProcessing processing, string step) =>
        processing.QProcessingNumberRead(step);
    internal static void TProcessingKeyHandle(QProcessing processing, string step, bool activate) =>
        processing.QProcessingKeyHandle(step, activate);
    internal static void TProcessingCancelAttach(QProcessing processing, Action handler) =>
        processing.QProcessingDragCanceled += handler;
    internal static int TProcessingIndexResolve(double y, IReadOnlyList<double> tops, IReadOnlyList<double> heights) =>
        QProcessing.QProcessingIndexResolve(y, tops, heights);

    internal static QClinic TClinicCreate() => new();
    internal static void TClinicPlanAttach(QClinic clinic, Action handler) => clinic.QClinicPlanChanged += handler;
    internal static void TClinicStepSet(QClinic clinic, string? step) => clinic.QClinicStepSet(step);
    internal static void TClinicActiveSet(QClinic clinic, bool active) => clinic.QClinicActiveSet(active);
    internal static void TClinicSalvageSet(QClinic clinic, LWorkFixSalvage salvage) =>
        clinic.QClinicSalvageSet(salvage);
    internal static void TSalvageActiveSet(QClinic clinic, bool active) => clinic.QSalvageActiveSet(active);
    internal static void TSalvagePersistentSet(QClinic clinic, bool persistent) =>
        clinic.QSalvagePersistentSet(persistent);
    internal static void TSalvageModeSet(QClinic clinic, LSalvageMode mode) => clinic.QSalvageModeSet(mode);
    internal static void TSalvageBasisSet(QClinic clinic, LSalvageBasis basis) => clinic.QSalvageBasisSet(basis);
    internal static LWorkFixSalvage TClinicSalvageCreate(
        bool active, LSalvageMode mode, LSalvageBasis basis, bool persistent) =>
        new(active, mode, basis, persistent);
    internal static LWorkFix TClinicPlanRead(QClinic clinic) => clinic.QClinicPlanRead();
    internal static void TClinicPlanApply(QClinic clinic, LWorkFix plan) => clinic.QClinicPlanApply(plan);
    internal static bool TClinicRepairCheck(QClinic clinic) => clinic.QClinicRepairCheck();
    internal static void TClinicChangeAttach(QClinic clinic, Action handler) => clinic.QClinicChanged += handler;
    internal static void TClinicSourceSet(QClinic clinic, string? path) => clinic.QClinicSourceSet(path);
    internal static void TClinicResultSet(QClinic clinic, string path, LFlawKind kind, LCheckupOutcome outcome) =>
        clinic.QClinicResultSet(path, kind, new LCheckupResult(path, kind, outcome));
    internal static void TClinicProgressSet(QClinic clinic, string path, double value) =>
        clinic.QClinicProgressSet(path, value);
    internal static void TClinicResultsRemove(QClinic clinic, params string[] paths) =>
        clinic.QClinicResultsRemove(paths);
    internal static LCheckupOutcome TClinicOutcomeRead(QClinic clinic) => clinic.QClinicResultRead().LCheckupOutcome;
    internal static double TClinicProgressRead(QClinic clinic) => clinic.QClinicProgressRead();
    internal static string TClinicSimpleRead(QClinic clinic) => clinic.QClinicSimpleRead();
    internal static string TClinicTitleRead(QClinic clinic) => clinic.QClinicTitleRead();

    internal static void TCurvePressHandle(QCurve curve, double x, double y, double size) =>
        curve.QCurvePressHandle(x, y, size);
    internal static bool TCurveMoveHandle(QCurve curve, double x, double y, double size) =>
        curve.QCurveMoveHandle(x, y, size);
    internal static bool TCurveReleaseHandle(QCurve curve) => curve.QCurveReleaseHandle();
    internal static bool TCurveDragRead(QCurve curve) => curve.QCurveDragActive;
    internal static void TCurvePointCommit(QCurve curve, string input, string output) =>
        curve.QCurvePointCommit(input, output);
    internal static double TCurveInputRead(QCurve curve) => curve.QCurveInputPercent;
    internal static void TCurveHistogramSet(QCurve curve, LHistogramCounts? histogram) =>
        curve.QCurveHistogramSet(histogram);
    internal static IReadOnlyList<QCurveLine> TCurveGridRead(QCurve curve, double size) =>
        curve.QCurveCanvas.QCurveGridRead(size);
    internal static IReadOnlyList<QCurvePoint> TCurveTrackRead(QCurve curve, double size) =>
        curve.QCurveCanvas.QCurveTrackRead(size);
    internal static IReadOnlyList<QCurveDot> TCurveDotsRead(QCurve curve, double size) =>
        curve.QCurveCanvas.QCurveDotsRead(size);
    internal static IReadOnlyList<QCurvePoint> TCurveHistogramRead(QCurve curve, double size) =>
        curve.QCurveCanvas.QCurveHistogramRead(size);

    internal static void TWhitebalanceToolAttach(QWhitebalance whitebalance, Action<bool, LNeutralTarget> handler) =>
        whitebalance.QWhitebalanceToolChanged += handler;
    internal static void TWhitebalanceToolToggle(QWhitebalance whitebalance, LNeutralTarget target, bool state) =>
        whitebalance.QWhitebalanceToolToggle(target, state);
    internal static bool TWhitebalanceGreyRead(QWhitebalance whitebalance) => whitebalance.QWhitebalanceGreyArmed;
    internal static bool TWhitebalanceWhiteRead(QWhitebalance whitebalance) => whitebalance.QWhitebalanceWhiteArmed;
    internal static string TWhitebalanceGuideRead(QWhitebalance whitebalance) => whitebalance.QWhitebalanceGuideRead();
    internal static void TWhitebalanceStatusSet(QWhitebalance whitebalance, string status) =>
        whitebalance.QWhitebalanceStatusSet(status);
    internal static QWhitebalanceReadout TWhitebalanceReadoutRead(QWhitebalance whitebalance) =>
        whitebalance.QWhitebalanceReadoutRead();
    internal static void TWhitebalanceMethodSelect(QWhitebalance whitebalance, int index) =>
        whitebalance.QWhitebalanceMethodSelect(index);
    internal static int TWhitebalanceIndexRead(QWhitebalance whitebalance) => whitebalance.QWhitebalanceMethodIndex;
    internal static void TWhitebalanceWheelHandle(
        QWhitebalance whitebalance, bool pressed, double x, double y, double size) =>
        whitebalance.QWhitebalanceWheelHandle(pressed, x, y, size);
    internal static LNeutralDot TWhitebalanceDotRead(QWhitebalance whitebalance, double size, double dot) =>
        whitebalance.QWhitebalanceDotRead(size, dot);

    internal static void TBlankWheelHandle(QBlank blank, bool pressed, double x, double y, double size) =>
        blank.QBlankWheelHandle(pressed, x, y, size);
    internal static LNeutralDot TBlankDotRead(QBlank blank, double size, double dot) => blank.QBlankDotRead(size, dot);
    internal static LNeutralBitmap TNeutralBitmapResolve(int size, double value) =>
        LNeutral.LNeutralBitmapResolve(size, value);

    internal static IReadOnlyList<QSensorPlan> TSensorPlansRead(QSensor sensor) => sensor.QSensorPlans;
    internal static bool TSensorEnabledRead(QSensor sensor, LDetectorKind kind) => sensor.QSensorEnabledRead(kind);
    internal static double TSensorValueRead(QSensor sensor, LDetectorKind kind, int row) =>
        sensor.QSensorValueRead(kind, row);
    internal static double TSensorDefaultRead(QSensor sensor, LDetectorKind kind, int row) =>
        sensor.QSensorDefaultRead(kind, row);
    internal static string TSensorUnitRead(QSensor sensor, LDetectorKind kind, int row) =>
        sensor.QSensorUnitRead(kind, row);
    internal static void TSensorValueSet(QSensor sensor, LDetectorKind kind, int row, double value) =>
        sensor.QSensorValueSet(kind, row, value);
    internal static void TSensorModeSelect(QSensor sensor, int index) => sensor.QSensorModeSelect(index);
    internal static void TSensorSpeedSelect(QSensor sensor, int index) => sensor.QSensorSpeedSelect(index);
    internal static void TSensorMetricSelect(QSensor sensor, int index) => sensor.QSensorMetricSelect(index);
    internal static QInspectorChoice TSensorChoiceRead(QSensor sensor, LDetectorKind kind) =>
        sensor.QSensorPreset.QSensorChoiceRead(kind);
    internal static void TSensorChoiceSelect(QSensor sensor, LDetectorKind kind, int index) =>
        sensor.QSensorPreset.QSensorChoiceSelect(kind, index);
    internal static void TSensorRunningSet(QSensor sensor, bool running) => sensor.QSensorRunningSet(running);
    internal static void TSensorRunHandle(QSensor sensor) => sensor.QSensorRunHandle();
    internal static void TSensorRunAttach(QSensor sensor, Action run, Action stop)
    {
        sensor.QSensorRunApplied += run;
        sensor.QSensorStopApplied += stop;
    }

    internal static IReadOnlyList<QInspectorRow> TLoudnessRowsRead(QLoudness loudness, bool dynamic) =>
        loudness.QLoudnessRowsRead(dynamic);
    internal static double TLoudnessValueRead(QLoudness loudness, bool dynamic, int index) =>
        loudness.QLoudnessValueRead(dynamic, index);
    internal static double TLoudnessDefaultRead(QLoudness loudness, bool dynamic, int index) =>
        loudness.QLoudnessDefaultRead(dynamic, index);
    internal static void TLoudnessValueSet(QLoudness loudness, bool dynamic, int index, double value) =>
        loudness.QLoudnessValueSet(dynamic, index, value);
    internal static void TLoudnessModeSelect(QLoudness loudness, int index) => loudness.QLoudnessModeSelect(index);
    internal static QInspectorChoice TLoudnessChoiceRead(QLoudness loudness, bool dynamic) =>
        loudness.QLoudnessChoiceRead(dynamic);
    internal static void TLoudnessChoiceSelect(QLoudness loudness, bool dynamic, int index) =>
        loudness.QLoudnessChoiceSelect(dynamic, index);

    internal static double TNoiseValueRead(QNoise noise, int index) => noise.QNoiseValueRead(index);
    internal static double TNoiseDefaultRead(QNoise noise, int index) => noise.QNoiseDefaultRead(index);
    internal static void TNoiseTypeSelect(QNoise noise, int index) => noise.QNoiseTypeSelect(index);
    internal static QInspectorChoice TNoiseChoiceRead(QNoise noise) => noise.QNoiseChoiceRead();
    internal static void TNoiseChoiceSelect(QNoise noise, int index) => noise.QNoiseChoiceSelect(index);

    internal static QInspectorChoice TFilterChoiceRead(QFilter filter) => filter.QFilterChoiceRead();
    internal static void TFilterChoiceSelect(QFilter filter, int index) => filter.QFilterChoiceSelect(index);
    internal static void TFilterPolesSelect(QFilter filter, int index) => filter.QFilterPolesSelect(index);

    internal static void TEqualizerRowsAttach(QEqualizer equalizer, Action handler) =>
        equalizer.QEqualizerRowsChanged += handler;
    internal static string TEqualizerFrequencyCommit(QEqualizer equalizer, int index, string text) =>
        equalizer.QEqualizerFrequencyCommit(index, text);
    internal static string TEqualizerFrequencyRead(QEqualizer equalizer, int index) =>
        equalizer.QEqualizerFrequencyRead(index);
    internal static double TEqualizerGainRead(QEqualizer equalizer, int index) => equalizer.QEqualizerGainRead(index);
    internal static void TEqualizerGainSet(QEqualizer equalizer, int index, double gain) =>
        equalizer.QEqualizerGainSet(index, gain);
    internal static QInspectorChoice TEqualizerChoiceRead(QEqualizer equalizer) => equalizer.QEqualizerChoiceRead();
    internal static void TEqualizerChoiceSelect(QEqualizer equalizer, int index) =>
        equalizer.QEqualizerChoiceSelect(index);

    internal static LDetectorBound TDetectorThresholdRead(LDetectorKind kind) => LDetector.LDetectorThresholdRead(kind);

    internal static QInfo TInfoCreate(QViewer viewer) => new(viewer);
    internal static void TInfoAttach(QInfo info, Action handler) => info.QInfoChanged += handler;
    internal static void TInfoDetach(QInfo info) => info.QInfoDetach();
    internal static IReadOnlyList<LInfoRow> TInfoRowsRead(QInfo info) =>
        LInfoReport.LInfoReportRead(info.QInfoViewer.QViewerCargo, LMediaProbe.LMediaAvailabilityCurrent);
    internal static UserControl TInfoFaceAttach(QInfo info, UserControl surface) =>
        QInfoFace.QInfoFaceAttach(info, surface);

    internal static QSource TSourceCreate(QViewer viewer, bool audioAllowed) =>
        new(TAtelierCreate().CAtelierUsher, viewer, audioAllowed);
    internal static void TSourcePathAttach(QSource source, Action<string> handler) =>
        source.QSourcePathApplied += handler;
    internal static void TSourceRefuseAttach(QSource source, Action<string, string> handler) =>
        source.QSourceRefuse += handler;
    internal static string TSourceFilterRead(QSource source) => source.QSourceFilterRead();
    internal static bool TSourcePlaceholderCheck(string text) => QSource.QSourcePlaceholderCheck(text);
    internal static bool TSourceKeyRun(QSource source, string key, string text) => source.QSourceKeyRun(key, text);
    internal static bool TSourceOpen(QSource source, string path) => source.QSourceOpen(path);
    internal static void TSourceDialogOpen(QSource source, bool? confirmed, string path) =>
        source.QSourceDialogOpen(confirmed, path);

    internal static QSection TSectionCreate(QFlow flow) => new(flow);
    internal static void TSectionAttach(QSection section, Action change, Action select)
    {
        section.QSectionChanged += change;
        section.QSectionSelectApplied += select;
    }
    internal static void TSectionDragAttach(QSection section, Action<int> start, Action<int> end, Action<int, int> move)
    {
        section.QSectionDrag.QSectionDragStarted += start;
        section.QSectionDrag.QSectionDragEnd += end;
        section.QSectionDrag.QSectionRowMoved += move;
    }
    internal static void TSectionEnabledAttach(QSection section, Action<bool> handler) =>
        section.QSectionEnabledApplied += handler;
    internal static IReadOnlyList<QSectionRow> TSectionRowsRead(QSection section) => section.QSectionRowsRead();
    internal static string TSectionTitleRead(QSection section) => section.QSectionTitleRead();
    internal static bool TSectionPressHandle(QSection section, int index, int clicks, double x, double y) =>
        section.QSectionDrag.QSectionPressHandle(index, clicks, x, y, 0, 0);
    internal static bool TSectionMoveHandle(
        QSection section, double x, double y, double minimum, bool pressed, double[] tops, double[] heights) =>
        section.QSectionDrag.QSectionMoveHandle(x, y, minimum, minimum, pressed, tops, heights);
    internal static bool TSectionReleaseHandle(QSection section, bool shift, bool control) =>
        section.QSectionDrag.QSectionReleaseHandle(shift, control);
    internal static void TSectionLostHandle(QSection section) => section.QSectionDrag.QSectionLostHandle();
    internal static void TSectionToggleHandle(QSection section, int index) => section.QSectionToggleHandle(index);
    internal static bool TSectionSeekHandle(QSection section, int index, int clicks, bool end) =>
        section.QSectionSeekHandle(index, clicks, end);
    internal static bool TSectionRenameHandle(QSection section, int index, int clicks) =>
        section.QSectionRenameHandle(index, clicks);
    internal static void TSectionTextSet(QSection section, string name, string prefix, string suffix) =>
        section.QSectionTextSet(name, prefix, suffix);
    internal static void TSectionEditCommit(QSection section) => section.QSectionEditCommit();
    internal static void TSectionEditCancel(QSection section) => section.QSectionEditCancel();
    internal static bool TSectionKeyRun(QSection section, string key) => section.QSectionKeyRun(key);
    internal static void TSectionBlurHandle(QSection section, int serial, bool inside) =>
        section.QSectionBlurHandle(serial, inside);
    internal static bool TSectionStepCheck(string text) => QSection.QSectionStepCheck(text);
}
