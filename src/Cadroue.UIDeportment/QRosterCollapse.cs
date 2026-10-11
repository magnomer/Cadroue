namespace Cadroue.UIDeportment;

public sealed class QRosterCollapse
{
    private readonly Action<Guid> lRosterCard;
    private readonly HashSet<Guid> lRosterCollapsedIds = [];

    public QRosterCollapse(Action<Guid> lCardUpdate)
    {
        lRosterCard = lCardUpdate;
    }

    public bool LRosterCollapsedCheck(Guid lBatchId) => lRosterCollapsedIds.Contains(lBatchId);

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
        lRosterCard(lBatchId);
        return lCollapsed;
    }

    public void LRosterStaleRemove(IReadOnlyCollection<Guid> lBatchIds) =>
        lRosterCollapsedIds.RemoveWhere(lBatchId => !lBatchIds.Contains(lBatchId));
}
