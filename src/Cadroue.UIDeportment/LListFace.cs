using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed record LListRow(
    string LListRowPath,
    string LListRowName,
    string LListRowTip,
    string LListRowState,
    bool LListRowLocked,
    bool LListRowLast);

public sealed record LListCard(bool LListCardLocked, string LListCardTitle, IReadOnlyList<LListRow> LListCardRows);

public sealed class LListFace
{
    private readonly LList lList;
    private readonly CUsher lListUsher;

    public LListFace(LList lOwner, CUsher lUsher)
    {
        lList = lOwner;
        lListUsher = lUsher;
    }

    public IReadOnlyList<LListCard> LListCardsRead()
    {
        var lCards = new List<LListCard>();
        var lShown = new HashSet<Guid>();
        IReadOnlyList<LDocketEntry> lEntries = lList.LListDocket.LDocketItemsRead();
        foreach (LDocketEntry lItem in lEntries)
        {
            if (!lItem.LDocketEntryLocked)
            {
                lCards.Add(new LListCard(false, string.Empty, [LListRowCreate(lItem, true)]));
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
            LListRow[] lRows = lGroup
                .Select((lEntry, lIndex) => LListRowCreate(lEntry, lIndex < lGroup.Length - 1))
                .ToArray();
            string lTitle = lGroup.Length == 1
                ? LLocalization.LLocalizationTextRead("List.Locked.SummaryOne")
                : LLocalization.LLocalizationFormat("List.Locked.SummaryMany", lGroup.Length);
            lCards.Add(new LListCard(true, lTitle, lRows));
        }

        return lCards;
    }

    public string LListStateRead(string lListPath)
    {
        bool lLocked = lList.LListDocket.LDocketLockCheck(lListPath);
        bool lSelected = lList.LListSelectionCheck(lListPath);
        return (lLocked, lSelected) switch
        {
            (true, true) => "LockedSelected",
            (true, false) => "Locked",
            (false, true) => "Selected",
            _ => "Plain"
        };
    }

    private LListRow LListRowCreate(LDocketEntry lItem, bool lLast)
    {
        string lPath = lItem.LDocketEntryPath;
        string lTip = lItem.LDocketEntryLocked
            ? $"{lPath}\n{LLocalization.LLocalizationTextRead("List.Locked.Tooltip")}"
            : lPath;
        return new LListRow(
            lPath, lListUsher.CUsherNameRead(lPath), lTip, LListStateRead(lPath), lItem.LDocketEntryLocked, lLast);
    }
}
