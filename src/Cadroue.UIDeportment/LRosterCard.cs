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
        CLineage lLineage,
        Guid lBatchId,
        CLineageAnswer lAnswer,
        IReadOnlyList<LWorkItem> lItems,
        bool lSelected,
        bool lCompleted,
        bool lCollapsed,
        Func<LWorkItem, bool> lOwnerCheck,
        Func<Guid, bool> lSelectedCheck)
    {
        CLineageBatch lRecord = lAnswer.CLineageBatches.First(lBatch => lBatch.CLineageBatchId == lBatchId);
        var lLookup = new Dictionary<Guid, LWorkItem>();
        foreach (LWorkItem lWorkItem in lItems)
        {
            lLookup.TryAdd(lWorkItem.LWorkId, lWorkItem);
        }

        var lSteps = new Dictionary<Guid, CLineageStep>();
        foreach (CLineageStep lStep in lAnswer.CLineageSteps)
        {
            lSteps.TryAdd(lStep.CLineageStepId, lStep);
        }

        return new LRosterCard
        {
            LRosterCardBatch = lBatchId,
            LRosterCardTitle = LRosterTitleFormat(lRecord.CLineageBatchEarliest, lRecord.CLineageBatchInitial),
            LRosterCardKey = lSelected ? LRosterCardSelected : lCompleted ? LRosterCardDone : LRosterCardPlain,
            LRosterCardBody = lCompleted ? LRosterCardDone : lSelected ? LRosterCardSelected : LRosterCardPlain,
            LRosterCardCollapsed = lCollapsed,
            LRosterCardTooltip = LLocalization.LLocalizationTextRead(
                lCollapsed ? "Roster.Card.Expand" : "Roster.Card.Collapse"),
            LRosterCardFiles = lAnswer.CLineageEntries
                .Where(lEntry => lEntry.CLineageEntryBatch == lBatchId)
                .Select(lEntry => LRosterFileCreate(
                    lLineage,
                    lEntry,
                    lLookup,
                    lSteps,
                    lRecord.CLineageBatchStages,
                    lSelected,
                    lOwnerCheck,
                    lSelectedCheck))
                .ToArray()
        };
    }

    public static string LRosterTitleFormat(DateTimeOffset lEarliest, int lInitialCount)
    {
        string lTitle = lEarliest.LocalDateTime.ToString(
            "yyyy-MM-dd tt h:mm", CultureInfo.CurrentUICulture);
        return lInitialCount == 1
            ? LLocalization.LLocalizationFormat("Roster.Card.One", lTitle)
            : LLocalization.LLocalizationFormat("Roster.Card.Many", lTitle, lInitialCount);
    }

    private static LRosterFile LRosterFileCreate(
        CLineage lLineage,
        CLineageEntry lEntry,
        IReadOnlyDictionary<Guid, LWorkItem> lLookup,
        IReadOnlyDictionary<Guid, CLineageStep> lSteps,
        IReadOnlySet<Guid> lStageIds,
        bool lCardSelected,
        Func<LWorkItem, bool> lOwnerCheck,
        Func<Guid, bool> lSelectedCheck)
    {
        IReadOnlyList<Guid> lIds = lEntry.CLineageEntryIds;
        bool lStage = lIds.Count > 0 && lStageIds.Contains(lIds[^1]);
        return new LRosterFile(
            LLineage.LLineageTitleFormat(lEntry),
            LRosterRow.LRosterShadeResolve(false, lCardSelected, lStage),
            lIds.Select((lId, lIndex) => LRosterRow.LRosterRowCreate(
                lLineage,
                lLookup[lId],
                lSteps[lId],
                lIndex == lIds.Count - 1,
                lStageIds.Contains(lId),
                lOwnerCheck(lLookup[lId]),
                LRosterRow.LRosterShadeResolve(
                    lSelectedCheck(lId),
                    lCardSelected,
                    lStageIds.Contains(lId)))).ToArray());
    }
}
