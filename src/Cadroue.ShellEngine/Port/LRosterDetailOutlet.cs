using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The engine's face for the roster detail gate: every member forwards to the same-named member of the engine's roster
// detail facade. It rejects a null engine and keeps it for every forward.
public sealed class LRosterDetailOutlet : LRosterDetailPort
{
    private readonly LEngine _lRosterDetailEngine;

    public LRosterDetailOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lRosterDetailEngine = lEngine;
    }

    public LRosterJob? LRosterDetailRead(
        LStation lStation,
        Guid lWorkId,
        Func<(Guid LRosterPlaceBatch, Guid LRosterPlaceRelay, string LRosterPlaceTab)> lPlace) =>
        _lRosterDetailEngine.LEngineRosterDetail.LRosterDetailRead(lStation, lWorkId, lPlace);
}
