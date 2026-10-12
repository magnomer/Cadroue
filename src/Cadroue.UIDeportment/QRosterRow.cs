using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record QRosterRow
{
    public const string QRosterShadeSelected = "Select";
    public const string QRosterShadeStage = "Stage";
    public const string QRosterShadePlain = "Plain";

    public required Guid QRosterRowId { get; init; }

    public required string QRosterRowStep { get; init; }

    public required string QRosterRowPriority { get; init; }

    public required string QRosterRowLength { get; init; }

    public required string QRosterRowProgress { get; init; }

    public required string QRosterRowRatio { get; init; }

    public required string QRosterRowState { get; init; }

    public required string QRosterRowKey { get; init; }

    public required string QRosterRowOwner { get; init; }

    public required bool QRosterRowLast { get; init; }

    public required bool QRosterRowStage { get; init; }

    public required string QRosterRowShade { get; init; }

    public static QRosterRow QRosterRowCreate(
        CLineage lLineage,
        LWorkItem lWorkItem,
        CLineageStep lStep,
        bool lLast,
        bool lStage,
        bool lOwned,
        string lShade)
    {
        return new QRosterRow
        {
            QRosterRowId = lWorkItem.LWorkId,
            QRosterRowStep = QLineage.QLineageStepFormat(lLineage, lStep),
            QRosterRowPriority = QRosterPriorityFormat(lWorkItem.LWorkPriority),
            QRosterRowLength = QRosterSpanFormat(lWorkItem.LWorkLength),
            QRosterRowProgress = QRosterProgressFormat(lWorkItem),
            QRosterRowRatio = QLineage.QLineageRatioFormat(lStep),
            QRosterRowState = QRosterStateFormat(lWorkItem.LWorkStateCurrent),
            QRosterRowKey = QRosterKeyRead(lWorkItem.LWorkStateCurrent),
            QRosterRowOwner = QRosterOwnerFormat(lWorkItem, lOwned),
            QRosterRowLast = lLast,
            QRosterRowStage = lStage,
            QRosterRowShade = lShade
        };
    }

    public static string QRosterShadeResolve(bool lSelected, bool lCardSelected, bool lStage)
    {
        if (lSelected)
        {
            return QRosterShadeSelected;
        }

        return !lCardSelected && lStage ? QRosterShadeStage : QRosterShadePlain;
    }

    public static string QRosterOwnerFormat(LWorkItem lWorkItem, bool lOwned)
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

    public static string QRosterProgressFormat(LWorkItem lWorkItem) =>
        lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning || lWorkItem.LWorkProgress > 0
            ? $"{lWorkItem.LWorkProgress:P0}"
            : "-";

    public static string QRosterPriorityFormat(LWorkPriority lPriority) =>
        lPriority == LWorkPriority.LWorkPriorityHigh
            ? LLocalization.LLocalizationTextRead("Roster.Priority.High")
            : LLocalization.LLocalizationTextRead("Roster.Priority.Normal");

    public static string QRosterSpanFormat(TimeSpan lSpan) => $"{lSpan:hh\\:mm\\:ss}";

    public static string QRosterKeyRead(LWorkState lWorkState) => lWorkState switch
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

    public static string QRosterStateFormat(LWorkState lWorkState) => lWorkState switch
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

    public static string QRosterPhaseFormat(LWorkState lWorkState, LWorkPhase lWorkPhase) => lWorkState switch
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
