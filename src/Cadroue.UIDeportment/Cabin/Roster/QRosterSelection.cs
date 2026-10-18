namespace Cadroue.UIDeportment;

public sealed class QRosterSelection
{
    private readonly Func<Guid, bool> qRosterItem;
    private readonly Func<Guid, bool> qRosterBatch;
    private readonly Func<IReadOnlyList<Guid>> qRosterOrder;
    private readonly Action qRosterSelection;
    private QRosterPick qRosterPick = new(Guid.Empty, Guid.Empty, new HashSet<Guid>());

    public QRosterSelection(
        Func<Guid, bool> lItemCheck,
        Func<Guid, bool> lBatchCheck,
        Func<IReadOnlyList<Guid>> lOrderRead,
        Action lSelectRaise)
    {
        qRosterItem = lItemCheck;
        qRosterBatch = lBatchCheck;
        qRosterOrder = lOrderRead;
        qRosterSelection = lSelectRaise;
    }

    public Guid QRosterCurrentId =>
        qRosterItem(qRosterPick.QRosterPickCurrent) ? qRosterPick.QRosterPickCurrent : Guid.Empty;

    public Guid QRosterCardId => qRosterBatch(qRosterPick.QRosterPickCard) ? qRosterPick.QRosterPickCard : Guid.Empty;

    public IReadOnlyCollection<Guid> QRosterSelectedIds => qRosterPick.QRosterPickIds.Where(qRosterItem).ToArray();

    public bool QRosterSelectedCheck(Guid lId) => qRosterPick.QRosterPickIds.Contains(lId) && qRosterItem(lId);

    public bool QRosterCardCheck(Guid lBatchId) => lBatchId == QRosterCardId;

    public void QRosterStepSelect(Guid lId, bool lRange, bool lToggle)
    {
        QRosterPick lPresent = QRosterPresentRead();
        QRosterPick lNext = QRosterPickResolve(lPresent, qRosterOrder(), lId, lRange, lToggle);
        QRosterPickSet(lNext);
        qRosterSelection();
    }

    private QRosterPick QRosterPresentRead() =>
        new(QRosterCurrentId, Guid.Empty, qRosterPick.QRosterPickIds.Where(qRosterItem).ToHashSet());

    private void QRosterPickSet(QRosterPick lPick) => qRosterPick = lPick;

    private static QRosterPick QRosterPickResolve(
        QRosterPick lPresent, IReadOnlyList<Guid> lOrder, Guid lId, bool lRange, bool lToggle)
    {
        Guid lCurrent = lPresent.QRosterPickCurrent;
        HashSet<Guid> lIds = lPresent.QRosterPickIds.ToHashSet();
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

    public void QRosterCardSelect(Guid lBatchId)
    {
        QRosterPickSet(new QRosterPick(Guid.Empty, lBatchId, new HashSet<Guid>()));
        qRosterSelection();
    }
}
