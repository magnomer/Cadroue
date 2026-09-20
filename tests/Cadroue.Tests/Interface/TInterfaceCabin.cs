using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

namespace Cadroue.Tests;

internal static partial class TInterface
{
    internal static LConsole TConsoleCreate() => new();
    internal static bool TConsoleSpinSet(LConsole console, bool spinning) => console.LConsoleSpinSet(spinning);
    internal static bool TConsoleProgressSet(LConsole console, double target) => console.LConsoleProgressSet(target);
    internal static void TConsoleProgressAttach(LConsole console, Action<double, bool> handler) =>
        console.LConsoleProgressApply += handler;
    internal static void TConsoleSpinAttach(LConsole console, Action<bool> handler) =>
        console.LConsoleSpinApply += handler;
    internal static IReadOnlyList<LConsoleRun> TConsoleRunsResolve(string line, string? accent) =>
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
        scene.LConsoleFocusClear += handler;
    internal static void TConsolePressHandle(LConsoleScene scene, bool dropOpen, bool focusWithin, bool inside) =>
        scene.LConsolePressHandle(dropOpen, focusWithin, inside);
    internal static string TConsoleStemResolve(string name, string path) =>
        LConsoleScene.LConsoleStemResolve(name, path);
    internal static string TConsoleNameCreate(string baseName, IReadOnlyList<string> names) =>
        LConsoleScene.LConsoleNameCreate(baseName, names);

    internal static LStation TStationCreate(LScheduleContract schedule)
    {
        LStation.LStationSchedule = schedule;
        LStation.LStationPost = action => action();
        LStation.LStationProgramSource = () => "ffmpeg";
        LStation.LStationPreferenceSource = () => LPreference.LPreferenceStateCurrent;
        return LStation.LStationCreate("Test worklist");
    }

    internal static LRoster TRosterCreate(LScheduleContract schedule, LStation station) => new(schedule, station);
    internal static void TRosterClose(LRoster roster) => roster.LRosterClose();
    internal static void TRosterRebuild(LRoster roster) => roster.LRosterRebuild();
    internal static void TRosterStepSelect(LRoster roster, Guid id, bool range, bool toggle) =>
        roster.LRosterStepSelect(id, range, toggle);
    internal static void TRosterCardSelect(LRoster roster, Guid batchId) => roster.LRosterCardSelect(batchId);
    internal static bool TRosterSelectedCheck(LRoster roster, Guid id) => roster.LRosterSelectedCheck(id);
    internal static bool TRosterCollapseToggle(LRoster roster, Guid batchId) => roster.LRosterCollapseToggle(batchId);
    internal static bool TRosterCollapsedCheck(LRoster roster, Guid batchId) => roster.LRosterCollapsedCheck(batchId);
    internal static void TRosterCardsAttach(LRoster roster, Action handler) => roster.LRosterCardsApply += handler;
    internal static void TRosterCardAttach(LRoster roster, Action<int, LRosterCard> handler) =>
        roster.LRosterCardApply += handler;
    internal static void TRosterDetailAttach(LRoster roster, Action handler) => roster.LRosterDetailApply += handler;
    internal static void TRosterDeferAttach(LRoster roster, Action handler) => roster.LRosterDetailDefer += handler;
    internal static void TRosterWarningAttach(LRoster roster, Action<string, string> handler) =>
        roster.LRosterWarningShow += handler;
    internal static void TRosterRemove(LRoster roster, Guid batchId) => roster.LRosterRemove(batchId);
    internal static IReadOnlyList<LRosterItem> TRosterMenuRead(LRoster roster, Guid id, LStrip? strip) =>
        roster.LRosterMenu.LRosterMenuRead(id, strip);
    internal static void TRosterItemRun(LRosterItem item) => item.LRosterItemAction();
    internal static bool TRosterDoneSet(LRoster roster, bool? isChecked) => roster.LRosterDoneSet(isChecked);
    internal static bool TRosterSharedSet(LRoster roster, bool? isChecked) => roster.LRosterSharedSet(isChecked);
    internal static LWorkItem? TRosterSelectRead(LRoster roster) => roster.LRosterSelectRead();
    internal static IReadOnlyList<LWorkItem> TRosterSelectionRead(LRoster roster) => roster.LRosterSelectionRead();
    internal static void TRosterElapsedTick(LRoster roster, bool visible) => roster.LRosterElapsedTick(visible);
    internal static void TRosterDetailRun(LRoster roster) => roster.LRosterDetailRun();
    internal static string TRosterOwnerFormat(LWorkItem work, bool owned) => LRosterRow.LRosterOwnerFormat(work, owned);
    internal static string TRosterProgressFormat(LWorkItem work) => LRosterRow.LRosterProgressFormat(work);
    internal static string TRosterPriorityFormat(LWorkPriority priority) => LRosterRow.LRosterPriorityFormat(priority);
    internal static string TRosterStateFormat(LWorkState state) => LRosterRow.LRosterStateFormat(state);
    internal static string TRosterKeyRead(LWorkState state) => LRosterRow.LRosterKeyRead(state);
    internal static string TRosterPhaseFormat(LWorkState state, LWorkPhase phase) =>
        LRosterRow.LRosterPhaseFormat(state, phase);
    internal static string TRosterSpanFormat(TimeSpan span) => LRosterRow.LRosterSpanFormat(span);
    internal static string TRosterShadeResolve(bool selected, bool cardSelected, bool stage) =>
        LRosterRow.LRosterShadeResolve(selected, cardSelected, stage);
    internal static LRosterRow TRosterRowCreate(
        LWorkItem work, LLineageEntry lineage, bool last, bool stage, bool owned, string shade) =>
        LRosterRow.LRosterRowCreate(work, lineage, last, stage, owned, shade);
    internal static string TRosterTitleFormat(IReadOnlyList<LWorkItem> items) => LRosterCard.LRosterTitleFormat(items);
    internal static IReadOnlyList<LLineageEntry> TLineageRead(
        IReadOnlyList<LWorkItem> items, Func<LWorkItem, Guid> read) => LLineage.LLineageRead(items, read);
    internal static string TLineageStepFormat(LWorkItem work, string subject) =>
        LLineage.LLineageStepFormat(work, subject);
    internal static string TLineageRatioFormat(LWorkItem work, string subject, long? origin) =>
        LLineage.LLineageRatioFormat(work, subject, origin);
    internal static string TLineageTitleFormat(LLineageEntry entry) => LLineage.LLineageTitleFormat(entry);
    internal static HashSet<Guid> TLineageStageRead(IReadOnlyList<LWorkItem> items) =>
        LLineage.LLineageStageRead(items);
    internal static int TLineageInitialRead(IReadOnlyList<LWorkItem> items) => LLineage.LLineageInitialRead(items);
    internal static long? TLineageSourceRead(LWorkItem work) => LLineage.LLineageSourceRead(work);
    internal static string? TLineagePathRead(string path) => LLineage.LLineagePathRead(path);
    internal static LRosterDetailKind TRosterKindRead(LRoster roster) =>
        LRosterDetail.LRosterKindRead(roster);
    internal static LRosterDetail TRosterDetailRead(LRoster roster) => LRosterDetail.LRosterDetailRead(roster);
    internal static LRosterDetail TRosterDetailCreate(LWorkItem? work, string owner) =>
        LRosterDetail.LRosterDetailCreate(work, owner);
    internal static IReadOnlyList<LRosterBar> TRosterBarsCreate(long? source, long? output) =>
        LRosterDetail.LRosterBarsCreate(source, output);
    internal static LRosterCompareRow TRosterCompareCreate(string source, string output) =>
        LRosterDetail.LRosterCompareCreate(source, output);
    internal static string TRosterMebiFormat(long? bytes) => LRosterFormat.LRosterMebiFormat(bytes);
    internal static string TRosterSpentFormat(LWorkItem work) => LRosterFormat.LRosterSpentFormat(work);
    internal static string TRosterSpeedFormat(LWorkItem work) => LRosterFormat.LRosterSpeedFormat(work);
    internal static string TRosterElapsedFormat(TimeSpan spent) =>
        LRosterFormat.LRosterElapsedFormat(spent);
    internal static string TRosterClockFormat(TimeSpan span) => LRosterFormat.LRosterClockFormat(span);
    internal static string TRosterStampFormat(DateTimeOffset? stamp) => LRosterFormat.LRosterStampFormat(stamp);
    internal static string TRosterContainerFormat(string path) => LRosterFormat.LRosterContainerFormat(path);
    internal static bool TRosterReencodeCheck(string mode) => LRosterFormat.LRosterReencodeCheck(mode);
    internal static LSummary TSummaryRead(IReadOnlyList<LWorkItem> items) => LSummary.LSummaryRead(items);
    internal static (long?, long?) TSummarySizeRead(IReadOnlyList<LWorkItem> items) =>
        LSummary.LSummarySizeRead(items);
    internal static (IReadOnlyList<string>, IReadOnlyList<string>) TSummaryPathsRead(
        IReadOnlyList<LWorkItem> items) => LSummary.LSummaryPathsRead(items);
    internal static string? TSummaryMeterFormat(IReadOnlyList<LWorkItem> items, long? output) =>
        LSummary.LSummaryMeterFormat(items, output);
    internal static string TSummaryFilesFormat(int count) => LSummary.LSummaryFilesFormat(count);

    internal static LEditTab TEditTabCreate(
        LPresetSelection preset,
        LInspector inspector,
        LViewer viewer,
        LList list,
        LDocket docket,
        LProcessing processing) => new(preset, inspector, viewer, list, docket, processing);
    internal static void TEditClose(LEditTab tab) => tab.LEditClose();
    internal static void TEditStart(LEditTab tab) => tab.LEditStart();
    internal static LSceneTabRecord TEditLayoutRead(LEditTab tab) => tab.LEditLayoutRead(new LSceneTabRecord());
    internal static void TEditLayoutApply(LEditTab tab, LSceneTabRecord? layout) => tab.LEditLayoutApply(layout);
    internal static void TEditCropHandle(LEditTab tab) => tab.LEditCropHandle();
    internal static void TEditSkipHandle(LEditTab tab) => tab.LEditSkipHandle();
    internal static void TEditLockHandle(LEditTab tab, bool locked) => tab.LEditLockHandle(locked);
    internal static void TEditPathHandle(LEditTab tab, string? path) => tab.LEditPathHandle(path);
    internal static void TEditHistogramAttach(LEditTab tab, Action handler) => tab.LEditHistogramDefer += handler;
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
        viewer.LViewerPreviewChange += () => handler(viewer.LViewerPreview.LColor);
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
        Func<string, LSidecarEditRecord?>? reader, Func<string, LSidecarEditRecord?, bool>? writer)
    {
        LLibrarian.LLibrarianEditReader = reader;
        LLibrarian.LLibrarianEditWriter = writer;
    }

    internal static LSplitTab TSplitTabCreate(
        LPresetSelection preset,
        LInspector inspector,
        LViewer viewer,
        LList list,
        LDocket docket,
        LProcessing processing,
        LFlow flow) => new(preset, inspector, viewer, list, docket, processing, flow);
    internal static IReadOnlyList<LProcessingRow> TSplitRowsRead() => LSplitTab.LSplitRows;
    internal static void TSplitClose(LSplitTab tab) => tab.LSplitClose();
    internal static void TSplitStart(LSplitTab tab) => tab.LSplitStart();
    internal static LSceneTabRecord TSplitLayoutRead(LSplitTab tab) => tab.LSplitLayoutRead(new LSceneTabRecord());
    internal static void TSplitLayoutApply(LSplitTab tab, LSceneTabRecord? layout) => tab.LSplitLayoutApply(layout);
    internal static void TSplitPathHandle(LSplitTab tab, string? path) => tab.LSplitPathHandle(path);
    internal static void TSplitLockHandle(LSplitTab tab, bool locked) => tab.LSplitLockHandle(locked);
    internal static void TSplitPersistentHandle(LSplitTab tab, bool persistent) =>
        tab.LSplitPersistentHandle(persistent);
    internal static void TSplitStateSave(LSplitTab tab) => tab.LSplitStateSave();
    internal static void TSplitStateLoad(LSplitTab tab, string path) => tab.LSplitStateLoad(path);
    internal static LDetectorSet TSplitStateRead(LSplitTab tab) => tab.LSplitStateRead();
    internal static void TSplitMissingAttach(LSplitTab tab, Action handler) => tab.LSplitPresetMissing += handler;
    internal static void TSplitRun(LSplitTab tab, LWorkPriority priority) => tab.LSplitRun(priority, default, default);
    internal static IReadOnlyList<LDetectorKind> TSplitStepsRead(LSplitTab tab) => tab.LSplitSweep.LSplitStepsRead();
    internal static Task TSplitSweepStart(LSplitTab tab) => tab.LSplitSweep.LSplitSweepStart();
    internal static void TSplitSweepCancel(LSplitTab tab) => tab.LSplitSweep.LSplitSweepCancel();
    internal static bool TSplitSweepCheck(LSplitTab tab) => tab.LSplitSweep.LSplitSweepRunning;
    internal static void TSplitBusyAttach(LSplitTab tab, Action<bool> handler) =>
        tab.LSplitSweep.LSplitBusyApply += handler;
    internal static void TSplitFailAttach(LSplitTab tab, Action<string, string> handler) =>
        tab.LSplitSweep.LSplitFailRaise += handler;
    internal static void TSplitLibrarianAttach(
        Func<string, LSidecarSplitRecord?>? reader, Func<string, LSidecarSplitRecord?, bool>? writer)
    {
        LLibrarian.LLibrarianSplitReader = reader;
        LLibrarian.LLibrarianSplitWriter = writer;
    }

    internal static void TInspectorSaveSuspend(LInspector inspector) => inspector.LInspectorSaveSuspend();
    internal static void TInspectorSaveResume(LInspector inspector) => inspector.LInspectorSaveResume();
    internal static int TDocketDeliveredAdd(LDocket docket, string path, bool locked) =>
        docket.LDocketDeliveredAdd([path], Guid.NewGuid(), locked);
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
    internal static LDetectorSet TDetectorSetCreate(
        IReadOnlyList<LDetectorStep> steps,
        LDetectorBlank? blank,
        LDetectorStillMode? still,
        LDetectorLuminanceMode? luminance,
        LDetectorMetricMode? metric,
        IReadOnlyDictionary<LDetectorKind, string> presets) => new(steps, blank, still, luminance, metric, presets);
    internal static LSidecarSplitRecord TDetectorSidecarFormat(LDetectorSet set) =>
        LDetectorSet.LDetectorSidecarFormat(set);
    internal static LDetectorSet TDetectorSidecarParse(LSidecarSplitRecord record) =>
        LDetectorSet.LDetectorSidecarParse(record);
    internal static List<LSceneDetector> TDetectorSceneFormat(LDetectorSet set) =>
        LDetectorSet.LDetectorSceneFormat(set);
    internal static LDetectorSet TDetectorSceneParse(IReadOnlyList<LSceneDetector> detectors) =>
        LDetectorSet.LDetectorSceneParse(detectors);

    internal static LFixTab TFixTabCreate(
        LPresetSelection preset,
        LClinic clinic,
        LViewer viewer,
        LList list,
        LDocket docket,
        LProcessing processing) => new(preset, clinic, viewer, list, docket, processing);
    internal static IReadOnlyList<LProcessingRow> TFixRowsRead() => LFixTab.LFixRows;
    internal static IReadOnlyList<LFixRow> TFixKindsRead() => LFixTab.LFixKinds;
    internal static void TFixClose(LFixTab tab) => tab.LFixClose();
    internal static LSceneTabRecord TFixLayoutRead(LFixTab tab) => tab.LFixLayoutRead(new LSceneTabRecord());
    internal static void TFixLayoutApply(LFixTab tab, LSceneTabRecord? layout) => tab.LFixLayoutApply(layout);
    internal static void TFixPathHandle(LFixTab tab, string? path) => tab.LFixPathHandle(path);
    internal static void TFixDiagnosisRun(LFixTab tab) => tab.LFixDiagnosisRun();
    internal static void TFixCheckupHandle(LFixTab tab, string path, LFlawKind kind, LCheckupOutcome outcome) =>
        tab.LFixCheckupHandle(new LCheckupResult(path, kind, outcome));
    internal static void TFixProgressHandle(LFixTab tab, string path, double progress) =>
        tab.LFixProgressHandle(path, progress);
    internal static void TFixClearHandle(LFixTab tab, params string[] removed) => tab.LFixClearHandle(removed);
    internal static void TFixItemsHandle(LFixTab tab, IReadOnlyList<LDocketEntry> added) => tab.LFixItemsHandle(added);
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
                return Array.Empty<LDossier>();
            };
    }
    internal static void TFixLibrarianAttach(
        Func<string, LSidecarFixRecord?>? reader, Func<string, LSidecarFixRecord?, bool>? writer)
    {
        LLibrarian.LLibrarianFixReader = reader;
        LLibrarian.LLibrarianFixWriter = writer;
    }

    internal static LAudioTab TAudioTabCreate(
        LPresetSelection preset,
        LInspector inspector,
        LViewer viewer,
        LList list,
        LDocket docket,
        LProcessing processing) => new(preset, inspector, viewer, list, docket, processing);
    internal static IReadOnlyList<LProcessingRow> TAudioRowsRead() => LAudioTab.LAudioRows;
    internal static LAudioKind? TAudioKindRead(string step) => LAudioTab.LAudioKindRead(step);
    internal static void TAudioClose(LAudioTab tab) => tab.LAudioClose();
    internal static LSceneTabRecord TAudioLayoutRead(LAudioTab tab) => tab.LAudioLayoutRead(new LSceneTabRecord());
    internal static void TAudioLayoutApply(LAudioTab tab, LSceneTabRecord? layout) => tab.LAudioLayoutApply(layout);
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
        viewer.LViewerPreviewChange += () => handler(viewer.LViewerAudioResolve());
    internal static void TAudioDeferAttach(LAudioTab tab, Action handler) => tab.LAudioViewerDefer += handler;
    internal static void TAudioMissingAttach(LAudioTab tab, Action handler) => tab.LAudioPresetMissing += handler;
    internal static void TAudioIncompatibleAttach(LAudioTab tab, Action handler) =>
        tab.LAudioPresetIncompatible += handler;
    internal static void TAudioRun(LAudioTab tab, LWorkPriority priority) => tab.LAudioRun(priority, default, default);
    internal static void TAudioAllRun(LAudioTab tab) => tab.LAudioAllRun(default, default);
    internal static void TAudioLibrarianAttach(
        Func<string, LSidecarAudioRecord?>? reader, Func<string, LSidecarAudioRecord?, bool>? writer)
    {
        LLibrarian.LLibrarianAudioReader = reader;
        LLibrarian.LLibrarianAudioWriter = writer;
    }

    internal static LWorkAudioStep TInspectorStepRead(LInspectorAudio audio, LAudioKind kind) =>
        audio.LInspectorStepRead(kind);
    internal static void TInspectorAudioApply(LInspectorAudio audio, LWorkAudio plan) =>
        audio.LInspectorAudioApply(plan);
    internal static bool TInspectorPersistentCheck(LInspectorAudio audio) => audio.LInspectorPersistentCheck();
    internal static void TInspectorPersistentApply(LInspectorAudio audio, LWorkAudio plan, bool skipPersistent) =>
        audio.LInspectorPersistentApply(plan, skipPersistent);
    internal static LWorkAudio TInspectorPersistentRead(LInspectorAudio audio) => audio.LInspectorPersistentRead();
    internal static void TInspectorAudioAttach(LInspectorAudio audio, Action handler) =>
        audio.LInspectorAudioChange += handler;
    internal static void TVolumePersistentSet(LVolume volume, bool persistent) =>
        volume.LVolumePersistentSet(persistent);

    internal static LMergeTab TMergeTabCreate(LPresetSelection preset, LDocket docket, LSceneTabRecord? layout) =>
        new(preset, docket, layout);
    internal static void TMergeClose(LMergeTab tab) => tab.LMergeClose();
    internal static LSceneTabRecord TMergeLayoutRead(LMergeTab tab) => tab.LMergeLayoutRead(new LSceneTabRecord());
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

    internal static LConvertTab TConvertTabCreate(LPresetSelection preset, LList list, LDocket docket) =>
        new(preset, list, docket);
    internal static void TConvertMissingAttach(LConvertTab tab, Action handler) => tab.LConvertPresetMissing += handler;
    internal static void TConvertRun(LConvertTab tab, LWorkPriority priority) =>
        tab.LConvertRun(priority, default, default);
    internal static void TConvertAllRun(LConvertTab tab) => tab.LConvertAllRun(default, default);
    internal static void TConvertItemsRun(LConvertTab tab, params string[] paths) =>
        tab.LConvertItemsRun(paths, default, default);

    internal static LFunnelTab TFunnelTabCreate(LList list, LDocket docket) => new(list, docket);
    internal static void TFunnelClose(LFunnelTab tab) => tab.LFunnelClose();
    internal static void TFunnelLayoutApply(LFunnelTab tab, LSceneTabRecord? layout) => tab.LFunnelLayoutApply(layout);
    internal static LSceneTabRecord TFunnelLayoutRead(LFunnelTab tab) => tab.LFunnelLayoutRead(new LSceneTabRecord());
    internal static IReadOnlyList<string> TFunnelEligibleRead(LFunnelTab tab) => tab.LFunnelEligibleRead();
    internal static void TFunnelRun(LFunnelTab tab) => tab.LFunnelRun();
    internal static void TFunnelAllRun(LFunnelTab tab) => tab.LFunnelAllRun();
    internal static void TFunnelItemsRun(LFunnelTab tab, params string[] paths) => tab.LFunnelItemsRun(paths);
    internal static void TFunnelStripAttach(LFunnel funnel, LStrip strip, Guid self) =>
        funnel.LFunnelStripAttach(strip, self);
    internal static void TFunnelStripDetach(LFunnel funnel) => funnel.LFunnelStripDetach();
    internal static void TFunnelTargetsResolve(LFunnel funnel) => funnel.LFunnelTargetsResolve();
    internal static IReadOnlyList<LFunnelTarget> TFunnelTargetsRead(LFunnel funnel) => funnel.LFunnelTargetsRead();
    internal static IReadOnlyList<LFunnelTarget> TFunnelOptionsRead(LFunnel funnel) => funnel.LFunnelOptionsRead();
    internal static IReadOnlyList<LFunnelSlot> TFunnelSlotsRead(LFunnel funnel) => funnel.LFunnelSlotsRead();
    internal static int TFunnelIndexRead(LFunnel funnel, Guid target) => funnel.LFunnelIndexRead(target);
    internal static void TFunnelSelectedRemove(LFunnel funnel) => funnel.LFunnelSelectedRemove();
    internal static void TFunnelTargetSelect(LFunnel funnel, LFunnelRule rule, int index) =>
        funnel.LFunnelTargetSelect(rule, index);
    internal static string TFunnelTextResolve(LFunnel funnel, LFunnelRule rule, LFunnelKind kind, string shown) =>
        funnel.LFunnelTextResolve(rule, kind, shown);
    internal static string TFunnelRegexResolve(LFunnel funnel, LFunnelRule rule, string shown) =>
        funnel.LFunnelRegexResolve(rule, shown);
    internal static void TFunnelRegexSet(LFunnel funnel, LFunnelRule rule, string regex) =>
        funnel.LFunnelRegexSet(rule, regex);
    internal static void TFunnelCaseToggle(LFunnel funnel, LFunnelRule rule, LFunnelKind kind) =>
        funnel.LFunnelCaseToggle(rule, kind);
    internal static void TFunnelCollapsedToggle(LFunnel funnel, LFunnelRule rule) =>
        funnel.LFunnelCollapsedToggle(rule);
    internal static void TFunnelCreateAttach(LFunnel funnel, Action<LFunnelRule> handler) =>
        funnel.LFunnelRuleCreate += handler;
    internal static void TFunnelDeleteAttach(LFunnel funnel, Action<LFunnelRule> handler) =>
        funnel.LFunnelRuleDelete += handler;
    internal static IReadOnlyList<LFunnelCondition> TFunnelConditionsRead() => LFunnel.LFunnelConditions;
    internal static void TFunnelPressHandle(LFunnel funnel, LFunnelRule rule, double x, double y) =>
        funnel.LFunnelDrag.LFunnelPressHandle(rule, x, y, 0, 0);
    internal static bool TFunnelMoveCheck(LFunnel funnel, LFunnelRule rule, bool pressed) =>
        funnel.LFunnelDrag.LFunnelMoveCheck(rule, pressed);
    internal static bool TFunnelDragResolve(LFunnel funnel, double x, double y, double minimumX, double minimumY) =>
        funnel.LFunnelDrag.LFunnelDragResolve(x, y, minimumX, minimumY);
    internal static void TFunnelDragMove(LFunnel funnel, double pointer, params double[] centers) =>
        funnel.LFunnelDrag.LFunnelDragMove(pointer, centers);
    internal static bool TFunnelReleaseCheck(LFunnel funnel, LFunnelRule rule) =>
        funnel.LFunnelDrag.LFunnelReleaseCheck(rule);
    internal static void TFunnelDragClear(LFunnel funnel) => funnel.LFunnelDrag.LFunnelDragClear();
    internal static bool TFunnelDragCheck(LFunnel funnel) => funnel.LFunnelDrag.LFunnelDragActive;
    internal static int TFunnelIndexResolve(double pointer, params double[] centers) =>
        LFunnelDrag.LFunnelIndexResolve(pointer, centers);
    internal static double TFunnelCenterResolve(double top, double height) =>
        LFunnelDrag.LFunnelCenterResolve(top, height);
    internal static void TMessengerDeliverAttach(Func<Guid, string, Guid, bool>? deliver) =>
        LMessenger.LMessengerDeliverSource = deliver;

    internal static LWorkFixStep TFixStepCreate(LFlawKind kind, bool repair, bool persistent) =>
        new(kind, repair, persistent);
    internal static LWorkFix TFixPlanCreate(IReadOnlyList<LWorkFixStep> steps, LWorkFixSalvage? salvage = null) =>
        new(steps) { LWorkFixSalvage = salvage ?? LWorkFixSalvage.LWorkSalvageCreate() };
    internal static LSidecarFixRecord TFixRecordCreate(LWorkFix plan) => LFix.LFixPersistentCreate(plan);
    internal static LWorkAudio TWorkAudioCreate(bool skip, params LWorkAudioStep[] steps) =>
        new(steps) { LWorkAudioSkip = skip };
    internal static LSidecarAudioRecord TAudioRecordCreate(LWorkAudio plan) => LAudio.LAudioPersistentCreate(plan);

    internal static IReadOnlyList<LDocketEntry> TDocketItemsRead(LDocket docket) => docket.LDocketItemsRead();
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
        action.LActionRun += handler;
    internal static void TActionAllAttach(LAction action, Action handler) => action.LActionAllAdd += handler;
    internal static void TActionItemsAttach(LAction action, Action<IReadOnlyList<string>> handler) =>
        action.LActionItemsAdd += handler;
    internal static void TActionCohortAttach(LAction action, Func<Guid, int> handler) =>
        action.LActionCohortAdd += handler;
    internal static void TActionEmptyAttach(LAction action, Action handler) => action.LActionEmptyRaise += handler;
    internal static void TActionFaceAttach(LAction action, Action handler) => action.LActionFaceChange += handler;
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
        viewer.LViewerVolumeChange += handler;
    internal static string TCompassVolumeFormat(double raw) => LCompass.LCompassVolumeFormat(raw);
    internal static double TCompassFillResolve(double host, double raw) => LCompass.LCompassFillResolve(host, raw);
    internal static IReadOnlyList<double> TCompassSeparatorsResolve(params double[] tops) =>
        LCompass.LCompassSeparatorsResolve(tops);
}
