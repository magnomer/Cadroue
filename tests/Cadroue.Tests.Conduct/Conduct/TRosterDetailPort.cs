using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// Answers the job a test sets; with none set the schedule holds no such item. Like the outlet, it asks the place only
// once it has an item.
internal sealed class TRosterDetailPort : LRosterDetailPort
{
    public LRosterJob? TRosterDetailJob { get; set; }

    public List<string> TRosterDetailCalls { get; } = [];

    public LStation? TRosterDetailStation { get; private set; }

    public LRosterJob? LRosterDetailRead(
        LStation lStation,
        Guid lWorkId,
        Func<(Guid LRosterPlaceBatch, Guid LRosterPlaceRelay, string LRosterPlaceTab)> lPlace)
    {
        TRosterDetailStation = lStation;
        if (TRosterDetailJob is null)
        {
            TRosterDetailCalls.Add($"{lWorkId}");
            return null;
        }

        (Guid lBatch, Guid lRelay, string lTab) = lPlace();
        TRosterDetailCalls.Add($"{lWorkId}:{lBatch}:{lRelay}:{lTab}");
        return TRosterDetailJob;
    }
}
