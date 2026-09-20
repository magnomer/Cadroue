using Cadroue.Application;
using Cadroue.Core;
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
}
