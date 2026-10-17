using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The roster card's rows, read from the station's schedule. With no schedule there is nothing to show.
public sealed class LRosterCardFacade
{
    internal LRosterCardFacade()
    {
    }

    public IReadOnlyList<LRosterRow> LRosterCardRead(IReadOnlyList<Guid> lIds, Guid lRunner)
    {
        if (LStation.LStationSchedule is not { } lSchedule)
        {
            return [];
        }

        return LScheduleLookup.LScheduleRecordsRead(lSchedule, lIds)
            .Select(lWorkItem => LRosterRow.LRosterRowCreate(
                lWorkItem, lSchedule.LScheduleOwnerCheck(lWorkItem, lRunner)))
            .ToArray();
    }
}
