using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The engine's face for the lineage gate: every member forwards to the same-named member of the engine's lineage
// facade. It rejects a null engine and keeps it for every forward.
public sealed class LLineageOutlet : LLineagePort
{
    private readonly LEngine _lLineageOutletEngine;

    public LLineageOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lLineageOutletEngine = lEngine;
    }

    public LLineageAnswer LLineageResolve(IReadOnlyList<Guid> lIds) =>
        _lLineageOutletEngine.LEngineLineage.LLineageResolve(lIds);

    public LLineageBatch LLineageBatchResolve(Guid lBatchId) =>
        _lLineageOutletEngine.LEngineLineage.LLineageBatchResolve(lBatchId);

    public string LLineageTitleRead(Guid lBatchId, Guid lRelaySource, string lTab) =>
        _lLineageOutletEngine.LEngineLineage.LLineageTitleRead(lBatchId, lRelaySource, lTab);
}
