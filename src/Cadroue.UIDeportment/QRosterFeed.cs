namespace Cadroue.UIDeportment;

public sealed class QRosterFeed
{
    private readonly List<QRosterCard> qRosterCards = [];
    private readonly IList<QRosterEntry> qRosterEntries;
    private readonly List<int> qRosterEntryCounts = [];

    public QRosterFeed()
    {
        System.Collections.ObjectModel.ObservableCollection<QRosterEntry> lEntries = [];
        qRosterEntries = lEntries;
        QRosterEntries = lEntries;
    }

    public IReadOnlyList<QRosterEntry> QRosterEntries { get; }

    public void QRosterFeedApply(IEnumerable<QRosterCard> lCards)
    {
        QRosterCard[] lNext = QRosterCard.QRosterReuseResolve(qRosterCards, lCards);
        QRosterCard.QRosterStepsResolve(qRosterCards.ToArray(), lNext).ToList().ForEach(QRosterEntriesApply);
        qRosterCards.Clear();
        qRosterCards.AddRange(lNext);
    }

    public void QRosterCardSet(int lIndex, QRosterCard lCard)
    {
        qRosterCards[lIndex] = lCard;
        QRosterEntriesRemove(lIndex);
        QRosterEntriesInsert(lIndex, qRosterCards[lIndex]);
    }

    private void QRosterEntriesApply(QRosterCardStep lStep)
    {
        if (lStep.QRosterStepCard is { } lCard)
        {
            QRosterEntriesInsert(lStep.QRosterStepIndex, lCard);
            return;
        }

        QRosterEntriesRemove(lStep.QRosterStepIndex);
    }

    private void QRosterEntriesInsert(int lCardIndex, QRosterCard lCard)
    {
        int lOffset = qRosterEntryCounts.Take(lCardIndex).Sum();
        IReadOnlyList<QRosterEntry> lEntries = QRosterEntry.QRosterEntriesCreate(lCard);
        for (int lIndex = 0; lIndex < lEntries.Count; lIndex++)
        {
            qRosterEntries.Insert(lOffset + lIndex, lEntries[lIndex]);
        }

        qRosterEntryCounts.Insert(lCardIndex, lEntries.Count);
    }

    private void QRosterEntriesRemove(int lCardIndex)
    {
        int lOffset = qRosterEntryCounts.Take(lCardIndex).Sum();
        for (int lIndex = 0; lIndex < qRosterEntryCounts[lCardIndex]; lIndex++)
        {
            qRosterEntries.RemoveAt(lOffset);
        }

        qRosterEntryCounts.RemoveAt(lCardIndex);
    }
}
