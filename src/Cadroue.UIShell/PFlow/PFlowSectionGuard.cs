using Cadroue.Core;
using Cadroue.Application;
using System.Windows;
using Cadroue.UIShell.PSShared;

namespace Cadroue.UIShell.PFlow;

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

    private bool PFlowInsideCheck(TimeSpan pFlowTime, int pFlowSkipIndex)
        => LPiece.LPieceInsideCheck(lSegment.LSegmentListRead(), pFlowTime, pFlowSkipIndex, PFlowOverlapAllowed);

    private TimeSpan PFlowLimitRead(TimeSpan pFlowFrom, TimeSpan pFlowCeiling, int pFlowSkipIndex)
        => LPiece.LPieceLimitRead(lSegment.LSegmentListRead(), pFlowFrom, pFlowCeiling, pFlowSkipIndex, PFlowOverlapAllowed);

    private TimeSpan PFlowFloorRead(TimeSpan pFlowUntil, int pFlowSkipIndex)
        => LPiece.LPieceFloorRead(lSegment.LSegmentListRead(), pFlowUntil, pFlowSkipIndex, PFlowOverlapAllowed);
}
