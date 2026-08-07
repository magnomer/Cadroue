using Cadroue.Core;
using Cadroue.MigrationInterface;
using System.Windows;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    private static bool PFlowOverlapAllowed => LPreference.LPreferenceStateCurrent.LPreferenceOverlapAllowed;

    private bool PFlowDestructiveConfirm(string pFlowQuestion)
    {
        if (!LPreference.LPreferenceStateCurrent.LPreferenceConfirmDestructive)
        {
            return true;
        }

        return MessageBox.Show(
            Window.GetWindow(this)!,
            pFlowQuestion,
            LLocalization.LLocalizationTextRead("Flow.Confirm.Title"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    private bool PFlowInsideCheck(TimeSpan pFlowTime, int pFlowSkipIndex)
        => LPiece.LPieceInsideCheck(lSegment.LSegmentListRead(), pFlowTime, pFlowSkipIndex, PFlowOverlapAllowed);

    private TimeSpan PFlowLimitRead(TimeSpan pFlowFrom, TimeSpan pFlowCeiling, int pFlowSkipIndex)
        => LPiece.LPieceLimitRead(lSegment.LSegmentListRead(), pFlowFrom, pFlowCeiling, pFlowSkipIndex, PFlowOverlapAllowed);

    private TimeSpan PFlowFloorRead(TimeSpan pFlowUntil, int pFlowSkipIndex)
        => LPiece.LPieceFloorRead(lSegment.LSegmentListRead(), pFlowUntil, pFlowSkipIndex, PFlowOverlapAllowed);
}
