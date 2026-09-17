using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.Tests;

internal static partial class TInterface
{
    internal static LInspector TInspectorCreate() => new();
    internal static void TInspectorAttach(LInspector inspector, Action handler) =>
        inspector.LInspectorChange += handler;
    internal static void TInspectorSourceSet(LInspector inspector, double width, double height) =>
        inspector.LInspectorSourceSet(width, height);
    internal static void TInspectorStepSet(LInspector inspector, string? step) => inspector.LInspectorStepSet(step);
    internal static void TInspectorMinimizedSet(LInspector inspector, bool minimized) =>
        inspector.LInspectorMinimizedSet(minimized);
    internal static bool TInspectorOwnerSet(LInspector inspector, string path) => inspector.LInspectorOwnerSet(path);
    internal static bool TInspectorFailureSet(LInspector inspector, string? path) =>
        inspector.LInspectorFailureSet(path);

    internal static LWorkGammaSettings TGammaSettingsCreate(
        double global, double red, double green, double blue, double highlight) =>
        new(global, red, green, blue, highlight);
    internal static LWorkCurveSettings TCurveSettingsRead(LWorkVideoStep step) => step.LWorkCurveRead();

    internal static LTone TToneCreate() => new();
    internal static void TToneAttach(LTone tone, Action handler) => tone.LToneChange += handler;
    internal static void TToneValueSet(LTone tone, LColorKind kind, double value) => tone.LToneValueSet(kind, value);
    internal static void TToneActiveSet(LTone tone, LColorKind kind, bool active) => tone.LToneActiveSet(kind, active);
    internal static void TToneCapableSet(LTone tone, bool capable) => tone.LToneCapableSet(capable);
    internal static LWorkVideoStep TToneStepRead(LTone tone, LColorKind kind) => tone.LToneStepRead(kind);

    internal static LGamma TGammaCreate() => new();
    internal static void TGammaAttach(LGamma gamma, Action handler) => gamma.LGammaChange += handler;
    internal static void TGammaValueSet(LGamma gamma, int index, double value) => gamma.LGammaValueSet(index, value);
    internal static void TGammaReset(LGamma gamma) => gamma.LGammaReset();
    internal static void TGammaCapableSet(LGamma gamma, bool capable, bool preview) =>
        gamma.LGammaCapableSet(capable, preview);
    internal static LWorkGammaSettings TGammaValueRead(LGamma gamma) => gamma.LGammaValue;
    internal static bool TGammaPreviewRead(LGamma gamma) => gamma.LGammaPreview;

    internal static LExposure TExposureCreate() => new();
    internal static void TExposureAttach(LExposure exposure, Action handler) => exposure.LExposureChange += handler;
    internal static void TExposureValueSet(LExposure exposure, double value) => exposure.LExposureValueSet(value);
    internal static LWorkVideoStep TExposureStepRead(LExposure exposure) => exposure.LExposureStep;

    internal static LCurve TCurveCreate() => new();
    internal static void TCurveAttach(LCurve curve, Action handler) => curve.LCurveChange += handler;
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

    internal static LWhitebalance TWhitebalanceCreate() => new();
    internal static void TWhitebalanceAttach(LWhitebalance whitebalance, Action handler) =>
        whitebalance.LWhitebalanceChange += handler;
    internal static void TWhitebalanceEstimateAttach(LWhitebalance whitebalance, Action<LWhitebalanceMethod> handler) =>
        whitebalance.LWhitebalanceEstimateChange += handler;
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
    internal static void TNoiseAttach(LNoise noise, Action handler) => noise.LNoiseChange += handler;
    internal static void TNoisePresetSelect(LNoise noise, string token) => noise.LNoisePresetSelect(token);
    internal static void TNoiseValueSet(LNoise noise, int index, double value) => noise.LNoiseValueSet(index, value);
    internal static void TNoiseStepSet(LNoise noise, LWorkAudioStep step) => noise.LNoiseStepSet(step);
    internal static string? TNoiseTokenRead(LNoise noise) => noise.LNoiseToken;
    internal static string? TNoiseMatchRead(LNoise noise) => noise.LNoiseMatchRead();
    internal static LWorkNoiseStep TNoiseStepRead(LNoise noise) => noise.LNoiseStep;

    internal static LLoudness TLoudnessCreate() => new();
    internal static void TLoudnessAttach(LLoudness loudness, Action handler) => loudness.LLoudnessChange += handler;
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
    internal static void TFilterAttach(LFilter filter, Action handler) => filter.LFilterChange += handler;
    internal static void TFilterPresetSelect(LFilter filter, string token) => filter.LFilterPresetSelect(token);
    internal static void TFilterFrequencySet(LFilter filter, double frequency) => filter.LFilterFrequencySet(frequency);
    internal static string? TFilterTokenRead(LFilter filter) => filter.LFilterToken;
    internal static string? TFilterMatchRead(LFilter filter) => filter.LFilterMatchRead();
    internal static LWorkPassStep TFilterStepRead(LFilter filter) => filter.LFilterStep;

    internal static LEqualizer TEqualizerCreate() => new();
    internal static void TEqualizerAttach(LEqualizer equalizer, Action handler) =>
        equalizer.LEqualizerChange += handler;
    internal static void TEqualizerPresetSelect(LEqualizer equalizer, string token) =>
        equalizer.LEqualizerPresetSelect(token);
    internal static void TEqualizerBandSet(LEqualizer equalizer, int index, double frequency, double gain) =>
        equalizer.LEqualizerBandSet(index, frequency, gain);
    internal static void TEqualizerBandAdd(LEqualizer equalizer) => equalizer.LEqualizerBandAdd();
    internal static void TEqualizerBandRemove(LEqualizer equalizer, int index) => equalizer.LEqualizerBandRemove(index);
    internal static string? TEqualizerTokenRead(LEqualizer equalizer) => equalizer.LEqualizerToken;
    internal static string? TEqualizerMatchRead(LEqualizer equalizer) => equalizer.LEqualizerMatchRead();
    internal static IReadOnlyList<LWorkBand> TEqualizerBandsRead(LEqualizer equalizer) => equalizer.LEqualizerBands;

    internal static LVolume TVolumeCreate() => new();
    internal static void TVolumeAttach(LVolume volume, Action handler) => volume.LVolumeChange += handler;
    internal static void TVolumeGainSet(LVolume volume, double gain) => volume.LVolumeGainSet(gain);
    internal static void TVolumeActiveSet(LVolume volume, bool active) => volume.LVolumeActiveSet(active);
    internal static double TVolumeGainRead(LVolume volume) => volume.LVolumeGain;

    internal static LSkip TSkipCreate() => new();
    internal static void TSkipAttach(LSkip skip, Action handler) => skip.LSkipChange += handler;
    internal static void TSkipActiveSet(LSkip skip, bool active) => skip.LSkipActiveSet(active);
    internal static void TSkipPersistentSet(LSkip skip, bool persistent) => skip.LSkipPersistentSet(persistent);

    internal static LCropboxState TCropboxStateCreate() => new();
    internal static void TCropboxStateAttach(LCropboxState state, Action handler) =>
        state.LCropboxStateChange += handler;
    internal static void TCropboxCropSet(LCropboxState state, LWorkCrop crop) => state.LCropboxCropSet(crop);
    internal static void TCropboxRatioSet(LCropboxState state, bool fixedRatio, bool lenient, int width, int height) =>
        state.LCropboxRatioSet(fixedRatio, lenient, width, height);

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
    internal static void TViewerPreviewSet(LViewer viewer, LPreviewState preview) => viewer.LViewerPreviewSet(preview);
    internal static void TViewerPlaybackUpdate(LViewer viewer, bool? playing, TimeSpan? position) =>
        viewer.LViewerPlaybackUpdate(playing, position);
    internal static void TViewerMediaCommit(LViewer viewer, LCargo cargo, bool persistent) =>
        viewer.LViewerMediaCommit(cargo, persistent);
    internal static void TViewerMediaRaise(LViewer viewer, LCargo cargo) => viewer.LViewerMediaRaise(cargo);
    internal static void TViewerMediaClose(LViewer viewer) => viewer.LViewerMediaClose();
    internal static bool TViewerNeutralSet(LViewer viewer, LNeutralTarget target) => viewer.LViewerNeutralSet(target);
    internal static bool TViewerNeutralCancel(LViewer viewer) => viewer.LViewerNeutralCancel();
    internal static bool TViewerNeutralReset(LViewer viewer) => viewer.LViewerNeutralReset();
    internal static LCargo TCargoCreate(string path, LMediaInfo? info, bool preview) =>
        new(path, info, info is not null, preview, null, null);
    internal static LMediaInfo TViewerInfoCreate(TimeSpan duration, int width, int height) =>
        new(duration, width, height, 25, "h264", false, "", 0, 0);
    internal static LPreviewState TPreviewCropCreate(double x, double y, double width, double height) =>
        LPreviewState.LPreviewDefaultCreate().LCropboxChange(new LCropbox(x, y, width, height));

    internal static LPlayer TPlayerCreate() => new();
    internal static bool TPlayerAccurateSet(LPlayer player) => player.LPlayerAccurateSet();
    internal static void TPlayerAccurateReset(LPlayer player) => player.LPlayerAccurateReset();
    internal static void TPlayerRendererSet(LPlayer player, bool pending) => player.LPlayerRendererSet(pending);
    internal static bool TPlayerSeekCommit(LPlayer player, int milliseconds) => player.LPlayerSeekCommit(milliseconds);
    internal static Task TPlayerOpenStart(LPlayer player, string path, Action<string> open) =>
        player.LPlayerOpenStart(path, open);
    internal static void TPlayerFilterSet(LPlayer player, string filter) => player.LPlayerFilterSet(filter);
    internal static void TPlayerAppliedReset(LPlayer player) => player.LPlayerAppliedReset();
    internal static void TPlayerEndSet(LPlayer player, TimeSpan? end) => player.LPlayerEndSet(end);

    internal static LCrop TCropCreate() => new();
    internal static void TCropGripSet(LCrop crop, int edgeX, int edgeY) => crop.LCropGripSet(edgeX, edgeY);
    internal static void TCropBodySet(LCrop crop) => crop.LCropBodySet();
    internal static void TCropDrawSet(LCrop crop) => crop.LCropDrawSet();
    internal static bool TCropMoveCheck(LCrop crop) => crop.LCropMoveCheck();
    internal static void TCropRatioSet(LCrop crop, double width, double height) =>
        crop.LCropRatioSet(width, height);
    internal static void TCropLockSet(LCrop crop, bool locked) => crop.LCropLockSet(locked);

    internal static LSLoupe TLoupeCreate() => new();
    internal static void TLoupePlayingAttach(LSLoupe loupe, Action<bool> handler) =>
        loupe.LSLoupePlayingChange += handler;
    internal static void TLoupePlayingSet(LSLoupe loupe, bool playing) => loupe.LSLoupePlayingSet(playing);
    internal static void TLoupeEndSet(LSLoupe loupe, bool ended) => loupe.LSLoupeEndSet(ended);
    internal static void TLoupeClose(LSLoupe loupe) => loupe.LSLoupeClose();
    internal static bool TLoupeResumeCheck(LSLoupe loupe) => loupe.LSLoupeResumeCheck();

    internal static LDocket TDocketCreate() => new();
    internal static int TDocketPathsAdd(LDocket docket, params string[] paths) => docket.LDocketPathsAdd(paths);
    internal static int TDocketPathsRemove(LDocket docket, params string[] paths) => docket.LDocketPathsRemove(paths);
    internal static LList TListCreate(LDocket docket) => new(docket);
    internal static void TListPathAttach(LList list, Action<string?> handler) => list.LListPathChange += handler;
    internal static void TListSelect(LList list, string? path) => list.LListSelect(path);
    internal static void TListPressSelect(LList list, string path, bool shift, bool control) =>
        list.LListPressSelect(path, shift, control);
    internal static void TListReleaseSelect(LList list) => list.LListReleaseSelect();
    internal static void TListAllSelect(LList list) => list.LListAllSelect();
    internal static IReadOnlyList<string> TListSelectionRead(LList list) => list.LListSelectionRead();
    internal static void TListSuccessorSet(LList list, params string[] removed) => list.LListSuccessorSet(removed);
    internal static void TListSuccessorReset(LList list) => list.LListSuccessorReset();
    internal static void TListRemovedApply(LList list, params string[] removed) => list.LListRemovedApply(removed);
    internal static void TListSuccessorSelect(LList list) => list.LListSuccessorSelect();

    internal static LFunnel TFunnelCreate() => new();
    internal static void TFunnelAttach(LFunnel funnel, Action handler) => funnel.LFunnelChange += handler;
    internal static void TFunnelRuleAttach(LFunnel funnel, Action<LFunnelRule> handler) =>
        funnel.LFunnelRuleChange += handler;
    internal static LFunnelRule TFunnelRuleAdd(LFunnel funnel, LFunnelForm form) => funnel.LFunnelRuleAdd(form);
    internal static void TFunnelRuleRemove(LFunnel funnel, LFunnelRule rule) => funnel.LFunnelRuleRemove(rule);
    internal static bool TFunnelRuleMove(LFunnel funnel, LFunnelRule rule, int target) =>
        funnel.LFunnelRuleMove(rule, target);
    internal static void TFunnelRuleSelect(LFunnel funnel, LFunnelRule rule) => funnel.LFunnelRuleSelect(rule);
    internal static void TFunnelJoinSet(LFunnel funnel, LFunnelRule rule, LFunnelKind kind, bool and) =>
        funnel.LFunnelJoinSet(rule, kind, and);
    internal static void TFunnelTextSet(LFunnel funnel, LFunnelRule rule, LFunnelKind kind, string text) =>
        funnel.LFunnelTextSet(rule, kind, text);
    internal static void TFunnelTargetSet(LFunnel funnel, LFunnelRule rule, Guid target) =>
        funnel.LFunnelTargetSet(rule, target);
    internal static void TFunnelTargetsResolve(LFunnel funnel, params Guid[] tabs) =>
        funnel.LFunnelTargetsResolve(tabs);
    internal static void TFunnelRulesRestore(LFunnel funnel, params LSceneFunnelRule[] records) =>
        funnel.LFunnelRulesRestore(records);
    internal static LSceneFunnelRule TFunnelRecordCreate(LFunnel funnel, LFunnelRule rule) =>
        funnel.LFunnelRecordCreate(rule);
    internal static LSceneFunnelRule TFunnelRecordCreate(int type, bool remainder, int target, string regex = "") =>
        new()
        {
            LSceneFunnelType = type,
            LSceneFunnelRemainder = remainder,
            LSceneFunnelTarget = target,
            LSceneFunnelRegex = regex,
        };
    internal static bool TFunnelRemainderCheck(LFunnel funnel) => funnel.LFunnelRemainderCheck();

    internal static LPresetSelection TPresetSelectionCreate(string name) => new(name);
    internal static LExport TExportCreate(LPresetSelection selection, bool smart) => new(selection, smart);
    internal static void TExportAttach(LExport export, Action handler) => export.LExportPresetsChange += handler;
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

    internal static LProcessing TProcessingCreate() => new();
    internal static void TProcessingOrderAttach(LProcessing processing, Action handler) =>
        processing.LProcessingOrderChange += handler;
    internal static void TProcessingStepAttach(LProcessing processing, Action<string> handler) =>
        processing.LProcessingStepChange += handler;
    internal static void TProcessingStepAdd(LProcessing processing, string step) => processing.LProcessingStepAdd(step);
    internal static void TProcessingOrderedSet(LProcessing processing, bool ordered) =>
        processing.LProcessingOrderedSet(ordered);
    internal static void TProcessingEnabledSet(LProcessing processing, string step, bool enabled) =>
        processing.LProcessingEnabledSet(step, enabled);
    internal static bool TProcessingStepSelect(LProcessing processing, string step) =>
        processing.LProcessingStepSelect(step);
    internal static bool TProcessingStepMove(LProcessing processing, int delta) =>
        processing.LProcessingStepMove(delta);
    internal static bool TProcessingIndexMove(LProcessing processing, int from, int to) =>
        processing.LProcessingIndexMove(from, to);
    internal static void TProcessingDragSet(LProcessing processing, int? index) => processing.LProcessingDragSet(index);

    internal static LClinic TClinicCreate() => new();
    internal static void TClinicPlanAttach(LClinic clinic, Action handler) => clinic.LClinicPlanChange += handler;
    internal static void TClinicStepSet(LClinic clinic, string? step) => clinic.LClinicStepSet(step);
    internal static void TClinicActiveSet(LClinic clinic, bool active) => clinic.LClinicActiveSet(active);
    internal static void TClinicSalvageSet(LClinic clinic, LWorkFixSalvage salvage) =>
        clinic.LClinicSalvageSet(salvage);
    internal static LWorkFixSalvage TClinicSalvageCreate(
        bool active, LSalvageMode mode, LSalvageBasis basis, bool persistent) =>
        new(active, mode, basis, persistent);
    internal static LWorkFix TClinicPlanRead(LClinic clinic) => clinic.LClinicPlanRead();
    internal static void TClinicPlanApply(LClinic clinic, LWorkFix plan) => clinic.LClinicPlanApply(plan);
    internal static bool TClinicRepairCheck(LClinic clinic) => clinic.LClinicRepairCheck();

    internal static LStrip TStripCreate(Func<string, string> title, Func<string, int, string> number) =>
        new(title, number);
    internal static LStripTab TStripTabCreate(string key) => new(key);
    internal static void TStripSelectAttach(LStrip strip, Action<LStripTab?> handler) =>
        strip.LStripSelectChange += handler;
    internal static void TStripTabAttach(LStrip strip, Action<LStripTab> handler) => strip.LStripTabChange += handler;
    internal static void TStripAdd(LStrip strip, LStripTab tab) => strip.LStripAdd(tab);
    internal static void TStripRemove(LStrip strip, LStripTab tab) => strip.LStripRemove(tab);
    internal static bool TStripMove(LStrip strip, LStripTab tab, int index) => strip.LStripMove(tab, index);
    internal static void TStripSelect(LStrip strip, LStripTab? tab) => strip.LStripSelect(tab);
    internal static bool TStripNameSet(LStrip strip, LStripTab tab, string? name) => strip.LStripNameSet(tab, name);
    internal static void TStripHoverSet(LStrip strip, LStripTab tab) => strip.LStripHoverSet(tab);
    internal static void TStripHoverClear(LStrip strip, LStripTab? tab) => strip.LStripHoverClear(tab);
    internal static void TStripUpdateSuspend(LStrip strip) => strip.LStripUpdateSuspend();
    internal static void TStripUpdateResume(LStrip strip) => strip.LStripUpdateResume();
    internal static void TStripTitleUpdate(LStrip strip) => strip.LStripTitleUpdate();

    internal static LFlow TFlowCreate() => new();
    internal static void TFlowEditAttach(LFlow flow, Action<bool> handler) => flow.LFlowEditChange += handler;
    internal static void TFlowSourceSet(LFlow flow, LMediaInfo media, string? path) => flow.LFlowSourceSet(media, path);
    internal static bool TFlowSourceMatch(LFlow flow, string? path) => flow.LFlowSourceMatch(path);
    internal static void TFlowSourceClear(LFlow flow) => flow.LFlowSourceClear();
    internal static void TFlowCursorSet(LFlow flow, TimeSpan cursor) => flow.LFlowCursorSet(cursor);
    internal static void TFlowSectionSet(LFlow flow, bool active) => flow.LFlowSectionSet(active);
    internal static void TFlowCommandSet(LFlow flow, bool active) => flow.LFlowCommandSet(active);
    internal static void TFlowUnloadSet(LFlow flow) => flow.LFlowUnloadSet();
    internal static bool TFlowEditSet(LFlow flow, bool editable) => flow.LFlowEditSet(editable);
    internal static bool TFlowEditCheck(LFlow flow) => flow.LFlowEditCheck();
    internal static bool TFlowSourceCheck(LFlow flow) => flow.LFlowSourceCheck();
    internal static bool TFlowScanCheck(LFlow flow) => flow.LFlowScanCheck();
    internal static bool TFlowStampSet(LFlow flow, string stamp) => flow.LFlowStampSet(stamp);
    internal static bool TFlowLosslesscutSet(LFlow flow, string path) => flow.LFlowLosslesscutSet(path);

    internal static LConsole TConsoleCreate() => new();
    internal static bool TConsoleSpinSet(LConsole console, bool spinning) => console.LConsoleSpinSet(spinning);
    internal static bool TConsoleCaretSet(LConsole console) => console.LConsoleCaretSet();
    internal static bool TConsoleProgressSet(LConsole console, double target) => console.LConsoleProgressSet(target);
    internal static void TConsoleReloadSet(LConsole console, string? name) => console.LConsoleReloadSet(name);
    internal static string? TConsoleReloadRead(LConsole console) => console.LConsoleReloadRead();
    internal static void TConsoleSceneSet(LConsole console, string name) => console.LConsoleSceneSet(name);
    internal static bool TConsoleSceneCheck(LConsole console, string name) => console.LConsoleSceneCheck(name);

    internal static LRoster TRosterCreate() => new();
    internal static void TRosterOrderAdd(LRoster roster, Guid id) => roster.LRosterOrderAdd(id);
    internal static void TRosterOrderClear(LRoster roster) => roster.LRosterOrderClear();
    internal static bool TRosterOrderMatch(LRoster roster, IReadOnlyList<Guid> ids) => roster.LRosterOrderMatch(ids);
    internal static void TRosterStepSelect(LRoster roster, Guid id, bool range, bool toggle) =>
        roster.LRosterStepSelect(id, range, toggle);
    internal static void TRosterCardSelect(LRoster roster, Guid batchId) => roster.LRosterCardSelect(batchId);
    internal static bool TRosterSelectedCheck(LRoster roster, Guid id) => roster.LRosterSelectedCheck(id);
    internal static bool TRosterCollapseToggle(LRoster roster, Guid batchId) => roster.LRosterCollapseToggle(batchId);
    internal static bool TRosterCollapsedCheck(LRoster roster, Guid batchId) => roster.LRosterCollapsedCheck(batchId);
    internal static void TRosterStaleRemove(LRoster roster, IReadOnlyCollection<Guid> batchIds) =>
        roster.LRosterStaleRemove(batchIds);
    internal static bool TRosterCloseSet(LRoster roster) => roster.LRosterCloseSet();

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
    internal static bool TColumnAppliedSet(LColumn column, double available) => column.LColumnAppliedSet(available);
    internal static bool TColumnHiddenCheck(LColumn column, int index) => column.LColumnHiddenCheck(index);
    internal static double TColumnWeightRead(LColumn column, int index) => column.LColumnWeightRead(index);
    internal static double TColumnFixedRead(LColumn column, int index) => column.LColumnFixedRead(index);
    internal static double TColumnPixelRead(LColumn column, int index) => column.LColumnPixelRead(index);

    internal static LLog TLogCreate() => new();
    internal static LLogRow TLogRowCreate() => new();
    internal static void TLogFileSet(LLog log, string path, string livePath) => log.LLogFileSet(path, livePath);
    internal static bool TLogLiveSet(LLog log, string livePath) => log.LLogLiveSet(livePath);
    internal static bool TLogFileCheck(LLog log, string path) => log.LLogFileCheck(path);
    internal static void TLogFollowSet(LLog log, bool follow) => log.LLogFollowSet(follow);
    internal static void TLogSnapshotSet(LLog log, long sequence) => log.LLogSnapshotSet(sequence);
    internal static bool TLogSnapshotCheck(LLog log, long sequence) => log.LLogSnapshotCheck(sequence);
    internal static bool TLogExpandToggle(LLog log, LLogRow row) => log.LLogExpandToggle(row);
}
