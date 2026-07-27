using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIShell.PAssets;
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
    private readonly Button pRosterEmptyButton;
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
        pRosterStartButton = PRosterButtonBuild(
            "Start", "PRosterStart.svg", "Start processing the queue",
            PRosterTheme.PRosterDoneBrush, PRosterStartHandle);
        pRosterPauseButton = PRosterButtonBuild(
            "Pause", "PRosterPause.svg", "Pause the queue and suspend the running job",
            null, PRosterPauseHandle);
        pRosterCancelButton = PRosterButtonBuild(
            "Cancel", "PRosterCancel.svg", "Cancel the running job and return it to the queue",
            PRosterTheme.PRosterFailBrush, PRosterCancelHandle);
        pRosterRemoveButton = PRosterButtonBuild(
            "Remove", "PRosterRemove.svg", "Remove the selected job(s)",
            null, PRosterRemoveHandle);
        pRosterClearButton = PRosterButtonBuild(
            "Clear done", "PRosterClearDone.svg", "Remove the finished jobs",
            null, PRosterDoneHandle);
        pRosterEmptyButton = PRosterButtonBuild(
            "Clear all", "PRosterClearAll.svg", "Empty the list except the running job",
            null, PRosterAllHandle);
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

    private static Button PRosterButtonBuild(
        string pLabel,
        string pIconName,
        string pTooltip,
        System.Windows.Media.Brush? pAccentBrush,
        RoutedEventHandler pClick)
    {
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(new System.Windows.Controls.Image
        {
            Source = PIcon.PIconRead(
                $"/PAssets/PPanels/{pIconName}",
                pAccentBrush ?? PRosterTheme.PRosterTextBrush),
            Width = PRosterTheme.PRosterIconSize,
            Height = PRosterTheme.PRosterIconSize,
            Stretch = System.Windows.Media.Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        pStack.Children.Add(new Border { Height = 2 });
        pStack.Children.Add(new TextBlock
        {
            Text = pLabel,
            FontSize = PRosterTheme.PRosterRowSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });

        var pButton = new Button
        {
            Width = PRosterTheme.PRosterButtonSize,
            Height = PRosterTheme.PRosterButtonSize,
            Margin = new Thickness(0, 0, 4, 0),
            Content = pStack,
            Style = PRosterButtonStyleCreate(),
            ToolTip = pTooltip
        };
        pButton.Click += pClick;
        return pButton;
    }

    private static Style PRosterButtonStyleCreate()
    {
        Style pStyle = PButton.PButtonCommandCreate();
        var pDisabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        pDisabled.Setters.Add(new Setter(UIElement.OpacityProperty, PRosterTheme.PRosterDisabledOpacity));
        pStyle.Triggers.Add(pDisabled);
        return pStyle;
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
