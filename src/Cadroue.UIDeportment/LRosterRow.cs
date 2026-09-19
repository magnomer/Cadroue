using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LRosterRow
{
    public const string LRosterShadeSelected = "Select";
    public const string LRosterShadeStage = "Stage";
    public const string LRosterShadePlain = "Plain";

    public required Guid LRosterRowId { get; init; }

    public required string LRosterRowStep { get; init; }

    public required string LRosterRowPriority { get; init; }

    public required string LRosterRowLength { get; init; }

    public required string LRosterRowProgress { get; init; }

    public required string LRosterRowRatio { get; init; }

    public required string LRosterRowState { get; init; }

    public required string LRosterRowKey { get; init; }

    public required string LRosterRowOwner { get; init; }

    public required bool LRosterRowLast { get; init; }

    public required bool LRosterRowStage { get; init; }

    public required string LRosterRowShade { get; init; }

    public static LRosterRow LRosterRowCreate(
        LWorkItem lWorkItem,
        LLineageEntry lLineage,
        bool lLast,
        bool lStage,
        bool lOwned,
        string lShade) => new()
    {
        LRosterRowId = lWorkItem.LWorkId,
        LRosterRowStep = LLineage.LLineageStepFormat(lWorkItem, lLineage.LLineageEntrySubject),
        LRosterRowPriority = LRosterPriorityFormat(lWorkItem.LWorkPriority),
        LRosterRowLength = LRosterSpanFormat(lWorkItem.LWorkDuration),
        LRosterRowProgress = LRosterProgressFormat(lWorkItem),
        LRosterRowRatio = LLineage.LLineageRatioFormat(
            lWorkItem, lLineage.LLineageEntrySubject, lLineage.LLineageEntryOrigin),
        LRosterRowState = LRosterStateFormat(lWorkItem.LWorkStateCurrent),
        LRosterRowKey = LRosterKeyRead(lWorkItem.LWorkStateCurrent),
        LRosterRowOwner = LRosterOwnerFormat(lWorkItem, lOwned),
        LRosterRowLast = lLast,
        LRosterRowStage = lStage,
        LRosterRowShade = lShade
    };

    public static string LRosterShadeResolve(bool lSelected, bool lCardSelected, bool lStage)
    {
        if (lSelected)
        {
            return LRosterShadeSelected;
        }

        return !lCardSelected && lStage ? LRosterShadeStage : LRosterShadePlain;
    }

    public static string LRosterOwnerFormat(LWorkItem lWorkItem, bool lOwned)
    {
        if (lWorkItem.LWorkOwnerRunner == Guid.Empty)
        {
            return "-";
        }

        if (lOwned)
        {
            return LLocalization.LLocalizationTextRead("Roster.Owner.ThisTab");
        }

        return lWorkItem.LWorkOwnerProcess == Environment.ProcessId
            ? LLocalization.LLocalizationTextRead("Roster.Owner.OtherTab")
            : LLocalization.LLocalizationTextRead("Roster.Owner.OtherWindow");
    }

    public static string LRosterProgressFormat(LWorkItem lWorkItem) =>
        lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning || lWorkItem.LWorkProgress > 0
            ? $"{lWorkItem.LWorkProgress:P0}"
            : "-";

    public static string LRosterPriorityFormat(LWorkPriority lPriority) =>
        lPriority == LWorkPriority.LWorkPriorityHigh
            ? LLocalization.LLocalizationTextRead("Roster.Priority.High")
            : LLocalization.LLocalizationTextRead("Roster.Priority.Normal");

    public static string LRosterSpanFormat(TimeSpan lSpan) => $"{lSpan:hh\\:mm\\:ss}";

    public static string LRosterKeyRead(LWorkState lWorkState) => lWorkState switch
    {
        LWorkState.LWorkStatePending => "Pending",
        LWorkState.LWorkStateRunning => "Running",
        LWorkState.LWorkStateDone => "Done",
        LWorkState.LWorkStateFailed => "Failed",
        LWorkState.LWorkStateUnresolved => "Unresolved",
        LWorkState.LWorkStatePartial => "Partial",
        LWorkState.LWorkStateBlocked => "Blocked",
        LWorkState.LWorkStateCancelled => "Cancelled",
        _ => "Pending"
    };

    public static string LRosterStateFormat(LWorkState lWorkState) => lWorkState switch
    {
        LWorkState.LWorkStatePending => LLocalization.LLocalizationTextRead("Roster.State.Pending"),
        LWorkState.LWorkStateRunning => LLocalization.LLocalizationTextRead("Roster.State.Running"),
        LWorkState.LWorkStateDone => LLocalization.LLocalizationTextRead("Roster.State.Done"),
        LWorkState.LWorkStateFailed => LLocalization.LLocalizationTextRead("Roster.State.Failed"),
        LWorkState.LWorkStateUnresolved => LLocalization.LLocalizationTextRead("Roster.State.Unresolved"),
        LWorkState.LWorkStatePartial => LLocalization.LLocalizationTextRead("Roster.State.Partial"),
        LWorkState.LWorkStateBlocked => LLocalization.LLocalizationTextRead("Roster.State.Blocked"),
        LWorkState.LWorkStateCancelled => LLocalization.LLocalizationTextRead("Roster.State.Cancelled"),
        _ => lWorkState.ToString()
    };

    public static string LRosterPhaseFormat(LWorkState lWorkState, LWorkPhase lWorkPhase) => lWorkState switch
    {
        LWorkState.LWorkStateDone => LLocalization.LLocalizationTextRead("Roster.State.Done"),
        LWorkState.LWorkStateFailed => LLocalization.LLocalizationTextRead("Roster.State.Failed"),
        LWorkState.LWorkStateUnresolved => LLocalization.LLocalizationTextRead("Roster.State.Unresolved"),
        LWorkState.LWorkStatePartial => LLocalization.LLocalizationTextRead("Roster.State.Partial"),
        LWorkState.LWorkStateBlocked => LLocalization.LLocalizationTextRead("Roster.State.Blocked"),
        _ => lWorkPhase switch
        {
            LWorkPhase.LWorkPhaseEncoding => LLocalization.LLocalizationTextRead("Roster.Phase.Processing"),
            LWorkPhase.LWorkPhaseStarted => LLocalization.LLocalizationTextRead("Roster.Phase.Started"),
            _ => LLocalization.LLocalizationTextRead("Roster.Phase.NotStarted")
        }
    };
}
