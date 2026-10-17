using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
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
    private readonly Control pLogCategoryPicker;
    private readonly CheckBox pLogVerboseBox;
    private readonly DispatcherTimer pLogFlushTimer;
    private readonly PSGrabber pLogGrabber;
    private readonly Window pLogOwner;

    public QLog QLog { get; }

    private PLogWindow(Window pOwner, QLog lLog)
    {
        QLog = lLog;
        pLogOwner = pOwner;
        pLogFileCombo = PLogComboBuild(320);
        pLogCategoryPicker = PLogCategoryBuild();
        pLogVerboseBox = PLogVerboseBuild();
        pLogFeed = PLogFeedBuild();

        PLogLookApply();
        QScrollbar.QScrollbarAttach(this);
        Content = PLogContentBuild();

        PLogAttach();
        QLog.QLogAttach();

        pLogFlushTimer = PLogTimerCreate();
        PLogTimerAttach();
        pLogFlushTimer.Start();

        PSGrabber.PSGrabberPlacementRestore(this, PLogPlacementKey);
        pLogGrabber = new PSGrabber(this);
        pLogGrabber.PSGrabberAttach();
        pLogOwner.Closed += PLogOwnerHandle;
        PLogOwnerPlace();
        PLogCloseAttach();
    }

    private void PLogLookApply()
    {
        Title = QLocalizationCatalog.QLocalizationTextRead("Log.Window.Title");
        Width = 860;
        Height = 560;
        MinWidth = 640;
        MinHeight = 380;
        ResizeMode = ResizeMode.NoResize;
        PSDialog.PSDialogApply(this, new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)));
    }

    private void PLogAttach()
    {
        QLog.QLogFilesChanged += PLogFilesApply;
        QLog.QLogRowsRebuilt += PLogRowsReset;
        QLog.QLogRowAppended += PLogRowAppend;
        QLog.QLogRowsRemoved += PLogRowsRemove;
        QLog.QLogScrolled += PLogFeedScroll;
        QLog.QLogErrorShown += PLogErrorShow;
        QLog.QLogCopied += PLogTextCopy;
        pLogFileCombo.SelectionChanged += PLogFileHandle;
        pLogFileCombo.DropDownOpened += PLogFilesHandle;
    }

    private static DispatcherTimer PLogTimerCreate() => new(DispatcherPriority.Background)
    {
        Interval = TimeSpan.FromMilliseconds(PLogFlushMilliseconds)
    };

    private void PLogTimerAttach() => pLogFlushTimer.Tick += PLogFlushHandle;

    private void PLogTimerDetach() => pLogFlushTimer.Tick -= PLogFlushHandle;

    private void PLogCloseAttach() => Closed += PLogCloseHandle;

    private void PLogDetach()
    {
        pLogFileCombo.SelectionChanged -= PLogFileHandle;
        pLogFileCombo.DropDownOpened -= PLogFilesHandle;
        Closed -= PLogCloseHandle;
        pLogOwner.Closed -= PLogOwnerHandle;
    }

    public static void PLogWindowShow(Window pOwner, Func<QLog> lLogCreate)
    {
        PLogWindow pWindow = System.Windows.Application.Current.Windows.OfType<PLogWindow>().FirstOrDefault()
            ?? new PLogWindow(pOwner, lLogCreate());
        pWindow.PLogFrontShow();
    }

    private void PLogFrontShow()
    {
        WindowState = PLook.PLookUnminimized[WindowState];
        Show();
        Activate();
    }

    private void PLogOwnerPlace()
    {
        (Left, Top) = QSashCenter.QSashCenterResolve(
            PLook.PLookManual[WindowStartupLocation],
            new QSashBounds(Left, Top, Width, Height),
            new QSashBounds(pLogOwner.Left, pLogOwner.Top, pLogOwner.ActualWidth, pLogOwner.ActualHeight));
        WindowStartupLocation = WindowStartupLocation.Manual;
    }

    private void PLogOwnerHandle(object? sender, EventArgs e) => Close();

    private void PLogFileHandle(object sender, SelectionChangedEventArgs e) =>
        QLog.QLogFileSelect(pLogFileCombo.SelectedIndex);

    private void PLogFilesHandle(object? sender, EventArgs e) => QLog.QLogFilesUpdate();

    private void PLogFlushHandle(object? sender, EventArgs e) => QLog.QLogTick();

    private void PLogFilesApply()
    {
        pLogFileCombo.Items.Clear();
        QLog.QLogFiles.ToList().ForEach(PLogItemAdd);
        pLogFileCombo.SelectedIndex = QLog.QLogFileIndex;
    }

    private void PLogItemAdd(QLogFile lFile) =>
        pLogFileCombo.Items.Add(new ComboBoxItem { Content = lFile.QLogFileLabel, Tag = lFile.QLogFilePath });

    private void PLogRowsReset()
    {
        pLogRowsShown.Clear();
        QLog.QLogRowsShown.ToList().ForEach(PLogRowAppend);
    }

    private void PLogRowAppend(QLogRow lRow) => pLogRowsShown.Add(new PLogRow(lRow));

    private void PLogRowsRemove(int pCount) => Enumerable.Range(0, pCount).ToList().ForEach(PLogRowRemove);

    private void PLogRowRemove(int pIndex) => pLogRowsShown.RemoveAt(0);

    private void PLogFeedScroll() => pLogFeed.ScrollIntoView(pLogRowsShown.LastOrDefault());

    private void PLogScrollHandle(object sender, ScrollChangedEventArgs e)
    {
        ScrollViewer? pViewer = e.OriginalSource as ScrollViewer;
        QLog.QLogScrollHandle(
            ReferenceEquals(pViewer?.TemplatedParent, pLogFeed),
            e.ExtentHeightChange,
            pViewer?.ScrollableHeight,
            pViewer?.VerticalOffset,
            PLogRow.PLogRowHeight);
    }

    private void PLogDetailToggle(object sender, RoutedEventArgs e)
    {
        PLogRow? pRow = QSender.QSenderItemRead<PLogRow>(e.OriginalSource);
        e.Handled = QLog.QLogExpandToggle(pRow?.QLogRow);
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
            QLog.QLogErrorRaise("Log.Error.Copy", pLogException.Message);
        }
    }

    private void PLogErrorShow(string pMessage)
    {
        PSWarning.PSWarningShow(this, QLocalizationCatalog.QLocalizationTextRead("Log.Window.Title"), pMessage);
    }

    private void PLogCloseHandle(object? sender, EventArgs e)
    {
        pLogFlushTimer.Stop();
        PLogTimerDetach();
        PSGrabber.PSGrabberPlacementSave(this, PLogPlacementKey);
        pLogGrabber.PSGrabberDetach();
        QLog.QLogDetach();
        PLogDetach();
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
            QLocalizationCatalog.QLocalizationTextRead("Log.Window.Title"),
            pLogBody);
    }

    private DockPanel PLogFileBuild()
    {
        var pLogFileRow = new DockPanel { Margin = new Thickness(18, 14, 18, 0), LastChildFill = false };

        Button pLogOpenButton = PSInline.PSInlineIconBuild(
            PLogOpenIcon,
            QLocalizationCatalog.QLocalizationTextRead("Log.Button.Open"),
            new Thickness(0, 0, 8, 0));
        PLogClickAttach(pLogOpenButton, (_, _) => QLog.QLogFolderOpen());
        Button pLogCopyButton = PLogButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Log.Button.Copy"),
            (_, _) => QLog.QLogTextCopy());

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

    private Control PLogCategoryBuild()
    {
        var pLogPicker = new Control
        {
            Width = 200,
            Height = PSField.PSFieldControlHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        QPickerControl.QPickerControlAttach(
            pLogPicker,
            QPicker.QPickerItemsCreate(QLog.QLogCategoriesRead(), Array.Empty<string>()),
            QLocalizationCatalog.QLocalizationTextRead("Log.Category.All"),
            QLog.QLogCategorySet);
        return pLogPicker;
    }

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
        QScrollbar.QScrollbarAttach(pLogFeedList);
        PLogFeedAttach(pLogFeedList);
        return pLogFeedList;
    }

    private void PLogFeedAttach(ListBox pLogFeedList)
    {
        pLogFeedList.AddHandler(
            ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler(PLogScrollHandle));
        pLogFeedList.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(PLogDetailToggle));
    }

    private CheckBox PLogVerboseBuild()
    {
        var pLogVerbose = new CheckBox
        {
            Content = QLocalizationCatalog.QLocalizationTextRead("Log.Verbose.Label"),
            VerticalAlignment = VerticalAlignment.Center,
            IsChecked = LTrace.LTraceVerbose,
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Log.Verbose.Tooltip")
        };

        QInteractionClick.QInteractionClickAttach(pLogVerbose);
        QCheckbox.QCheckboxAttach(pLogVerbose);
        PLogVerboseAttach(pLogVerbose);
        return pLogVerbose;
    }

    private void PLogVerboseAttach(CheckBox pLogVerbose)
    {
        pLogVerbose.Checked += (_, _) => QLog.QLogVerboseSet(true);
        pLogVerbose.Unchecked += (_, _) => QLog.QLogVerboseSet(false);
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
        QDropdown.QDropdownAttach(pLogCombo);
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
            VerticalAlignment = VerticalAlignment.Center
        };
        PLogClickAttach(pLogButton, pLogClick);
        QButton.QButtonAttach(pLogButton, QButtonLook.QButtonLookWhite);
        return pLogButton;
    }

    private static void PLogClickAttach(Button pLogButton, RoutedEventHandler pLogClick) =>
        pLogButton.Click += pLogClick;
}
