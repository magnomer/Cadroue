using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

/// <summary>
/// Worklist surface: transport, overall progress, the queued jobs and a detail pane
/// for the selected one. It renders <see cref="LSchedule"/> and never keeps its own
/// copy of the queue — the backend stays the ground truth.
/// </summary>
public sealed class PRoster : UserControl
{
    private static readonly Brush PRosterLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PRosterTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PRosterMutedBrush = new SolidColorBrush(Color.FromRgb(0x62, 0x6F, 0x83));

    private readonly LSchedule lRosterSchedule = LSchedule.LScheduleCurrent;
    private readonly LRunner lRosterRunner;
    private readonly ListView pRosterTable;
    private readonly ProgressBar pRosterProgress;
    private readonly TextBlock pRosterStatus;
    private readonly Button pRosterStartButton;
    private readonly Button pRosterPauseButton;
    private readonly StackPanel pRosterDetailPanel;
    private readonly List<LWorkItem> pRosterWatchedItems = new();

    public PRoster()
    {
        FocusVisualStyle = null;
        // The schedule is single-threaded; the runner marshals every state write here.
        lRosterRunner = new LRunner(lRosterSchedule, pAction => Dispatcher.Invoke(pAction));
        pRosterProgress = new ProgressBar { Height = 8, Minimum = 0, Maximum = 1, Value = 0 };
        pRosterStatus = new TextBlock { Foreground = PRosterMutedBrush, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
        pRosterStartButton = PRosterButtonBuild("Start", PRosterStartHandle);
        pRosterPauseButton = PRosterButtonBuild("Pause", PRosterPauseHandle);
        pRosterTable = PRosterTableBuild();
        pRosterDetailPanel = new StackPanel();

        Content = PPanel.PPanelBorderBuild(PRosterBuild());

        lRosterSchedule.LScheduleChange += PRosterScheduleHandle;
        Unloaded += PRosterUnloadHandle;
        PRosterScheduleHandle(lRosterSchedule);
    }

    private UIElement PRosterBuild()
    {
        var pRoot = new DockPanel { Margin = new Thickness(14) };

        var pTransport = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        pTransport.Children.Add(pRosterStartButton);
        pTransport.Children.Add(pRosterPauseButton);
        pTransport.Children.Add(PRosterButtonBuild("Cancel", PRosterCancelHandle));
        pTransport.Children.Add(new Border { Width = 14 });
        pTransport.Children.Add(pRosterStatus);
        DockPanel.SetDock(pTransport, Dock.Top);
        pRoot.Children.Add(pTransport);

        var pProgressBox = new Border { Margin = new Thickness(0, 0, 0, 14), Child = pRosterProgress };
        DockPanel.SetDock(pProgressBox, Dock.Top);
        pRoot.Children.Add(pProgressBox);

        var pBody = new Grid();
        pBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 240 });
        pBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        pBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 200 });

        Grid.SetColumn(pRosterTable, 0);
        pBody.Children.Add(pRosterTable);

        var pSplitter = new GridSplitter
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = Brushes.Transparent,
            ResizeBehavior = GridResizeBehavior.PreviousAndNext,
            ResizeDirection = GridResizeDirection.Columns,
            ShowsPreview = false,
            Focusable = false
        };
        Grid.SetColumn(pSplitter, 1);
        pBody.Children.Add(pSplitter);

        UIElement pDetail = PRosterDetailBuild();
        Grid.SetColumn(pDetail, 2);
        pBody.Children.Add(pDetail);

        pRoot.Children.Add(pBody);
        return pRoot;
    }

    private static Button PRosterButtonBuild(string pLabel, RoutedEventHandler pClick)
    {
        var pButton = new Button
        {
            Content = pLabel,
            Width = 88,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            Style = PButton.PButtonWhiteCreate()
        };
        pButton.Click += pClick;
        return pButton;
    }

    private ListView PRosterTableBuild()
    {
        var pView = new GridView();
        pView.Columns.Add(new GridViewColumn
        {
            Header = "Output",
            Width = 240,
            DisplayMemberBinding = new Binding(nameof(LWorkItem.LWorkOutputName))
        });
        pView.Columns.Add(new GridViewColumn
        {
            Header = "Start",
            Width = 80,
            DisplayMemberBinding = new Binding(nameof(LWorkItem.LWorkStart)) { StringFormat = @"hh\:mm\:ss" }
        });
        pView.Columns.Add(new GridViewColumn
        {
            Header = "Length",
            Width = 80,
            DisplayMemberBinding = new Binding(nameof(LWorkItem.LWorkDuration)) { StringFormat = @"hh\:mm\:ss" }
        });
        pView.Columns.Add(new GridViewColumn
        {
            Header = "State",
            Width = 90,
            DisplayMemberBinding = new Binding(nameof(LWorkItem.LWorkStateCurrent)) { Converter = new PRosterStateLabel() }
        });

        var pTable = new ListView
        {
            View = pView,
            ItemsSource = lRosterSchedule.LScheduleRecords,
            BorderBrush = PRosterLineBrush,
            BorderThickness = new Thickness(1),
            // The schedule collection is shared; without this every worklist view would
            // share one selection through the default collection view.
            IsSynchronizedWithCurrentItem = false
        };
        pTable.SelectionChanged += (_, _) => PRosterDetailUpdate();
        return pTable;
    }

    private UIElement PRosterDetailBuild()
    {
        var pPanel = new StackPanel { Margin = new Thickness(12, 0, 0, 0) };
        pPanel.Children.Add(new TextBlock
        {
            Text = "Job detail",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = PRosterTextBrush,
            Margin = new Thickness(0, 0, 0, 10)
        });
        pPanel.Children.Add(pRosterDetailPanel);
        PRosterDetailUpdate();

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            FocusVisualStyle = null,
            Content = pPanel
        };
    }

    private void PRosterDetailUpdate()
    {
        pRosterDetailPanel.Children.Clear();

        if (pRosterTable.SelectedItem is not LWorkItem pWorkItem)
        {
            pRosterDetailPanel.Children.Add(new TextBlock
            {
                Text = "Select a job to see its settings.",
                Foreground = PRosterMutedBrush,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        LWorkOutput pOutput = pWorkItem.LWorkOutput;
        PRosterRowAdd("State", PRosterStateLabel.PRosterStateFormat(pWorkItem.LWorkStateCurrent));
        PRosterRowAdd("Kind", pWorkItem.LWorkKind.ToString().Replace("LWorkKind", string.Empty));
        PRosterRowAdd("Priority", pWorkItem.LWorkPriority == LWorkPriority.LWorkPriorityHigh ? "High" : "Normal");
        PRosterRowAdd("Source", pWorkItem.LWorkSourcePath);
        PRosterRowAdd("Range", $"{pWorkItem.LWorkStart:hh\\:mm\\:ss} - {pWorkItem.LWorkEnd:hh\\:mm\\:ss}  ({pWorkItem.LWorkDuration:hh\\:mm\\:ss})");
        PRosterRowAdd("Output", pWorkItem.LWorkOutputPath);
        PRosterRowAdd("Container", pOutput.LWorkOutputContainer);
        PRosterRowAdd("Export mode", pOutput.LWorkOutputExportMode);
        PRosterRowAdd("Video", $"{pOutput.LWorkOutputVideoMode} ({pOutput.LWorkOutputVideoStream})");
        PRosterRowAdd("Encoder", pOutput.LWorkOutputVideoEncoder);
        PRosterRowAdd("Rate control", pOutput.LWorkOutputRateControl);
        PRosterRowAdd("Quality", pOutput.LWorkOutputQuality);
        PRosterRowAdd("Speed preset", pOutput.LWorkOutputSpeedPreset);
        PRosterRowAdd("Size / FPS", $"{pOutput.LWorkOutputVideoSize} / {pOutput.LWorkOutputVideoFps}");
        PRosterRowAdd("Pixel format", pOutput.LWorkOutputPixelFormat);

        if (pOutput.LWorkOutputVideoExtras.Count > 0)
        {
            PRosterRowAdd("Extras", string.Join("  ", pOutput.LWorkOutputVideoExtras.Select(pExtra => $"{pExtra.Key} {pExtra.Value}")));
        }

        PRosterRowAdd("Audio", $"{pOutput.LWorkOutputAudioMode} ({pOutput.LWorkOutputAudioStream})");
        PRosterRowAdd("Audio codec", $"{pOutput.LWorkOutputAudioEncoder}  {pOutput.LWorkOutputAudioBitrate}");
        PRosterRowAdd("Audio format", $"{pOutput.LWorkOutputAudioSampleRate}  {pOutput.LWorkOutputAudioChannels}");
        PRosterRowAdd("Queued", pWorkItem.LWorkCreateTime.ToString("yyyy-MM-dd HH:mm:ss"));

        if (!string.IsNullOrWhiteSpace(pWorkItem.LWorkMessage))
        {
            PRosterRowAdd("Message", pWorkItem.LWorkMessage);
        }
    }

    private void PRosterRowAdd(string pLabel, string pValue)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(94) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock
        {
            Text = pLabel,
            Foreground = PRosterMutedBrush,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Top
        });

        var pValueBlock = new TextBlock
        {
            Text = pValue,
            Foreground = PRosterTextBrush,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(pValueBlock, 1);
        pGrid.Children.Add(pValueBlock);
        pRosterDetailPanel.Children.Add(pGrid);
    }

    private void PRosterScheduleHandle(LSchedule lSchedule)
    {
        PRosterWatchStop();
        foreach (LWorkItem lWorkItem in lSchedule.LScheduleRecords)
        {
            lWorkItem.PropertyChanged += PRosterItemHandle;
            pRosterWatchedItems.Add(lWorkItem);
        }

        PRosterProgressUpdate();
    }

    private void PRosterItemHandle(object? pSender, PropertyChangedEventArgs pArguments)
    {
        PRosterProgressUpdate();
        if (ReferenceEquals(pSender, pRosterTable.SelectedItem))
        {
            PRosterDetailUpdate();
        }
    }

    private void PRosterProgressUpdate()
    {
        int pTotal = lRosterSchedule.LScheduleRecords.Count;
        int pDone = lRosterSchedule.LScheduleDoneCount;

        // The bar shows only the file being encoded right now. It is deliberately not a
        // queue-completion bar: job count is reported as text instead.
        LWorkItem? pRunning = lRosterSchedule.LScheduleRecords
            .FirstOrDefault(pWorkItem => pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning);

        pRosterProgress.Maximum = 1;
        pRosterProgress.Value = pRunning?.LWorkProgress ?? 0;
        pRosterStartButton.IsEnabled = !lRosterSchedule.LScheduleRunning && pTotal > 0;
        pRosterPauseButton.IsEnabled = lRosterSchedule.LScheduleRunning;

        string pRunState = lRosterRunner.LRunnerSuspended
            ? "Suspended"
            : lRosterSchedule.LScheduleRunning ? "Running" : "Paused";
        string pQueueText = $"{pDone} of {pTotal} done, {lRosterSchedule.LSchedulePendingRead().Count} pending";

        pRosterStatus.Text = pTotal == 0
            ? "No work queued."
            : pRunning is null
                ? $"{pRunState}  -  {pQueueText}"
                : $"{pRunState}  -  {pRunning.LWorkOutputName}  {pRunning.LWorkProgress:P0}  -  {pQueueText}";
    }

    private void PRosterStartHandle(object pSender, RoutedEventArgs pArguments) => lRosterRunner.LRunnerStart();

    private void PRosterPauseHandle(object pSender, RoutedEventArgs pArguments) => lRosterRunner.LRunnerPause();

    private void PRosterCancelHandle(object pSender, RoutedEventArgs pArguments) => lRosterRunner.LRunnerCancel();

    private void PRosterUnloadHandle(object pSender, RoutedEventArgs pArguments)
    {
        lRosterSchedule.LScheduleChange -= PRosterScheduleHandle;
        Unloaded -= PRosterUnloadHandle;
        PRosterWatchStop();
    }

    private void PRosterWatchStop()
    {
        foreach (LWorkItem pWorkItem in pRosterWatchedItems)
        {
            pWorkItem.PropertyChanged -= PRosterItemHandle;
        }

        pRosterWatchedItems.Clear();
    }
}

/// <summary>Renders an <see cref="LWorkState"/> as the label shown in the job list.</summary>
public sealed class PRosterStateLabel : IValueConverter
{
    internal static string PRosterStateFormat(LWorkState pWorkState) => pWorkState switch
    {
        LWorkState.LWorkStatePending => "Pending",
        LWorkState.LWorkStateRunning => "Running",
        LWorkState.LWorkStateDone => "Done",
        LWorkState.LWorkStateFailed => "Failed",
        LWorkState.LWorkStateCancelled => "Cancelled",
        _ => pWorkState.ToString()
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is LWorkState pWorkState ? PRosterStateFormat(pWorkState) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
