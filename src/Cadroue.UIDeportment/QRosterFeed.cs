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
        QRosterStepsApply(lNext);
        QRosterCardsSet(lNext);
    }

    public void QRosterCardSet(int lIndex, QRosterCard lCard)
    {
        QRosterSlotSet(lIndex, lCard);
        QRosterEntriesRemove(lIndex);
        QRosterEntriesInsert(lIndex, lCard);
    }

    private void QRosterStepsApply(QRosterCard[] lNext) =>
        QRosterCard.QRosterStepsResolve(qRosterCards.ToArray(), lNext).ToList().ForEach(QRosterEntriesApply);

    private void QRosterCardsSet(QRosterCard[] lNext)
    {
        qRosterCards.Clear();
        qRosterCards.AddRange(lNext);
    }

    private void QRosterSlotSet(int lIndex, QRosterCard lCard) => qRosterCards[lIndex] = lCard;

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
        int lOffset = QRosterOffsetRead(lCardIndex);
        IReadOnlyList<QRosterEntry> lEntries = QRosterEntry.QRosterEntriesCreate(lCard);
        QRosterRowsInsert(lCardIndex, lOffset, lEntries);
    }

    private void QRosterEntriesRemove(int lCardIndex)
    {
        int lOffset = QRosterOffsetRead(lCardIndex);
        QRosterRowsRemove(lCardIndex, lOffset);
    }

    private int QRosterOffsetRead(int lCardIndex) => qRosterEntryCounts.Take(lCardIndex).Sum();

    private void QRosterRowsInsert(int lCardIndex, int lOffset, IReadOnlyList<QRosterEntry> lEntries)
    {
        for (int lIndex = 0; lIndex < lEntries.Count; lIndex++)
        {
            qRosterEntries.Insert(lOffset + lIndex, lEntries[lIndex]);
        }

        qRosterEntryCounts.Insert(lCardIndex, lEntries.Count);
    }

    private void QRosterRowsRemove(int lCardIndex, int lOffset)
    {
        for (int lIndex = 0; lIndex < qRosterEntryCounts[lCardIndex]; lIndex++)
        {
            qRosterEntries.RemoveAt(lOffset);
        }

        qRosterEntryCounts.RemoveAt(lCardIndex);
    }
}
