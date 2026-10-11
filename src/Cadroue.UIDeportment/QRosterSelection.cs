namespace Cadroue.UIDeportment;

public sealed class QRosterSelection
{
    private readonly Func<Guid, bool> lRosterItem;
    private readonly Func<Guid, bool> lRosterBatch;
    private readonly Func<IReadOnlyList<Guid>> lRosterOrder;
    private readonly Action lRosterSelection;
    private QRosterPick lRosterPick = new(Guid.Empty, Guid.Empty, new HashSet<Guid>());

    public QRosterSelection(
        Func<Guid, bool> lItemCheck,
        Func<Guid, bool> lBatchCheck,
        Func<IReadOnlyList<Guid>> lOrderRead,
        Action lSelectRaise)
    {
        lRosterItem = lItemCheck;
        lRosterBatch = lBatchCheck;
        lRosterOrder = lOrderRead;
        lRosterSelection = lSelectRaise;
    }

    public Guid LRosterCurrentId =>
        lRosterItem(lRosterPick.LRosterPickCurrent) ? lRosterPick.LRosterPickCurrent : Guid.Empty;

    public Guid LRosterCardId => lRosterBatch(lRosterPick.LRosterPickCard) ? lRosterPick.LRosterPickCard : Guid.Empty;

    public IReadOnlyCollection<Guid> LRosterSelectedIds => lRosterPick.LRosterPickIds.Where(lRosterItem).ToArray();

    public bool LRosterSelectedCheck(Guid lId) => lRosterPick.LRosterPickIds.Contains(lId) && lRosterItem(lId);

    public bool LRosterCardCheck(Guid lBatchId) => lBatchId == LRosterCardId;

    public void LRosterStepSelect(Guid lId, bool lRange, bool lToggle)
    {
        var lPresent = new QRosterPick(
            LRosterCurrentId, Guid.Empty, lRosterPick.LRosterPickIds.Where(lRosterItem).ToHashSet());
        lRosterPick = LRosterPickResolve(lPresent, lRosterOrder(), lId, lRange, lToggle);
        lRosterSelection();
    }

    private static QRosterPick LRosterPickResolve(
        QRosterPick lPresent, IReadOnlyList<Guid> lOrder, Guid lId, bool lRange, bool lToggle)
    {
        Guid lCurrent = lPresent.LRosterPickCurrent;
        HashSet<Guid> lIds = lPresent.LRosterPickIds.ToHashSet();
        if (lRange && lCurrent != Guid.Empty)
        {
            List<Guid> lOrdered = lOrder.ToList();
            int lAnchor = lOrdered.IndexOf(lCurrent);
            int lTarget = lOrdered.IndexOf(lId);
            if (lAnchor >= 0 && lTarget >= 0)
            {
                lIds.Clear();
                for (int lIndex = Math.Min(lAnchor, lTarget); lIndex <= Math.Max(lAnchor, lTarget); lIndex++)
                {
                    lIds.Add(lOrdered[lIndex]);
                }
            }
        }
        else if (lToggle)
        {
            if (!lIds.Add(lId))
            {
                lIds.Remove(lId);
            }

            lCurrent = lId;
        }
        else
        {
            lIds = [lId];
            lCurrent = lId;
        }

        return new QRosterPick(lCurrent, Guid.Empty, lIds);
    }

    public void LRosterCardSelect(Guid lBatchId)
    {
        lRosterPick = new QRosterPick(Guid.Empty, lBatchId, new HashSet<Guid>());
        lRosterSelection();
    }
}
