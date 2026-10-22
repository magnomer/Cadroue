using System.Windows.Controls;

using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Media;
using Cadroue.ShellEngine;
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
        processing.QProcessingDrag.QProcessingDragStart(step, x, y);
    internal static void TProcessingDragMove(
        QProcessing processing,
        double x,
        double y,
        bool pressed,
        IReadOnlyList<double> tops,
        IReadOnlyList<double> heights) =>
        processing.QProcessingDrag.QProcessingDragMove(x, y, pressed, 4, 4, tops, heights);
    internal static void TProcessingDragClear(QProcessing processing) =>
        processing.QProcessingDrag.QProcessingDragClear();
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

    internal static void TClinicStepSet(CClinic clinic, string? step) => clinic.CClinicStepSet(step);
    internal static LWorkFixSalvage TClinicSalvageCreate(
        bool active, LSalvageMode mode, LSalvageBasis basis, bool persistent) =>
        new(active, mode, basis, persistent);
    internal static LWorkFix TClinicPlanRead(CClinic clinic)
    {
        LClinicOutlet plan = new();
        clinic.CClinicPlanApply(plan);
        return plan.LClinicPlan;
    }

    internal static void TClinicPlanApply(CClinic clinic, LWorkFix plan) =>
        clinic.CClinicPlanAccept(new LClinicOutlet(plan));
    internal static void TClinicSourceSet(CClinic clinic, string? path) => clinic.CClinicSourceSet(path);
    internal static LCheckupOutcome TClinicOutcomeRead(CClinic clinic) =>
        Enum.Parse<LCheckupOutcome>(clinic.CClinicResultFormat(
        [
            nameof(LCheckupOutcome.LCheckupOutcomeUntested),
            nameof(LCheckupOutcome.LCheckupOutcomeScanning),
            nameof(LCheckupOutcome.LCheckupOutcomeClean),
            nameof(LCheckupOutcome.LCheckupOutcomeFailed),
            string.Empty,
            string.Empty,
            string.Empty
        ]));
    internal static double TClinicProgressRead(CClinic clinic) => clinic.CClinicProgressRead();

    internal static void TCurvePressHandle(QCurve curve, double x, double y, double size) =>
        curve.QCurveDrag.QCurvePressHandle(x, y, size);
    internal static bool TCurveMoveHandle(QCurve curve, double x, double y, double size) =>
        curve.QCurveDrag.QCurveMoveHandle(x, y, size);
    internal static bool TCurveReleaseHandle(QCurve curve) => curve.QCurveDrag.QCurveReleaseHandle();
    internal static bool TCurveDragRead(QCurve curve) => curve.QCurveDrag.QCurveDragActive;
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

    internal static void TWhitebalanceToolAttach(CWhitebalance whitebalance, Action<bool, LNeutralTarget> handler) =>
        whitebalance.CWhitebalanceToolChanged += (armed, white) =>
            handler(armed, white ? LNeutralTarget.LNeutralTargetWhite : LNeutralTarget.LNeutralTargetGrey);
    internal static void TWhitebalanceToolToggle(CWhitebalance whitebalance, LNeutralTarget target, bool state) =>
        whitebalance.CWhitebalanceToolToggle(target == LNeutralTarget.LNeutralTargetWhite, state);
    internal static bool TWhitebalanceGreyRead(CWhitebalance whitebalance) => whitebalance.CWhitebalanceGreyArmed;
    internal static bool TWhitebalanceWhiteRead(CWhitebalance whitebalance) => whitebalance.CWhitebalanceWhiteArmed;
    internal static string TWhitebalanceGuideRead(CWhitebalance whitebalance) => whitebalance.CWhitebalanceGuideShown
        ? QLocalizationCatalog.QLocalizationTextRead(whitebalance.CWhitebalanceGuideKey)
        : string.Empty;
    internal static void TWhitebalanceStatusSet(CWhitebalance whitebalance, string status) =>
        whitebalance.CWhitebalanceStatusSet(status);
    internal static (bool, int, int, int) TWhitebalanceReadoutRead(CWhitebalance whitebalance) =>
        whitebalance.CWhitebalanceReadoutRead();
    internal static void TWhitebalanceMethodSelect(CWhitebalance whitebalance, int index) =>
        whitebalance.CWhitebalanceMethodSelect(index);
    internal static int TWhitebalanceIndexRead(CWhitebalance whitebalance) => whitebalance.CWhitebalanceMethodIndex;
    internal static void TWhitebalanceWheelHandle(
        CWhitebalance whitebalance, bool pressed, double x, double y, double size) =>
        whitebalance.CWhitebalanceWheelHandle(pressed, x, y, size);
    internal static LNeutralDot TWhitebalanceDotRead(CWhitebalance whitebalance, double size, double dot)
    {
        (double left, double top, bool present) = whitebalance.CWhitebalanceDotRead(size, dot);
        return new LNeutralDot(left, top, present);
    }

    internal static void TBlankWheelHandle(QBlank blank, bool pressed, double x, double y, double size) =>
        blank.QBlankWheelHandle(pressed, x, y, size);
    internal static LNeutralDot TBlankDotRead(QBlank blank, double size, double dot) => blank.QBlankDotRead(size, dot);
    internal static LNeutralBitmap TNeutralBitmapResolve(int size, double value) =>
        LNeutralCanvas.LNeutralBitmapResolve(size, value);

    internal static IReadOnlyList<QSensorPlan> TSensorPlansRead(QSensor sensor) => sensor.QSensorPlans;
    internal static bool TSensorEnabledRead(QSensor sensor, LDetectorKind kind) =>
        sensor.CDetector.CDetectorEnabledRead(kind);
    internal static double TSensorValueRead(QSensor sensor, LDetectorKind kind, int row) =>
        sensor.CDetector.CDetectorValueRead(kind, row);
    internal static double TSensorDefaultRead(QSensor sensor, LDetectorKind kind, int row) =>
        sensor.CDetector.CDetectorDefaultRead(kind, row);
    internal static string TSensorUnitRead(QSensor sensor, LDetectorKind kind, int row) =>
        sensor.CDetector.CDetectorUnitRead(kind, row);
    internal static void TSensorValueSet(QSensor sensor, LDetectorKind kind, int row, double value) =>
        sensor.CDetector.CDetectorValueSet(kind, row, value);
    internal static void TSensorModeSelect(QSensor sensor, int index) => sensor.CDetector.CDetectorModeSelect(index);
    internal static void TSensorSpeedSelect(QSensor sensor, int index) => sensor.CDetector.CDetectorSpeedSelect(index);
    internal static void TSensorMetricSelect(QSensor sensor, int index) =>
        sensor.CDetector.CDetectorMetricSelect(index);
    internal static QInspectorChoice TSensorChoiceRead(QSensor sensor, LDetectorKind kind) =>
        sensor.QSensorPreset.QSensorChoiceRead(kind);
    internal static void TSensorChoiceSelect(QSensor sensor, LDetectorKind kind, int index) =>
        sensor.CDetector.CDetectorPreset.CDetectorChoiceSelect(kind, index);
    internal static IReadOnlyList<QInspectorRow> TLoudnessRowsRead(CLoudness loudness, bool dynamic) =>
        new QLoudness(loudness).QLoudnessRowsRead(dynamic);
    internal static double TLoudnessValueRead(CLoudness loudness, bool dynamic, int index) =>
        loudness.CLoudnessValueRead(dynamic, index);
    internal static double TLoudnessDefaultRead(CLoudness loudness, bool dynamic, int index) =>
        loudness.CLoudnessDefaultRead(dynamic, index);
    internal static void TLoudnessValueSet(CLoudness loudness, bool dynamic, int index, double value) =>
        loudness.CLoudnessValueSet(dynamic, index, value);
    internal static void TLoudnessModeSelect(CLoudness loudness, int index) => loudness.CLoudnessModeSelect(index);
    internal static QInspectorChoice TLoudnessChoiceRead(CLoudness loudness, bool dynamic) =>
        new QLoudness(loudness).QLoudnessChoiceRead(dynamic);
    internal static void TLoudnessChoiceSelect(CLoudness loudness, bool dynamic, int index) =>
        loudness.CLoudnessChoiceSelect(dynamic, index);
    internal static int TLoudnessModeRead(CLoudness loudness) =>
        loudness.CLoudnessModes.ToList().FindIndex(row => row.CChoiceChosen);
    internal static IReadOnlyList<string> TLoudnessNamesRead(CLoudness loudness) =>
        new QLoudness(loudness).QLoudnessModeNames;

    internal static double TNoiseValueRead(CNoise noise, int index) => noise.CNoiseValueRead(index);
    internal static double TNoiseDefaultRead(CNoise noise, int index) => noise.CNoiseDefaultRead(index);
    internal static void TNoiseTypeSelect(CNoise noise, int index) => noise.CNoiseTypeSelect(index);
    internal static QInspectorChoice TNoiseChoiceRead(CNoise noise) => new QNoise(noise).QNoiseChoiceRead();
    internal static void TNoiseChoiceSelect(CNoise noise, int index) => noise.CNoiseChoiceSelect(index);

    internal static IReadOnlyList<string> TFilterNamesRead(CFilter filter) =>
        new QFilter(filter).QFilterChoices.Select(QChoice.QChoiceTextRead).ToList();
    internal static int TFilterChosenRead(CFilter filter) =>
        filter.CFilterChoices.ToList().FindIndex(row => row.CChoiceChosen);
    internal static int TFilterPoleRead(CFilter filter) =>
        filter.CFilterPoleChoices.ToList().FindIndex(row => row.CChoiceChosen);
    internal static void TFilterChoiceSelect(CFilter filter, int index) => filter.CFilterChoiceSelect(index);
    internal static void TFilterPolesSelect(CFilter filter, int index) => filter.CFilterPolesSelect(index);

    internal static void TEqualizerRowsAttach(CEqualizer equalizer, Action handler) =>
        new QEqualizer(equalizer).QEqualizerRowsChanged += handler;
    internal static IReadOnlyList<QEqualizerBand> TEqualizerRowsRead(CEqualizer equalizer) =>
        new QEqualizer(equalizer).QEqualizerRows;
    internal static string TEqualizerFrequencyCommit(CEqualizer equalizer, int index, string text) =>
        new QEqualizer(equalizer).QEqualizerFrequencyCommit(index, text);
    internal static string TEqualizerFrequencyRead(CEqualizer equalizer, int index) =>
        new QEqualizer(equalizer).QEqualizerFrequencyRead(index);
    internal static double TEqualizerGainRead(CEqualizer equalizer, int index) => equalizer.CEqualizerGainRead(index);
    internal static void TEqualizerGainSet(CEqualizer equalizer, int index, double gain) =>
        new QEqualizer(equalizer).QEqualizerGainSet(index, gain);
    internal static QInspectorChoice TEqualizerChoiceRead(CEqualizer equalizer) =>
        new QEqualizer(equalizer).QEqualizerChoiceRead();
    internal static void TEqualizerChoiceSelect(CEqualizer equalizer, int index) =>
        new QEqualizer(equalizer).QEqualizerChoiceSelect(index);

    internal static LDetectorBound TDetectorThresholdRead(LDetectorKind kind) => LDetector.LDetectorThresholdRead(kind);

    internal static QInfo TInfoCreate(QViewer viewer) => new(viewer, TAtelierCreate().CAtelierInfo);
    internal static void TInfoAttach(QInfo info, Action handler) => info.QInfoChanged += handler;
    internal static void TInfoDetach(QInfo info) => info.QInfoDetach();
    internal static IReadOnlyList<LInfoRow> TInfoRowsRead(QInfo info) =>
        LInfoReport.LInfoReportRead(info.QInfoViewer.QViewerCargo, LMediaAvailability.LMediaAvailabilityCurrent);
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
        section.QSectionEdit.QSectionTextSet(name, prefix, suffix);
    internal static int? TSectionEditRead(QSection section) =>
        section.QFlow.QFlowSection.QFlowSectionGate.CSectionList.CSectionEdit.CSectionEditIndex;
    internal static int? TSectionDragRead(QSection section) =>
        section.QFlow.QFlowSection.QFlowSectionGate.CSectionList.CSectionDrag.CSectionDragIndex;
    internal static bool TSectionKeyRun(QSection section, string key) => section.QSectionEdit.QSectionKeyRun(key);
    internal static void TSectionBlurHandle(QSection section, int serial, bool inside) =>
        section.QSectionEdit.QSectionBlurHandle(serial, inside);
    internal static bool TSectionStepCheck(string text) => QSection.QSectionStepCheck(text);
}
