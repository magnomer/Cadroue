using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The log's rows: every row read or appended, capped at the row maximum, and the ones the category filter shows.
public sealed class QLogFeed
{
    private const int QLogRowMaximum = 5000;
    private readonly List<QLogRow> qLogRowsAll = [];
    private readonly List<QLogRow> qLogRowsShown = [];
    private readonly HashSet<LTraceKind> qLogCategories = [];

    public event Action? QLogRowsRebuilt;
    public event Action<QLogRow>? QLogRowAppended;
    public event Action<int>? QLogRowsRemoved;

    public IReadOnlyList<QLogRow> QLogRowsShown => qLogRowsShown;

    internal void QLogRowsReset() => qLogRowsAll.Clear();

    internal static IEnumerable<QLogRow> QLogRowsParse(string lText) =>
        LTraceText.LTraceEntryParse(lText).Select(lEntry => new QLogRow(lEntry));

    internal void QLogRowsAdd(IEnumerable<QLogRow> lRows) => qLogRowsAll.AddRange(lRows);

    // The entries the snapshot already settled are skipped.
    internal void QLogBatchAppend(List<(long QLogSequence, LTraceEntry QLogEntry)> lBatch, Func<long, bool> lSettled)
    {
        foreach ((long lSequence, LTraceEntry lEntry) in lBatch)
        {
            if (lSettled(lSequence))
            {
                continue;
            }

            var lRow = new QLogRow(lEntry);
            qLogRowsAll.Add(lRow);
            if (QLogCategoryCheck(lRow))
            {
                qLogRowsShown.Add(lRow);
                QLogRowAppended?.Invoke(lRow);
            }
        }

        if (qLogRowsAll.Count > QLogRowMaximum)
        {
            QLogExcessRemove();
        }
    }

    internal static LTraceKind[] QLogKindsParse(IReadOnlyList<string> lTokens) =>
        lTokens.Select(LTraceText.LTraceKindFind).ToArray();

    internal void QLogFilterSet(LTraceKind[] lKinds)
    {
        qLogCategories.Clear();
        foreach (LTraceKind lKind in lKinds)
        {
            qLogCategories.Add(lKind);
        }
    }

    private bool QLogCategoryCheck(QLogRow lRow) =>
        qLogCategories.Count == 0 || qLogCategories.Contains(lRow.QLogRowCategory);

    internal void QLogRowsApply()
    {
        qLogRowsShown.Clear();
        qLogRowsShown.AddRange(qLogRowsAll.Where(QLogCategoryCheck));
        QLogRowsRebuilt?.Invoke();
    }

    internal void QLogExcessRemove()
    {
        int lExcess = qLogRowsAll.Count - QLogRowMaximum;
        if (lExcess <= 0)
        {
            return;
        }

        var lRemoved = new HashSet<QLogRow>(qLogRowsAll.Take(lExcess));
        qLogRowsAll.RemoveRange(0, lExcess);
        int lShownExcess = qLogRowsShown.TakeWhile(lRemoved.Contains).Count();
        qLogRowsShown.RemoveRange(0, lShownExcess);
        QLogRowsRemoved?.Invoke(lShownExcess);
    }
}
