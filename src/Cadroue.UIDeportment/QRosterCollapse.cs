namespace Cadroue.UIDeportment;

public sealed class QRosterCollapse
{
    private readonly Action<Guid> qRosterCard;
    private readonly HashSet<Guid> qRosterCollapsedIds = [];

    public QRosterCollapse(Action<Guid> lCardUpdate)
    {
        qRosterCard = lCardUpdate;
    }

    public bool QRosterCollapsedCheck(Guid lBatchId) => qRosterCollapsedIds.Contains(lBatchId);

    public void QRosterCollapseSet(Guid lBatchId, bool lCollapsed)
    {
        if (lCollapsed)
        {
            qRosterCollapsedIds.Add(lBatchId);
        }
        else
        {
            qRosterCollapsedIds.Remove(lBatchId);
        }
    }

    public bool QRosterCollapseToggle(Guid lBatchId)
    {
        bool lCollapsed = !qRosterCollapsedIds.Contains(lBatchId);
        QRosterCollapseSet(lBatchId, lCollapsed);
        qRosterCard(lBatchId);
        return lCollapsed;
    }

    public void QRosterStaleRemove(IReadOnlyCollection<Guid> lBatchIds) =>
        qRosterCollapsedIds.RemoveWhere(lBatchId => !lBatchIds.Contains(lBatchId));

    public void QRosterCompletedApply(Guid lBatchId, bool lCompleted, bool lCollapseDone)
    {
        if (lCollapseDone)
        {
            QRosterCollapseSet(lBatchId, lCompleted);
        }
    }
}
