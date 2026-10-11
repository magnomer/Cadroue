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
    internal static void TInterfaceHomeStart() =>
        RuntimeHelpers.RunModuleConstructor(typeof(Interface.TInterface).Module.ModuleHandle);

    internal static LWorkGammaSettings TGammaSettingsCreate(
        double global, double red, double green, double blue, double highlight) =>
        new(global, red, green, blue, highlight);
    internal static LWorkCurveSettings TCurveSettingsRead(LWorkVideoStep step) => step.LWorkCurveRead();

    internal static LTone TToneCreate() => new();
    internal static void TToneAttach(LTone tone, Action handler) => tone.LToneChanged += handler;
    internal static void TToneValueSet(LTone tone, LColorKind kind, double value) => tone.LToneValueSet(kind, value);
    internal static void TToneActiveSet(LTone tone, LColorKind kind, bool active) => tone.LToneActiveSet(kind, active);
    internal static void TToneCapableSet(LTone tone, bool capable) => tone.LToneCapableSet(capable);
    internal static LWorkVideoStep TToneStepRead(LTone tone, LColorKind kind) => tone.LToneStepRead(kind);

    internal static LGamma TGammaCreate() => new();
    internal static void TGammaAttach(LGamma gamma, Action handler) => gamma.LGammaChanged += handler;
    internal static void TGammaValueSet(LGamma gamma, int index, double value) => gamma.LGammaValueSet(index, value);
    internal static void TGammaReset(LGamma gamma) => gamma.LGammaReset();
    internal static void TGammaCapableSet(LGamma gamma, bool capable, bool preview) =>
        gamma.LGammaCapableSet(capable, preview);
    internal static LWorkGammaSettings TGammaValueRead(LGamma gamma) => gamma.LGammaValue;
    internal static bool TGammaPreviewRead(LGamma gamma) => gamma.LGammaPreview;

    internal static LExposure TExposureCreate() => new();
    internal static void TExposureAttach(LExposure exposure, Action handler) => exposure.LExposureChanged += handler;
    internal static void TExposureValueSet(LExposure exposure, double value) => exposure.LExposureValueSet(value);
    internal static LWorkVideoStep TExposureStepRead(LExposure exposure) => exposure.LExposureStep;

    internal static LCurve TCurveCreate() => new();
    internal static void TCurveAttach(LCurve curve, Action handler) => curve.LCurveChanged += handler;
    internal static void TCurveChannelSelect(LCurve curve, int channel) => curve.LCurveChannelSelect(channel);
    internal static void TCurvePointAdd(LCurve curve, double input, double output) =>
        curve.LCurvePointAdd(input, output);
    internal static void TCurvePointSet(LCurve curve, double input, double output) =>
        curve.LCurvePointSet(input, output);
    internal static void TCurvePointDelete(LCurve curve) => curve.LCurvePointDelete();
    internal static void TCurveChannelReset(LCurve curve) => curve.LCurveChannelReset();
    internal static int TCurveChannelRead(LCurve curve) => curve.LCurveChannel;
    internal static int TCurveSelectedRead(LCurve curve) => curve.LCurveSelected;
    internal static IReadOnlyList<LWorkCurvePoint> TCurvePointsRead(LCurve curve) => curve.LCurvePoints;
    internal static LWorkVideoStep TCurveStepRead(LCurve curve) => curve.LCurveStepRead();
    internal static void TCurveStepSet(LCurve curve, LWorkVideoStep step) => curve.LCurveStepSet(step);
    internal static void TCurvePointSelect(LCurve curve, int index) => curve.LCurvePointSelect(index);

    internal static LBlank TBlankCreate() => new();
    internal static void TBlankStepSet(LBlank blank, LDetectorBlank step) => blank.LBlankStepSet(step);
    internal static void TBlankTypeSet(LBlank blank, LDetectorType type) => blank.LBlankTypeSet(type);
    internal static void TBlankWheelSet(LBlank blank, double x, double y) => blank.LBlankWheelSet(x, y);
    internal static void TBlankSampleSet(LBlank blank, int red, int green, int blue) =>
        blank.LBlankSampleSet(red, green, blue);
    internal static bool TBlankPresentRead(LBlank blank) => blank.LBlankWheelPresent;
    internal static LDetectorBlank TBlankStepRead(LBlank blank) => blank.LBlankStep;
    internal static LDetectorBlank TBlankStepCreate(LDetectorType type) =>
        LDetectorBlank.LDetectorBlankCreate() with { LDetectorBlankType = type };

    internal static LWhitebalance TWhitebalanceCreate() => new();
    internal static void TWhitebalanceAttach(LWhitebalance whitebalance, Action handler) =>
        whitebalance.LWhitebalanceChanged += handler;
    internal static void TWhitebalanceEstimateAttach(LWhitebalance whitebalance, Action<LWhitebalanceMethod> handler) =>
        whitebalance.LWhitebalanceEstimateChanged += handler;
    internal static void TWhitebalanceSampleSet(LWhitebalance whitebalance, LNeutralSample sample) =>
        whitebalance.LWhitebalanceSampleSet(sample);
    internal static void TWhitebalanceMethodSet(LWhitebalance whitebalance, LWhitebalanceMethod method) =>
        whitebalance.LWhitebalanceMethodSet(method);
    internal static void TWhitebalanceReset(LWhitebalance whitebalance) => whitebalance.LWhitebalanceReset();
    internal static void TWhitebalanceToolSet(LWhitebalance whitebalance, bool armed, LNeutralTarget target) =>
        whitebalance.LWhitebalanceToolSet(armed, target);
    internal static void TWhitebalanceCapableSet(LWhitebalance whitebalance, bool capable, bool preview) =>
        whitebalance.LWhitebalanceCapableSet(capable, preview);
    internal static LWorkWhitebalanceSettings TWhitebalanceValueRead(LWhitebalance whitebalance) =>
        whitebalance.LWhitebalanceValue;
    internal static LWorkVideoStep TWhitebalanceStepRead(LWhitebalance whitebalance) => whitebalance.LWhitebalanceStep;
    internal static bool TWhitebalanceManualRead(LWhitebalance whitebalance) => whitebalance.LWhitebalanceManual;
    internal static bool TWhitebalanceToolRead(LWhitebalance whitebalance) => whitebalance.LWhitebalanceToolArmed;
    internal static LNeutralWheel TWhitebalanceWheelRead(LWhitebalance whitebalance) =>
        whitebalance.LWhitebalanceWheelRead();
    internal static LNeutralSample TNeutralSampleCreate(
        int red, int green, int blue, double redGain, double greenGain, double blueGain) =>
        new(LNeutralOutcome.LNeutralOutcomeResolved, red, green, blue, redGain, greenGain, blueGain);

    internal static LNoise TNoiseCreate() => new();
    internal static void TNoiseAttach(LNoise noise, Action handler) => noise.LNoiseChanged += handler;
    internal static void TNoisePresetSelect(LNoise noise, string token) => noise.LNoisePresetSelect(token);
    internal static void TNoiseValueSet(LNoise noise, int index, double value) => noise.LNoiseValueSet(index, value);
    internal static void TNoiseStepSet(LNoise noise, LWorkAudioStep step) => noise.LNoiseStepSet(step);
    internal static string? TNoiseTokenRead(LNoise noise) => noise.LNoiseToken;
    internal static string? TNoiseMatchRead(LNoise noise) => noise.LNoiseMatchRead();
    internal static LWorkNoiseStep TNoiseStepRead(LNoise noise) => noise.LNoiseStep;

    internal static LSensor TSensorCreate() => new();
    internal static IReadOnlyList<string> TDetectorTokensRead(LDetectorKind kind) =>
        LDetector.LDetectorTokensRead(kind);
    internal static void TSensorPresetSelect(LSensor sensor, LDetectorKind kind, string token) =>
        sensor.LSensorPresetSelect(kind, token);
    internal static void TSensorTokenSet(LSensor sensor, LDetectorKind kind, string token) =>
        sensor.LSensorTokenSet(kind, token);
    internal static void TSensorStepSet(LSensor sensor, LDetectorStep step) => sensor.LSensorStepSet(step);
    internal static void TSensorThresholdSet(LSensor sensor, LDetectorKind kind, double threshold) =>
        sensor.LSensorThresholdSet(kind, threshold);
    internal static void TSensorMetricSet(LSensor sensor, LDetectorMetricMode metric) =>
        sensor.LSensorMetricSet(metric);
    internal static string? TSensorTokenRead(LSensor sensor, LDetectorKind kind) => sensor.LSensorTokenRead(kind);
    internal static string? TSensorMatchRead(LSensor sensor, LDetectorKind kind) => sensor.LSensorMatchRead(kind);
    internal static LDetectorStep TSensorStepRead(LSensor sensor, LDetectorKind kind) => sensor.LSensorStepRead(kind);

    internal static LLoudness TLoudnessCreate() => new();
    internal static void TLoudnessAttach(LLoudness loudness, Action handler) => loudness.LLoudnessChanged += handler;
    internal static void TLoudnessPresetSelect(LLoudness loudness, string token) =>
        loudness.LLoudnessPresetSelect(token);
    internal static void TLoudnessModeSet(LLoudness loudness, LLeveling mode) => loudness.LLoudnessModeSet(mode);
    internal static void TLoudnessValueSet(LLoudness loudness, int index, double value) =>
        loudness.LLoudnessValueSet(index, value);
    internal static void TLoudnessDynamicSet(LLoudness loudness, int index, double value) =>
        loudness.LLoudnessDynamicSet(index, value);
    internal static string? TLoudnessTokenRead(LLoudness loudness) => loudness.LLoudnessToken;
    internal static string? TLoudnessMatchRead(LLoudness loudness) => loudness.LLoudnessMatchRead();
    internal static LWorkNormalizeStep TLoudnessStepRead(LLoudness loudness) => loudness.LLoudnessStep;

    internal static LFilter TFilterCreate(bool high) => new(high);
    internal static void TFilterAttach(LFilter filter, Action handler) => filter.LFilterChanged += handler;
    internal static void TFilterPresetSelect(LFilter filter, string token) => filter.LFilterPresetSelect(token);
    internal static void TFilterFrequencySet(LFilter filter, double frequency) => filter.LFilterFrequencySet(frequency);
    internal static string? TFilterTokenRead(LFilter filter) => filter.LFilterToken;
    internal static string? TFilterMatchRead(LFilter filter) => filter.LFilterMatchRead();
    internal static LWorkPassStep TFilterStepRead(LFilter filter) => filter.LFilterStep;

    internal static LEqualizer TEqualizerCreate() => new();
    internal static void TEqualizerAttach(LEqualizer equalizer, Action handler) =>
        equalizer.LEqualizerChanged += handler;
    internal static void TEqualizerPresetSelect(LEqualizer equalizer, string token) =>
        equalizer.LEqualizerPresetSelect(token);
    internal static void TEqualizerBandSet(LEqualizer equalizer, int index, double frequency, double gain) =>
        equalizer.LEqualizerBandSet(index, frequency, gain);
    internal static void TEqualizerBandAdd(LEqualizer equalizer) => equalizer.LEqualizerBandAdd();
    internal static void TEqualizerBandRemove(LEqualizer equalizer, int index) => equalizer.LEqualizerBandRemove(index);
    internal static string? TEqualizerTokenRead(LEqualizer equalizer) => equalizer.LEqualizerToken;
    internal static string? TEqualizerMatchRead(LEqualizer equalizer) => equalizer.LEqualizerMatchRead();
    internal static IReadOnlyList<LWorkBand> TEqualizerBandsRead(LEqualizer equalizer) => equalizer.LEqualizerBands;
    internal static LWorkAudioStep TEqualizerStepRead(LEqualizer equalizer) => equalizer.LEqualizerStepRead();

    internal static LVolume TVolumeCreate() => new();
    internal static void TVolumeAttach(LVolume volume, Action handler) => volume.LVolumeChanged += handler;
    internal static void TVolumeGainSet(LVolume volume, double gain) => volume.LVolumeGainSet(gain);
    internal static void TVolumeActiveSet(LVolume volume, bool active) => volume.LVolumeActiveSet(active);
    internal static double TVolumeGainRead(LVolume volume) => volume.LVolumeGain;

    internal static LSkip TSkipCreate() => new();
    internal static void TSkipAttach(LSkip skip, Action handler) => skip.LSkipChanged += handler;
    internal static void TSkipActiveSet(LSkip skip, bool active) => skip.LSkipActiveSet(active);
    internal static void TSkipPersistentSet(LSkip skip, bool persistent) => skip.LSkipPersistentSet(persistent);

    internal static LSLoupe TLoupeCreate() => new();
    internal static void TLoupePlayingAttach(LSLoupe loupe, Action<bool> handler) =>
        loupe.LSLoupePlayingChanged += handler;
    internal static void TLoupePlayingSet(LSLoupe loupe, bool playing) => loupe.LSLoupePlayingSet(playing);
    internal static void TLoupeEndSet(LSLoupe loupe, bool ended) => loupe.LSLoupeEndSet(ended);
    internal static void TLoupeClose(LSLoupe loupe) => loupe.LSLoupeClose();
    internal static bool TLoupeResumeCheck(LSLoupe loupe) => loupe.LSLoupeResumeCheck();
    internal static void TLoupeViewerAttach(LSLoupe loupe, LViewer viewer) => loupe.LSLoupeViewerAttach(viewer);
    internal static void TLoupeDetach(LSLoupe loupe) => loupe.LSLoupeDetach();
    internal static Task TLoupeStart(LSLoupe loupe) => loupe.LSLoupeStart();
    internal static void TLoupePlayToggle(LSLoupe loupe) => loupe.LSLoupePlayToggle();
    internal static void TLoupeSeek(LSLoupe loupe, TimeSpan position) => loupe.LSLoupeSeek(position);
    internal static void TLoupeTick(LSLoupe loupe) => loupe.LSLoupeTick();
    internal static void TLoupeFloatSet(LSLoupe loupe, LSLoupeFloat mode) => loupe.LSLoupeFloatSet(mode);
    internal static void TLoupeFloatRestore(LSLoupe loupe) => loupe.LSLoupeFloatRestore();
    internal static IReadOnlyList<LSLoupeButton> TLoupeButtonsRead(LSLoupe loupe) => loupe.LSLoupeButtonsRead();
    internal static void TLoupeFloatAttach(LSLoupe loupe, Action handler) => loupe.LSLoupeFloatChanged += handler;
    internal static void TLoupeEngineAttach(LSLoupe loupe, Action handler) => loupe.LSLoupeEngineCreated += handler;
    internal static void TLoupeCloseAttach(LSLoupe loupe, Action handler) => loupe.LSLoupeCloseApplied += handler;

    internal static LGroup TGroupCreate(LGroupSelection selection) => new(TAtelierCreate().CAtelierUsher, selection);
    internal static void TGroupAttach(LGroup group, Action handler) => group.LGroupChanged += handler;
    internal static bool TGroupAdd(LGroup group, IReadOnlyList<string> paths, string name) =>
        group.LGroupAdd(paths, name);
    internal static bool TGroupPathsInsert(LGroup group, int index, IReadOnlyList<string> paths, int insertAt) =>
        group.LGroupPathsInsert(index, paths, insertAt);
    internal static void TGroupRemove(LGroup group, int index) => group.LGroupRemove(index);
    internal static void TGroupEditStart(LGroup group, int index) => group.LGroupEditStart(index);
    internal static void TGroupEditCancel(LGroup group) => group.LGroupEditCancel();
    internal static bool TGroupNameCommit(LGroup group, string name) => group.LGroupNameCommit(name);
    internal static bool TGroupNameSet(LGroup group, int index, string name) => group.LGroupNameSet(index, name);
    internal static void TGroupFaceAttach(LGroup group, Action handler) => group.LGroupFaceChanged += handler;
    internal static void TGroupRequestAttach(LGroup group, Action<IReadOnlyList<string>> handler) =>
        group.LGroupDrag.LGroupPathsRequest += handler;
    internal static void TGroupOpenAttach(LGroup group, Action<string> handler) =>
        group.LGroupDrag.LGroupItemOpened += handler;
    internal static void TGroupSourceAttach(LGroup group, Func<IReadOnlyList<string>> source) =>
        group.LGroupSourceAttach(source);
    internal static void TGroupAutoUpdate(LGroup group) => group.LGroupAutoUpdate();
    internal static IReadOnlyList<LGroupCard> TGroupCardsRead(LGroup group) => group.LGroupFace.LGroupCardsRead();
    internal static LGroupSwitch TGroupModeRead(LGroup group) => group.LGroupFace.LGroupModeRead();
    internal static IReadOnlyList<LGroupSwitch> TGroupSwitchesRead(LGroup group) =>
        group.LGroupFace.LGroupSwitchesRead();
    internal static void TGroupToggleRun(LGroup group, string key, bool right) =>
        group.LGroupFace.LGroupToggleRun(key, right);
    internal static bool TGroupLabelHandle(LGroup group, int index, int clicks) =>
        group.LGroupLabelHandle(index, clicks);
    internal static bool TGroupKeyRun(LGroup group, string key, string text) => group.LGroupKeyRun(key, text);
    internal static bool TGroupCardAccept(
        LGroup group, int target, int insertAt, string[]? files, int? moveIndex, string? movePath, string[]? paths) =>
        group.LGroupDrag.LGroupCardAccept(target, insertAt, files, moveIndex, movePath, paths);
    internal static bool TGroupPanelAccept(
        LGroup group, bool handled, string[]? files, int? moveIndex, string? movePath, string[]? paths) =>
        group.LGroupDrag.LGroupPanelAccept(handled, files, moveIndex, movePath, paths);
    internal static void TGroupPressHandle(LGroup group, int index, string path, double x, double y) =>
        group.LGroupDrag.LGroupPressHandle(index, path, x, y, 0, 0);
    internal static bool TGroupDragResolve(LGroup group, double x, double y, double minimum, bool pressed) =>
        group.LGroupDrag.LGroupDragResolve(x, y, minimum, minimum, pressed);
    internal static void TGroupDragClear(LGroup group) => group.LGroupDrag.LGroupDragClear();
    internal static int TGroupInsertResolve(double y, IReadOnlyList<double> tops, IReadOnlyList<double> heights) =>
        LGroupDrag.LGroupInsertResolve(y, tops, heights);

    internal static LPresetSelection TPresetSelectionCreate(string name) => new(name);
    internal static LExport TExportCreate(LPresetSelection selection, bool smart) => new(selection, smart);
    internal static void TExportAttach(LExport export, Action handler) => export.LExportPresetsChanged += handler;
    internal static void TExportOwnerAttach(LExport export) => export.LExportAttach();
    internal static void TExportSelect(LExport export, string name) => export.LExportSelect(name);
    internal static void TExportEditStart(LExport export, string name) => export.LExportEditStart(name);
    internal static void TExportEditCancel(LExport export) => export.LExportEditCancel();
    internal static bool TExportNameCommit(LExport export, string oldName, string newName) =>
        export.LExportNameCommit(oldName, newName);
    internal static void TExportDragStart(LExport export, string name) => export.LExportDragStart(name);
    internal static bool TExportDragMove(LExport export) => export.LExportDragMove();
    internal static bool TExportDragClear(LExport export) => export.LExportDragClear();
    internal static void TExportSync(LExport export) => export.LExportSync();

    internal static CAtelier TAtelierCreate() => new(new LUsherOutlet());
    internal static LStrip TStripCreate(Func<string, string> title, Func<string, int, string> number) =>
        new(TAtelierCreate().CAtelierUsher, title, number);
    internal static LStripTab TStripTabCreate(string key) => new(key);
    internal static void TStripSelectAttach(LStrip strip, Action<LStripTab?> handler) =>
        strip.LStripSelectChanged += handler;
    internal static void TStripTabAttach(LStrip strip, Action<LStripTab> handler) => strip.LStripTabChanged += handler;
    internal static void TStripAdd(LStrip strip, LStripTab tab) => strip.LStripAdd(tab);
    internal static void TStripRemove(LStrip strip, LStripTab tab) => strip.LStripRemove(tab);
    internal static bool TStripMove(LStrip strip, LStripTab tab, int index) => strip.LStripMove(tab, index);
    internal static void TStripSelect(LStrip strip, LStripTab? tab) => strip.LStripSelect(tab);
    internal static bool TStripNameSet(LStrip strip, LStripTab tab, string? name) => strip.LStripNameSet(tab, name);
    internal static void TStripHoverSet(LStrip strip, LStripTab tab) => strip.QStripSeparator.LStripHoverSet(tab);
    internal static void TStripHoverClear(LStrip strip, LStripTab? tab) => strip.QStripSeparator.LStripHoverClear(tab);
    internal static void TStripUpdateSuspend(LStrip strip) => strip.LStripUpdateSuspend();
    internal static bool TStripRelayCheck(LStripTab tab) => tab.LStripRelayCheck();
    internal static bool TStripCohortRun(LStripTab tab, Guid cohort) => tab.LStripCohortRun(cohort);
    internal static void TStripUpdateResume(LStrip strip) => strip.LStripUpdateResume();
    internal static void TStripTitleUpdate(LStrip strip) => strip.LStripTitleUpdate();

    internal static LColumn TColumnCreate(
        IReadOnlyList<double> minimums, IReadOnlyList<double>? stored, IReadOnlyList<bool>? compact, int flex) =>
        new(minimums, stored, compact, flex);
    internal static bool TColumnHiddenSet(LColumn column, int index, bool hidden) =>
        column.LColumnHiddenSet(index, hidden);
    internal static bool TColumnWidthSet(LColumn column, int index, double width) =>
        column.LColumnWidthSet(index, width);
    internal static IReadOnlyList<double> TColumnWeightsRead(LColumn column) => column.LColumnWeightsRead();
    internal static bool TColumnWeightsCommit(LColumn column, IReadOnlyList<double> widths) =>
        column.LColumnWeightsCommit(widths);
    internal static double TColumnTotalRead(LColumn column, double splitter) => column.LColumnTotalRead(splitter);
    internal static double[] TColumnMinimumRead(LColumn column, double available) =>
        column.LColumnMinimumRead(available);
    internal static double[]? TColumnDefaultResolve(LColumn column, double available, IReadOnlyList<double> minimums) =>
        column.LColumnDefaultResolve(available, minimums);
    internal static bool TColumnDragResolve(
        LColumn column, int left, double delta, double[] widths, IReadOnlyList<double> minimums) =>
        column.LColumnDragResolve(left, delta, widths, minimums);
    internal static bool TColumnDefaultsRead(LColumn column) => column.LColumnDefaultsRead();
    internal static LColumnPlan TColumnPlanCreate(
        IReadOnlyList<double> minimums, IReadOnlyList<double>? stored, IReadOnlyList<bool>? compact, int flex) =>
        new(minimums, stored, compact, flex);
    internal static LColumn TColumnOwnerRead(LColumnPlan plan) => plan.LColumn;
    internal static void TColumnPlanAttach(LColumnPlan plan, Action handler) => plan.LColumnPlanChanged += handler;
    internal static void TColumnWidthAttach(LColumnPlan plan, Action handler) => plan.LColumnWidthChanged += handler;
    internal static IReadOnlyList<LColumnSlot> TColumnSlotsResolve(LColumnPlan plan) => plan.LColumnSlotsResolve();
    internal static void TColumnWidthSet(LColumnPlan plan, int index, double width) =>
        plan.LColumnWidthSet(index, width);
    internal static void TColumnHiddenSet(LColumnPlan plan, int index, bool hidden) =>
        plan.LColumnHiddenSet(index, hidden);
    internal static void TColumnLayoutHandle(LColumnPlan plan, double slot, double actual) =>
        plan.LColumnLayoutHandle(slot, actual);
    internal static void TColumnDragHandle(
        LColumnPlan plan, int left, double delta, double slot, double actual, IReadOnlyList<double> widths) =>
        plan.LColumnDragHandle(left, delta, slot, actual, widths);
    internal static double TColumnGridResolve(double slot, double actual) =>
        LColumnPlan.LColumnGridResolve(slot, actual);
    internal static double TColumnTotalRead(LColumnPlan plan) => plan.LColumnTotalRead();
    internal static bool TColumnAppliedSet(LColumn column, double available) => column.LColumnAppliedSet(available);
    internal static bool TColumnHiddenCheck(LColumn column, int index) => column.LColumnHiddenCheck(index);
    internal static double TColumnWeightRead(LColumn column, int index) => column.LColumnWeightRead(index);
    internal static double TColumnFixedRead(LColumn column, int index) => column.LColumnFixedRead(index);
    internal static double TColumnPixelRead(LColumn column, int index) => column.LColumnPixelRead(index);

    internal static LAsk TAskCreate(string question, string action) => new(question, action);
    internal static void TAskAttach(Action<LAsk, Action<bool>> handler) => LAskNotice.LAskRaised += handler;
    internal static void TAskDetach(Action<LAsk, Action<bool>> handler) => LAskNotice.LAskRaised -= handler;
    internal static void TAskPublish(LAsk? ask, Action<bool> answer) => LAskNotice.LAskPublish(ask, answer);

    internal static LProgram TProgramCreate() => new(TAtelierCreate());
    internal static bool TProgramDepotApply(LProgram program) => program.LProgramDepotApply();
    internal static string? TProgramDepotRead(LProgram program) => program.LProgramDepotRoot;
    internal static bool TProgramLanguageNormalize() => LProgram.LProgramLanguageNormalize();
    internal static string TDepotRootRead() => LDepot.LDepotRootRead();
    internal static void TDepotRootSet(string? root) => LDepot.LDepotRootSet(root);
    internal static void TDepotIndexRelease() => LDepotIndex.LDepotIndexRelease();
    internal static string TDepotIndexFind() => LDepot.LDepotIndexFind();
}
