using System.Collections.ObjectModel;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LSchedule : LScheduleContract
{
    private readonly ObservableCollection<LWorkItem> lScheduleItems = new();
    private readonly Dictionary<Guid, LWorkItem> lScheduleLiveItems = new();

    public LSchedule()
    {
        LScheduleRecords = new ReadOnlyObservableCollection<LWorkItem>(lScheduleItems);
    }

    public ReadOnlyObservableCollection<LWorkItem> LScheduleRecords { get; }

    public event Action<LScheduleContract>? LScheduleChange;

    public event Action<LWorkItem, LScheduleNotice>? LScheduleItemChange;

    public int LScheduleDoneCount =>
        lScheduleItems.Count(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateDone);

    public void LScheduleChangeRaise() => LScheduleChange?.Invoke(this);

    public void LScheduleItemRaise(LWorkItem lWorkItem, LScheduleNotice lScheduleNotice) =>
        LScheduleItemChange?.Invoke(lWorkItem, lScheduleNotice);

    public void LScheduleLoad()
    {
        LDepotIndex.LDepotIndexCreate();
        LScheduleStaleClaim();
        if (LDepotIndex.LDepotDirty)
        {
            LDepotIndex.LDepotIndexRebuild();
        }

        LScheduleItemsBuild(LDepotIndex.LDepotDirty
            ? LScheduleFolderRead()
            : LScheduleIndexRead());
    }

    private void LScheduleItemsBuild(IEnumerable<(LDepotFolder LDepotFolder, LWorkRecord LWorkRecord)> lSchedulePairs)
    {
        var lScheduleLoaded = new List<LWorkItem>();
        var lScheduleRunningIds = new HashSet<Guid>();
        foreach ((LDepotFolder lDepotFolder, LWorkRecord lWorkRecord) in lSchedulePairs)
        {
            if (!LScheduleScopeMatch(lWorkRecord))
            {
                continue;
            }

            if (lDepotFolder == LDepotFolder.LDepotFolderRunning
                && lScheduleLiveItems.TryGetValue(lWorkRecord.LWorkId, out LWorkItem? lWorkLive))
            {
                lScheduleRunningIds.Add(lWorkRecord.LWorkId);
                lScheduleLoaded.Add(lWorkLive);
                continue;
            }

            LWorkItem lWorkItem = lWorkRecord.LWorkItemCreate();
            lWorkItem.LWorkStateCurrent = LScheduleStateRead(lDepotFolder, lWorkItem.LWorkStateCurrent);
            lScheduleLoaded.Add(lWorkItem);
        }

        foreach (Guid lScheduleLiveId in lScheduleLiveItems.Keys.ToArray())
        {
            if (!lScheduleRunningIds.Contains(lScheduleLiveId))
            {
                lScheduleLiveItems.Remove(lScheduleLiveId);
            }
        }

        lScheduleItems.Clear();
        foreach (LWorkItem lWorkItem in lScheduleLoaded.OrderBy(lItem => lItem.LWorkCreateTime))
        {
            lScheduleItems.Add(lWorkItem);
        }

        LScheduleChange?.Invoke(this);
    }

    private IEnumerable<(LDepotFolder, LWorkRecord)> LScheduleIndexRead()
    {
        foreach ((LDepotFolder lDepotFolder, string lDepotRecord) in LDepotIndex.LDepotRecordsRead())
        {
            if (LScheduleStore.LScheduleRecordParse(lDepotRecord) is { } lWorkRecord)
            {
                yield return (lDepotFolder, lWorkRecord);
            }
        }
    }

    private static IEnumerable<(LDepotFolder, LWorkRecord)> LScheduleFolderRead()
    {
        foreach (LDepotFolder lDepotFolder in Enum.GetValues<LDepotFolder>())
        {
            foreach (string lDepotFilePath in LDepot.LDepotFilesRead(lDepotFolder))
            {
                if (LScheduleStore.LScheduleRecordRead(lDepotFilePath) is { } lWorkRecord)
                {
                    yield return (lDepotFolder, lWorkRecord);
                }
            }
        }
    }

    public IReadOnlyList<LWorkItem> LSchedulePendingRead() =>
        lScheduleItems
            .Where(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStatePending)
            .ToArray();

    public bool LSchedulePendingExist() =>
        LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderScheduled)
            .Select(LScheduleStore.LScheduleRecordRead)
            .Any(lWorkRecord => lWorkRecord is { } lScheduleRecord && LScheduleScopeMatch(lScheduleRecord));

    private static LWorkState LScheduleStateRead(LDepotFolder lDepotFolder, LWorkState lScheduleFileState) =>
        lDepotFolder switch
        {
            LDepotFolder.LDepotFolderRunning => LWorkState.LWorkStateRunning,
            LDepotFolder.LDepotFolderDone => LScheduleTerminalRead(lScheduleFileState, LWorkState.LWorkStateDone),
            LDepotFolder.LDepotFolderFailed => LScheduleTerminalRead(lScheduleFileState, LWorkState.LWorkStateFailed),
            LDepotFolder.LDepotFolderCancelled => LWorkState.LWorkStateCancelled,
            _ => lScheduleFileState == LWorkState.LWorkStateRunning ? LWorkState.LWorkStatePending : lScheduleFileState
        };

    private static LWorkState LScheduleTerminalRead(LWorkState lScheduleFileState, LWorkState lScheduleFolderState) =>
        lScheduleFileState is LWorkState.LWorkStatePartial
            or LWorkState.LWorkStateUnresolved
            or LWorkState.LWorkStateBlocked
            ? lScheduleFileState
            : lScheduleFolderState;
}
