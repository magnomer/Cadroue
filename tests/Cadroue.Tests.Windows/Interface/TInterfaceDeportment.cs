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
        QPlacement.QPlacementGateSeam = TAtelierCreate().CAtelierPlacement;
    }

    internal static LWorkGammaSettings TGammaSettingsCreate(
        double global, double red, double green, double blue, double highlight) =>
        new(global, red, green, blue, highlight);
    internal static LWorkCurveSettings TCurveSettingsRead(LWorkVideoStep step) => step.LWorkCurveRead();

    internal static CTone TToneCreate() => TAtelierCreate().CAtelierInspectorCreate().CInspectorTone;
    internal static void TToneAttach(CTone tone, Action handler) => tone.CToneChanged += handler;
    internal static void TToneValueSet(CTone tone, LColorKind kind, double value) =>
        tone.CToneValueSet(TToneSlotRead(kind), value);
    internal static void TToneActiveSet(CTone tone, LColorKind kind, bool active) =>
        tone.CToneActiveSet(TToneSlotRead(kind), active);
    internal static void TToneCapableSet(CTone tone, bool capable) => tone.CToneCapableSet(capable);
    internal static LWorkVideoStep TToneStepRead(CTone tone, LColorKind kind) => kind switch
    {
        LColorKind.LColorKindContrast => LWorkVideoStep.LWorkContrastCreate(tone.CToneActives[1], tone.CToneValues[1]),
        LColorKind.LColorKindSaturation =>
            LWorkVideoStep.LWorkSaturationCreate(tone.CToneActives[2], tone.CToneValues[2]),
        _ => LWorkVideoStep.LWorkBrightnessCreate(tone.CToneActives[0], tone.CToneValues[0])
    };
    private static int TToneSlotRead(LColorKind kind) => kind switch
    {
        LColorKind.LColorKindContrast => 1,
        LColorKind.LColorKindSaturation => 2,
        _ => 0
    };

    internal static CGamma TGammaCreate() => TAtelierCreate().CAtelierInspectorCreate().CInspectorGamma;
    internal static void TGammaAttach(CGamma gamma, Action handler) => gamma.CGammaChanged += handler;
    internal static void TGammaValueSet(CGamma gamma, int index, double value) => gamma.CGammaValueSet(index, value);
    internal static void TGammaReset(CGamma gamma) => gamma.CGammaReset();
    internal static void TGammaCapableSet(CGamma gamma, bool capable, bool preview) =>
        gamma.CGammaCapableSet(capable, preview);
    internal static LWorkGammaSettings TGammaValueRead(CGamma gamma) =>
        new(
            gamma.CGammaValues[0],
            gamma.CGammaValues[1],
            gamma.CGammaValues[2],
            gamma.CGammaValues[3],
            gamma.CGammaValues[4]);
    internal static bool TGammaPreviewRead(CGamma gamma) => gamma.CGammaPreview;

    internal static CExposure TExposureCreate() => TAtelierCreate().CAtelierInspectorCreate().CInspectorExposure;
    internal static void TExposureAttach(CExposure exposure, Action handler) => exposure.CExposureChanged += handler;
    internal static void TExposureValueSet(CExposure exposure, double value) => exposure.CExposureValueSet(value);
    internal static LWorkVideoStep TExposureStepRead(CExposure exposure) =>
        LWorkVideoStep.LWorkExposureCreate(exposure.CExposureActive, exposure.CExposureValue);

    internal static QCurve TCurveCreate() => new(TAtelierCreate().CAtelierInspectorCreate().CInspectorCurve);
    internal static void TCurveAttach(QCurve curve, Action handler) => curve.QCurveChanged += handler;
    internal static void TCurveChannelSelect(QCurve curve, int channel) => curve.QCurveChannelSelect(channel);
    internal static void TCurvePointDelete(QCurve curve) => curve.QCurvePointDelete();
    internal static void TCurveChannelReset(QCurve curve) => curve.QCurveChannelReset();
    internal static int TCurveChannelRead(QCurve curve) => curve.QCurveChannel;
    internal static int TCurveSelectedRead(QCurve curve) => curve.QCurveSelected;
    internal static IReadOnlyList<LWorkCurvePoint> TCurvePointsRead(QCurve curve) => curve.QCurvePoints;
    internal static LWorkVideoStep TCurveStepRead(QCurve curve) => curve.QCurveStepRead();
    internal static void TCurveStepSet(QCurve curve, LWorkVideoStep step) => curve.QCurveStepSet(step);

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

    internal static CWhitebalance TWhitebalanceCreate() =>
        TAtelierCreate().CAtelierInspectorCreate().CInspectorWhitebalance;
    internal static void TWhitebalanceAttach(CWhitebalance whitebalance, Action handler) =>
        whitebalance.CWhitebalanceChanged += handler;
    internal static void TWhitebalanceEstimateAttach(CWhitebalance whitebalance, Action<LWhitebalanceMethod> handler) =>
        whitebalance.CWhitebalanceEstimateChanged +=
            token => handler(LWhitebalancePort.LWhitebalanceMethodRead(token));
    internal static void TWhitebalanceEstimateStart(CWhitebalance whitebalance) =>
        whitebalance.CWhitebalanceEstimateStart();
    internal static void TWhitebalanceSampleSet(CWhitebalance whitebalance, LNeutralSample sample) =>
        whitebalance.CWhitebalanceSampleAccept(new LWhitebalanceSampleOutlet(sample, _ => { }));
    internal static void TWhitebalanceMethodSet(CWhitebalance whitebalance, LWhitebalanceMethod method) =>
        whitebalance.CWhitebalanceMethodSelect(method switch
        {
            LWhitebalanceMethod.LWhitebalanceMethodAverage => 0,
            LWhitebalanceMethod.LWhitebalanceMethodMinmax => 1,
            LWhitebalanceMethod.LWhitebalanceMethodMedian => 2,
            _ => 3
        });
    internal static void TWhitebalanceReset(CWhitebalance whitebalance) => whitebalance.CWhitebalanceReset();
    internal static void TWhitebalanceToolSet(CWhitebalance whitebalance, bool armed, LNeutralTarget target) =>
        whitebalance.CWhitebalanceToolSet(armed, target == LNeutralTarget.LNeutralTargetWhite);
    internal static void TWhitebalanceCapableSet(CWhitebalance whitebalance, bool capable, bool preview) =>
        whitebalance.CWhitebalanceCapableSet(capable, preview);
    internal static LWorkWhitebalanceSettings TWhitebalanceValueRead(CWhitebalance whitebalance) =>
        TWhitebalanceStepRead(whitebalance).LWorkWhitebalanceRead();
    internal static LWorkVideoStep TWhitebalanceStepRead(CWhitebalance whitebalance) =>
        LWhitebalancePort.LWhitebalanceStepCreate(whitebalance.CWhitebalanceAnswer);
    internal static bool TWhitebalanceManualRead(CWhitebalance whitebalance) => whitebalance.CWhitebalanceManual;
    internal static bool TWhitebalanceToolRead(CWhitebalance whitebalance) => whitebalance.CWhitebalanceArmed;
    internal static LNeutralTarget TWhitebalanceTargetRead(CWhitebalance whitebalance) =>
        whitebalance.CWhitebalanceWhite ? LNeutralTarget.LNeutralTargetWhite : LNeutralTarget.LNeutralTargetGrey;
    internal static LNeutralWheel TWhitebalanceWheelRead(CWhitebalance whitebalance)
    {
        (double x, double y, bool present) = whitebalance.CWhitebalanceWheelRead();
        return new LNeutralWheel(x, y, present);
    }
    internal static LNeutralSample TNeutralSampleCreate(
        int red, int green, int blue, double redGain, double greenGain, double blueGain) =>
        new(LNeutralOutcome.LNeutralOutcomeResolved, red, green, blue, redGain, greenGain, blueGain);

    internal static CNoise TNoiseCreate() => TAtelierCreate().CAtelierInspectorCreate().CInspectorNoise;
    internal static void TNoiseAttach(CNoise noise, Action handler) => noise.CNoiseChanged += handler;
    internal static void TNoisePresetSelect(CNoise noise, string token) => noise.CNoisePresetSelect(token);
    internal static void TNoiseValueSet(CNoise noise, int index, double value) => noise.CNoiseValueSet(index, value);
    internal static void TNoiseStepSet(CNoise noise, LWorkAudioStep step)
    {
        (bool active, IReadOnlyList<double> values, bool track, int type, string? preset) =
            new LInspectorOutlet(new LWorkAudio(new List<LWorkAudioStep> { step })).LInspectorNoiseRead();
        noise.CNoiseStepSet(active, values, track, type, preset);
    }
    internal static string? TNoiseTokenRead(CNoise noise) => noise.CNoiseToken;
    internal static string? TNoiseMatchRead(CNoise noise) => noise.CNoisePreset;
    internal static LWorkNoiseStep TNoiseStepRead(CNoise noise) =>
        (LWorkNoiseStep)LNoisePort.LNoiseStepCreate(
            noise.CNoiseActive, noise.CNoiseValues, noise.CNoiseTrack, noise.CNoiseType);

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

    internal static CLoudness TLoudnessCreate() => TAtelierCreate().CAtelierInspectorCreate().CInspectorLoudness;
    internal static void TLoudnessAttach(CLoudness loudness, Action handler) => loudness.CLoudnessChanged += handler;
    internal static void TLoudnessPresetSelect(CLoudness loudness, string token) =>
        loudness.CLoudnessPresetSelect(token);
    internal static void TLoudnessModeSet(CLoudness loudness, LLeveling mode) =>
        loudness.CLoudnessModeSelect(mode == LLeveling.LLevelingDynamic ? 1 : 0);
    internal static void TLoudnessValueSet(CLoudness loudness, int index, double value) =>
        loudness.CLoudnessValueSet(false, index, value);
    internal static void TLoudnessDynamicSet(CLoudness loudness, int index, double value) =>
        loudness.CLoudnessValueSet(true, index, value);
    internal static string? TLoudnessTokenRead(CLoudness loudness) => loudness.CLoudnessToken;
    internal static string? TLoudnessMatchRead(CLoudness loudness) => loudness.CLoudnessPreset;
    internal static LWorkNormalizeStep TLoudnessStepRead(CLoudness loudness) =>
        (LWorkNormalizeStep)LLoudnessPort.LLoudnessStepCreate(
            loudness.CLoudnessActive, loudness.CLoudnessDynamic, loudness.CLoudnessValues, loudness.CLoudnessTwoPass);

    internal static CFilter TFilterCreate(bool high) => high
        ? TAtelierCreate().CAtelierInspectorCreate().CInspectorHighpass
        : TAtelierCreate().CAtelierInspectorCreate().CInspectorLowpass;
    internal static void TFilterAttach(CFilter filter, Action handler) => filter.CFilterChanged += handler;
    internal static void TFilterPresetSelect(CFilter filter, string token) => filter.CFilterPresetSelect(token);
    internal static void TFilterFrequencySet(CFilter filter, double frequency) => filter.CFilterValueSet(0, frequency);
    internal static string? TFilterTokenRead(CFilter filter) => filter.CFilterToken;
    internal static string? TFilterMatchRead(CFilter filter) => filter.CFilterPreset;
    internal static double TFilterValueRead(CFilter filter, int slot) => filter.CFilterValueRead(slot);

    internal static CEqualizer TEqualizerCreate() => TAtelierCreate().CAtelierInspectorCreate().CInspectorEqualizer;
    internal static void TEqualizerAttach(CEqualizer equalizer, Action handler) =>
        equalizer.CEqualizerChanged += handler;
    internal static void TEqualizerPresetSelect(CEqualizer equalizer, string token) =>
        equalizer.CEqualizerPresetSelect(token);
    internal static void TEqualizerBandSet(CEqualizer equalizer, int index, double frequency, double gain) =>
        equalizer.CEqualizerBandSet(index, frequency, gain);
    internal static void TEqualizerBandAdd(CEqualizer equalizer) => new QEqualizer(equalizer).QEqualizerBandAdd();
    internal static void TEqualizerBandRemove(CEqualizer equalizer, int index) =>
        new QEqualizer(equalizer).QEqualizerBandRemove(index);
    internal static string? TEqualizerTokenRead(CEqualizer equalizer) => equalizer.CEqualizerToken;
    internal static string? TEqualizerMatchRead(CEqualizer equalizer) => equalizer.CEqualizerPreset;
    internal static IReadOnlyList<LWorkBand> TEqualizerBandsRead(CEqualizer equalizer) =>
        equalizer.CEqualizerBands.Select(band => new LWorkBand(band.CEqualizerFrequency, band.CEqualizerGain)).ToList();
    internal static LWorkAudioStep TEqualizerStepRead(CEqualizer equalizer) =>
        LEqualizerPort.LEqualizerStepCreate(equalizer.CEqualizerActive, equalizer.CEqualizerBands);

    internal static CVolume TVolumeCreate() => TAtelierCreate().CAtelierInspectorCreate().CInspectorVolume;
    internal static void TVolumeAttach(CVolume volume, Action handler) => volume.CVolumeChanged += handler;
    internal static void TVolumeGainSet(CVolume volume, double gain) => volume.CVolumeGainSet(gain);
    internal static void TVolumeActiveSet(CVolume volume, bool active) => volume.CVolumeActiveSet(active);
    internal static double TVolumeGainRead(CVolume volume) => volume.CVolumeGain;

    internal static QSkip TSkipCreate() => new(TAtelierCreate().CAtelierInspectorCreate().CInspectorSkip);
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
    internal static void TLoupeFloatSet(QSLoupe loupe, QSLoupeFloat mode) =>
        loupe.QSLoupeFloatChoice.QSLoupeFloatSet(mode);
    internal static void TLoupeFloatRestore(QSLoupe loupe) => loupe.QSLoupeFloatChoice.QSLoupeFloatRestore();
    internal static IReadOnlyList<QSLoupeButton> TLoupeButtonsRead(QSLoupe loupe) =>
        loupe.QSLoupeFloatChoice.QSLoupeButtonsRead();
    internal static void TLoupeFloatAttach(QSLoupe loupe, Action handler) =>
        loupe.QSLoupeFloatChoice.QSLoupeFloatChanged += handler;
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
    internal static QExport TExportCreate(LPresetSelection selection, string key)
    {
        CAtelier atelier = TAtelierCreate();
        return new(atelier.CAtelierUsher, atelier.CAtelierEncoder, selection, new QWorkspaceFacts(key));
    }
    internal static void TExportAttach(QExport export, Action handler) => export.QExportPresetsChanged += handler;
    internal static void TExportOwnerAttach(QExport export) => export.QExportAttach();
    internal static void TExportSelect(QExport export, string name) => export.QExportSelect(name);
    internal static void TExportEditStart(QExport export, string name) => export.QExportEditStart(name);
    internal static void TExportEditCancel(QExport export) => export.QExportEditCancel();
    internal static bool TExportNameCommit(QExport export, string oldName, string newName) =>
        export.QExportNameCommit(oldName, newName);
    internal static void TExportDragStart(QExport export, string name) =>
        export.QExportDrag.QExportDragStart(name, 0, 0);
    internal static bool TExportDragMove(QExport export) => export.QExportDrag.QExportDragMove();
    internal static bool TExportDragClear(QExport export) => export.QExportDrag.QExportDragClear();
    internal static void TExportSync(QExport export) => export.QExportSync();

    internal static QStrip TStripCreate(Func<string, string> title, Func<string, int, string> number) =>
        new(TAtelierCreate(), title, number);
    internal static QStripTab TStripTabCreate(string key) => new(key, TAtelierCreate());
    internal static void TStripSelectAttach(QStrip strip, Action<QStripTab?> handler) =>
        strip.QStripSelectChanged += handler;
    internal static void TStripTabAttach(QStrip strip, Action<QStripTab> handler) => strip.QStripTabChanged += handler;
    internal static void TStripAdd(QStrip strip, QStripTab tab) => strip.QStripAdd(tab);
    internal static void TStripRemove(QStrip strip, QStripTab tab) => strip.QStripRemove(tab);
    internal static bool TStripMove(QStrip strip, QStripTab tab, int index) => strip.QStripMove(tab, index);
    internal static void TStripSelect(QStrip strip, QStripTab? tab) => strip.QStripSelect(tab);
    internal static bool TStripNameSet(QStrip strip, QStripTab tab, string? name) =>
        strip.QStripTitle.QStripNameSet(tab, name);
    internal static void TStripHoverSet(QStrip strip, QStripTab tab) => strip.QStripSeparator.QStripHoverSet(tab);
    internal static void TStripHoverClear(QStrip strip, QStripTab? tab) => strip.QStripSeparator.QStripHoverClear(tab);
    internal static void TStripUpdateSuspend(QStrip strip) => strip.QStripUpdateSuspend();
    internal static bool TStripRelayCheck(QStripTab tab) => tab.QStripRelayCheck();
    internal static bool TStripCohortRun(QStripTab tab, Guid cohort) => tab.QStripCohortRun(cohort);
    internal static void TStripUpdateResume(QStrip strip) => strip.QStripUpdateResume();
    internal static void TStripTitleUpdate(QStrip strip) => strip.QStripTitle.QStripTitleUpdate();

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
        LEngineSeam.LEngineStationAttach);
    internal static bool TProgramDepotApply(QProgram program) => program.QProgramDepot.QProgramDepotApply();
    internal static string? TProgramDepotRead(QProgram program) => program.QProgramDepot.QProgramDepotRoot;
    internal static bool TProgramLanguageNormalize() => QProgram.QProgramLanguageNormalize();
    internal static void TProgramLanguageLoad(string? language) => QProgram.QProgramLanguageLoad(language);
    internal static string TLocalizationCatalogRead(string key) => QLocalizationCatalog.QLocalizationTextRead(key);
    internal static string TDepotRootRead() => LDepot.LDepotRootRead();
    internal static void TDepotRootSet(string? root) => LDepot.LDepotRootSet(root);
    internal static void TDepotIndexRelease() => LDepotIndex.LDepotIndexRelease();
    internal static string TDepotIndexFind() => LDepot.LDepotIndexFind();
}
