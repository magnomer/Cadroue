using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PCabin;

public sealed partial class PRoster
{
    private CheckBox? pRosterSharedBox;
    private CheckBox? pRosterCompletedBox;

    private UIElement PRosterOptionsBuild()
    {
        var pSharedToggle = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Roster.Queue.Shared"),
            FontSize = PRosterTheme.PRosterRowSize,
            IsChecked = LPreference.LPreferenceStateCurrent.LPreferenceWorklistShared
        };
        PCheckbox.PCheckboxApply(pSharedToggle);
        pSharedToggle.Checked += (_, _) => PRosterSharedApply(true);
        pSharedToggle.Unchecked += (_, _) => PRosterSharedApply(false);
        pRosterSharedBox = pSharedToggle;

        var pCollapseToggle = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Roster.Queue.CollapseCompleted"),
            FontSize = PRosterTheme.PRosterRowSize,
            IsChecked = LPreference.LPreferenceStateCurrent.LPreferenceCollapseDone,
            Margin = new Thickness(18, 0, 0, 0)
        };
        PCheckbox.PCheckboxApply(pCollapseToggle);
        pCollapseToggle.Checked += (_, _) => PRosterCompletedApply(true);
        pCollapseToggle.Unchecked += (_, _) => PRosterCompletedApply(false);
        pRosterCompletedBox = pCollapseToggle;

        var pOptions = new StackPanel { Orientation = Orientation.Horizontal };
        pOptions.Children.Add(pSharedToggle);
        pOptions.Children.Add(pCollapseToggle);

        return new Border
        {
            Padding = PRosterTheme.PRosterHeaderPadding,
            Background = PRosterTheme.PRosterHeaderBrush,
            BorderBrush = PRosterTheme.PRosterLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = pOptions
        };
    }

    private void PRosterSharedApply(bool pShared)
    {
        if (pShared == LPreference.LPreferenceStateCurrent.LPreferenceWorklistShared)
        {
            return;
        }

        LPreferenceState pNext = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        pNext.LPreferenceWorklistShared = pShared;
        LPreference.LPreferenceStateSet(pNext);
        pRosterSchedule.LScheduleLoad();
    }

    private void PRosterCompletedApply(bool pCollapseCompleted)
    {
        if (pCollapseCompleted == LPreference.LPreferenceStateCurrent.LPreferenceCollapseDone)
        {
            return;
        }

        LPreferenceState pNext = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        pNext.LPreferenceCollapseDone = pCollapseCompleted;
        LPreference.LPreferenceStateSet(pNext);
        if (pCollapseCompleted)
        {
            PRosterCompletedSync(pRosterSchedule.LScheduleRecords.Where(PRosterVisibleCheck));
        }
    }
}
