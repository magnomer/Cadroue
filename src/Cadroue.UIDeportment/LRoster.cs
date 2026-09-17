namespace Cadroue.UIDeportment;

public sealed class LRoster
{
    private readonly List<Guid> lRosterOrderedIds = [];
    private readonly HashSet<Guid> lRosterSelectedIds = [];
    private readonly HashSet<Guid> lRosterCollapsedIds = [];
    private bool lRosterClosed;
    private bool lRosterDetailPending;
    private Guid lRosterCurrentId;
    private Guid lRosterCardId;

    public bool LRosterClosed => lRosterClosed;

    public bool LRosterDetailPending => lRosterDetailPending;

    public Guid LRosterCurrentId => lRosterCurrentId;

    public Guid LRosterCardId => lRosterCardId;

    public IReadOnlyList<Guid> LRosterOrderedIds => lRosterOrderedIds;

    public IReadOnlyCollection<Guid> LRosterSelectedIds => lRosterSelectedIds;

    public bool LRosterCloseSet()
    {
        if (lRosterClosed)
        {
            return false;
        }

        lRosterClosed = true;
        return true;
    }

    public void LRosterPendingSet(bool lDetailPending) => lRosterDetailPending = lDetailPending;

    public bool LRosterSelectedCheck(Guid lId) => lRosterSelectedIds.Contains(lId);

    public bool LRosterCollapsedCheck(Guid lBatchId) => lRosterCollapsedIds.Contains(lBatchId);

    public bool LRosterCardCheck(Guid lBatchId) => lBatchId == lRosterCardId;

    public void LRosterCollapseSet(Guid lBatchId, bool lCollapsed)
    {
        if (lCollapsed)
        {
            lRosterCollapsedIds.Add(lBatchId);
        }
        else
        {
            lRosterCollapsedIds.Remove(lBatchId);
        }
    }

    public bool LRosterCollapseToggle(Guid lBatchId)
    {
        bool lCollapsed = !lRosterCollapsedIds.Contains(lBatchId);
        LRosterCollapseSet(lBatchId, lCollapsed);
        return lCollapsed;
    }

    public bool LRosterOrderMatch(IReadOnlyList<Guid> lIds) => lRosterOrderedIds.SequenceEqual(lIds);

    public void LRosterOrderClear() => lRosterOrderedIds.Clear();

    public void LRosterOrderAdd(Guid lId) => lRosterOrderedIds.Add(lId);

    public void LRosterStaleRemove(IReadOnlyCollection<Guid> lBatchIds)
    {
        var lPresent = lRosterOrderedIds.ToHashSet();
        lRosterSelectedIds.RemoveWhere(lId => !lPresent.Contains(lId));
        if (!lPresent.Contains(lRosterCurrentId))
        {
            lRosterCurrentId = Guid.Empty;
        }

        if (lRosterCardId != Guid.Empty && !lBatchIds.Contains(lRosterCardId))
        {
            lRosterCardId = Guid.Empty;
        }

        lRosterCollapsedIds.RemoveWhere(lBatchId => !lBatchIds.Contains(lBatchId));
    }

    public void LRosterStepSelect(Guid lId, bool lRange, bool lToggle)
    {
        lRosterCardId = Guid.Empty;
        if (lRange && lRosterCurrentId != Guid.Empty)
        {
            int lAnchor = lRosterOrderedIds.IndexOf(lRosterCurrentId);
            int lTarget = lRosterOrderedIds.IndexOf(lId);
            if (lAnchor >= 0 && lTarget >= 0)
            {
                lRosterSelectedIds.Clear();
                for (int lIndex = Math.Min(lAnchor, lTarget); lIndex <= Math.Max(lAnchor, lTarget); lIndex++)
                {
                    lRosterSelectedIds.Add(lRosterOrderedIds[lIndex]);
                }
            }

            return;
        }

        if (lToggle)
        {
            if (!lRosterSelectedIds.Add(lId))
            {
                lRosterSelectedIds.Remove(lId);
            }

            lRosterCurrentId = lId;
            return;
        }

        lRosterSelectedIds.Clear();
        lRosterSelectedIds.Add(lId);
        lRosterCurrentId = lId;
    }

    public void LRosterCardSelect(Guid lBatchId)
    {
        lRosterCardId = lBatchId;
        lRosterSelectedIds.Clear();
        lRosterCurrentId = Guid.Empty;
    }
}
