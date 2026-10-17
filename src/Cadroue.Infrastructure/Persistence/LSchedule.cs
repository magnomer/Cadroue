using System.Collections.ObjectModel;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LSchedule : LScheduleContract
{
    private readonly ObservableCollection<LWorkItem> lScheduleItems = new();
    private readonly Dictionary<Guid, LWorkItem> lScheduleLiveItems = new();

    public LSchedule()
    {
        LScheduleRecords = new ReadOnlyObservableCollection<LWorkItem>(lScheduleItems);
    }

    public ReadOnlyObservableCollection<LWorkItem> LScheduleRecords { get; }

    public event Action<LScheduleContract>? LScheduleChanged;

    public event Action<LWorkItem, LScheduleNotice>? LScheduleItemChanged;

    public int LScheduleDoneCount =>
        lScheduleItems.Count(lWorkItem => lWorkItem.LWorkRunState.LWorkStateCurrent == LWorkState.LWorkStateDone);

    internal static string LScheduleIdShorten(Guid lWorkId) => lWorkId.ToString("N")[..8];

    public void LScheduleChangeRaise() => LScheduleChanged?.Invoke(this);

    public void LScheduleItemRaise(LWorkItem lWorkItem, LScheduleNotice lScheduleNotice) =>
        LScheduleItemChanged?.Invoke(lWorkItem, lScheduleNotice);

    internal void LScheduleItemAdd(LWorkItem lWorkItem) => lScheduleItems.Add(lWorkItem);

    internal LWorkItem? LScheduleLiveRead(Guid lWorkId) =>
        lScheduleLiveItems.TryGetValue(lWorkId, out LWorkItem? lWorkLive) ? lWorkLive : null;

    internal void LScheduleLiveSet(LWorkItem lWorkItem) => lScheduleLiveItems[lWorkItem.LWorkId] = lWorkItem;

    internal void LScheduleLiveRemove(Guid lWorkId) => lScheduleLiveItems.Remove(lWorkId);

    public void LScheduleLoad()
    {
        LDepotIndex.LDepotIndexCreate();
        LScheduleStale.LScheduleStaleClaim();
        IReadOnlyList<LScheduleEntry>? lScheduleRebuilt = LDepotIndex.LDepotDirty
            ? LDepotIndex.LDepotIndexRebuild()
            : null;

        LScheduleItemsBuild(LDepotIndex.LDepotDirty
            ? LScheduleStore.LScheduleFolderRead()
            : lScheduleRebuilt ?? LScheduleStore.LScheduleIndexRead());
    }

    private void LScheduleItemsBuild(IEnumerable<LScheduleEntry> lScheduleEntries)
    {
        var lScheduleLoaded = new List<LWorkItem>();
        var lScheduleRunningIds = new HashSet<Guid>();
        foreach ((LDepotFolder lDepotFolder, LWorkDocument lWorkRecord) in lScheduleEntries)
        {
            if (!LScheduleScope.LScheduleScopeMatch(lWorkRecord))
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
            LWorkRunState lWorkRunState = lWorkItem.LWorkRunState;
            lWorkRunState.LWorkStateCurrent =
                LScheduleEntry.LScheduleStateRead(lDepotFolder, lWorkRunState.LWorkStateCurrent);
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

        LScheduleChanged?.Invoke(this);
    }

    public IReadOnlyList<LWorkItem> LSchedulePendingRead() =>
        lScheduleItems
            .Where(lWorkItem => lWorkItem.LWorkRunState.LWorkStateCurrent == LWorkState.LWorkStatePending)
            .ToArray();
}
