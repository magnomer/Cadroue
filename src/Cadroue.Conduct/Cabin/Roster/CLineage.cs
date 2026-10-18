using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed class CLineage
{
    private readonly LLineagePort _cLineagePort;

    internal CLineage(LLineagePort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cLineagePort = port;
    }

    public CLineageAnswer CLineageRead(IReadOnlyList<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        return CLineageAnswer.CLineageAnswerCreate(_cLineagePort.LLineageResolve(ids));
    }

    public CLineageBatch CLineageBatchRead(Guid batch) =>
        CLineageBatch.CLineageBatchCreate(_cLineagePort.LLineageBatchResolve(batch));

    public string CLineageTitleRead(Guid batch, Guid relay, string tab) =>
        _cLineagePort.LLineageTitleRead(batch, relay, tab);
}
