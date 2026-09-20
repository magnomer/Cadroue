using Cadroue.Application;
using Cadroue.Core;
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
        LCrop crop,
        LList list,
        LDocket docket,
        LProcessing processing) => new(preset, inspector, viewer, crop, list, docket, processing);
    internal static void TEditClose(LEditTab tab) => tab.LEditClose();
    internal static void TEditStart(LEditTab tab) => tab.LEditStart();
    internal static LSceneTabRecord TEditLayoutRead(LEditTab tab) => tab.LEditLayoutRead(new LSceneTabRecord());
    internal static void TEditLayoutApply(LEditTab tab, LSceneTabRecord? layout) => tab.LEditLayoutApply(layout);
    internal static void TEditCropHandle(LEditTab tab) => tab.LEditCropHandle();
    internal static void TEditSkipHandle(LEditTab tab) => tab.LEditSkipHandle();
    internal static void TEditLockHandle(LEditTab tab, bool locked) => tab.LEditLockHandle(locked);
    internal static void TEditPathHandle(LEditTab tab, string? path) => tab.LEditPathHandle(path);
    internal static void TEditRotateAttach(LEditTab tab, Action<LRotateFlip> handler) =>
        tab.LEditRotateApply += handler;
    internal static void TEditRectAttach(LEditTab tab, Action<LCropbox?> handler) => tab.LEditRectApply += handler;
    internal static void TEditActiveAttach(LEditTab tab, Action<bool> handler) => tab.LEditActiveApply += handler;
    internal static void TEditLockAttach(LEditTab tab, Action<bool> handler) => tab.LEditLockApply += handler;
    internal static void TEditToolAttach(LEditTab tab, Action<bool> handler) => tab.LEditToolApply += handler;
    internal static void TEditNeutralAttach(LEditTab tab, Action handler) => tab.LEditNeutralCancel += handler;
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
    internal static void TEditPreviewAttach(LEditTab tab, Action<LColor> handler) =>
        tab.LEditColor.LEditPreviewApply += handler;
    internal static void TEditEstimateAttach(LEditTab tab, Action<LWhitebalanceMethod> handler) =>
        tab.LEditColor.LEditEstimateRead += handler;
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
}
