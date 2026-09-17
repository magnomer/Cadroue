using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PDeck;

public sealed partial class PRoster : UserControl
{
    private readonly LScheduleContract pRosterSchedule = PProgram.LScheduleCurrent;
    private readonly LStation pRosterStation = LStation.LStationCreate(
        LLocalization.LLocalizationTextRead("Roster.Title.Worklist"));
    private readonly Grid pRosterBody;
    private readonly StackPanel pRosterQueuePanel = new();
    private readonly ScrollViewer pRosterQueueScroller;
    private readonly StackPanel pRosterDetailPanel;
    private readonly TextBlock pRosterDetailTitle;
    private readonly System.Windows.Threading.DispatcherTimer pRosterElapsedTimer;

    public LRoster LRoster { get; } = new();

    public PRoster(LSceneTabRecord? lPreferenceTabLayout = null)
    {
        FocusVisualStyle = null;
        PScrollbar.PScrollbarApply(this);
        pRosterDetailTitle = PRosterTitleBuild(LLocalization.LLocalizationTextRead("Roster.Title.JobDetail"));
        pRosterQueueScroller = PRosterQueueBuild();
        pRosterDetailPanel = new StackPanel();

        pRosterBody = new Grid();
        Content = PRosterBuild(lPreferenceTabLayout);

        pRosterSchedule.LScheduleChange += PRosterScheduleHandle;
        pRosterSchedule.LScheduleItemChange += PRosterItemHandle;
        PRosterConsoleAttach();
        IsVisibleChanged += PRosterVisibleHandle;
        Unloaded += PRosterUnloadHandle;

        pRosterElapsedTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        pRosterElapsedTimer.Tick += PRosterElapsedTick;
        pRosterElapsedTimer.Start();

        PRosterScheduleHandle(pRosterSchedule);
    }

    private void PRosterElapsedTick(object? pSender, EventArgs pArguments)
    {
        if (LRoster.LRosterClosed || !IsVisible)
        {
            return;
        }

        if (PRosterRunningCheck() || PRosterMeasuringCheck())
        {
            PRosterDetailUpdate();
        }
    }

    private bool PRosterRunningCheck()
    {
        foreach (LWorkItem pWorkItem in pRosterSchedule.LScheduleRecords)
        {
            if (pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning)
            {
                return true;
            }
        }

        return false;
    }

    private bool PRosterMeasuringCheck()
    {
        if (LRoster.LRosterCardId != Guid.Empty)
        {
            return pRosterSchedule.LScheduleRecords.Any(
                pWorkItem => LRoster.LRosterCardCheck(pWorkItem.LWorkBatchId) && !pWorkItem.LWorkSourceMeasured);
        }

        return PRosterSelectRead() is { LWorkSourceMeasured: false };
    }

    public bool PRosterBusyCheck() => pRosterStation.LStationBusyCheck();

    private void PRosterVisibleHandle(object pSender, DependencyPropertyChangedEventArgs pArguments)
    {
        if (IsVisible)
        {
            PConsole.PConsoleCurrent?.PConsoleStationSet(pRosterStation);
            if (pRosterSharedBox is { } pToggle)
            {
                pToggle.IsChecked = LPreference.LPreferenceStateCurrent.LPreferenceWorklistShared;
            }

            if (pRosterCompletedBox is { } pCollapseToggle)
            {
                pCollapseToggle.IsChecked = LPreference.LPreferenceStateCurrent.LPreferenceCollapseDone;
            }
        }
    }

    private void PRosterConsoleAttach()
    {
        pRosterStation.LStationSelectionSource = PRosterSelectionRead;
        PConsole.PConsoleCurrent?.PConsoleStationSet(pRosterStation);
    }

    private static TextBlock PRosterTitleBuild(string pTitle) => new()
    {
        Text = pTitle,
        FontSize = PRosterTheme.PRosterTitleSize,
        FontWeight = FontWeights.SemiBold,
        Foreground = PRosterTheme.PRosterTitleBrush,
        VerticalAlignment = VerticalAlignment.Center
    };
}
