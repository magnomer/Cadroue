using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PControlBar;

public sealed class PLogWindow : Window
{
    private const string PLogWindowPlacementKey = "Log";

    private static PLogWindow? pLogWindowCurrent;
    private readonly TextBox pLogText;

    private PLogWindow()
    {
        Title = "Log";
        Width = 760;
        Height = 520;
        MinWidth = 520;
        MinHeight = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        pLogText = new TextBox
        {
            Text = LAppLog.LTextRead(),
            IsReadOnly = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            BorderThickness = new Thickness(0)
        };

        Content = PLogContentBuild();
        PSShared.PSGrabber.PSGrabberPlacementRestore(this, PLogWindowPlacementKey);
        LTrace.LTraceAppend += PLogAppendHandle;
        Closed += (_, _) =>
        {
            PSShared.PSGrabber.PSGrabberPlacementSave(this, PLogWindowPlacementKey);
            LTrace.LTraceAppend -= PLogAppendHandle;
            pLogWindowCurrent = null;
        };
    }

    public static void PLogWindowShow(Window? pOwner)
    {
        if (pLogWindowCurrent is not null)
        {
            pLogWindowCurrent.Activate();
            return;
        }

        pLogWindowCurrent = new PLogWindow
        {
            Owner = pOwner
        };
        pLogWindowCurrent.Show();
    }

    private UIElement PLogContentBuild()
    {
        var pRoot = new DockPanel();
        var pToolbar = new DockPanel
        {
            Margin = new Thickness(10, 8, 10, 8)
        };

        var pVerbose = PLogVerboseBuild();
        DockPanel.SetDock(pVerbose, Dock.Left);
        pToolbar.Children.Add(pVerbose);

        var pActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        pActions.Children.Add(PLogButtonBuild("Copy", (_, _) => Clipboard.SetText(pLogText.Text)));
        pActions.Children.Add(PLogButtonBuild("Clear", (_, _) =>
        {
            LAppLog.LClear();
            pLogText.Clear();
        }));
        pToolbar.Children.Add(pActions);
        DockPanel.SetDock(pToolbar, Dock.Top);
        pRoot.Children.Add(pToolbar);
        pRoot.Children.Add(pLogText);
        return pRoot;
    }

    private static CheckBox PLogVerboseBuild()
    {
        var pVerbose = new CheckBox
        {
            Content = "Verbose",
            VerticalAlignment = VerticalAlignment.Center,
            IsChecked = LTrace.LTraceVerbose,
            ToolTip = "Record every drawing, viewer, backend and FFmpeg step. Slows the program down; use it to diagnose, not day to day."
        };

        PMainWindow.PCheckbox.PCheckboxApply(pVerbose);
        pVerbose.Checked += (_, _) => PLogVerboseSet(true);
        pVerbose.Unchecked += (_, _) => PLogVerboseSet(false);
        return pVerbose;
    }

    private static void PLogVerboseSet(bool pVerboseOn)
    {
        if (LTrace.LTraceVerbose == pVerboseOn)
        {
            return;
        }

        LTrace.LTraceVerbose = pVerboseOn;
        LPreferenceState pPreferenceNext = App.LPreferenceStateCurrent.LPreferenceClone();
        pPreferenceNext.LPreferenceLogVerbose = pVerboseOn;
        App.LPreferenceStateSet(pPreferenceNext);
    }

    private static Button PLogButtonBuild(string pText, RoutedEventHandler pClick)
    {
        var pButton = new Button
        {
            Content = pText,
            MinWidth = 72,
            Height = 28,
            Margin = new Thickness(6, 0, 0, 0)
        };
        pButton.Click += pClick;
        return pButton;
    }

    private void PLogAppendHandle(string pEntry)
    {
        if (string.IsNullOrEmpty(pEntry))
        {
            return;
        }

        Dispatcher.InvokeAsync(
            () =>
            {
                pLogText.AppendText(pEntry);
                pLogText.ScrollToEnd();
            },
            System.Windows.Threading.DispatcherPriority.Background);
    }
}
