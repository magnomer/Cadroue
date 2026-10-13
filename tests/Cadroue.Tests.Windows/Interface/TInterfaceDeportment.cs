using System.Runtime.CompilerServices;

using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static partial class TInterface
{
    [ModuleInitializer]
    internal static void TInterfaceHomeStart()
    {
        RuntimeHelpers.RunModuleConstructor(typeof(Interface.TInterface).Module.ModuleHandle);
        _ = TProgramCreate();
    }

    internal static LWorkGammaSettings TGammaSettingsCreate(
        double global, double red, double green, double blue, double highlight) =>
        new(global, red, green, blue, highlight);
    internal static LWorkCurveSettings TCurveSettingsRead(LWorkVideoStep step) => step.LWorkCurveRead();

    internal static QTone TToneCreate() => new();
    internal static void TToneAttach(QTone tone, Action handler) => tone.QToneChanged += handler;
    internal static void TToneValueSet(QTone tone, LColorKind kind, double value) => tone.QToneValueSet(kind, value);
    internal static void TToneActiveSet(QTone tone, LColorKind kind, bool active) => tone.QToneActiveSet(kind, active);
    internal static void TToneCapableSet(QTone tone, bool capable) => tone.QToneCapableSet(capable);
    internal static LWorkVideoStep TToneStepRead(QTone tone, LColorKind kind) => tone.QToneStepRead(kind);

    internal static QGamma TGammaCreate() => new();
    internal static void TGammaAttach(QGamma gamma, Action handler) => gamma.QGammaChanged += handler;
    internal static void TGammaValueSet(QGamma gamma, int index, double value) => gamma.QGammaValueSet(index, value);
    internal static void TGammaReset(QGamma gamma) => gamma.QGammaReset();
    internal static void TGammaCapableSet(QGamma gamma, bool capable, bool preview) =>
        gamma.QGammaCapableSet(capable, preview);
    internal static LWorkGammaSettings TGammaValueRead(QGamma gamma) => gamma.QGammaValue;
    internal static bool TGammaPreviewRead(QGamma gamma) => gamma.QGammaPreview;

    internal static QExposure TExposureCreate() => new();
    internal static void TExposureAttach(QExposure exposure, Action handler) => exposure.QExposureChanged += handler;
    internal static void TExposureValueSet(QExposure exposure, double value) => exposure.QExposureValueSet(value);
    internal static LWorkVideoStep TExposureStepRead(QExposure exposure) => exposure.QExposureStep;

    internal static QCurve TCurveCreate() => new();
    internal static void TCurveAttach(QCurve curve, Action handler) => curve.QCurveChanged += handler;
    internal static void TCurveChannelSelect(QCurve curve, int channel) => curve.QCurveChannelSelect(channel);
    internal static void TCurvePointAdd(QCurve curve, double input, double output) =>
        curve.QCurvePointAdd(input, output);
    internal static void TCurvePointSet(QCurve curve, double input, double output) =>
        curve.QCurvePointSet(input, output);
    internal static void TCurvePointDelete(QCurve curve) => curve.QCurvePointDelete();
    internal static void TCurveChannelReset(QCurve curve) => curve.QCurveChannelReset();
    internal static int TCurveChannelRead(QCurve curve) => curve.QCurveChannel;
    internal static int TCurveSelectedRead(QCurve curve) => curve.QCurveSelected;
    internal static IReadOnlyList<LWorkCurvePoint> TCurvePointsRead(QCurve curve) => curve.QCurvePoints;
    internal static LWorkVideoStep TCurveStepRead(QCurve curve) => curve.QCurveStepRead();
    internal static void TCurveStepSet(QCurve curve, LWorkVideoStep step) => curve.QCurveStepSet(step);
    internal static void TCurvePointSelect(QCurve curve, int index) => curve.QCurvePointSelect(index);

    internal static QBlank TBlankCreate() => new();
    internal static void TBlankStepSet(QBlank blank, LDetectorBlank step) => blank.QBlankStepSet(step);
    internal static void TBlankTypeSet(QBlank blank, LDetectorType type) => blank.QBlankTypeSet(type);
    internal static void TBlankWheelSet(QBlank blank, double x, double y) => blank.QBlankWheelSet(x, y);
    internal static void TBlankSampleSet(QBlank blank, int red, int green, int blue) =>
        blank.QBlankSampleSet(red, green, blue);
    internal static bool TBlankPresentRead(QBlank blank) => blank.QBlankWheelPresent;
    internal static LDetectorBlank TBlankStepRead(QBlank blank) => blank.QBlankStep;
    internal static LDetectorBlank TBlankStepCreate(LDetectorType type) =>
        LDetectorBlank.LDetectorBlankCreate() with { LDetectorBlankType = type };

    internal static QWhitebalance TWhitebalanceCreate() => new();
    internal static void TWhitebalanceAttach(QWhitebalance whitebalance, Action handler) =>
        whitebalance.QWhitebalanceChanged += handler;
    internal static void TWhitebalanceEstimateAttach(QWhitebalance whitebalance, Action<LWhitebalanceMethod> handler) =>
        whitebalance.QWhitebalanceEstimateChanged += handler;
    internal static void TWhitebalanceSampleSet(QWhitebalance whitebalance, LNeutralSample sample) =>
        whitebalance.QWhitebalanceSampleSet(sample);
    internal static void TWhitebalanceMethodSet(QWhitebalance whitebalance, LWhitebalanceMethod method) =>
        whitebalance.QWhitebalanceMethodSet(method);
    internal static void TWhitebalanceReset(QWhitebalance whitebalance) => whitebalance.QWhitebalanceReset();
    internal static void TWhitebalanceToolSet(QWhitebalance whitebalance, bool armed, LNeutralTarget target) =>
        whitebalance.QWhitebalanceToolSet(armed, target);
    internal static void TWhitebalanceCapableSet(QWhitebalance whitebalance, bool capable, bool preview) =>
        whitebalance.QWhitebalanceCapableSet(capable, preview);
    internal static LWorkWhitebalanceSettings TWhitebalanceValueRead(QWhitebalance whitebalance) =>
        whitebalance.QWhitebalanceValue;
    internal static LWorkVideoStep TWhitebalanceStepRead(QWhitebalance whitebalance) => whitebalance.QWhitebalanceStep;
    internal static bool TWhitebalanceManualRead(QWhitebalance whitebalance) => whitebalance.QWhitebalanceManual;
    internal static bool TWhitebalanceToolRead(QWhitebalance whitebalance) => whitebalance.QWhitebalanceToolArmed;
    internal static LNeutralWheel TWhitebalanceWheelRead(QWhitebalance whitebalance) =>
        whitebalance.QWhitebalanceWheelRead();
    internal static LNeutralSample TNeutralSampleCreate(
        int red, int green, int blue, double redGain, double greenGain, double blueGain) =>
        new(LNeutralOutcome.LNeutralOutcomeResolved, red, green, blue, redGain, greenGain, blueGain);

    internal static QNoise TNoiseCreate() => new();
    internal static void TNoiseAttach(QNoise noise, Action handler) => noise.QNoiseChanged += handler;
    internal static void TNoisePresetSelect(QNoise noise, string token) => noise.QNoisePresetSelect(token);
    internal static void TNoiseValueSet(QNoise noise, int index, double value) => noise.QNoiseValueSet(index, value);
    internal static void TNoiseStepSet(QNoise noise, LWorkAudioStep step) => noise.QNoiseStepSet(step);
    internal static string? TNoiseTokenRead(QNoise noise) => noise.QNoiseToken;
    internal static string? TNoiseMatchRead(QNoise noise) => noise.QNoiseMatchRead();
    internal static LWorkNoiseStep TNoiseStepRead(QNoise noise) => noise.QNoiseStep;

    internal static QSensor TSensorCreate() => new();
    internal static IReadOnlyList<string> TDetectorTokensRead(LDetectorKind kind) =>
        LDetectorCatalog.LDetectorTokensRead(kind);
    internal static void TSensorPresetSelect(QSensor sensor, LDetectorKind kind, string token) =>
        sensor.QSensorPreset.QSensorPresetSelect(kind, token);
    internal static void TSensorTokenSet(QSensor sensor, LDetectorKind kind, string token) =>
        sensor.QSensorPreset.QSensorTokenSet(kind, token);
    internal static void TSensorStepSet(QSensor sensor, LDetectorStep step) => sensor.QSensorStepSet(step);
    internal static void TSensorThresholdSet(QSensor sensor, LDetectorKind kind, double threshold) =>
        sensor.QSensorThresholdSet(kind, threshold);
    internal static void TSensorMetricSet(QSensor sensor, LDetectorMetricMode metric) =>
        sensor.QSensorMetricSet(metric);
    internal static string? TSensorTokenRead(QSensor sensor, LDetectorKind kind) =>
        sensor.QSensorPreset.QSensorTokenRead(kind);
    internal static string? TSensorMatchRead(QSensor sensor, LDetectorKind kind) =>
        sensor.QSensorPreset.QSensorMatchRead(kind);
    internal static LDetectorStep TSensorStepRead(QSensor sensor, LDetectorKind kind) => sensor.QSensorStepRead(kind);

    internal static QLoudness TLoudnessCreate() => new();
    internal static void TLoudnessAttach(QLoudness loudness, Action handler) => loudness.QLoudnessChanged += handler;
    internal static void TLoudnessPresetSelect(QLoudness loudness, string token) =>
        loudness.QLoudnessPresetSelect(token);
    internal static void TLoudnessModeSet(QLoudness loudness, LLeveling mode) => loudness.QLoudnessModeSet(mode);
    internal static void TLoudnessValueSet(QLoudness loudness, int index, double value) =>
        loudness.QLoudnessValueSet(index, value);
    internal static void TLoudnessDynamicSet(QLoudness loudness, int index, double value) =>
        loudness.QLoudnessDynamicSet(index, value);
    internal static string? TLoudnessTokenRead(QLoudness loudness) => loudness.QLoudnessToken;
    internal static string? TLoudnessMatchRead(QLoudness loudness) => loudness.QLoudnessMatchRead();
    internal static LWorkNormalizeStep TLoudnessStepRead(QLoudness loudness) => loudness.QLoudnessStep;

    internal static QFilter TFilterCreate(bool high) => new(high);
    internal static void TFilterAttach(QFilter filter, Action handler) => filter.QFilterChanged += handler;
    internal static void TFilterPresetSelect(QFilter filter, string token) => filter.QFilterPresetSelect(token);
    internal static void TFilterFrequencySet(QFilter filter, double frequency) => filter.QFilterFrequencySet(frequency);
    internal static string? TFilterTokenRead(QFilter filter) => filter.QFilterToken;
    internal static string? TFilterMatchRead(QFilter filter) => filter.QFilterMatchRead();
    internal static LWorkPassStep TFilterStepRead(QFilter filter) => filter.QFilterStep;

    internal static QEqualizer TEqualizerCreate() => new();
    internal static void TEqualizerAttach(QEqualizer equalizer, Action handler) =>
        equalizer.QEqualizerChanged += handler;
    internal static void TEqualizerPresetSelect(QEqualizer equalizer, string token) =>
        equalizer.QEqualizerPresetSelect(token);
    internal static void TEqualizerBandSet(QEqualizer equalizer, int index, double frequency, double gain) =>
        equalizer.QEqualizerBandSet(index, frequency, gain);
    internal static void TEqualizerBandAdd(QEqualizer equalizer) => equalizer.QEqualizerBandAdd();
    internal static void TEqualizerBandRemove(QEqualizer equalizer, int index) => equalizer.QEqualizerBandRemove(index);
    internal static string? TEqualizerTokenRead(QEqualizer equalizer) => equalizer.QEqualizerToken;
    internal static string? TEqualizerMatchRead(QEqualizer equalizer) => equalizer.QEqualizerMatchRead();
    internal static IReadOnlyList<LWorkBand> TEqualizerBandsRead(QEqualizer equalizer) => equalizer.QEqualizerBands;
    internal static LWorkAudioStep TEqualizerStepRead(QEqualizer equalizer) => equalizer.QEqualizerStepRead();

    internal static QVolume TVolumeCreate() => new();
    internal static void TVolumeAttach(QVolume volume, Action handler) => volume.QVolumeChanged += handler;
    internal static void TVolumeGainSet(QVolume volume, double gain) => volume.QVolumeGainSet(gain);
    internal static void TVolumeActiveSet(QVolume volume, bool active) => volume.QVolumeActiveSet(active);
    internal static double TVolumeGainRead(QVolume volume) => volume.QVolumeGain;

    internal static QSkip TSkipCreate() => new();
    internal static void TSkipAttach(QSkip skip, Action handler) => skip.QSkipChanged += handler;
    internal static void TSkipActiveSet(QSkip skip, bool active) => skip.QSkipActiveSet(active);
    internal static void TSkipPersistentSet(QSkip skip, bool persistent) => skip.QSkipPersistentSet(persistent);

    internal static QSLoupe TLoupeCreate() =>
        TInterfaceViewer.TViewerCreate().QViewerMedia.QViewerLoupe.QViewerLoupeCreate();
    internal static void TLoupePlayingAttach(QSLoupe loupe, Action<bool> handler) =>
        loupe.QSLoupePlayingChanged += handler;
    internal static void TLoupePlayingSet(QSLoupe loupe, bool playing) => loupe.QSLoupePlayingSet(playing);
    internal static void TLoupeEndSet(QSLoupe loupe, bool ended) => loupe.QSLoupeEndSet(ended);
    internal static void TLoupeClose(QSLoupe loupe) => loupe.QSLoupeClose();
    internal static bool TLoupeResumeCheck(QSLoupe loupe) => loupe.QSLoupeResumeCheck();
    internal static QSLoupe TLoupeViewerCreate(QViewer viewer)
    {
        var loupe = viewer.QViewerMedia.QViewerLoupe.QViewerLoupeCreate();
        loupe.QSLoupeViewerAttach();
        return loupe;
    }
    internal static void TLoupeDetach(QSLoupe loupe) => loupe.QSLoupeDetach();
    internal static Task TLoupeStart(QSLoupe loupe) => loupe.QSLoupeStart();
    internal static void TLoupePlayToggle(QSLoupe loupe) => loupe.QSLoupePlayToggle();
    internal static void TLoupeSeek(QSLoupe loupe, TimeSpan position) => loupe.QSLoupeSeek(position);
    internal static void TLoupeTick(QSLoupe loupe) => loupe.QSLoupeTick();
    internal static void TLoupeFloatSet(QSLoupe loupe, QSLoupeFloat mode) => loupe.QSLoupeFloatSet(mode);
    internal static void TLoupeFloatRestore(QSLoupe loupe) => loupe.QSLoupeFloatRestore();
    internal static IReadOnlyList<QSLoupeButton> TLoupeButtonsRead(QSLoupe loupe) => loupe.QSLoupeButtonsRead();
    internal static void TLoupeFloatAttach(QSLoupe loupe, Action handler) => loupe.QSLoupeFloatChanged += handler;
    internal static void TLoupeEngineAttach(QSLoupe loupe, Action handler) => loupe.QSLoupeEngineCreated += handler;
    internal static void TLoupeCloseAttach(QSLoupe loupe, Action handler) => loupe.QSLoupeCloseApplied += handler;

    internal static QGroup TGroupCreate(LGroupSelection selection) => new(TAtelierCreate().CAtelierUsher, selection);
    internal static void TGroupAttach(QGroup group, Action handler) => group.QGroupChanged += handler;
    internal static bool TGroupAdd(QGroup group, IReadOnlyList<string> paths, string name) =>
        group.QGroupAdd(paths, name);
    internal static bool TGroupPathsInsert(QGroup group, int index, IReadOnlyList<string> paths, int insertAt) =>
        group.QGroupPathsInsert(index, paths, insertAt);
    internal static void TGroupRemove(QGroup group, int index) => group.QGroupRemove(index);
    internal static void TGroupEditStart(QGroup group, int index) => group.QGroupName.QGroupEditStart(index);
    internal static void TGroupEditCancel(QGroup group) => group.QGroupName.QGroupEditCancel();
    internal static bool TGroupNameCommit(QGroup group, string name) => group.QGroupName.QGroupNameCommit(name);
    internal static bool TGroupNameSet(QGroup group, int index, string name) =>
        group.QGroupName.QGroupNameSet(index, name);
    internal static void TGroupFaceAttach(QGroup group, Action handler) => group.QGroupFaceChanged += handler;
    internal static void TGroupRequestAttach(QGroup group, Action<IReadOnlyList<string>> handler) =>
        group.QGroupDrag.QGroupPathsRequest += handler;
    internal static void TGroupOpenAttach(QGroup group, Action<string> handler) =>
        group.QGroupDrag.QGroupItemOpened += handler;
    internal static void TGroupSourceAttach(QGroup group, Func<IReadOnlyList<string>> source) =>
        group.QGroupSourceAttach(source);
    internal static void TGroupAutoUpdate(QGroup group) => group.QGroupAutoUpdate();
    internal static IReadOnlyList<QGroupCard> TGroupCardsRead(QGroup group) => group.QGroupFace.QGroupCardsRead();
    internal static QGroupSwitch TGroupModeRead(QGroup group) => group.QGroupFace.QGroupModeRead();
    internal static IReadOnlyList<QGroupSwitch> TGroupSwitchesRead(QGroup group) =>
        group.QGroupFace.QGroupSwitchesRead();
    internal static void TGroupToggleRun(QGroup group, string key, bool right) =>
        group.QGroupFace.QGroupToggleRun(key, right);
    internal static bool TGroupLabelHandle(QGroup group, int index, int clicks) =>
        group.QGroupName.QGroupLabelHandle(index, clicks);
    internal static bool TGroupKeyRun(QGroup group, string key, string text) =>
        group.QGroupName.QGroupKeyRun(key, text);
    internal static bool TGroupCardAccept(
        QGroup group, int target, int insertAt, string[]? files, int? moveIndex, string? movePath, string[]? paths) =>
        group.QGroupDrag.QGroupCardAccept(target, insertAt, files, moveIndex, movePath, paths);
    internal static bool TGroupPanelAccept(
        QGroup group, bool handled, string[]? files, int? moveIndex, string? movePath, string[]? paths) =>
        group.QGroupDrag.QGroupPanelAccept(handled, files, moveIndex, movePath, paths);
    internal static void TGroupPressHandle(QGroup group, int index, string path, double x, double y) =>
        group.QGroupDrag.QGroupPressHandle(index, path, x, y, 0, 0);
    internal static bool TGroupDragResolve(QGroup group, double x, double y, double minimum, bool pressed) =>
        group.QGroupDrag.QGroupDragResolve(x, y, minimum, minimum, pressed);
    internal static void TGroupDragClear(QGroup group) => group.QGroupDrag.QGroupDragClear();
    internal static int TGroupInsertResolve(double y, IReadOnlyList<double> tops, IReadOnlyList<double> heights) =>
        QGroupDrag.QGroupInsertResolve(y, tops, heights);

    internal static LPresetSelection TPresetSelectionCreate(string name) => new(name, string.Empty);
    internal static QExport TExportCreate(LPresetSelection selection, string key) =>
        new(TAtelierCreate().CAtelierUsher, selection, new QWorkspaceFacts(key));
    internal static void TExportAttach(QExport export, Action handler) => export.QExportPresetsChanged += handler;
    internal static void TExportOwnerAttach(QExport export) => export.QExportAttach();
    internal static void TExportSelect(QExport export, string name) => export.QExportSelect(name);
    internal static void TExportEditStart(QExport export, string name) => export.QExportEditStart(name);
    internal static void TExportEditCancel(QExport export) => export.QExportEditCancel();
    internal static bool TExportNameCommit(QExport export, string oldName, string newName) =>
        export.QExportNameCommit(oldName, newName);
    internal static void TExportDragStart(QExport export, string name) => export.QExportDragStart(name, 0, 0);
    internal static bool TExportDragMove(QExport export) => export.QExportDragMove();
    internal static bool TExportDragClear(QExport export) => export.QExportDragClear();
    internal static void TExportSync(QExport export) => export.QExportSync();

    internal static CAtelier TAtelierCreate() => new(new LUsherOutlet(), new LLineageOutlet(), new LWorkspaceOutlet());
    internal static QStrip TStripCreate(Func<string, string> title, Func<string, int, string> number) =>
        new(
            TAtelierCreate().CAtelierUsher, TAtelierCreate().CAtelierLineage, TAtelierCreate().CAtelierWorkspaceCreate,
            title,
            number);
    internal static QStripTab TStripTabCreate(string key) =>
        new(
            key, TAtelierCreate().CAtelierUsher, TAtelierCreate().CAtelierLineage,
            TAtelierCreate().CAtelierWorkspaceCreate());
    internal static void TStripSelectAttach(QStrip strip, Action<QStripTab?> handler) =>
        strip.QStripSelectChanged += handler;
    internal static void TStripTabAttach(QStrip strip, Action<QStripTab> handler) => strip.QStripTabChanged += handler;
    internal static void TStripAdd(QStrip strip, QStripTab tab) => strip.QStripAdd(tab);
    internal static void TStripRemove(QStrip strip, QStripTab tab) => strip.QStripRemove(tab);
    internal static bool TStripMove(QStrip strip, QStripTab tab, int index) => strip.QStripMove(tab, index);
    internal static void TStripSelect(QStrip strip, QStripTab? tab) => strip.QStripSelect(tab);
    internal static bool TStripNameSet(QStrip strip, QStripTab tab, string? name) => strip.QStripNameSet(tab, name);
    internal static void TStripHoverSet(QStrip strip, QStripTab tab) => strip.QStripSeparator.QStripHoverSet(tab);
    internal static void TStripHoverClear(QStrip strip, QStripTab? tab) => strip.QStripSeparator.QStripHoverClear(tab);
    internal static void TStripUpdateSuspend(QStrip strip) => strip.QStripUpdateSuspend();
    internal static bool TStripRelayCheck(QStripTab tab) => tab.QStripRelayCheck();
    internal static bool TStripCohortRun(QStripTab tab, Guid cohort) => tab.QStripCohortRun(cohort);
    internal static void TStripUpdateResume(QStrip strip) => strip.QStripUpdateResume();
    internal static void TStripTitleUpdate(QStrip strip) => strip.QStripTitleUpdate();

    internal static QColumn TColumnCreate(
        IReadOnlyList<double> minimums, IReadOnlyList<double>? stored, IReadOnlyList<bool>? compact, int flex) =>
        new(minimums, stored, compact, flex);
    internal static bool TColumnHiddenSet(QColumn column, int index, bool hidden) =>
        column.QColumnHiddenSet(index, hidden);
    internal static bool TColumnWidthSet(QColumn column, int index, double width) =>
        column.QColumnWidthSet(index, width);
    internal static IReadOnlyList<double> TColumnWeightsRead(QColumn column) => column.QColumnWeightsRead();
    internal static bool TColumnWeightsCommit(QColumn column, IReadOnlyList<double> widths) =>
        column.QColumnWeightsCommit(widths);
    internal static double TColumnTotalRead(QColumn column, double splitter) => column.QColumnTotalRead(splitter);
    internal static double[] TColumnMinimumRead(QColumn column, double available) =>
        column.QColumnMinimumRead(available);
    internal static double[]? TColumnDefaultResolve(QColumn column, double available, IReadOnlyList<double> minimums) =>
        column.QColumnDefaultResolve(available, minimums);
    internal static bool TColumnDragResolve(
        QColumn column, int left, double delta, double[] widths, IReadOnlyList<double> minimums) =>
        column.QColumnDragResolve(left, delta, widths, minimums);
    internal static bool TColumnDefaultsRead(QColumn column) => column.QColumnDefaultsRead();
    internal static QColumnPlan TColumnPlanCreate(
        IReadOnlyList<double> minimums, IReadOnlyList<double>? stored, IReadOnlyList<bool>? compact, int flex) =>
        new(minimums, stored, compact, flex);
    internal static QColumn TColumnOwnerRead(QColumnPlan plan) => plan.QColumn;
    internal static void TColumnPlanAttach(QColumnPlan plan, Action handler) => plan.QColumnPlanChanged += handler;
    internal static void TColumnWidthAttach(QColumnPlan plan, Action handler) => plan.QColumnWidthChanged += handler;
    internal static IReadOnlyList<QColumnSlot> TColumnSlotsResolve(QColumnPlan plan) => plan.QColumnSlotsResolve();
    internal static void TColumnWidthSet(QColumnPlan plan, int index, double width) =>
        plan.QColumnWidthSet(index, width);
    internal static void TColumnHiddenSet(QColumnPlan plan, int index, bool hidden) =>
        plan.QColumnHiddenSet(index, hidden);
    internal static void TColumnLayoutHandle(QColumnPlan plan, double slot, double actual) =>
        plan.QColumnLayoutHandle(slot, actual);
    internal static void TColumnDragHandle(
        QColumnPlan plan, int left, double delta, double slot, double actual, IReadOnlyList<double> widths) =>
        plan.QColumnDragHandle(left, delta, slot, actual, widths);
    internal static double TColumnGridResolve(double slot, double actual) =>
        QColumnPlan.QColumnGridResolve(slot, actual);
    internal static double TColumnTotalRead(QColumnPlan plan) => plan.QColumnTotalRead();
    internal static bool TColumnAppliedSet(QColumn column, double available) => column.QColumnAppliedSet(available);
    internal static bool TColumnHiddenCheck(QColumn column, int index) => column.QColumnHiddenCheck(index);
    internal static double TColumnWeightRead(QColumn column, int index) => column.QColumnWeightRead(index);
    internal static double TColumnFixedRead(QColumn column, int index) => column.QColumnFixedRead(index);
    internal static double TColumnPixelRead(QColumn column, int index) => column.QColumnPixelRead(index);

    internal static QAsk TAskCreate(string question, string action) => new(question, action);
    internal static void TAskAttach(Action<QAsk, Action<bool>> handler) => QAskNotice.QAskRaised += handler;
    internal static void TAskDetach(Action<QAsk, Action<bool>> handler) => QAskNotice.QAskRaised -= handler;
    internal static void TAskPublish(QAsk? ask, Action<bool> answer) => QAskNotice.QAskPublish(ask, answer);

    internal static QProgram TProgramCreate() => new(
        Interface.TInterface.TInterfaceEngine.LEngineSchedule,
        Interface.TInterface.TInterfaceEngine.LEngineStationAttach);
    internal static bool TProgramDepotApply(QProgram program) => program.QProgramDepotApply();
    internal static string? TProgramDepotRead(QProgram program) => program.QProgramDepotRoot;
    internal static bool TProgramLanguageNormalize() => QProgram.QProgramLanguageNormalize();
    internal static string TDepotRootRead() => LDepot.LDepotRootRead();
    internal static void TDepotRootSet(string? root) => LDepot.LDepotRootSet(root);
    internal static void TDepotIndexRelease() => LDepotIndex.LDepotIndexRelease();
    internal static string TDepotIndexFind() => LDepot.LDepotIndexFind();
}
