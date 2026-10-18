using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed record QListRow(
    string QListRowPath,
    string QListRowName,
    string QListRowTip,
    string QListRowState,
    bool QListRowLocked,
    bool QListRowLast);

public sealed record QListCard(bool QListCardLocked, string QListCardTitle, IReadOnlyList<QListRow> QListCardRows);

// The list's cards and row looks: paints the list gate's cards with their wording and maps a row's marks to its look.
// Both reads serve the Veneer list (PList).
public sealed class QListFace
{
    private readonly CList qListGate;

    public QListFace(CList lGate)
    {
        qListGate = lGate;
    }

    // A row's look by whether it is locked and whether it is selected.
    private static readonly IReadOnlyDictionary<(bool, bool), string> QListLooks =
        new Dictionary<(bool, bool), string>
        {
            [(true, true)] = "LockedSelected",
            [(true, false)] = "Locked",
            [(false, true)] = "Selected",
            [(false, false)] = "Plain",
        };

    // The line a locked row's tip adds under its path.
    private static string QListTip => QLocalizationCatalog.QLocalizationTextRead("List.Locked.Tooltip");

    public IReadOnlyList<QListCard> QListCardsRead() =>
        qListGate.CListCardsRead()
            .Select(lCard => new QListCard(
                lCard.CListCardLocked,
                (lCard.CListCardLocked, lCard.CListCardRows.Count) switch
                {
                    (false, _) => string.Empty,
                    (true, 1) => QLocalizationCatalog.QLocalizationTextRead("List.Locked.SummaryOne"),
                    (true, int lCount) => QLocalizationCatalog.QLocalizationFormat("List.Locked.SummaryMany", lCount)
                },
                lCard.CListCardRows
                    .Select(lRow => new QListRow(
                        lRow.CListRowPath,
                        lRow.CListRowName,
                        lRow.CListRowLocked
                            ? $"{lRow.CListRowPath}\n{QListTip}"
                            : lRow.CListRowPath,
                        QListLooks[(lRow.CListRowLocked, lRow.CListRowSelected)],
                        lRow.CListRowLocked,
                        lRow.CListRowLast))
                    .ToArray()))
            .ToArray();

    public string QListStateRead(string lListPath)
    {
        CListRow lRow = qListGate.CListRowRead(lListPath);
        return QListLooks[(lRow.CListRowLocked, lRow.CListRowSelected)];
    }
}
