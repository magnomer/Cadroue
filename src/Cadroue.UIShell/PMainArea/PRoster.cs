using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster : UserControl
{
    private readonly LSchedule pRosterSchedule = LSchedule.LScheduleCurrent;
    private readonly LStation pRosterStation = LStation.LStationCreate(LLocalization.LLocalizationTextRead("Roster.Title.Worklist"));
    private readonly Grid pRosterBody;
    private readonly ListBox pRosterQueueList;
    private readonly StackPanel pRosterDetailPanel;
    private readonly TextBlock pRosterDetailTitle;
    private readonly List<LWorkItem> pRosterWatchedItems = new();
    private bool pRosterClosed;

    public PRoster(LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        FocusVisualStyle = null;
        pRosterDetailTitle = PRosterTitleBuild(LLocalization.LLocalizationTextRead("Roster.Title.JobDetail"));
        pRosterQueueList = PRosterQueueBuild();
        pRosterDetailPanel = new StackPanel();

        pRosterBody = new Grid();
        Content = PRosterBuild(lPreferenceTabLayout);

        pRosterSchedule.LScheduleChange += PRosterScheduleHandle;
        PRosterConsoleAttach();
        IsVisibleChanged += PRosterVisibleHandle;
        Unloaded += PRosterUnloadHandle;

        PRosterScheduleHandle(pRosterSchedule);
    }

    public bool PRosterBusyCheck() => pRosterStation.LStationBusyCheck();

    private void PRosterVisibleHandle(object pSender, DependencyPropertyChangedEventArgs pArguments)
    {
        if (IsVisible)
        {
            PConsole.PConsoleCurrent?.PConsoleStationSet(pRosterStation);
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
