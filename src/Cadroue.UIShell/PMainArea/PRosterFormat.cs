using System.IO;
using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private static string PRosterPendingFormat(string pMediaPath) =>
        pRosterMediaPending.Contains(pMediaPath) ? LLocalization.LLocalizationTextRead("Roster.Value.Reading") : LLocalization.LLocalizationTextRead("Roster.Value.Unknown");

    private static string PRosterContainerFormat(string pMediaPath)
    {
        string pExtension = Path.GetExtension(pMediaPath).TrimStart('.');
        return pExtension.Length == 0 ? LLocalization.LLocalizationTextRead("Roster.Value.Unknown") : pExtension.ToUpperInvariant();
    }

    private static string PRosterPhaseFormat(LWorkState pWorkState, LWorkPhase pWorkPhase) => pWorkState switch
    {
        LWorkState.LWorkStateDone => LLocalization.LLocalizationTextRead("Roster.State.Done"),
        LWorkState.LWorkStateFailed => LLocalization.LLocalizationTextRead("Roster.State.Failed"),
        _ => pWorkPhase switch
        {
            LWorkPhase.LWorkPhaseEncoding => LLocalization.LLocalizationTextRead("Roster.Phase.Processing"),
            LWorkPhase.LWorkPhaseStarted => LLocalization.LLocalizationTextRead("Roster.Phase.Started"),
            _ => LLocalization.LLocalizationTextRead("Roster.Phase.NotStarted")
        }
    };

}
