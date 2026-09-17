using Cadroue.Core;
using Cadroue.Application;
using System.Windows;
using Cadroue.UIVeneer.PSCasement;

namespace Cadroue.UIVeneer.PFlow;

public sealed partial class PFlow
{
    private static bool PFlowOverlapAllowed => LPreference.LPreferenceStateCurrent.LPreferenceOverlapAllowed;

    private bool PFlowDestructiveConfirm(string pFlowQuestion, string pFlowAction)
    {
        if (!LPreference.LPreferenceStateCurrent.LPreferenceConfirmDestructive)
        {
            return true;
        }

        return PSAlert.PSAlertConfirm(
            Window.GetWindow(this)!,
            LLocalization.LLocalizationTextRead("Flow.Confirm.Title"),
            pFlowQuestion,
            pFlowAction);
    }
}
