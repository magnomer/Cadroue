using Cadroue.Application;
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

public sealed class QListFace
{
    private readonly QList qList;
    private readonly CUsher qListUsher;

    public QListFace(QList lOwner, CUsher lUsher)
    {
        qList = lOwner;
        qListUsher = lUsher;
    }

    public IReadOnlyList<QListCard> QListCardsRead()
    {
        var lCards = new List<QListCard>();
        var lShown = new HashSet<Guid>();
        IReadOnlyList<LDocketEntry> lEntries = qList.QListDocket.LDocketItemsRead();
        foreach (LDocketEntry lItem in lEntries)
        {
            if (!lItem.LDocketEntryLocked)
            {
                lCards.Add(new QListCard(false, string.Empty, [QListRowCreate(lItem, true)]));
                continue;
            }

            if (!lShown.Add(lItem.LDocketEntryBatch))
            {
                continue;
            }

            LDocketEntry[] lGroup = lEntries
                .Where(lCandidate => lCandidate.LDocketEntryLocked
                    && lCandidate.LDocketEntryBatch == lItem.LDocketEntryBatch)
                .ToArray();
            QListRow[] lRows = lGroup
                .Select((lEntry, lIndex) => QListRowCreate(lEntry, lIndex < lGroup.Length - 1))
                .ToArray();
            string lTitle = lGroup.Length == 1
                ? QLocalizationCatalog.QLocalizationTextRead("List.Locked.SummaryOne")
                : QLocalizationCatalog.QLocalizationFormat("List.Locked.SummaryMany", lGroup.Length);
            lCards.Add(new QListCard(true, lTitle, lRows));
        }

        return lCards;
    }

    public string QListStateRead(string lListPath)
    {
        bool lLocked = qList.QListDocket.LDocketLockCheck(lListPath);
        bool lSelected = qList.QListSelectionCheck(lListPath);
        return (lLocked, lSelected) switch
        {
            (true, true) => "LockedSelected",
            (true, false) => "Locked",
            (false, true) => "Selected",
            _ => "Plain"
        };
    }

    private QListRow QListRowCreate(LDocketEntry lItem, bool lLast)
    {
        string lPath = lItem.LDocketEntryPath;
        string lTip = lItem.LDocketEntryLocked
            ? $"{lPath}\n{QLocalizationCatalog.QLocalizationTextRead("List.Locked.Tooltip")}"
            : lPath;
        return new QListRow(
            lPath, qListUsher.CUsherNameRead(lPath), lTip, QListStateRead(lPath), lItem.LDocketEntryLocked, lLast);
    }
}
