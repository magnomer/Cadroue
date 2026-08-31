using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public static class PRosterStateLabel
{
    internal static string PRosterStateFormat(LWorkState pWorkState) => pWorkState switch
    {
        LWorkState.LWorkStatePending => LLocalization.LLocalizationTextRead("Roster.State.Pending"),
        LWorkState.LWorkStateRunning => LLocalization.LLocalizationTextRead("Roster.State.Running"),
        LWorkState.LWorkStateDone => LLocalization.LLocalizationTextRead("Roster.State.Done"),
        LWorkState.LWorkStateFailed => LLocalization.LLocalizationTextRead("Roster.State.Failed"),
        LWorkState.LWorkStateUnresolved => LLocalization.LLocalizationTextRead("Roster.State.Unresolved"),
        LWorkState.LWorkStatePartial => LLocalization.LLocalizationTextRead("Roster.State.Partial"),
        LWorkState.LWorkStateBlocked => LLocalization.LLocalizationTextRead("Roster.State.Blocked"),
        LWorkState.LWorkStateCancelled => LLocalization.LLocalizationTextRead("Roster.State.Cancelled"),
        _ => pWorkState.ToString()
    };
}
