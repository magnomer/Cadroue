using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PConsole : UserControl
{
    private const double PConsoleProgressHeight = 3;
    private const double PConsoleSwitchWidth = 34;
    private const double PConsoleSwitchIconSize = 18;

    private readonly LSchedule pConsoleSchedule = LSchedule.LScheduleCurrent;
    private readonly Button pConsolePreviousButton;
    private readonly Button pConsoleNextButton;
    private readonly CheckBox pConsoleAutoBox;
    private readonly ProgressBar pConsoleProgress;
    private readonly TextBlock pConsoleStatus;
    private readonly Button pConsoleStartButton;
    private readonly Button pConsolePauseButton;
    private readonly Button pConsoleCancelButton;
    private readonly Button pConsoleRemoveButton;
    private readonly Button pConsoleClearButton;
    private readonly Button pConsoleEmptyButton;
    private readonly List<LWorkItem> pConsoleWatchedItems = new();

    public PConsole()
    {
        FocusVisualStyle = null;
        pConsoleProgress = PConsoleProgressBuild();
        pConsoleStatus = PConsoleLabelBuild(PRosterTheme.PRosterMutedBrush);
        pConsoleStartButton = PConsoleButtonBuild(
            "Start", "PRosterStart.svg", "Start processing the queue",
            PRosterTheme.PRosterDoneBrush, PConsoleStartHandle);
        pConsolePauseButton = PConsoleButtonBuild(
            "Pause", "PRosterPause.svg", "Pause the queue and suspend the running job",
            null, PConsolePauseHandle);
        pConsoleCancelButton = PConsoleButtonBuild(
            "Cancel", "PRosterCancel.svg", "Cancel the running job and return it to the queue",
            PRosterTheme.PRosterFailBrush, PConsoleCancelHandle);
        pConsoleRemoveButton = PConsoleButtonBuild(
            "Remove", "PRosterRemove.svg", "Remove the selected job(s) in the Worklist",
            null, PConsoleRemoveHandle);
        pConsoleClearButton = PConsoleButtonBuild(
            "Clear done", "PRosterClearDone.svg", "Remove the finished jobs",
            null, PConsoleDoneHandle);
        pConsoleEmptyButton = PConsoleButtonBuild(
            "Clear all", "PRosterClearAll.svg", "Empty the list except the running job",
            null, PConsoleAllHandle);
        pConsoleAutoBox = PConsoleAutoBoxBuild();
        pConsolePreviousButton = PConsoleSwitchBuild(
            "PConsolePrevious.svg", "Control the previous worklist", PConsolePreviousHandle);
        pConsoleNextButton = PConsoleSwitchBuild(
            "PConsoleNext.svg", "Control the next worklist", PConsoleNextHandle);

        Content = PConsoleBuild();
        PConsoleCurrent = this;

        pConsoleSchedule.LScheduleChange += PConsoleScheduleHandle;
        LStation.LStationChange += PConsoleStationHandle;
        PConsoleDepotAttach();
        Unloaded += PConsoleUnloadHandle;

        PConsoleScheduleHandle(pConsoleSchedule);
        Dispatcher.BeginInvoke(new Action(() => pConsoleSchedule.LScheduleReload()));
    }

    public static PConsole? PConsoleCurrent { get; private set; }

    public void PConsoleProgressRefresh() => PConsoleProgressUpdate();

    private UIElement PConsoleBuild()
    {
        var pButtons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pButtons.Children.Add(pConsoleStartButton);
        pButtons.Children.Add(pConsolePauseButton);
        pButtons.Children.Add(pConsoleCancelButton);
        pButtons.Children.Add(new Border { Width = 10 });
        pButtons.Children.Add(pConsoleRemoveButton);
        pButtons.Children.Add(pConsoleClearButton);
        pButtons.Children.Add(pConsoleEmptyButton);

        var pRow = new Grid();
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pButtons, 0);
        Grid.SetColumn(pConsoleStatus, 1);
        Grid.SetColumn(pConsoleAutoBox, 2);
        pConsoleStatus.Margin = new Thickness(14, 0, 0, 0);
        pRow.Children.Add(pButtons);
        pRow.Children.Add(pConsoleStatus);
        pRow.Children.Add(pConsoleAutoBox);

        var pStack = new StackPanel();
        pStack.Children.Add(pRow);
        pStack.Children.Add(new Border { Height = 8 });
        pStack.Children.Add(pConsoleProgress);

        var pCard = new Border
        {
            Padding = new Thickness(8, 10, 8, 10),
            Background = Brushes.White,
            SnapsToDevicePixels = true,
            Child = pStack
        };

        Background = Brushes.White;
        var pRoot = new Grid();
        pRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PConsoleSwitchWidth) });
        pRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PConsoleSwitchWidth) });
        Grid.SetColumn(pConsolePreviousButton, 0);
        Grid.SetColumn(pCard, 1);
        Grid.SetColumn(pConsoleNextButton, 2);
        pRoot.Children.Add(pConsolePreviousButton);
        pRoot.Children.Add(pCard);
        pRoot.Children.Add(pConsoleNextButton);
        return pRoot;
    }

    private CheckBox PConsoleAutoBoxBuild()
    {
        var pAutoBox = new CheckBox
        {
            Content = "Auto resume",
            FontSize = PRosterTheme.PRosterRowSize,
            Foreground = PRosterTheme.PRosterTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 4, 0),
            FocusVisualStyle = null,
            ToolTip = "Keep this worklist watching the queue and start the next job as soon as one is waiting"
        };
        pAutoBox.Checked += PConsoleAutoHandle;
        pAutoBox.Unchecked += PConsoleAutoHandle;
        return pAutoBox;
    }

    private static Button PConsoleSwitchBuild(string pIconName, string pTooltip, RoutedEventHandler pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Source = PIcon.PIconRead($"/PAssets/PPanels/{pIconName}", PRosterTheme.PRosterTextBrush),
                Width = PConsoleSwitchIconSize,
                Height = PConsoleSwitchIconSize,
                Stretch = Stretch.Uniform
            },
            Width = PConsoleSwitchWidth,
            VerticalAlignment = VerticalAlignment.Stretch,
            Visibility = Visibility.Collapsed,
            Style = PConsoleButtonStyleCreate(),
            ToolTip = pTooltip
        };
        pButton.Click += pClick;
        return pButton;
    }

    private static Button PConsoleButtonBuild(
        string pLabel,
        string pIconName,
        string pTooltip,
        Brush? pAccentBrush,
        RoutedEventHandler pClick)
    {
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(new Image
        {
            Source = PIcon.PIconRead(
                $"/PAssets/PPanels/{pIconName}",
                pAccentBrush ?? PRosterTheme.PRosterTextBrush),
            Width = PRosterTheme.PRosterIconSize,
            Height = PRosterTheme.PRosterIconSize,
            Stretch = Stretch.Uniform,
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
            Style = PConsoleButtonStyleCreate(),
            ToolTip = pTooltip
        };
        pButton.Click += pClick;
        return pButton;
    }

    private static Style PConsoleButtonStyleCreate()
    {
        Style pStyle = PButton.PButtonCommandCreate();
        var pDisabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        pDisabled.Setters.Add(new Setter(UIElement.OpacityProperty, PRosterTheme.PRosterDisabledOpacity));
        pStyle.Triggers.Add(pDisabled);
        return pStyle;
    }

    private static TextBlock PConsoleLabelBuild(Brush pBrush) => new()
    {
        FontSize = PRosterTheme.PRosterRowSize,
        Foreground = pBrush,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis
    };

    private static ProgressBar PConsoleProgressBuild() => new()
    {
        Height = PConsoleProgressHeight,
        Minimum = 0,
        Maximum = 1,
        Value = 0,
        Background = PRosterTheme.PRosterTrackBrush,
        Foreground = PRosterTheme.PRosterRunBrush,
        BorderThickness = new Thickness(0),
        Template = PConsoleProgressTemplateCreate()
    };

    private static ControlTemplate PConsoleProgressTemplateCreate()
    {
        var pTrack = new FrameworkElementFactory(typeof(Border));
        pTrack.Name = "PART_Track";
        pTrack.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pTrack.SetValue(Border.CornerRadiusProperty, new CornerRadius(PConsoleProgressHeight / 2));

        var pIndicator = new FrameworkElementFactory(typeof(Border));
        pIndicator.Name = "PART_Indicator";
        pIndicator.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        pIndicator.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
        pIndicator.SetValue(Border.CornerRadiusProperty, new CornerRadius(PConsoleProgressHeight / 2));

        var pRoot = new FrameworkElementFactory(typeof(Grid));
        pRoot.AppendChild(pTrack);
        pRoot.AppendChild(pIndicator);
        return new ControlTemplate(typeof(ProgressBar)) { VisualTree = pRoot };
    }
}
