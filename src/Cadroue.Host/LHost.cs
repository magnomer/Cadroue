using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer;

// One list's docket over the media scan, its traces naming a path as the list gate does.
static LListPort LListOutletCreate(Func<string, string> lName) => new LListOutlet(lName, LListScan);

// The media paths under the requested ones, and each folder skipped with its reason.
static async Task<(IReadOnlyList<string>, IReadOnlyList<(string, string)>)> LListScan(
    IReadOnlyList<string> lPaths, CancellationToken lToken)
{
    LMediaScanResult lScan = await LMedia.LMediaPathScan(lPaths, lToken);
    return (lScan.LMediaScanPaths,
        lScan.LMediaScanNotices.Select(lNotice => (lNotice.LMediaScanFolder, lNotice.LMediaScanReason)).ToArray());
}

int code = 1;
Thread thread = new(() =>
{
    PProgram application = new();
    application.PProgramAttach(LTraceLog.LTraceErrorRecord, QProgram.QProgramDebounceTick);
    LRigFactory.LRigFactoryAttach(LFlawScanner.LFlawScanRun);
    LRig rig = LRigFactory.LRigFactoryCreate();
    CAtelier atelier = new(new LAtelierPort
    {
        LAtelierUsher = new LUsherOutlet(),
        LAtelierLineage = new LLineageOutlet(),
        LAtelierWorkspace = new LWorkspaceOutlet(),
        LAtelierPlacement = new LPlacementOutlet(LPreferenceStateStore.LPreferencePlacementRead),
        LAtelierViewer = new LViewerOutlet(
            lPath => System.IO.Path.GetFileName(lPath),
            () => System.DateTimeOffset.Now,
            System.Environment.NewLine),
        LAtelierEdit = new LEditOutlet(),
        LAtelierRoster = new LRosterOutlet(),
        LAtelierFix = new LFixOutlet(),
        LAtelierConvert = new LConvertOutlet(),
        LAtelierTone = new LToneOutlet(() => LFlyleaf.LFlyleafActive, LRenderer.LRendererEngineRead),
        LAtelierEqualizer = new LEqualizerOutlet(),
        LAtelierClinic = new LClinicOutlet(),
        LAtelierRosterCard = new LRosterCardOutlet(),
        LAtelierMerge = new LMergeOutlet(lMessage => LTraceLog.LTraceWarningRecord(lMessage)),
        LAtelierNoise = new LNoiseOutlet(),
        LAtelierConsole = new LConsoleOutlet(() => LRenderer.LRendererProgramCurrent),
        LAtelierFlow = new LFlowOutlet(
            rig.LRigKeyframe,
            rig.LRigWaveform,
            () => LFrameStore.LFrameStateCurrent.LFrameFlowHeight),
        LAtelierRosterDetail = new LRosterDetailOutlet(() => System.DateTimeOffset.Now),
        LAtelierWhitebalance = new LWhitebalanceOutlet(),
        LAtelierFilter = new LFilterOutlet(),
        LAtelierScene = new LConsoleSceneOutlet(new LSceneStore(
            LScene.LSceneCatalogueLoad,
            () => LScene.LSceneNames,
            () => LScene.LSceneActiveName,
            LScene.LSceneActiveSet,
            LScene.LSceneRead,
            LScene.LSceneMatch,
            LScene.LSceneSave,
            LScene.LSceneDelete,
            LScene.LSceneFileResolve,
            LScene.LSceneFileSave,
            LScene.LSceneFileLoad,
            LScene.LSceneStemResolve,
            lMessage => LTraceLog.LTraceLoadingRecord(lMessage))),
        LAtelierList = LListOutletCreate,
    });
    QPlacement.QPlacementGateSeam = atelier.CAtelierPlacement;

    LEngine engine = new(rig);
    QProgram program = new(engine.LEngineSchedule, engine.LEngineStationAttach);

    application.Startup += (_, e) =>
    {
        program.QProgramStart(
            e.Args,
            application.PProgramDispatch,
            application.PProgramDefer,
            PProgram.PProgramNamesRead,
            PProgram.PProgramResourceRead,
            PProgram.PProgramPlacementKeys,
            application.PProgramDebounceDefer,
            PProgramFlyleaf.PProgramFlyleafStart,
            PProgramFlyleaf.PProgramVerboseApply);
        application.PProgramWindowShow(new QWindow(atelier));
    };
    application.Exit += (_, e) => program.QProgramClose(e.ApplicationExitCode);

    code = application.Run();
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();
return code;
