using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The engine's face for the console gate: every member forwards to the same-named member of the engine's console
// facade. It rejects a null engine and keeps it for every forward.
public sealed class LConsoleOutlet : LConsolePort
{
    private readonly LEngine _lConsoleOutletEngine;

    public LConsoleOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lConsoleOutletEngine = lEngine;
    }

    public Action LConsoleAttach(Action lChanged, Action lDeferred, Action<Predicate<LStation>, double> lProgressed) =>
        _lConsoleOutletEngine.LEngineConsole.LConsoleAttach(lChanged, lDeferred, lProgressed);

    public void LConsoleLoad() => _lConsoleOutletEngine.LEngineConsole.LConsoleLoad();

    public LConsoleAnswer LConsoleRead(LStation? lStation) =>
        _lConsoleOutletEngine.LEngineConsole.LConsoleRead(lStation);

    public LStation LConsoleStationResolve(LStation? lStation) =>
        _lConsoleOutletEngine.LEngineConsole.LConsoleStationResolve(lStation);

    public LStation? LConsoleStationMove(LStation? lStation, int lStep) =>
        _lConsoleOutletEngine.LEngineConsole.LConsoleStationMove(lStation, lStep);

    public LStation LConsoleStart(LStation? lStation) => _lConsoleOutletEngine.LEngineConsole.LConsoleStart(lStation);

    public LStation LConsolePause(LStation? lStation) => _lConsoleOutletEngine.LEngineConsole.LConsolePause(lStation);

    public LStation LConsoleStop(LStation? lStation) => _lConsoleOutletEngine.LEngineConsole.LConsoleStop(lStation);

    public (LStation LConsoleStation, bool LConsoleChanged) LConsoleAutoSave(LStation? lStation, bool lAuto) =>
        _lConsoleOutletEngine.LEngineConsole.LConsoleAutoSave(lStation, lAuto);

    public LStation LConsoleCancel(LStation? lStation, Func<int, string, bool> lConfirm) =>
        _lConsoleOutletEngine.LEngineConsole.LConsoleCancel(lStation, lConfirm);

    public (LStation LConsoleStation, int LConsoleTotal, int LConsoleHeld, int LConsoleBlocked) LConsoleRemove(
        LStation? lStation) => _lConsoleOutletEngine.LEngineConsole.LConsoleRemove(lStation);

    public void LConsoleDoneClear() => _lConsoleOutletEngine.LEngineConsole.LConsoleDoneClear();

    public bool LConsoleActiveCheck() => _lConsoleOutletEngine.LEngineConsole.LConsoleActiveCheck();

    public Task LConsoleAllClear() => _lConsoleOutletEngine.LEngineConsole.LConsoleAllClear();

    public double LConsolePeriodRead() => _lConsoleOutletEngine.LEngineConsole.LConsolePeriodRead();
}
