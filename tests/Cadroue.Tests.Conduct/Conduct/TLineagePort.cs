using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;
using Cadroue.Tests.Interface;

namespace Cadroue.Tests;

internal sealed class TLineagePort : LLineagePort
{
    public List<IReadOnlyList<Guid>> TLineageCalls { get; } = [];

    public List<string> TLineageTitles { get; } = [];

    public LLineageAnswer TLineageAnswer { get; set; } = TInterface.TLineageAnswerCreate([], [], []);

    public LLineageAnswer LLineageResolve(IReadOnlyList<Guid> lIds)
    {
        TLineageCalls.Add(lIds);
        return TLineageAnswer;
    }

    public LLineageBatch TLineageBatch { get; set; } =
        TInterface.TLineageBatchCreate(
            Guid.Empty, new HashSet<Guid>(), 0, default, [], [], null, null, 0, 0, [], false);

    public LLineageBatch LLineageBatchResolve(Guid lBatchId)
    {
        TLineageCalls.Add([lBatchId]);
        return TLineageBatch;
    }

    public string LLineageTitleRead(Guid lBatchId, Guid lRelaySource, string lTab)
    {
        string lTitle = $"{lBatchId}|{lRelaySource}|{lTab}";
        TLineageTitles.Add(lTitle);
        return lTitle + ".title";
    }
}
