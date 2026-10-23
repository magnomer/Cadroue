using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static partial class TInterface
{
    internal static IReadOnlyList<QConsoleSpan> TConsoleRunsResolve(string line, string? accent) =>
        QConsoleLine.QConsoleRunsResolve(line, accent);
    internal static string? TConsoleRemovalFormat(IReadOnlyDictionary<Guid, LScheduleRemoval> outcomes) =>
        QConsoleLine.QConsoleRemovalFormat(outcomes);
    internal static int TConsoleIndexResolve(int index, int step, int count) =>
        LConsoleFacade.LConsoleIndexResolve(index, step, count);

    internal static LConsoleSceneOutlet TSceneOutletCreate() =>
        new(new LSceneStore(
            LSceneCatalogue.LSceneCatalogueLoad,
            () => LSceneCatalogue.LSceneNames,
            () => LScene.LSceneActiveName,
            LScene.LSceneActiveSet,
            LSceneCatalogue.LSceneRead,
            LSceneCanonical.LSceneMatch,
            LSceneCatalogue.LSceneSave,
            LSceneCatalogue.LSceneDelete,
            LSceneFile.LSceneFileResolve,
            LSceneFile.LSceneFileSave,
            LSceneFile.LSceneFileLoad,
            LSceneFile.LSceneStemResolve,
            lMessage => LTraceLog.LTraceLoadingRecord(lMessage)));
    internal static QConsoleScene TConsoleSceneCreate() =>
        new(TAtelierCreate().CAtelierConsoleCreate().CConsoleScene, new QEnvoy());
    internal static bool TConsoleDirtyCheck(string name) =>
        TSceneOutletCreate().LConsoleSceneRead(name, null).LConsoleSceneDirty;
    internal static bool TConsoleDeleteHandle(QConsoleScene scene, string? name) => scene.QConsoleDeleteHandle(name);
    internal static void TConsoleFocusAttach(QConsoleScene scene, Action handler) =>
        scene.QConsoleFocusCleared += handler;
    internal static void TConsolePressHandle(QConsoleScene scene, bool dropOpen, bool focusWithin, bool inside) =>
        scene.QConsolePressHandle(dropOpen, focusWithin, inside);
    internal static string TConsoleStemResolve(string name, string path) => LSceneFile.LSceneStemResolve(name, path);
    internal static string TConsoleNameCreate(string baseName, IReadOnlyList<string> names) =>
        LConsoleSceneOutlet.LConsoleSceneCreate(baseName, names);

    internal static QEditTab TEditTabCreate(LPresetSelection preset, QViewer viewer, QList list) =>
        new(
            TAtelierCreate().CAtelierUsher,
            TAtelierCreate().CAtelierInspectorCreate(),
            TAtelierCreate().CAtelierEdit,
            preset,
            viewer,
            list);
    internal static QInspector TEditInspectorRead(QEditTab tab) => tab.QEditInspector;
    internal static QProcessing TEditProcessingRead(QEditTab tab) => tab.QEditProcessing;
    internal static void TEditClose(QEditTab tab) => tab.QEditClose();
    internal static void TEditStart(QEditTab tab) => tab.QEditStart();
    internal static LSceneTabEntry TEditLayoutRead(QEditTab tab) => tab.QEditLayoutRead(new LSceneTabEntry());
    internal static void TEditLayoutApply(QEditTab tab, LSceneTabEntry? layout) => tab.QEditLayoutApply(layout);
    internal static void TEditCropHandle(QEditTab tab) => tab.QEditCropboxHandle();
    internal static void TEditSkipHandle(QEditTab tab) => tab.QEditSkipHandle();
    internal static void TEditLockHandle(QEditTab tab, bool locked) => tab.QEditTool.QEditLockHandle(locked);
    internal static void TEditPathHandle(QEditTab tab, string? path) => tab.QEditPathHandle(path);
    internal static void TEditHistogramAttach(QEditTab tab, Action handler) => tab.QEditHistogramDeferred += handler;
    internal static void TEditMissingAttach(QEditTab tab, Action handler) => tab.QEditPresetMissing += handler;
    internal static void TEditRun(QEditTab tab, bool urgent) => tab.QEditRun(urgent, default, default);
    internal static LEditPlan TEditStateRead(QEditTab tab) => tab.QEditStore.QEditStateRead();
    internal static LEditPlan? TEditCarriedRead(QEditTab tab) => tab.QEditStore.QEditCarriedRead();
    internal static void TEditStateSave(QEditTab tab) => tab.QEditStore.QEditStateSave();
    internal static void TEditPersistentSave(QEditTab tab) => tab.QEditStore.QEditPersistentSave();
    internal static string TEditPlanFormat(LEditPlan? plan) => QEditTabTrace.QEditPlanFormat(plan);
    internal static string TEditCropFormat(LWorkCrop? crop) => QEditTabTrace.QEditCropFormat(crop);
    internal static string TEditRectFormat(LCropbox? rect) => QEditTabTrace.QEditRectFormat(rect);
    internal static LWorkVideo TEditVideoRead(QEditTab tab, bool mpvOnlyCapable) =>
        tab.QEditColor.QEditVideoRead(mpvOnlyCapable);
    internal static void TEditColorUpdate(QEditTab tab) => tab.QEditColor.QEditColorUpdate();
    internal static void TEditPreviewAttach(QViewer viewer, Action<LColor> handler) =>
        viewer.QViewerPreviewChanged += () => handler(viewer.QViewerPreview.LColor);
    internal static void TEditColorApply(QEditTab tab) => tab.QEditColor.QEditColorApply();
    internal static void TEditNeutralHandle(QEditTab tab, LNeutralSample sample) =>
        tab.QEditColor.QEditWhitebalance.QEditNeutralHandle(sample);
    internal static void TEditEstimateHandle(QEditTab tab, string token) =>
        tab.QEditColor.QEditWhitebalance.QEditEstimateHandle(token);
    internal static void TEditEstimateApply(QEditTab tab, double x, double y, bool present) =>
        tab.QEditColor.QEditWhitebalance.QEditEstimateApply(new LNeutralWheel(x, y, present));
    internal static void TEditHistogramApply(QEditTab tab, int width, int height, byte[]? pixels) =>
        tab.QEditColor.QEditHistogramApply(pixels is null ? null : new LMediaFrame(width, height, pixels));
    internal static void TEditLibrarianAttach(
        Func<string, LSidecarEditEntry?>? reader, Func<string, LSidecarEditEntry?, bool>? writer)
    {
        LLibrarian.LLibrarianEditReader = reader;
        LLibrarian.LLibrarianEditWriter = writer;
    }

    internal static QSplitTab TSplitTabCreate(LPresetSelection preset, QViewer viewer, QList list, QFlow flow) =>
        new(TAtelierCreate().CAtelierInspectorCreate(), preset, viewer, list, flow);
    internal static QInspector TSplitInspectorRead(QSplitTab tab) => tab.QSplitInspector;
    internal static QProcessing TSplitProcessingRead(QSplitTab tab) => tab.QSplitProcessing;
    internal static IReadOnlyList<QProcessingRow> TSplitRowsRead() => QSplitTab.QSplitRows;
    internal static void TSplitClose(QSplitTab tab) => tab.QSplitClose();
    internal static void TSplitStart(QSplitTab tab) => tab.QSplitStart();
    internal static LSceneTabEntry TSplitLayoutRead(QSplitTab tab) => tab.QSplitLayoutRead(new LSceneTabEntry());
    internal static void TSplitLayoutApply(QSplitTab tab, LSceneTabEntry? layout) => tab.QSplitLayoutApply(layout);
    internal static void TSplitPathHandle(QViewer viewer, string? path) => viewer.QViewerWorkspace.CWorkspaceOpen(path);
    internal static void TSplitLockHandle(QSplitTab tab, bool locked) => tab.QSplitLockHandle(locked);
    internal static void TSplitPersistentHandle(QSplitTab tab, bool persistent) =>
        tab.QSplitPersistentHandle(persistent);
    internal static void TSplitStateSave(QSplitTab tab) => tab.QSplitDetector.QSplitStateSave();
    internal static void TSplitStateLoad(QSplitTab tab, string path) => tab.QSplitDetector.QSplitStateLoad(path);
    internal static LDetectorBundle TSplitStateRead(QSplitTab tab) => tab.QSplitDetector.QSplitStateRead();
    internal static void TSplitMissingAttach(QSplitTab tab, Action handler) =>
        tab.QSplitWork.QSplitPresetMissing += handler;
    internal static void TSplitRun(QSplitTab tab, bool urgent) => tab.QSplitWork.QSplitRun(urgent, default, default);
    internal static IReadOnlyList<LDetectorKind> TSplitStepsRead(QSplitTab tab) =>
        tab.QSplitDetector.QSplitStepsRead();
    internal static Task TSplitSweepStart(QSplitTab tab) => tab.QSplitSweep.QSplitSweepStart();
    internal static void TSplitSweepCancel(QSplitTab tab) => tab.QSplitSweep.QSplitSweepCancel();
    internal static bool TSplitSweepCheck(QSplitTab tab) => tab.QSplitSweep.QSplitSweepRunning;
    internal static void TSplitBusyAttach(QSplitTab tab, Action<bool> handler) =>
        tab.QSplitSweep.QSplitBusyApplied += handler;
    internal static void TSplitFailAttach(QSplitTab tab, Action<string, string> handler) =>
        tab.QSplitSweep.QSplitFailRaised += handler;
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
    internal static void TSensorEnabledSet(QSensor sensor, LDetectorKind kind, bool enabled) =>
        sensor.CDetector.CDetectorEnabledSet(kind, enabled);
    internal static void TSensorModeSet(QSensor sensor, LDetectorStillMode mode) =>
        sensor.CDetector.CDetectorModeSet(mode);
    internal static void TSensorSpeedSet(QSensor sensor, LDetectorLuminanceMode speed) =>
        sensor.CDetector.CDetectorSpeedSet(speed);
    internal static void TSensorPersistentSet(QInspector inspector, bool persistent) =>
        inspector.QInspectorGate.CInspectorSweep.CSweepPersistentSet(persistent);
    internal static string TSensorNameRead(LDetectorKind kind) => QSensorName.QSensorNameRead(kind);
    internal static LDetectorKind? TSensorKindRead(string? name) => QSensorName.QSensorKindRead(name);

    internal static QFixTab TFixTabCreate(LPresetSelection preset, QViewer viewer, QList list) =>
        new(TAtelierCreate().CAtelierInspectorCreate(), preset, viewer, list, TAtelierCreate().CAtelierFix);
    internal static CClinic TFixClinicRead(QFixTab tab) => tab.QFixClinicGate;
    internal static QProcessing TFixProcessingRead(QFixTab tab) => tab.QFixProcessing;
    internal static IReadOnlyList<QProcessingRow> TFixRowsRead() => QFixCatalog.QFixRows;
    internal static IReadOnlyList<QFixRow> TFixKindsRead() => QFixCatalog.QFixKinds;
    internal static void TFixClose(QFixTab tab) => tab.QFixClose();
    internal static LSceneTabEntry TFixLayoutRead(QFixTab tab) => tab.QFixLayoutRead(new LSceneTabEntry());
    internal static void TFixLayoutApply(QFixTab tab, LSceneTabEntry? layout) => tab.QFixLayoutApply(layout);
    internal static void TFixPathHandle(QViewer viewer, string? path) => viewer.QViewerWorkspace.CWorkspaceOpen(path);
    internal static void TFixDiagnosisRun(QFixTab tab) => tab.QFixClinic.QClinicDiagnosisRun();
    internal static void TFixCheckupHandle(QFixTab tab, string path, LFlawKind kind, LCheckupOutcome outcome) =>
        tab.QFixCheckupHandle(new LCheckupResult(path, kind, outcome));
    internal static void TFixProgressHandle(QFixTab tab, string path, double progress) =>
        tab.QFixProgressHandle(path, progress);
    internal static void TFixClearHandle(QFixTab tab, params string[] removed) => tab.QFixClearHandle(removed);
    internal static void TFixChangeHandle(QFixTab tab) => tab.QFixChangeHandle();
    internal static void TFixStateSave(QFixTab tab) => tab.QFixChangeHandle();
    internal static void TFixPlanRestore(QFixTab tab, string path) => tab.QFixPlanRestore(path);
    internal static void TFixMissingAttach(QFixTab tab, Action handler) => tab.QFixPresetMissing += handler;
    internal static void TFixRun(QFixTab tab, bool urgent) => tab.QFixRun(urgent, default, default);
    internal static void TFixAllRun(QFixTab tab) => tab.QFixAllRun(default, default);
    internal static void TFixCheckupAttach(QFixTab tab, Action<LCheckupResult> handler) =>
        tab.QFixCheckup.LCheckupReady += handler;
    internal static void TFixCheckupDetach(QFixTab tab, Action<LCheckupResult> handler) =>
        tab.QFixCheckup.LCheckupReady -= handler;
    internal static void TFixProgressAttach(QFixTab tab, Action<string, double> handler) =>
        tab.QFixCheckup.LCheckupProgress += handler;
    internal static void TFixProgressDetach(QFixTab tab, Action<string, double> handler) =>
        tab.QFixCheckup.LCheckupProgress -= handler;
    internal static void TFixCheckupStart(QFixTab tab, string path) =>
        tab.QFixCheckup.LCheckupStart(
            [path], [LFlawKind.LFlawKindContainer], lCheckupForce: true);
    internal static void TFixFaceAttach(QFixTab tab, Dispatcher dispatcher) =>
        tab.QFixFaceAttach(dispatcher, () => { });
    internal static void TFixFaceDetach(QFixTab tab) => tab.QFixFace.QFixFaceDetach();
    internal static void TClinicSaveSuspend(CClinic clinic) => clinic.CClinicSaveSuspend();
    internal static void TClinicSaveResume(CClinic clinic) => clinic.CClinicSaveResume();
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

    internal static QAudioTab TAudioTabCreate(LPresetSelection preset, QViewer viewer, QList list) =>
        new(
            TAtelierCreate().CAtelierUsher,
            TAtelierCreate().CAtelierInspectorCreate(),
            TAtelierCreate().CAtelierEdit.CEditAudio,
            preset,
            viewer,
            list);
    internal static QInspector TAudioInspectorRead(QAudioTab tab) => tab.QAudioInspector;
    internal static QProcessing TAudioProcessingRead(QAudioTab tab) => tab.QAudioProcessing;
    internal static IReadOnlyList<QProcessingRow> TAudioRowsRead() => QAudioProcessing.QAudioRows;
    internal static LAudioKind? TAudioKindRead(string step) => QAudioTab.QAudioKindRead(step);
    internal static void TAudioClose(QAudioTab tab) => tab.QAudioClose();
    internal static LSceneTabEntry TAudioLayoutRead(QAudioTab tab) => tab.QAudioLayoutRead(new LSceneTabEntry());
    internal static void TAudioLayoutApply(QAudioTab tab, LSceneTabEntry? layout) => tab.QAudioLayoutApply(layout);
    internal static void TAudioSkipHandle(QAudioTab tab) => tab.QAudioSkipHandle();
    internal static void TAudioChangeHandle(QAudioTab tab) => tab.QAudioChangeHandle();
    internal static void TAudioItemsHandle(QAudioTab tab, IReadOnlyList<LDocketEntry> added) =>
        tab.QAudioStore.QAudioItemsHandle(added);
    internal static void TAudioPersistentSave(QAudioTab tab) => tab.QAudioStore.QAudioPersistentSave();
    internal static void TAudioViewerApply(QAudioTab tab) => tab.QAudioViewerApply();
    internal static LWorkAudio TAudioPlanRead(QAudioTab tab) => tab.QAudioStore.QAudioPlanRead();
    internal static void TAudioStateSave(QAudioTab tab) => tab.QAudioStore.QAudioStateSave();
    internal static void TAudioPlanRestore(QAudioTab tab, string path, bool ownerFirst) =>
        tab.QAudioStore.QAudioPlanRestore(path, ownerFirst);
    internal static void TAudioFilterAttach(QViewer viewer, Action<string> handler) =>
        viewer.QViewerPreviewChanged += () => handler(viewer.QViewerAudio.QViewerAudioResolve());
    internal static void TAudioDeferAttach(QAudioTab tab, Action handler) => tab.QAudioViewerDeferred += handler;
    internal static void TAudioMissingAttach(QAudioTab tab, Action handler) => tab.QAudioPresetMissing += handler;
    internal static void TAudioIncompatibleAttach(QAudioTab tab, Action handler) =>
        tab.QAudioPresetIncompatible += handler;
    internal static void TAudioRun(QAudioTab tab, bool urgent) => tab.QAudioRun(urgent, default, default);
    internal static void TAudioAllRun(QAudioTab tab) => tab.QAudioAllRun(default, default);
    internal static void TAudioLibrarianAttach(
        Func<string, LSidecarAudioEntry?>? reader, Func<string, LSidecarAudioEntry?, bool>? writer)
    {
        LLibrarian.LLibrarianAudioReader = reader;
        LLibrarian.LLibrarianAudioWriter = writer;
    }

    internal static void TVolumePersistentSet(CVolume volume, bool persistent) =>
        volume.CVolumePersistentSet(persistent);

    internal static QMergeTab TMergeTabCreate(LPresetSelection preset, QViewer viewer, QList list)
    {
        CAtelier atelier = TAtelierCreate();
        return new(atelier.CAtelierUsher, preset, viewer, list, atelier.CAtelierMerge);
    }
    internal static void TMergeLayoutApply(QMergeTab tab, LSceneTabEntry? layout) => tab.QMergeLayoutApply(layout);
    internal static void TMergeClose(QMergeTab tab) => tab.QMergeClose();
    internal static LSceneTabEntry TMergeLayoutRead(QMergeTab tab) => tab.QMergeLayoutRead(new LSceneTabEntry());
    internal static IReadOnlyList<string> TMergePathsRead(QMergeTab tab) => tab.QMergePathsRead();
    internal static IReadOnlyList<string> TMergeEligibleRead(QMergeTab tab) => tab.QMergeEligibleRead();
    internal static void TMergeMissingAttach(QMergeTab tab, Action handler) => tab.QMergePresetMissing += handler;
    internal static void TMergeRun(QMergeTab tab, bool urgent) => tab.QMergeRun(urgent, default, default);
    internal static int TMergeCohortRun(QMergeTab tab, Guid cohort) => tab.QMergeCohortRun(cohort, default, default);

    internal static void TGroupAutoChange(LGroupSelection selection, bool auto) => selection.LGroupAutoChange(auto);
    internal static void TGroupStrictChange(LGroupSelection selection, bool strict) =>
        selection.LGroupStrictChange(strict);

    internal static QConvertTab TConvertTabCreate(LPresetSelection preset, QViewer viewer, QList list) =>
        new(preset, viewer, list, TAtelierCreate().CAtelierConvert);
    internal static void TConvertMissingAttach(QConvertTab tab, Action handler) => tab.QConvertPresetMissing += handler;
    internal static void TConvertRun(QConvertTab tab, bool urgent) =>
        tab.QConvertRun(urgent, default, default);
    internal static void TConvertAllRun(QConvertTab tab) => tab.QConvertAllRun(default, default);
    internal static void TConvertItemsRun(QConvertTab tab, params string[] paths) =>
        tab.QConvertItemsRun(paths, default, default);

    internal static void TMessengerDeliverAttach(Func<Guid, string, Guid, bool>? deliver) =>
        LMessenger.LMessengerDeliverSource = deliver;

    internal static LWorkFixStep TFixStepCreate(LFlawKind kind, bool repair, bool persistent) =>
        new(kind, repair, persistent);
    internal static LWorkFix TFixPlanCreate(IReadOnlyList<LWorkFixStep> steps, LWorkFixSalvage? salvage = null) =>
        new(steps) { LWorkFixSalvage = salvage ?? LWorkFixSalvage.LWorkSalvageCreate() };
    internal static LSidecarFixEntry TFixRecordCreate(LWorkFix plan) => LFixSidecar.LFixPersistentCreate(plan);
    internal static LWorkAudio TWorkAudioCreate(bool skip, params LWorkAudioStep[] steps) =>
        new(steps) { LWorkAudioSkip = skip };
    internal static LSidecarAudioEntry TAudioRecordCreate(LWorkAudio plan) =>
        LAudioSidecar.LAudioPersistentCreate(plan);

    internal static QAction TActionCreate() => new();
    internal static void TActionDocketAttach(QAction action, LDocket docket) => action.QActionDocketAttach(docket);
    internal static void TActionEligibleAttach(QAction action, Func<IReadOnlyList<string>> source) =>
        action.QActionEligibleAttach(source);
    internal static bool TActionEligibleCheck(QAction action, params string[] chosen) =>
        action.QActionEligibleCheck(chosen);
    internal static void TActionStripAttach(QStrip strip) => QActionRelay.QActionStripAttach(strip);
    internal static void TActionListAttach(QAction action, QList list) => action.QActionListAttach(list);
    internal static void TActionRelayAttach(QAction action, QStrip strip, QStripTab tab) =>
        action.QActionRelayAttach(strip, tab);
    internal static void TActionAutoSet(QAction action, bool? state) => action.QActionAutoSet(state);
    internal static void TActionRelaySelect(QAction action, Guid target) => action.QActionRelaySelect(target);
    internal static void TActionRelayApply(QAction action, Guid target) => action.QActionRelayApply(target);
    internal static IReadOnlyList<QActionOption> TActionOptionsRead(QAction action) => action.QActionOptionsRead();
    internal static IReadOnlyList<QActionOption> TActionMenuRead(QAction action) => action.QActionMenuRead();
    internal static void TActionRunAttach(QAction action, Action<bool> handler) =>
        action.QActionExecuted += (urgent, _, _) => handler(urgent);
    internal static void TActionAllAttach(QAction action, Action handler) =>
        action.QActionAllAdded += (_, _) => handler();
    internal static void TActionItemsAttach(QAction action, Action<IReadOnlyList<string>> handler) =>
        action.QActionItemsAdded += (paths, _, _) => handler(paths);
    internal static void TActionCohortAttach(QAction action, Func<Guid, int> handler) =>
        action.QActionCohortAdded += (cohort, _, _) => handler(cohort);
    internal static (Guid TActionTarget, Guid TActionSource) TActionRouteRead(QAction action)
    {
        (Guid TActionTarget, Guid TActionSource) route = default;
        Action<IReadOnlyList<string>, Guid, Guid> capture = (_, target, source) => route = (target, source);
        action.QActionItemsAdded += capture;
        action.QActionItemsRun(Array.Empty<string>());
        action.QActionItemsAdded -= capture;
        return route;
    }
    internal static void TActionEmptyAttach(QAction action, Action handler) => action.QActionEmptyRaised += handler;
    internal static void TActionFaceAttach(QAction action, Action handler) => action.QActionFaceChanged += handler;
    internal static void TActionAllRun(QAction action) => action.QActionAllRun();
    internal static void TActionHighRun(QAction action) => action.QActionHighRun();
    internal static void TActionListRun(QAction action) => action.QActionListRun();
    internal static bool TActionCohortRun(QAction action, Guid cohort) => action.QActionCohortRun(cohort);
    internal static void TActionAccept(Guid target, string path, Guid cohort) =>
        QActionRelay.QActionAccept(target, path, cohort);

    internal static QCompass TCompassCreate(QFlow flow, QViewer viewer, bool sections) => new(flow, viewer, sections);
    internal static IReadOnlyList<QCompassGroup> TCompassGroupsRead(QCompass compass) => compass.QCompassGroupsRead();
    internal static IReadOnlyList<int> TCompassSectionRead(QCompass compass) => compass.QCompassSectionRead();
    internal static QCompassButton TCompassPlayRead(bool playing) => QCompass.QCompassPlayRead(playing);
    internal static void TCompassRun(QCompass compass, string key) => compass.QCompassRun(key);
    internal static void TCompassWaveformToggle(QCompass compass) => compass.QCompassWaveformToggle();
    internal static void TCompassVolumeSet(QCompass compass, double raw) => compass.QCompassVolumeSet(raw);
    internal static void TCompassVolumeAttach(QViewer viewer, Action<double> handler) =>
        viewer.QViewerAudio.QViewerVolumeChanged += handler;
    internal static string TCompassVolumeFormat(double raw) => QCompass.QCompassVolumeFormat(raw);
    internal static double TCompassFillResolve(double host, double raw) => QCompass.QCompassFillResolve(host, raw);
    internal static IReadOnlyList<double> TCompassSeparatorsResolve(params double[] tops) =>
        QCompass.QCompassSeparatorsResolve(tops);
}
