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
    application.PProgramAttach(LTraceLog.LTraceErrorRecord, LProgram.LProgramDebounceTick);
    CAtelier atelier = new(new LUsherOutlet(), new LLineageOutlet());

    LRigFactory.LRigFactoryAttach(LFlawScanner.LFlawScanRun);
    LEngine engine = new(LRigFactory.LRigFactoryCreate());
    LProgram program = new(engine.LEngineSchedule, engine.LEngineStationAttach);

    application.Startup += (_, e) =>
    {
        program.LProgramStart(
            e.Args,
            application.PProgramDispatch,
            application.PProgramDefer,
            PProgram.PProgramNamesRead,
            PProgram.PProgramResourceRead,
            PProgram.PProgramPlacementKeys,
            application.PProgramDebounceDefer,
            PProgramFlyleaf.PProgramFlyleafStart,
            PProgramFlyleaf.PProgramVerboseApply);
        application.PProgramWindowShow(new LWindow(atelier));
    };
    application.Exit += (_, e) => program.LProgramClose(e.ApplicationExitCode);

    code = application.Run();
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();
return code;
