namespace Cadroue.ShellEngine;

public interface LLineagePort
{
    LLineageAnswer LLineageResolve(IReadOnlyList<Guid> lIds);

    LLineageBatch LLineageBatchResolve(Guid lBatchId);

    string LLineageTitleRead(Guid lBatchId, Guid lRelaySource, string lTab);
}
