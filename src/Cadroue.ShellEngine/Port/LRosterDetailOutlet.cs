namespace Cadroue.ShellEngine;

// The roster detail's item, read from the station's schedule with its tab title. With no schedule, or for the empty
// id, there is nothing to show. The clock the host hands in times an item that still runs.
public sealed class LRosterDetailOutlet : LRosterDetailPort
{
    private readonly Func<DateTimeOffset> _lRosterDetailClock;

    public LRosterDetailOutlet(Func<DateTimeOffset> lClock)
    {
        ArgumentNullException.ThrowIfNull(lClock);
        _lRosterDetailClock = lClock;
    }

    public LRosterJob? LRosterDetailRead(
        LStation lStation,
        Guid lWorkId,
        Func<(Guid LRosterPlaceBatch, Guid LRosterPlaceRelay, string LRosterPlaceTab)> lPlace)
    {
        ArgumentNullException.ThrowIfNull(lStation);
        ArgumentNullException.ThrowIfNull(lPlace);
        if (lWorkId == Guid.Empty
            || LStation.LStationSchedule is not { } lSchedule
            || lSchedule.LScheduleRecords.FirstOrDefault(lWorkItem => lWorkItem.LWorkId == lWorkId) is not { } lItem)
        {
            return null;
        }

        (Guid lBatch, Guid lRelay, string lTab) = lPlace();
        bool lOwned = lStation.LStationRunner.LRunnerOwnerCheck(lItem);
        return LRosterJob.LRosterJobCreate(
            lItem, lOwned, LCartographer.LCartographerTitleRead(lBatch, lRelay, lTab), _lRosterDetailClock);
    }
}
