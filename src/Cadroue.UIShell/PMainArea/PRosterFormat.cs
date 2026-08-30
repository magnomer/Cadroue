using System.IO;
using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private static string PRosterContainerFormat(string pMediaPath)
    {
        string pExtension = Path.GetExtension(pMediaPath).TrimStart('.');
        return pExtension.Length == 0 ? LLocalization.LLocalizationTextRead("Roster.Value.Unknown") : pExtension.ToUpperInvariant();
    }

    private static string PRosterPhaseFormat(LWorkState pWorkState, LWorkPhase pWorkPhase) => pWorkState switch
    {
        LWorkState.LWorkStateDone => LLocalization.LLocalizationTextRead("Roster.State.Done"),
        LWorkState.LWorkStateFailed => LLocalization.LLocalizationTextRead("Roster.State.Failed"),
        LWorkState.LWorkStateUnresolved => LLocalization.LLocalizationTextRead("Roster.State.Unresolved"),
        LWorkState.LWorkStatePartial => LLocalization.LLocalizationTextRead("Roster.State.Partial"),
        LWorkState.LWorkStateBlocked => LLocalization.LLocalizationTextRead("Roster.State.Blocked"),
        _ => pWorkPhase switch
        {
            LWorkPhase.LWorkPhaseEncoding => LLocalization.LLocalizationTextRead("Roster.Phase.Processing"),
            LWorkPhase.LWorkPhaseStarted => LLocalization.LLocalizationTextRead("Roster.Phase.Started"),
            _ => LLocalization.LLocalizationTextRead("Roster.Phase.NotStarted")
        }
    };

}
