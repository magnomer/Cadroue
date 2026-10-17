namespace Cadroue.ShellEngine;

// The engine's face for the roster gate: every member forwards to the same-named member of the engine's roster
// facade. It rejects a null engine and keeps it for every forward.
public sealed class LRosterOutlet : LRosterPort
{
    private readonly LEngine _lRosterOutletEngine;

    public LRosterOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lRosterOutletEngine = lEngine;
    }

    public bool LRosterSharedRead() => _lRosterOutletEngine.LEngineRoster.LRosterSharedRead();

    public bool LRosterDoneRead() => _lRosterOutletEngine.LEngineRoster.LRosterDoneRead();

    public bool LRosterSharedSave(bool lShared) => _lRosterOutletEngine.LEngineRoster.LRosterSharedSave(lShared);

    public bool LRosterDoneSave(bool lDone) => _lRosterOutletEngine.LEngineRoster.LRosterDoneSave(lDone);

    public (bool LRosterMenuRestartable, bool LRosterMenuCancellable, int LRosterMenuCount,
        IReadOnlyList<string> LRosterMenuPaths, Action LRosterMenuAction) LRosterMenuRead(
        LStation lStation, Guid lClicked) =>
        _lRosterOutletEngine.LEngineRoster.LRosterMenuRead(lStation, lClicked);
}
