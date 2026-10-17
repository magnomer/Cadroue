using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The engine's face for the roster card gate: every member forwards to the same-named member of the engine's
// roster card facade. It rejects a null engine and keeps it for every forward.
public sealed class LRosterCardOutlet : LRosterCardPort
{
    private readonly LEngine _lCardOutletEngine;

    public LRosterCardOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lCardOutletEngine = lEngine;
    }

    public IReadOnlyList<LRosterRow> LRosterCardRead(IReadOnlyList<Guid> lIds, Guid lRunner) =>
        _lCardOutletEngine.LEngineRosterCard.LRosterCardRead(lIds, lRunner);
}
