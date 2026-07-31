using System.Windows;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    private static bool PFlowOverlapAllowed => PProgram.LPreferenceStateCurrent.LPreferenceOverlapAllowed;

    private bool PFlowDestructiveConfirm(string pFlowQuestion)
    {
        if (!PProgram.LPreferenceStateCurrent.LPreferenceConfirmDestructive)
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
    {
        if (PFlowOverlapAllowed)
        {
            return false;
        }

        for (int pFlowIndex = 0; pFlowIndex < lSectionList.Count; pFlowIndex++)
        {
            if (pFlowIndex == pFlowSkipIndex)
            {
                continue;
            }

            LSegment pFlowSection = lSectionList[pFlowIndex];
            if (pFlowTime >= pFlowSection.LSegmentStart && pFlowTime < pFlowSection.LSegmentEnd)
            {
                return true;
            }
        }

        return false;
    }

    private TimeSpan PFlowLimitRead(TimeSpan pFlowFrom, TimeSpan pFlowCeiling, int pFlowSkipIndex)
    {
        if (PFlowOverlapAllowed)
        {
            return pFlowCeiling;
        }

        TimeSpan pFlowLimit = pFlowCeiling;
        for (int pFlowIndex = 0; pFlowIndex < lSectionList.Count; pFlowIndex++)
        {
            if (pFlowIndex == pFlowSkipIndex)
            {
                continue;
            }

            TimeSpan pFlowStart = lSectionList[pFlowIndex].LSegmentStart;
            if (pFlowStart > pFlowFrom && pFlowStart < pFlowLimit)
            {
                pFlowLimit = pFlowStart;
            }
        }

        return pFlowLimit;
    }

    private TimeSpan PFlowFloorRead(TimeSpan pFlowUntil, int pFlowSkipIndex)
    {
        if (PFlowOverlapAllowed)
        {
            return TimeSpan.Zero;
        }

        TimeSpan pFlowFloor = TimeSpan.Zero;
        for (int pFlowIndex = 0; pFlowIndex < lSectionList.Count; pFlowIndex++)
        {
            if (pFlowIndex == pFlowSkipIndex)
            {
                continue;
            }

            TimeSpan pFlowEnd = lSectionList[pFlowIndex].LSegmentEnd;
            if (pFlowEnd <= pFlowUntil && pFlowEnd > pFlowFloor)
            {
                pFlowFloor = pFlowEnd;
            }
        }

        return pFlowFloor;
    }
}
