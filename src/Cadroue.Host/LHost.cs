using System.Threading;
using Cadroue.Conduct;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer;

int code = 1;
Thread thread = new(() =>
{
    PProgram application = new();
    application.PProgramAttach(LTraceLog.LTraceErrorRecord, QProgram.QProgramDebounceTick);
    CAtelier atelier = new(
        new LUsherOutlet(),
        new LLineageOutlet(),
        new LWorkspaceOutlet(),
        new LPlacementOutlet(LPreferenceStateStore.LPreferencePlacementRead),
        new LViewerOutlet(
            lPath => System.IO.Path.GetFileName(lPath),
            () => System.DateTimeOffset.Now,
            System.Environment.NewLine));
    QPlacement.QPlacementGateSeam = atelier.CAtelierPlacement;

    LRigFactory.LRigFactoryAttach(LFlawScanner.LFlawScanRun);
    LEngine engine = new(LRigFactory.LRigFactoryCreate());
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
