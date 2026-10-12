using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static partial class TInterface
{
    internal static LConsole TConsoleCreate() => new();
    internal static bool TConsoleProgressSet(LConsole console, double target) => console.LConsoleProgressSet(target);
    internal static void TConsoleProgressAttach(LConsole console, Action<double, bool> handler) =>
        console.LConsoleProgressApplied += handler;
    internal static IReadOnlyList<LConsoleSpan> TConsoleRunsResolve(string line, string? accent) =>
        LConsole.LConsoleRunsResolve(line, accent);
    internal static string? TConsoleRemovalFormat(IReadOnlyDictionary<Guid, LScheduleRemoval> outcomes) =>
        LConsole.LConsoleRemovalFormat(outcomes);
    internal static int TConsoleIndexResolve(int index, int step, int count) =>
        LConsoleStation.LConsoleIndexResolve(index, step, count);

    internal static LConsoleScene TConsoleSceneCreate() => new();
    internal static void TConsoleReloadSet(LConsoleScene scene, string? name) => scene.LConsoleReloadSet(name);
    internal static string? TConsoleReloadRead(LConsoleScene scene) => scene.LConsoleReloadRead();
    internal static void TConsoleRowHandle(LConsoleScene scene, string? name) => scene.LConsoleRowHandle(name);
    internal static void TConsoleSceneSet(LConsoleScene scene, string name) => scene.LConsoleSceneSet(name);
    internal static bool TConsoleSceneCheck(LConsoleScene scene, string name) => scene.LConsoleSceneCheck(name);
    internal static bool TConsoleDirtyCheck(LConsoleScene scene) => scene.LConsoleDirtyCheck();
    internal static bool TConsoleDeleteHandle(LConsoleScene scene, string? name) => scene.LConsoleDeleteHandle(name);
    internal static void TConsoleFocusAttach(LConsoleScene scene, Action handler) =>
        scene.LConsoleFocusCleared += handler;
    internal static void TConsolePressHandle(LConsoleScene scene, bool dropOpen, bool focusWithin, bool inside) =>
        scene.LConsolePressHandle(dropOpen, focusWithin, inside);
    internal static string TConsoleStemResolve(string name, string path) =>
        LConsoleScene.LConsoleStemResolve(name, path);
    internal static string TConsoleNameCreate(string baseName, IReadOnlyList<string> names) =>
        LConsoleScene.LConsoleNameCreate(baseName, names);

    internal static LEditTab TEditTabCreate(LPresetSelection preset, LViewer viewer, LList list) =>
        new(TAtelierCreate().CAtelierUsher, preset, viewer, list);
    internal static LInspector TEditInspectorRead(LEditTab tab) => tab.LEditInspector;
    internal static LProcessing TEditProcessingRead(LEditTab tab) => tab.LEditProcessing;
    internal static void TEditClose(LEditTab tab) => tab.LEditClose();
    internal static void TEditStart(LEditTab tab) => tab.LEditStart();
    internal static LSceneTabEntry TEditLayoutRead(LEditTab tab) => tab.LEditLayoutRead(new LSceneTabEntry());
    internal static void TEditLayoutApply(LEditTab tab, LSceneTabEntry? layout) => tab.LEditLayoutApply(layout);
    internal static void TEditCropHandle(LEditTab tab) => tab.LEditCropHandle();
    internal static void TEditSkipHandle(LEditTab tab) => tab.LEditSkipHandle();
    internal static void TEditLockHandle(LEditTab tab, bool locked) => tab.QEditTool.LEditLockHandle(locked);
    internal static void TEditPathHandle(LEditTab tab, string? path) => tab.LEditPathHandle(path);
    internal static void TEditHistogramAttach(LEditTab tab, Action handler) => tab.LEditHistogramDeferred += handler;
    internal static void TEditMissingAttach(LEditTab tab, Action handler) => tab.LEditPresetMissing += handler;
    internal static void TEditRun(LEditTab tab, LWorkPriority priority) => tab.LEditRun(priority, default, default);
    internal static LEditPlan TEditStateRead(LEditTab tab) => tab.LEditStore.LEditStateRead();
    internal static LEditPlan? TEditCarriedRead(LEditTab tab) => tab.LEditStore.LEditCarriedRead();
    internal static void TEditStateSave(LEditTab tab) => tab.LEditStore.LEditStateSave();
    internal static void TEditPersistentSave(LEditTab tab) => tab.LEditStore.LEditPersistentSave();
    internal static string TEditPlanFormat(LEditPlan? plan) => LEditTabPlan.LEditPlanFormat(plan);
    internal static string TEditCropFormat(LWorkCrop? crop) => LEditTabPlan.LEditCropFormat(crop);
    internal static string TEditRectFormat(LCropbox? rect) => LEditTabPlan.LEditRectFormat(rect);
    internal static LWorkVideo TEditVideoRead(LEditTab tab, bool mpvOnlyCapable) =>
        tab.LEditColor.LEditVideoRead(mpvOnlyCapable);
    internal static void TEditColorUpdate(LEditTab tab) => tab.LEditColor.LEditColorUpdate();
    internal static void TEditPreviewAttach(LViewer viewer, Action<LColor> handler) =>
        viewer.LViewerPreviewChanged += () => handler(viewer.LViewerPreview.LColor);
    internal static void TEditColorApply(LEditTab tab) => tab.LEditColor.LEditColorApply();
    internal static void TEditNeutralHandle(LEditTab tab, LNeutralSample sample) =>
        tab.LEditColor.LEditNeutralHandle(sample);
    internal static void TEditEstimateHandle(LEditTab tab, LWhitebalanceMethod method) =>
        tab.LEditColor.LEditEstimateHandle(method);
    internal static void TEditEstimateApply(LEditTab tab, double x, double y, bool present) =>
        tab.LEditColor.LEditEstimateApply(new LNeutralWheel(x, y, present));
    internal static void TEditHistogramApply(LEditTab tab, int width, int height, byte[]? pixels) =>
        tab.LEditColor.LEditHistogramApply(pixels is null ? null : new LMediaFrame(width, height, pixels));
    internal static void TEditLibrarianAttach(
        Func<string, LSidecarEditEntry?>? reader, Func<string, LSidecarEditEntry?, bool>? writer)
    {
        LLibrarian.LLibrarianEditReader = reader;
        LLibrarian.LLibrarianEditWriter = writer;
    }

    internal static LSplitTab TSplitTabCreate(LPresetSelection preset, LViewer viewer, LList list, LFlow flow) =>
        new(preset, viewer, list, flow);
    internal static LInspector TSplitInspectorRead(LSplitTab tab) => tab.LSplitInspector;
    internal static LProcessing TSplitProcessingRead(LSplitTab tab) => tab.LSplitProcessing;
    internal static IReadOnlyList<LProcessingRow> TSplitRowsRead() => LSplitTab.LSplitRows;
    internal static void TSplitClose(LSplitTab tab) => tab.LSplitClose();
    internal static void TSplitStart(LSplitTab tab) => tab.LSplitStart();
    internal static LSceneTabEntry TSplitLayoutRead(LSplitTab tab) => tab.LSplitLayoutRead(new LSceneTabEntry());
    internal static void TSplitLayoutApply(LSplitTab tab, LSceneTabEntry? layout) => tab.LSplitLayoutApply(layout);
    internal static void TSplitPathHandle(LSplitTab tab, string? path) => tab.LSplitPathHandle(path);
    internal static void TSplitLockHandle(LSplitTab tab, bool locked) => tab.LSplitLockHandle(locked);
    internal static void TSplitPersistentHandle(LSplitTab tab, bool persistent) =>
        tab.LSplitPersistentHandle(persistent);
    internal static void TSplitStateSave(LSplitTab tab) => tab.LSplitStateSave();
    internal static void TSplitStateLoad(LSplitTab tab, string path) => tab.LSplitStateLoad(path);
    internal static LDetectorBundle TSplitStateRead(LSplitTab tab) => tab.LSplitStateRead();
    internal static void TSplitMissingAttach(LSplitTab tab, Action handler) => tab.LSplitPresetMissing += handler;
    internal static void TSplitRun(LSplitTab tab, LWorkPriority priority) => tab.LSplitRun(priority, default, default);
    internal static IReadOnlyList<LDetectorKind> TSplitStepsRead(LSplitTab tab) => tab.LSplitSweep.LSplitStepsRead();
    internal static Task TSplitSweepStart(LSplitTab tab) => tab.LSplitSweep.LSplitSweepStart();
    internal static void TSplitSweepCancel(LSplitTab tab) => tab.LSplitSweep.LSplitSweepCancel();
    internal static bool TSplitSweepCheck(LSplitTab tab) => tab.LSplitSweep.LSplitSweepRunning;
    internal static void TSplitBusyAttach(LSplitTab tab, Action<bool> handler) =>
        tab.LSplitSweep.LSplitBusyApplied += handler;
    internal static void TSplitFailAttach(LSplitTab tab, Action<string, string> handler) =>
        tab.LSplitSweep.LSplitFailRaised += handler;
    internal static void TSplitLibrarianAttach(
        Func<string, LSidecarSplitEntry?>? reader, Func<string, LSidecarSplitEntry?, bool>? writer)
    {
        LLibrarian.LLibrarianSplitReader = reader;
        LLibrarian.LLibrarianSplitWriter = writer;
    }

    internal static LDetectorStep TDetectorStepCreate(
        LDetectorKind kind, bool enabled, double threshold, double minimum, double window) =>
        new(kind, enabled, threshold, minimum, window);
    internal static LDetectorBlank TDetectorBlankCreate(
        bool enabled,
        LDetectorType type,
        double hue,
        double saturation,
        double brightness,
        double tolerance,
        double coverage,
        double minimum) => new(enabled, type, hue, saturation, brightness, tolerance, coverage, minimum);
    internal static void TSensorEnabledSet(LSensor sensor, LDetectorKind kind, bool enabled) =>
        sensor.LSensorEnabledSet(kind, enabled);
    internal static void TSensorModeSet(LSensor sensor, LDetectorStillMode mode) => sensor.LSensorModeSet(mode);
    internal static void TSensorSpeedSet(LSensor sensor, LDetectorLuminanceMode speed) => sensor.LSensorSpeedSet(speed);
    internal static void TSensorPersistentSet(LSensor sensor, bool persistent) =>
        sensor.LSensorPersistentSet(persistent);
    internal static string TSensorNameRead(LDetectorKind kind) => LSensor.LSensorNameRead(kind);
    internal static LDetectorKind? TSensorKindRead(string? name) => LSensor.LSensorKindRead(name);

    internal static LFixTab TFixTabCreate(LPresetSelection preset, LViewer viewer, LList list) =>
        new(preset, viewer, list);
    internal static LClinic TFixClinicRead(LFixTab tab) => tab.LFixClinic;
    internal static LProcessing TFixProcessingRead(LFixTab tab) => tab.LFixProcessing;
    internal static IReadOnlyList<LProcessingRow> TFixRowsRead() => LFixTab.LFixRows;
    internal static IReadOnlyList<LFixRow> TFixKindsRead() => LFixTab.LFixKinds;
    internal static void TFixClose(LFixTab tab) => tab.LFixClose();
    internal static LSceneTabEntry TFixLayoutRead(LFixTab tab) => tab.LFixLayoutRead(new LSceneTabEntry());
    internal static void TFixLayoutApply(LFixTab tab, LSceneTabEntry? layout) => tab.LFixLayoutApply(layout);
    internal static void TFixPathHandle(LFixTab tab, string? path) => tab.LFixPathHandle(path);
    internal static void TFixDiagnosisRun(LFixTab tab) => tab.LFixDiagnosisRun();
    internal static void TFixCheckupHandle(LFixTab tab, string path, LFlawKind kind, LCheckupOutcome outcome) =>
        tab.LFixCheckupHandle(new LCheckupResult(path, kind, outcome));
    internal static void TFixProgressHandle(LFixTab tab, string path, double progress) =>
        tab.LFixProgressHandle(path, progress);
    internal static void TFixClearHandle(LFixTab tab, params string[] removed) => tab.LFixClearHandle(removed);
    internal static void TFixChangeHandle(LFixTab tab) => tab.LFixChangeHandle();
    internal static void TFixStateSave(LFixTab tab) => tab.LFixStateSave();
    internal static void TFixPlanRestore(LFixTab tab, string path) => tab.LFixPlanRestore(path);
    internal static void TFixMissingAttach(LFixTab tab, Action handler) => tab.LFixPresetMissing += handler;
    internal static void TFixRun(LFixTab tab, LWorkPriority priority) => tab.LFixRun(priority, default, default);
    internal static void TFixAllRun(LFixTab tab) => tab.LFixAllRun(default, default);
    internal static void TFixCheckupAttach(LFixTab tab, Action<LCheckupResult> handler) =>
        tab.LFixCheckup.LCheckupReady += handler;
    internal static void TClinicSaveSuspend(LClinic clinic) => clinic.LClinicSaveSuspend();
    internal static void TClinicSaveResume(LClinic clinic) => clinic.LClinicSaveResume();
    internal static void TFixScannerAttach(Action<string>? scanner)
    {
        LLibrarian.LLibrarianDiagnosisReader = scanner is null ? null : _ => null;
        LCheckup.LCheckupScannerSeam = scanner is null
            ? null
            : (path, _, _, _) =>
            {
                scanner(path);
                return Task.FromResult<IReadOnlyList<LDossier>>(Array.Empty<LDossier>());
            };
    }
    internal static void TFixLibrarianAttach(
        Func<string, LSidecarFixEntry?>? reader, Func<string, LSidecarFixEntry?, bool>? writer)
    {
        LLibrarian.LLibrarianFixReader = reader;
        LLibrarian.LLibrarianFixWriter = writer;
    }

    internal static LAudioTab TAudioTabCreate(LPresetSelection preset, LViewer viewer, LList list) =>
        new(TAtelierCreate().CAtelierUsher, preset, viewer, list);
    internal static LInspector TAudioInspectorRead(LAudioTab tab) => tab.LAudioInspector;
    internal static LProcessing TAudioProcessingRead(LAudioTab tab) => tab.LAudioProcessing;
    internal static IReadOnlyList<LProcessingRow> TAudioRowsRead() => LAudioTab.LAudioRows;
    internal static LAudioKind? TAudioKindRead(string step) => LAudioTab.LAudioKindRead(step);
    internal static void TAudioClose(LAudioTab tab) => tab.LAudioClose();
    internal static LSceneTabEntry TAudioLayoutRead(LAudioTab tab) => tab.LAudioLayoutRead(new LSceneTabEntry());
    internal static void TAudioLayoutApply(LAudioTab tab, LSceneTabEntry? layout) => tab.LAudioLayoutApply(layout);
    internal static void TAudioPathHandle(LAudioTab tab, string? path) => tab.LAudioPathHandle(path);
    internal static void TAudioSkipHandle(LAudioTab tab) => tab.LAudioSkipHandle();
    internal static void TAudioChangeHandle(LAudioTab tab) => tab.LAudioChangeHandle();
    internal static void TAudioItemsHandle(LAudioTab tab, IReadOnlyList<LDocketEntry> added) =>
        tab.LAudioItemsHandle(added);
    internal static void TAudioPersistentSave(LAudioTab tab) => tab.LAudioPersistentSave();
    internal static void TAudioViewerApply(LAudioTab tab) => tab.LAudioViewerApply();
    internal static LWorkAudio TAudioPlanRead(LAudioTab tab) => tab.LAudioPlanRead();
    internal static void TAudioStateSave(LAudioTab tab) => tab.LAudioStateSave();
    internal static void TAudioPlanRestore(LAudioTab tab, string path, bool ownerFirst) =>
        tab.LAudioPlanRestore(path, ownerFirst);
    internal static void TAudioFilterAttach(LViewer viewer, Action<string> handler) =>
        viewer.LViewerPreviewChanged += () => handler(viewer.LViewerAudioResolve());
    internal static void TAudioDeferAttach(LAudioTab tab, Action handler) => tab.LAudioViewerDeferred += handler;
    internal static void TAudioMissingAttach(LAudioTab tab, Action handler) => tab.LAudioPresetMissing += handler;
    internal static void TAudioIncompatibleAttach(LAudioTab tab, Action handler) =>
        tab.LAudioPresetIncompatible += handler;
    internal static void TAudioRun(LAudioTab tab, LWorkPriority priority) => tab.LAudioRun(priority, default, default);
    internal static void TAudioAllRun(LAudioTab tab) => tab.LAudioAllRun(default, default);
    internal static void TAudioLibrarianAttach(
        Func<string, LSidecarAudioEntry?>? reader, Func<string, LSidecarAudioEntry?, bool>? writer)
    {
        LLibrarian.LLibrarianAudioReader = reader;
        LLibrarian.LLibrarianAudioWriter = writer;
    }

    internal static void TVolumePersistentSet(LVolume volume, bool persistent) =>
        volume.LVolumePersistentSet(persistent);

    internal static LMergeTab TMergeTabCreate(LPresetSelection preset, LViewer viewer, LList list) =>
        new(TAtelierCreate().CAtelierUsher, preset, viewer, list);
    internal static void TMergeLayoutApply(LMergeTab tab, LSceneTabEntry? layout) => tab.LMergeLayoutApply(layout);
    internal static void TMergeClose(LMergeTab tab) => tab.LMergeClose();
    internal static LSceneTabEntry TMergeLayoutRead(LMergeTab tab) => tab.LMergeLayoutRead(new LSceneTabEntry());
    internal static IReadOnlyList<LWorkGroup> TMergeGroupsRead(LMergeTab tab, Guid cohort = default) =>
        tab.LMergeGroupsRead(cohort);
    internal static IReadOnlyDictionary<string, Guid> TMergeRelaysRead(LMergeTab tab) => tab.LMergeRelaysRead();
    internal static IReadOnlyList<string> TMergePathsRead(LMergeTab tab) => tab.LMergePathsRead();
    internal static IReadOnlyList<string> TMergeEligibleRead(LMergeTab tab) => tab.LMergeEligibleRead();
    internal static void TMergeMissingAttach(LMergeTab tab, Action handler) => tab.LMergePresetMissing += handler;
    internal static void TMergeRun(LMergeTab tab, LWorkPriority priority) => tab.LMergeRun(priority, default, default);
    internal static int TMergeCohortRun(LMergeTab tab, Guid cohort) => tab.LMergeCohortRun(cohort, default, default);

    internal static void TGroupAutoChange(LGroupSelection selection, bool auto) => selection.LGroupAutoChange(auto);
    internal static void TGroupStrictChange(LGroupSelection selection, bool strict) =>
        selection.LGroupStrictChange(strict);

    internal static LConvertTab TConvertTabCreate(LPresetSelection preset, LViewer viewer, LList list) =>
        new(preset, viewer, list);
    internal static void TConvertMissingAttach(LConvertTab tab, Action handler) => tab.LConvertPresetMissing += handler;
    internal static void TConvertRun(LConvertTab tab, LWorkPriority priority) =>
        tab.LConvertRun(priority, default, default);
    internal static void TConvertAllRun(LConvertTab tab) => tab.LConvertAllRun(default, default);
    internal static void TConvertItemsRun(LConvertTab tab, params string[] paths) =>
        tab.LConvertItemsRun(paths, default, default);

    internal static void TMessengerDeliverAttach(Func<Guid, string, Guid, bool>? deliver) =>
        LMessenger.LMessengerDeliverSource = deliver;

    internal static LWorkFixStep TFixStepCreate(LFlawKind kind, bool repair, bool persistent) =>
        new(kind, repair, persistent);
    internal static LWorkFix TFixPlanCreate(IReadOnlyList<LWorkFixStep> steps, LWorkFixSalvage? salvage = null) =>
        new(steps) { LWorkFixSalvage = salvage ?? LWorkFixSalvage.LWorkSalvageCreate() };
    internal static LSidecarFixEntry TFixRecordCreate(LWorkFix plan) => LFix.LFixPersistentCreate(plan);
    internal static LWorkAudio TWorkAudioCreate(bool skip, params LWorkAudioStep[] steps) =>
        new(steps) { LWorkAudioSkip = skip };
    internal static LSidecarAudioEntry TAudioRecordCreate(LWorkAudio plan) => LAudio.LAudioPersistentCreate(plan);

    internal static LAction TActionCreate() => new();
    internal static void TActionDocketAttach(LAction action, LDocket docket) => action.LActionDocketAttach(docket);
    internal static void TActionEligibleAttach(LAction action, Func<IReadOnlyList<string>> source) =>
        action.LActionEligibleAttach(source);
    internal static bool TActionEligibleCheck(LAction action, params string[] chosen) =>
        action.LActionEligibleCheck(chosen);
    internal static void TActionStripAttach(LStrip strip) => LAction.LActionStripAttach(strip);
    internal static void TActionListAttach(LAction action, LList list) => action.LActionListAttach(list);
    internal static void TActionRelayAttach(LAction action, LStrip strip, LStripTab tab) =>
        action.LActionRelayAttach(strip, tab);
    internal static void TActionAutoSet(LAction action, bool? state) => action.LActionAutoSet(state);
    internal static void TActionRelaySelect(LAction action, Guid target) => action.LActionRelaySelect(target);
    internal static void TActionRelayApply(LAction action, Guid target) => action.LActionRelayApply(target);
    internal static IReadOnlyList<LActionOption> TActionOptionsRead(LAction action) => action.LActionOptionsRead();
    internal static IReadOnlyList<LActionOption> TActionMenuRead(LAction action) => action.LActionMenuRead();
    internal static void TActionRunAttach(LAction action, Action<LWorkPriority> handler) =>
        action.LActionExecuted += (priority, _, _) => handler(priority);
    internal static void TActionAllAttach(LAction action, Action handler) =>
        action.LActionAllAdded += (_, _) => handler();
    internal static void TActionItemsAttach(LAction action, Action<IReadOnlyList<string>> handler) =>
        action.LActionItemsAdded += (paths, _, _) => handler(paths);
    internal static void TActionCohortAttach(LAction action, Func<Guid, int> handler) =>
        action.LActionCohortAdded += (cohort, _, _) => handler(cohort);
    internal static (Guid TActionTarget, Guid TActionSource) TActionRouteRead(LAction action)
    {
        (Guid TActionTarget, Guid TActionSource) route = default;
        Action<IReadOnlyList<string>, Guid, Guid> capture = (_, target, source) => route = (target, source);
        action.LActionItemsAdded += capture;
        action.LActionItemsRun(Array.Empty<string>());
        action.LActionItemsAdded -= capture;
        return route;
    }
    internal static void TActionEmptyAttach(LAction action, Action handler) => action.LActionEmptyRaised += handler;
    internal static void TActionFaceAttach(LAction action, Action handler) => action.LActionFaceChanged += handler;
    internal static void TActionAllRun(LAction action) => action.LActionAllRun();
    internal static void TActionHighRun(LAction action) => action.LActionHighRun();
    internal static void TActionListRun(LAction action) => action.LActionListRun();
    internal static bool TActionCohortRun(LAction action, Guid cohort) => action.LActionCohortRun(cohort);
    internal static void TActionAccept(Guid target, string path, Guid cohort) =>
        LAction.LActionAccept(target, path, cohort);

    internal static LCompass TCompassCreate(LFlow flow, LViewer viewer, bool sections) => new(flow, viewer, sections);
    internal static IReadOnlyList<LCompassGroup> TCompassGroupsRead(LCompass compass) => compass.LCompassGroupsRead();
    internal static IReadOnlyList<int> TCompassSectionRead(LCompass compass) => compass.LCompassSectionRead();
    internal static LCompassButton TCompassPlayRead(bool playing) => LCompass.LCompassPlayRead(playing);
    internal static void TCompassRun(LCompass compass, string key) => compass.LCompassRun(key);
    internal static void TCompassWaveformToggle(LCompass compass) => compass.LCompassWaveformToggle();
    internal static void TCompassVolumeSet(LCompass compass, double raw) => compass.LCompassVolumeSet(raw);
    internal static void TCompassVolumeAttach(LViewer viewer, Action<double> handler) =>
        viewer.LViewerVolumeChanged += handler;
    internal static string TCompassVolumeFormat(double raw) => LCompass.LCompassVolumeFormat(raw);
    internal static double TCompassFillResolve(double host, double raw) => LCompass.LCompassFillResolve(host, raw);
    internal static IReadOnlyList<double> TCompassSeparatorsResolve(params double[] tops) =>
        LCompass.LCompassSeparatorsResolve(tops);
}
