namespace Cadroue.UIDeportment;

// The roster's entry list kept in step with its cards: an unchanged card keeps its entries, the rest are inserted
// or removed in place.
public sealed class QRosterFeed
{
    private sealed record QRosterCardStep(int QRosterStepIndex, QRosterCard? QRosterStepCard);

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
        QRosterCard[] lNext = QRosterReuseResolve(qRosterCards, lCards);
        QRosterStepsApply(lNext);
        QRosterCardsSet(lNext);
    }

    public void QRosterCardSet(int lIndex, QRosterCard lCard)
    {
        QRosterSlotSet(lIndex, lCard);
        QRosterEntriesRemove(lIndex);
        QRosterEntriesInsert(lIndex, lCard);
    }

    private static QRosterCard[] QRosterReuseResolve(
        IReadOnlyList<QRosterCard> lPrevious, IEnumerable<QRosterCard> lNext)
    {
        Dictionary<Guid, QRosterCard> lKept = lPrevious.ToDictionary(lCard => lCard.QRosterCardBatch);
        return lNext
            .Select(lCard => lKept.GetValueOrDefault(lCard.QRosterCardBatch) is { } lOld && lOld.QRosterCardMatch(lCard)
                ? lOld
                : lCard)
            .ToArray();
    }

    private void QRosterStepsApply(QRosterCard[] lNext) =>
        QRosterStepsResolve(qRosterCards.ToArray(), lNext).ToList().ForEach(QRosterEntriesApply);

    private static IReadOnlyList<QRosterCardStep> QRosterStepsResolve(
        IReadOnlyList<QRosterCard> lPrevious,
        IReadOnlyList<QRosterCard> lNext)
    {
        var lSteps = new List<QRosterCardStep>();
        var lKept = new HashSet<QRosterCard>(lNext, ReferenceEqualityComparer.Instance);
        int lOld = 0;
        int lNew = 0;
        while (lNew < lNext.Count)
        {
            if (lOld < lPrevious.Count && ReferenceEquals(lPrevious[lOld], lNext[lNew]))
            {
                lOld++;
                lNew++;
            }
            else if (lOld < lPrevious.Count && !lKept.Contains(lPrevious[lOld]))
            {
                lSteps.Add(new QRosterCardStep(lNew, null));
                lOld++;
            }
            else
            {
                lSteps.Add(new QRosterCardStep(lNew, lNext[lNew]));
                lNew++;
            }
        }

        lSteps.AddRange(Enumerable.Range(lOld, lPrevious.Count - lOld).Select(_ => new QRosterCardStep(lNew, null)));
        return lSteps;
    }

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
