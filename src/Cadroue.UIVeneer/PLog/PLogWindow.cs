using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

public sealed class PLogWindow : Window
{
    private const string PLogPlacementKey = "Log";
    private const int PLogFlushMilliseconds = 200;
    private const string PLogOpenIcon = "/PAsset/PPanel/POpen.svg";

    private readonly PLogFolder pLogFolder;
    private readonly PLogFilter pLogFilter;
    private readonly PLogFeed pLogFeed;
    private readonly DispatcherTimer pLogFlushTimer;
    private readonly PSGrabber pLogGrabber;
    private readonly Window pLogOwner;

    public QLog QLog { get; }

    private PLogWindow(Window pOwner, QLog lLog)
    {
        QLog = lLog;
        pLogOwner = pOwner;
        pLogFolder = new PLogFolder(lLog);
        pLogFilter = new PLogFilter(lLog);
        pLogFeed = new PLogFeed(lLog);

        PLogLookApply();
        QScrollbar.QScrollbarAttach(this);
        Content = PLogContentBuild();

        PLogAttach();
        QLog.QLogAttach();

        pLogFlushTimer = PLogTimerCreate();
        PLogTimerAttach();
        pLogFlushTimer.Start();

        PSGrabberPlacement.PSGrabberPlacementRestore(this, PLogPlacementKey);
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
        pLogFolder.PLogFolderAttach();
        pLogFeed.PLogRowsAttach();
        QLog.QLogErrorShown += PLogErrorShow;
        QLog.QLogCopied += PLogTextCopy;
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
        pLogFolder.PLogFolderDetach();
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

    private void PLogFlushHandle(object? sender, EventArgs e) => QLog.QLogTick();

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
        PSGrabberPlacement.PSGrabberPlacementSave(this, PLogPlacementKey);
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

        DockPanel pLogFilterRow = pLogFilter.PLogFilterBuild();
        DockPanel.SetDock(pLogFilterRow, Dock.Top);
        pLogBody.Children.Add(pLogFilterRow);

        pLogBody.Children.Add(new Border
        {
            BorderBrush = PSField.PSFieldLine,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(18, 0, 18, 18),
            Child = pLogFeed.PLogFeedList
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

        ComboBox pLogFileCombo = pLogFolder.PLogFolderCombo;
        DockPanel.SetDock(pLogFileCombo, Dock.Left);
        DockPanel.SetDock(pLogOpenButton, Dock.Left);
        DockPanel.SetDock(pLogCopyButton, Dock.Right);
        pLogFileRow.Children.Add(pLogFileCombo);
        pLogFileRow.Children.Add(pLogOpenButton);
        pLogFileRow.Children.Add(pLogCopyButton);
        return pLogFileRow;
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
