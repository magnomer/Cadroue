using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster : UserControl
{
    private static readonly Brush PRosterLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PRosterTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PRosterMutedBrush = new SolidColorBrush(Color.FromRgb(0x62, 0x6F, 0x83));

    private readonly LSchedule lRosterSchedule = LSchedule.LScheduleCurrent;
    private readonly LRunner lRosterRunner;
    private readonly Grid pRosterBody;
    private readonly ListView pRosterTable;
    private readonly ProgressBar pRosterProgress;
    private readonly TextBlock pRosterStatus;
    private readonly Button pRosterStartButton;
    private readonly Button pRosterPauseButton;
    private readonly StackPanel pRosterDetailPanel;
    private readonly List<LWorkItem> pRosterWatchedItems = new();
    private readonly LDepotWatch lRosterDepotWatch = new();

    public PRoster(LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        FocusVisualStyle = null;
        lRosterRunner = new LRunner(lRosterSchedule, pAction => Dispatcher.Invoke(pAction));
        pRosterProgress = new ProgressBar { Height = 8, Minimum = 0, Maximum = 1, Value = 0 };
        pRosterStatus = new TextBlock { Foreground = PRosterMutedBrush, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
        pRosterStartButton = PRosterButtonBuild("Start", PRosterStartHandle);
        pRosterPauseButton = PRosterButtonBuild("Pause", PRosterPauseHandle);
        pRosterTable = PRosterTableBuild();
        pRosterDetailPanel = new StackPanel();

        pRosterBody = new Grid();
        Content = PPanel.PPanelBorderBuild(PRosterBuild(lPreferenceTabLayout));

        lRosterSchedule.LScheduleChange += PRosterScheduleHandle;
        Unloaded += PRosterUnloadHandle;

        lRosterDepotWatch.LDepotChange += PRosterDepotHandle;
        lRosterDepotWatch.LDepotWatchStart();
        lRosterSchedule.LScheduleReload();
    }

}
