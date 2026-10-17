using System;
using System.Threading;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer;

int code = 1;
Thread thread = new(() =>
{
    PProgram application = new();
    application.PProgramAttach(LTraceLog.LTraceErrorRecord, QProgram.QProgramDebounceTick);
    LRigFactory.LRigFactoryAttach(LFlawScanner.LFlawScanRun);
    LRig rig = LRigFactory.LRigFactoryCreate();
    LEngine engine = new(rig);
    CAtelier atelier = new(new LAtelierPort
    {
        LAtelierUsher = new LUsherOutlet(engine),
        LAtelierLineage = new LLineageOutlet(engine),
        LAtelierWorkspace = new LWorkspaceOutlet(engine),
        LAtelierPlacement = new LPlacementOutlet(LPreferenceStateStore.LPreferencePlacementRead),
        LAtelierViewer = new LViewerOutlet(engine),
        LAtelierEdit = new LEditOutlet(engine),
        LAtelierRoster = new LRosterOutlet(engine),
        LAtelierFix = new LFixOutlet(),
        LAtelierConvert = new LConvertOutlet(engine),
        LAtelierTone = new LToneOutlet(engine),
        LAtelierEqualizer = new LEqualizerOutlet(engine),
        LAtelierClinic = new LClinicOutlet(),
        LAtelierRosterCard = new LRosterCardOutlet(engine),
        LAtelierMerge = new LMergeOutlet(engine),
        LAtelierNoise = new LNoiseOutlet(engine),
        LAtelierConsole = new LConsoleOutlet(engine),
        LAtelierFlow = new LFlowOutlet(engine),
        LAtelierMap = new LMapOutlet(engine),
        LAtelierRosterDetail = new LRosterDetailOutlet(engine),
        LAtelierWhitebalance = new LWhitebalanceOutlet(engine),
        LAtelierFilter = new LFilterOutlet(engine),
        LAtelierAudio = new LAudioOutlet(engine),
        LAtelierLoudness = new LLoudnessOutlet(engine),
        LAtelierVideo = new LVideoOutlet(engine),
        LAtelierScene = new LConsoleSceneOutlet(new LSceneStore(
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
            lMessage => LTraceLog.LTraceLoadingRecord(lMessage))),
        LAtelierList = LListOutlet.LListOutletCreate(LRigFactoryList.LRigListCreate()),
        LAtelierRelay = new LRelayOutlet(engine),
    });
    QPlacement.QPlacementGateSeam = atelier.CAtelierPlacement;

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
