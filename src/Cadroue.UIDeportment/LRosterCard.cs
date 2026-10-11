using System.Globalization;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record LRosterFile(
    string LRosterFileTitle,
    string LRosterFileShade,
    IReadOnlyList<LRosterRow> LRosterFileRows)
{
    public bool LRosterFileMatch(LRosterFile lOther) =>
        LRosterFileTitle == lOther.LRosterFileTitle
        && LRosterFileShade == lOther.LRosterFileShade
        && LRosterFileRows.SequenceEqual(lOther.LRosterFileRows);
}

public sealed record LRosterCardStep(int LRosterStepIndex, LRosterCard? LRosterStepCard);

public sealed record LRosterEntry(
    LRosterCard LRosterEntryCard,
    string LRosterEntryKind,
    string LRosterEntryFrame,
    LRosterFile? LRosterEntryFile,
    LRosterRow? LRosterEntryRow)
{
    public const string LRosterKindHead = "Head";
    public const string LRosterKindFile = "File";
    public const string LRosterKindRow = "Row";
    public const string LRosterFrameWhole = "Whole";
    public const string LRosterFrameTop = "Top";
    public const string LRosterFrameMiddle = "Middle";
    public const string LRosterFrameBottom = "Bottom";

    public static IReadOnlyList<LRosterEntry> LRosterEntriesCreate(LRosterCard lCard)
    {
        LRosterEntry[] lBody = lCard.LRosterCardCollapsed
            ? []
            : lCard.LRosterCardFiles
                .SelectMany(lFile => lFile.LRosterFileRows
                    .Select(lRow => new LRosterEntry(lCard, LRosterKindRow, LRosterFrameMiddle, lFile, lRow))
                    .Prepend(new LRosterEntry(lCard, LRosterKindFile, LRosterFrameMiddle, lFile, null)))
                .ToArray();
        var lHead = new LRosterEntry(
            lCard, LRosterKindHead, lBody.Length == 0 ? LRosterFrameWhole : LRosterFrameTop, null, null);
        return lBody.Length == 0
            ? [lHead]
            : [lHead, .. lBody[..^1], lBody[^1] with { LRosterEntryFrame = LRosterFrameBottom }];
    }
}

public sealed record LRosterCard
{
    public const string LRosterCardSelected = "Select";
    public const string LRosterCardDone = "Done";
    public const string LRosterCardPlain = "Plain";

    public required Guid LRosterCardBatch { get; init; }

    public required string LRosterCardTitle { get; init; }

    public required string LRosterCardKey { get; init; }

    public required string LRosterCardBody { get; init; }

    public required bool LRosterCardCollapsed { get; init; }

    public required string LRosterCardTooltip { get; init; }

    public required IReadOnlyList<LRosterFile> LRosterCardFiles { get; init; }

    public bool LRosterCardMatch(LRosterCard lOther) =>
        LRosterCardBatch == lOther.LRosterCardBatch
        && LRosterCardTitle == lOther.LRosterCardTitle
        && LRosterCardKey == lOther.LRosterCardKey
        && LRosterCardBody == lOther.LRosterCardBody
        && LRosterCardCollapsed == lOther.LRosterCardCollapsed
        && LRosterCardTooltip == lOther.LRosterCardTooltip
        && LRosterCardFiles.Count == lOther.LRosterCardFiles.Count
        && LRosterCardFiles.Zip(lOther.LRosterCardFiles).All(lPair => lPair.First.LRosterFileMatch(lPair.Second));

    public static LRosterCard[] LRosterReuseResolve(
        IReadOnlyList<LRosterCard> lPrevious, IEnumerable<LRosterCard> lNext)
    {
        Dictionary<Guid, LRosterCard> lKept = lPrevious.ToDictionary(lCard => lCard.LRosterCardBatch);
        return lNext
            .Select(lCard => lKept.GetValueOrDefault(lCard.LRosterCardBatch) is { } lOld && lOld.LRosterCardMatch(lCard)
                ? lOld
                : lCard)
            .ToArray();
    }

    public static IReadOnlyList<LRosterCardStep> LRosterStepsResolve(
        IReadOnlyList<LRosterCard> lPrevious,
        IReadOnlyList<LRosterCard> lNext)
    {
        var lSteps = new List<LRosterCardStep>();
        var lKept = new HashSet<LRosterCard>(lNext, ReferenceEqualityComparer.Instance);
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
                lSteps.Add(new LRosterCardStep(lNew, null));
                lOld++;
            }
            else
            {
                lSteps.Add(new LRosterCardStep(lNew, lNext[lNew]));
                lNew++;
            }
        }

        lSteps.AddRange(Enumerable.Range(lOld, lPrevious.Count - lOld).Select(_ => new LRosterCardStep(lNew, null)));
        return lSteps;
    }

    public static LRosterCard LRosterCardCreate(
        CUsher lUsher,
        Guid lBatchId,
        IReadOnlyList<LLineageEntry> lLineages,
        IReadOnlyList<LWorkItem> lItems,
        bool lSelected,
        bool lCompleted,
        bool lCollapsed,
        Func<LWorkItem, bool> lOwnerCheck,
        Func<Guid, bool> lSelectedCheck)
    {
        HashSet<Guid> lStageIds = LLineage.LLineageStageRead(lUsher, lItems);
        return new LRosterCard
        {
            LRosterCardBatch = lBatchId,
            LRosterCardTitle = LRosterTitleFormat(lUsher, lItems),
            LRosterCardKey = lSelected ? LRosterCardSelected : lCompleted ? LRosterCardDone : LRosterCardPlain,
            LRosterCardBody = lCompleted ? LRosterCardDone : lSelected ? LRosterCardSelected : LRosterCardPlain,
            LRosterCardCollapsed = lCollapsed,
            LRosterCardTooltip = LLocalization.LLocalizationTextRead(
                lCollapsed ? "Roster.Card.Expand" : "Roster.Card.Collapse"),
            LRosterCardFiles = lLineages
                .Select(lLineage => LRosterFileCreate(
                    lUsher, lLineage, lStageIds, lSelected, lOwnerCheck, lSelectedCheck))
                .ToArray()
        };
    }

    public static string LRosterTitleFormat(CUsher lUsher, IReadOnlyList<LWorkItem> lBatchItems)
    {
        string lTitle = lBatchItems.Min(lWorkItem => lWorkItem.LWorkCreateTime).LocalDateTime.ToString(
            "yyyy-MM-dd tt h:mm", CultureInfo.CurrentUICulture);
        int lInitialCount = LLineage.LLineageInitialRead(lUsher, lBatchItems);
        return lInitialCount == 1
            ? LLocalization.LLocalizationFormat("Roster.Card.One", lTitle)
            : LLocalization.LLocalizationFormat("Roster.Card.Many", lTitle, lInitialCount);
    }

    private static LRosterFile LRosterFileCreate(
        CUsher lUsher,
        LLineageEntry lLineage,
        HashSet<Guid> lStageIds,
        bool lCardSelected,
        Func<LWorkItem, bool> lOwnerCheck,
        Func<Guid, bool> lSelectedCheck)
    {
        IReadOnlyList<LWorkItem> lItems = lLineage.LLineageEntryItems;
        bool lStage = lItems.Count > 0 && lStageIds.Contains(lItems[^1].LWorkId);
        return new LRosterFile(
            LLineage.LLineageTitleFormat(lUsher, lLineage),
            LRosterRow.LRosterShadeResolve(false, lCardSelected, lStage),
            lItems.Select((lWorkItem, lIndex) => LRosterRow.LRosterRowCreate(
                lUsher,
                lWorkItem,
                lLineage,
                lIndex == lItems.Count - 1,
                lStageIds.Contains(lWorkItem.LWorkId),
                lOwnerCheck(lWorkItem),
                LRosterRow.LRosterShadeResolve(
                    lSelectedCheck(lWorkItem.LWorkId),
                    lCardSelected,
                    lStageIds.Contains(lWorkItem.LWorkId)))).ToArray());
    }
}
