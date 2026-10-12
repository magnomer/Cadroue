using System.Globalization;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record QRosterFile(
    string QRosterFileTitle,
    string QRosterFileShade,
    IReadOnlyList<QRosterRow> QRosterFileRows)
{
    public bool QRosterFileMatch(QRosterFile lOther) =>
        QRosterFileTitle == lOther.QRosterFileTitle
        && QRosterFileShade == lOther.QRosterFileShade
        && QRosterFileRows.SequenceEqual(lOther.QRosterFileRows);
}

public sealed record QRosterCardStep(int QRosterStepIndex, QRosterCard? QRosterStepCard);

public sealed record QRosterEntry(
    QRosterCard QRosterEntryCard,
    string QRosterEntryKind,
    string QRosterEntryFrame,
    QRosterFile? QRosterEntryFile,
    QRosterRow? QRosterEntryRow)
{
    public const string QRosterKindHead = "Head";
    public const string QRosterKindFile = "File";
    public const string QRosterKindRow = "Row";
    public const string QRosterFrameWhole = "Whole";
    public const string QRosterFrameTop = "Top";
    public const string QRosterFrameMiddle = "Middle";
    public const string QRosterFrameBottom = "Bottom";

    public static IReadOnlyList<QRosterEntry> QRosterEntriesCreate(QRosterCard lCard)
    {
        QRosterEntry[] lBody = lCard.QRosterCardCollapsed
            ? []
            : lCard.QRosterCardFiles
                .SelectMany(lFile => lFile.QRosterFileRows
                    .Select(lRow => new QRosterEntry(lCard, QRosterKindRow, QRosterFrameMiddle, lFile, lRow))
                    .Prepend(new QRosterEntry(lCard, QRosterKindFile, QRosterFrameMiddle, lFile, null)))
                .ToArray();
        var lHead = new QRosterEntry(
            lCard, QRosterKindHead, lBody.Length == 0 ? QRosterFrameWhole : QRosterFrameTop, null, null);
        return lBody.Length == 0
            ? [lHead]
            : [lHead, .. lBody[..^1], lBody[^1] with { QRosterEntryFrame = QRosterFrameBottom }];
    }
}

public sealed record QRosterCard
{
    public const string QRosterCardSelected = "Select";
    public const string QRosterCardDone = "Done";
    public const string QRosterCardPlain = "Plain";

    public required Guid QRosterCardBatch { get; init; }

    public required string QRosterCardTitle { get; init; }

    public required string QRosterCardKey { get; init; }

    public required string QRosterCardBody { get; init; }

    public required bool QRosterCardCollapsed { get; init; }

    public required string QRosterCardTooltip { get; init; }

    public required IReadOnlyList<QRosterFile> QRosterCardFiles { get; init; }

    public bool QRosterCardMatch(QRosterCard lOther) =>
        QRosterCardBatch == lOther.QRosterCardBatch
        && QRosterCardTitle == lOther.QRosterCardTitle
        && QRosterCardKey == lOther.QRosterCardKey
        && QRosterCardBody == lOther.QRosterCardBody
        && QRosterCardCollapsed == lOther.QRosterCardCollapsed
        && QRosterCardTooltip == lOther.QRosterCardTooltip
        && QRosterCardFiles.Count == lOther.QRosterCardFiles.Count
        && QRosterCardFiles.Zip(lOther.QRosterCardFiles).All(lPair => lPair.First.QRosterFileMatch(lPair.Second));

    public static QRosterCard[] QRosterReuseResolve(
        IReadOnlyList<QRosterCard> lPrevious, IEnumerable<QRosterCard> lNext)
    {
        Dictionary<Guid, QRosterCard> lKept = lPrevious.ToDictionary(lCard => lCard.QRosterCardBatch);
        return lNext
            .Select(lCard => lKept.GetValueOrDefault(lCard.QRosterCardBatch) is { } lOld && lOld.QRosterCardMatch(lCard)
                ? lOld
                : lCard)
            .ToArray();
    }

    public static IReadOnlyList<QRosterCardStep> QRosterStepsResolve(
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

    public static QRosterCard QRosterCardCreate(
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

        return new QRosterCard
        {
            QRosterCardBatch = lBatchId,
            QRosterCardTitle = QRosterTitleFormat(lRecord.CLineageBatchEarliest, lRecord.CLineageBatchInitial),
            QRosterCardKey = lSelected ? QRosterCardSelected : lCompleted ? QRosterCardDone : QRosterCardPlain,
            QRosterCardBody = lCompleted ? QRosterCardDone : lSelected ? QRosterCardSelected : QRosterCardPlain,
            QRosterCardCollapsed = lCollapsed,
            QRosterCardTooltip = LLocalization.LLocalizationTextRead(
                lCollapsed ? "Roster.Card.Expand" : "Roster.Card.Collapse"),
            QRosterCardFiles = lAnswer.CLineageEntries
                .Where(lEntry => lEntry.CLineageEntryBatch == lBatchId)
                .Select(lEntry => QRosterFileCreate(
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

    public static string QRosterTitleFormat(DateTimeOffset lEarliest, int lInitialCount)
    {
        string lTitle = lEarliest.LocalDateTime.ToString(
            "yyyy-MM-dd tt h:mm", CultureInfo.CurrentUICulture);
        return lInitialCount == 1
            ? LLocalization.LLocalizationFormat("Roster.Card.One", lTitle)
            : LLocalization.LLocalizationFormat("Roster.Card.Many", lTitle, lInitialCount);
    }

    private static QRosterFile QRosterFileCreate(
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
        return new QRosterFile(
            QLineage.QLineageTitleFormat(lEntry),
            QRosterRow.QRosterShadeResolve(false, lCardSelected, lStage),
            lIds.Select((lId, lIndex) => QRosterRow.QRosterRowCreate(
                lLineage,
                lLookup[lId],
                lSteps[lId],
                lIndex == lIds.Count - 1,
                lStageIds.Contains(lId),
                lOwnerCheck(lLookup[lId]),
                QRosterRow.QRosterShadeResolve(
                    lSelectedCheck(lId),
                    lCardSelected,
                    lStageIds.Contains(lId)))).ToArray());
    }
}
