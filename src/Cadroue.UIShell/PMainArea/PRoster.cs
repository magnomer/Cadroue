using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster : UserControl
{
    private readonly LSchedule pRosterSchedule = LSchedule.LScheduleCurrent;
    private readonly LRunner pRosterRunner;
    private readonly Grid pRosterBody;
    private readonly ListBox pRosterQueueList;
    private readonly TextBlock pRosterQueueTitle;
    private readonly ProgressBar pRosterProgress;
    private readonly TextBlock pRosterStatus;
    private readonly Button pRosterStartButton;
    private readonly Button pRosterPauseButton;
    private readonly Button pRosterCancelButton;
    private readonly Button pRosterRemoveButton;
    private readonly Button pRosterClearButton;
    private readonly StackPanel pRosterDetailPanel;
    private readonly TextBlock pRosterDetailTitle;
    private readonly List<LWorkItem> pRosterWatchedItems = new();

    public PRoster(LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        FocusVisualStyle = null;
        pRosterRunner = new LRunner(pRosterSchedule, pAction => Dispatcher.Invoke(pAction))
        {
            LRunnerProgramPath = App.LRendererProgramCurrent
        };

        pRosterProgress = PRosterProgressBuild();
        pRosterStatus = PRosterLabelBuild(PRosterTheme.PRosterMutedBrush);
        pRosterQueueTitle = PRosterTitleBuild("Queue");
        pRosterDetailTitle = PRosterTitleBuild("Job detail");
        pRosterStartButton = PRosterButtonBuild("Start", PRosterStartHandle);
        pRosterPauseButton = PRosterButtonBuild("Pause", PRosterPauseHandle);
        pRosterCancelButton = PRosterButtonBuild("Cancel", PRosterCancelHandle);
        pRosterRemoveButton = PRosterButtonBuild("Remove", PRosterRemoveHandle);
        pRosterClearButton = PRosterButtonBuild("Clear done", PRosterClearHandle);
        pRosterQueueList = PRosterQueueBuild();
        pRosterDetailPanel = new StackPanel();

        pRosterBody = new Grid();
        Content = PRosterBuild(lPreferenceTabLayout);

        pRosterSchedule.LScheduleChange += PRosterScheduleHandle;
        PRosterDepotAttach();
        Unloaded += PRosterUnloadHandle;

        PRosterScheduleHandle(pRosterSchedule);
        Dispatcher.BeginInvoke(new Action(() => pRosterSchedule.LScheduleReload()));
    }

    private static Button PRosterButtonBuild(string pLabel, RoutedEventHandler pClick)
    {
        var pButton = new Button
        {
            Content = pLabel,
            MinWidth = 78,
            Margin = new Thickness(0, 0, 6, 0),
            FontSize = PRosterTheme.PRosterRowSize,
            Style = PButton.PButtonWhiteCreate()
        };
        pButton.Click += pClick;
        return pButton;
    }

    private static TextBlock PRosterTitleBuild(string pTitle) => new()
    {
        Text = pTitle,
        FontSize = PRosterTheme.PRosterTitleSize,
        FontWeight = FontWeights.SemiBold,
        Foreground = PRosterTheme.PRosterTitleBrush,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static TextBlock PRosterLabelBuild(System.Windows.Media.Brush pBrush) => new()
    {
        FontSize = PRosterTheme.PRosterRowSize,
        Foreground = pBrush,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis
    };
}
