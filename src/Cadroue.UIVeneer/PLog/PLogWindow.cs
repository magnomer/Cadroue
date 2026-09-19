using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

public sealed class PLogWindow : Window
{
    private const string PLogPlacementKey = "Log";
    private const int PLogFlushMilliseconds = 200;
    private const string PLogOpenIcon = "/PAsset/PPanel/POpen.svg";

    private readonly ObservableCollection<PLogRow> pLogRowsShown = new();
    private readonly ListBox pLogFeed;
    private readonly ComboBox pLogFileCombo;
    private readonly PPicker pLogCategoryPicker;
    private readonly CheckBox pLogVerboseBox;
    private readonly DispatcherTimer pLogFlushTimer;
    private readonly PSGrabber pLogGrabber;
    private readonly Window pLogOwner;

    public LLog LLog { get; } = new();

    private PLogWindow(Window pOwner)
    {
        pLogOwner = pOwner;
        pLogFileCombo = PLogComboBuild(320);
        pLogCategoryPicker = PLogCategoryBuild();
        pLogVerboseBox = PLogVerboseBuild();
        pLogFeed = PLogFeedBuild();

        Title = LLocalization.LLocalizationTextRead("Log.Window.Title");
        Width = 860;
        Height = 560;
        MinWidth = 640;
        MinHeight = 380;
        ResizeMode = ResizeMode.NoResize;
        PSDialog.PSDialogApply(this, new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)));
        PScrollbar.PScrollbarApply(this);
        Content = PLogContentBuild();

        LLog.LLogFilesChange += PLogFilesApply;
        LLog.LLogRowsReset += PLogRowsReset;
        LLog.LLogRowAppend += PLogRowAppend;
        LLog.LLogRowsRemove += PLogRowsRemove;
        LLog.LLogScroll += PLogFeedScroll;
        LLog.LLogErrorShow += PLogErrorShow;
        LLog.LLogCopy += PLogTextCopy;
        pLogFileCombo.SelectionChanged += PLogFileHandle;
        pLogFileCombo.DropDownOpened += PLogFilesHandle;
        LLog.LLogAttach();

        pLogFlushTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(PLogFlushMilliseconds)
        };
        pLogFlushTimer.Tick += PLogFlushHandle;
        pLogFlushTimer.Start();

        PSGrabber.PSGrabberPlacementRestore(this, PLogPlacementKey);
        pLogGrabber = new PSGrabber(this);
        pLogGrabber.PSGrabberAttach();
        pLogOwner.Closed += PLogOwnerHandle;
        PLogOwnerPlace();
        Closed += PLogCloseHandle;
    }

    public static void PLogWindowShow(Window pOwner)
    {
        PLogWindow pWindow = System.Windows.Application.Current.Windows.OfType<PLogWindow>().FirstOrDefault()
            ?? new PLogWindow(pOwner);
        pWindow.WindowState = PLook.PLookUnminimized[pWindow.WindowState];
        pWindow.Show();
        pWindow.Activate();
    }

    private void PLogOwnerPlace()
    {
        (Left, Top) = LSash.LSashCenterResolve(
            PLook.PLookManual[WindowStartupLocation],
            new LSashBounds(Left, Top, Width, Height),
            new LSashBounds(pLogOwner.Left, pLogOwner.Top, pLogOwner.ActualWidth, pLogOwner.ActualHeight));
        WindowStartupLocation = WindowStartupLocation.Manual;
    }

    private void PLogOwnerHandle(object? sender, EventArgs e) => Close();

    private void PLogFileHandle(object sender, SelectionChangedEventArgs e) =>
        LLog.LLogFileSelect(pLogFileCombo.SelectedIndex);

    private void PLogFilesHandle(object? sender, EventArgs e) => LLog.LLogFilesUpdate();

    private void PLogFlushHandle(object? sender, EventArgs e) => LLog.LLogTick();

    private void PLogFilesApply()
    {
        pLogFileCombo.Items.Clear();
        LLog.LLogFiles.ToList().ForEach(PLogItemAdd);
        pLogFileCombo.SelectedIndex = LLog.LLogFileIndex;
    }

    private void PLogItemAdd(LLogFile lFile) =>
        pLogFileCombo.Items.Add(new ComboBoxItem { Content = lFile.LLogFileLabel, Tag = lFile.LLogFilePath });

    private void PLogRowsReset()
    {
        pLogRowsShown.Clear();
        LLog.LLogRowsShown.ToList().ForEach(PLogRowAppend);
    }

    private void PLogRowAppend(LLogRow lRow) => pLogRowsShown.Add(new PLogRow(lRow));

    private void PLogRowsRemove(int pCount) => Enumerable.Range(0, pCount).ToList().ForEach(PLogRowRemove);

    private void PLogRowRemove(int pIndex) => pLogRowsShown.RemoveAt(0);

    private void PLogFeedScroll() => pLogFeed.ScrollIntoView(pLogRowsShown.LastOrDefault());

    private void PLogScrollHandle(object sender, ScrollChangedEventArgs e)
    {
        ScrollViewer? pViewer = e.OriginalSource as ScrollViewer;
        LLog.LLogScrollHandle(
            ReferenceEquals(pViewer?.TemplatedParent, pLogFeed),
            e.ExtentHeightChange,
            pViewer?.ScrollableHeight,
            pViewer?.VerticalOffset,
            PLogRow.PLogRowHeight);
    }

    private void PLogDetailToggle(object sender, RoutedEventArgs e)
    {
        PLogRow? pRow = PSender.PSenderItemRead<PLogRow>(e.OriginalSource);
        e.Handled = LLog.LLogExpandToggle(pRow?.LLogRow);
        pRow?.PLogRowUpdate();
    }

    private void PLogTextCopy(string pText)
    {
        try
        {
            Clipboard.SetText(pText);
        }
        catch (COMException pLogException)
        {
            LLog.LLogErrorRaise("Log.Error.Copy", pLogException.Message);
        }
    }

    private void PLogErrorShow(string pMessage)
    {
        Debug.WriteLine(pMessage);
        PSWarning.PSWarningShow(this, LLocalization.LLocalizationTextRead("Log.Window.Title"), pMessage);
    }

    private void PLogCloseHandle(object? sender, EventArgs e)
    {
        pLogFlushTimer.Stop();
        pLogFlushTimer.Tick -= PLogFlushHandle;
        PSGrabber.PSGrabberPlacementSave(this, PLogPlacementKey);
        pLogGrabber.PSGrabberDetach();
        LLog.LLogDetach();
        pLogFileCombo.SelectionChanged -= PLogFileHandle;
        pLogFileCombo.DropDownOpened -= PLogFilesHandle;
        Closed -= PLogCloseHandle;
        pLogOwner.Closed -= PLogOwnerHandle;
    }

    private UIElement PLogContentBuild()
    {
        var pLogBody = new DockPanel
        {
            Background = Brushes.White
        };

        DockPanel pLogFileRow = PLogFileBuild();
        DockPanel.SetDock(pLogFileRow, Dock.Top);
        pLogBody.Children.Add(pLogFileRow);

        DockPanel pLogFilterRow = PLogFilterBuild();
        DockPanel.SetDock(pLogFilterRow, Dock.Top);
        pLogBody.Children.Add(pLogFilterRow);

        pLogBody.Children.Add(new Border
        {
            BorderBrush = PSField.PSFieldLine,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(18, 0, 18, 18),
            Child = pLogFeed
        });

        return PSDialog.PSDialogBuild(
            this,
            LLocalization.LLocalizationTextRead("Log.Window.Title"),
            pLogBody);
    }

    private DockPanel PLogFileBuild()
    {
        var pLogFileRow = new DockPanel { Margin = new Thickness(18, 14, 18, 0), LastChildFill = false };

        Button pLogOpenButton = PSInline.PSInlineIconBuild(
            PLogOpenIcon,
            LLocalization.LLocalizationTextRead("Log.Button.Open"),
            new Thickness(0, 0, 8, 0));
        pLogOpenButton.Click += (_, _) => LLog.LLogFolderOpen();
        Button pLogCopyButton = PLogButtonBuild(
            LLocalization.LLocalizationTextRead("Log.Button.Copy"),
            (_, _) => LLog.LLogTextCopy());

        DockPanel.SetDock(pLogFileCombo, Dock.Left);
        DockPanel.SetDock(pLogOpenButton, Dock.Left);
        DockPanel.SetDock(pLogCopyButton, Dock.Right);
        pLogFileRow.Children.Add(pLogFileCombo);
        pLogFileRow.Children.Add(pLogOpenButton);
        pLogFileRow.Children.Add(pLogCopyButton);
        return pLogFileRow;
    }

    private DockPanel PLogFilterBuild()
    {
        var pLogFilterRow = new DockPanel { Margin = new Thickness(18, 9, 18, 12), LastChildFill = false };
        DockPanel.SetDock(pLogCategoryPicker, Dock.Left);
        DockPanel.SetDock(pLogVerboseBox, Dock.Right);
        pLogFilterRow.Children.Add(pLogCategoryPicker);
        pLogFilterRow.Children.Add(pLogVerboseBox);
        return pLogFilterRow;
    }

    private PPicker PLogCategoryBuild()
    {
        var pLogPicker = new PPicker(
            LLog.LLogCategoriesRead(),
            Array.Empty<string>(),
            LLocalization.LLocalizationTextRead("Log.Category.All"))
        {
            Width = 200,
            Height = PSField.PSFieldControlHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        pLogPicker.PPickerChange += PLogCategoryHandle;
        return pLogPicker;
    }

    private void PLogCategoryHandle() => LLog.LLogCategorySet(pLogCategoryPicker.PPickerSelectionRead());

    private ListBox PLogFeedBuild()
    {
        var pLogFeedList = new ListBox
        {
            ItemsSource = pLogRowsShown,
            BorderThickness = new Thickness(0),
            Background = Brushes.White,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(6)
        };

        PLogRow.PLogRowApply(pLogFeedList);
        ScrollViewer.SetHorizontalScrollBarVisibility(pLogFeedList, ScrollBarVisibility.Disabled);
        VirtualizingPanel.SetIsVirtualizing(pLogFeedList, true);
        VirtualizingPanel.SetVirtualizationMode(pLogFeedList, VirtualizationMode.Recycling);
        PScrollbar.PScrollbarApply(pLogFeedList);
        pLogFeedList.AddHandler(
            ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler(PLogScrollHandle));
        pLogFeedList.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(PLogDetailToggle));
        return pLogFeedList;
    }

    private CheckBox PLogVerboseBuild()
    {
        var pLogVerbose = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Log.Verbose.Label"),
            VerticalAlignment = VerticalAlignment.Center,
            IsChecked = LTrace.LTraceVerbose,
            ToolTip = LLocalization.LLocalizationTextRead("Log.Verbose.Tooltip")
        };

        PCheckbox.PCheckboxApply(pLogVerbose);
        pLogVerbose.Checked += (_, _) => LLog.LLogVerboseSet(true);
        pLogVerbose.Unchecked += (_, _) => LLog.LLogVerboseSet(false);
        return pLogVerbose;
    }

    private static ComboBox PLogComboBuild(double pLogWidth)
    {
        var pLogCombo = new ComboBox
        {
            Width = pLogWidth,
            Height = PSField.PSFieldControlHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        PDropdown.PDropdownApply(pLogCombo);
        return pLogCombo;
    }

    private static Button PLogButtonBuild(string pLogText, RoutedEventHandler pLogClick)
    {
        var pLogButton = new Button
        {
            Content = pLogText,
            MinWidth = 84,
            Height = PSField.PSFieldControlHeight,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Style = PButton.PButtonWhiteCreate()
        };
        pLogButton.Click += pLogClick;
        return pLogButton;
    }
}
