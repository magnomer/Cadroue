using System;
using System.Threading;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer;

Func<LEngine, LAtelierPort> port = static engine => new LAtelierPort
{
    LAtelierUsher = engine.LEngineUsher,
    LAtelierLineage = engine.LEngineLineage,
    LAtelierWorkspace = engine.LEngineWorkspace,
    LAtelierPlacement = new LPlacementOutlet(LPreferenceStateStore.LPreferencePlacementRead),
    LAtelierViewer = engine.LEngineViewer,
    LAtelierEdit = new LEditOutlet(engine),
    LAtelierRoster = engine.LEngineRoster,
    LAtelierRosterMenu = engine.LEngineRosterMenu,
    LAtelierFix = engine.LEngineFix,
    LAtelierFixPlan = engine.LEngineFixPlan,
    LAtelierConvert = engine.LEngineConvert,
    LAtelierTone = engine.LEngineTone,
    LAtelierEqualizer = engine.LEngineEqualizer,
    LAtelierClinic = new LClinicOutlet(),
    LAtelierRosterCard = engine.LEngineRosterCard,
    LAtelierMerge = engine.LEngineMerge,
    LAtelierNoise = engine.LEngineNoise,
    LAtelierConsole = engine.LEngineConsole,
    LAtelierFlow = engine.LEngineFlow,
    LAtelierMap = engine.LEngineMap,
    LAtelierRosterDetail = engine.LEngineRosterDetail,
    LAtelierWhitebalance = engine.LEngineWhitebalance,
    LAtelierFilter = engine.LEngineFilter,
    LAtelierAudio = engine.LEngineAudio,
    LAtelierLoudness = engine.LEngineLoudness,
    LAtelierVideo = engine.LEngineVideo,
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
    LAtelierEncoder = LSEncoderOutlet.LSEncoderOutletCreate(),
    LAtelierRelay = engine.LEngineRelay,
    LAtelierTrial = new LSTrialOutlet(
        LTrial.LTrialVideoRun,
        lAvailable => LTrialCatalog.LTrialSetApply(lAvailable),
        LTrialCatalog.LTrialSetCheck,
        LTrial.LTrialAudioRun,
        LTrialCatalog.LTrialAudioApply,
        LTrialCatalog.LTrialAudioCheck,
        LTraceLog.LTraceInfoRecord),
    LAtelierRepertoire = new LSRepertoireOutlet(
        LRepertoireVideo.LRepertoireEncodersRead,
        LRepertoireCatalog.LRepertoireContainerNames,
        LRepertoireVideo.LRepertoireContainerCheck,
        LRepertoireVideo.LRepertoireTokenResolve,
        LCapability.LCapabilityRead,
        LRepertoireAudioCatalog.LRepertoireAudioCandidates,
        LRepertoireCatalog.LRepertoireAudioCheck,
        LRepertoireAudioCatalog.LRepertoireAudioResolve,
        LSCodec.LSCodecAudioRead,
        LSCodec.LSCodecAudioRead),
};

int code = 1;
Thread thread = new(() =>
{
    PProgram application = new();
    application.PProgramAttach(LTraceLog.LTraceErrorRecord, QProgram.QProgramDebounceTick);
    LRigFactory.LRigFactoryAttach(LFlawScanner.LFlawScanRun);
    LRig rig = LRigFactory.LRigFactoryCreate();
    LEngine engine = new(rig);
    CAtelier atelier = new(port(engine));
    QPlacement.QPlacementGateSeam = atelier.CAtelierPlacement;

    QProgram program = new(engine.LEngineSchedule, LEngineSeam.LEngineStationAttach);

    application.Startup += (_, e) =>
    {
        program.QProgramStart(
            e.Args,
            application.PProgramDispatch,
            application.PProgramDefer,
            PProgramResource.PProgramNamesRead,
            PProgramResource.PProgramResourceRead,
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
