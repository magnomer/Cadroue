namespace Cadroue.ShellEngine;

// The roster detail's item, read from the station's schedule with its tab title. With no schedule, or for the empty
// id, there is nothing to show. The system clock times an item that still runs.
public sealed class LRosterDetailFacade
{
    internal LRosterDetailFacade()
    {
    }

    public LRosterJob? LRosterDetailRead(
        LStation lStation,
        Guid lWorkId,
        Func<(Guid LRosterPlaceBatch, Guid LRosterPlaceRelay, string LRosterPlaceTab)> lPlace)
    {
        ArgumentNullException.ThrowIfNull(lStation);
        ArgumentNullException.ThrowIfNull(lPlace);
        if (lWorkId == Guid.Empty
            || LStationEngine.LStationSchedule is not { } lSchedule
            || lSchedule.LScheduleRecords.FirstOrDefault(lWorkItem => lWorkItem.LWorkId == lWorkId) is not { } lItem)
        {
            return null;
        }

        (Guid lBatch, Guid lRelay, string lTab) = lPlace();
        bool lOwned = lStation.LStationRunner.LRunnerOwnerCheck(lItem);
        return LRosterJob.LRosterJobCreate(
            lItem, lOwned, LCartographerTitle.LCartographerTitleRead(lBatch, lRelay, lTab), () => DateTimeOffset.Now);
    }
}
