using System.Globalization;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record LRosterFile(
    string LRosterFileTitle,
    string LRosterFileShade,
    IReadOnlyList<LRosterRow> LRosterFileRows);

public sealed class LRosterCard
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

    public static LRosterCard LRosterCardCreate(
        Guid lBatchId,
        IReadOnlyList<LLineageEntry> lLineages,
        IReadOnlyList<LWorkItem> lItems,
        bool lSelected,
        bool lCompleted,
        bool lCollapsed,
        Func<LWorkItem, bool> lOwnerCheck,
        Func<Guid, bool> lSelectedCheck)
    {
        HashSet<Guid> lStageIds = LLineage.LLineageStageRead(lItems);
        return new LRosterCard
        {
            LRosterCardBatch = lBatchId,
            LRosterCardTitle = LRosterTitleFormat(lItems),
            LRosterCardKey = lSelected ? LRosterCardSelected : lCompleted ? LRosterCardDone : LRosterCardPlain,
            LRosterCardBody = lCompleted ? LRosterCardDone : lSelected ? LRosterCardSelected : LRosterCardPlain,
            LRosterCardCollapsed = lCollapsed,
            LRosterCardTooltip = LLocalization.LLocalizationTextRead(
                lCollapsed ? "Roster.Card.Expand" : "Roster.Card.Collapse"),
            LRosterCardFiles = lLineages
                .Select(lLineage => LRosterFileCreate(lLineage, lStageIds, lSelected, lOwnerCheck, lSelectedCheck))
                .ToArray()
        };
    }

    public static string LRosterTitleFormat(IReadOnlyList<LWorkItem> lBatchItems)
    {
        string lTitle = lBatchItems.Min(lWorkItem => lWorkItem.LWorkCreateTime).LocalDateTime.ToString(
            "yyyy-MM-dd tt h:mm", CultureInfo.CurrentUICulture);
        int lInitialCount = LLineage.LLineageInitialRead(lBatchItems);
        return lInitialCount == 1
            ? LLocalization.LLocalizationFormat("Roster.Card.One", lTitle)
            : LLocalization.LLocalizationFormat("Roster.Card.Many", lTitle, lInitialCount);
    }

    private static LRosterFile LRosterFileCreate(
        LLineageEntry lLineage,
        HashSet<Guid> lStageIds,
        bool lCardSelected,
        Func<LWorkItem, bool> lOwnerCheck,
        Func<Guid, bool> lSelectedCheck)
    {
        IReadOnlyList<LWorkItem> lItems = lLineage.LLineageEntryItems;
        bool lStage = lItems.Count > 0 && lStageIds.Contains(lItems[^1].LWorkId);
        return new LRosterFile(
            LLineage.LLineageTitleFormat(lLineage),
            LRosterRow.LRosterShadeResolve(false, lCardSelected, lStage),
            lItems.Select((lWorkItem, lIndex) => LRosterRow.LRosterRowCreate(
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
