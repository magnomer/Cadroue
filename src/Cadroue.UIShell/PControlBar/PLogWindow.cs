using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PControlBar;

public sealed class PLogWindow : Window
{
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
        LAppLog.LLogAppend += PLogAppendHandle;
        Closed += (_, _) =>
        {
            LAppLog.LLogAppend -= PLogAppendHandle;
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
        var pToolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(10, 8, 10, 8),
            HorizontalAlignment = HorizontalAlignment.Right
        };

        pToolbar.Children.Add(PLogButtonBuild("Copy", (_, _) => Clipboard.SetText(pLogText.Text)));
        pToolbar.Children.Add(PLogButtonBuild("Clear", (_, _) =>
        {
            LAppLog.LClear();
            pLogText.Clear();
        }));
        DockPanel.SetDock(pToolbar, Dock.Top);
        pRoot.Children.Add(pToolbar);
        pRoot.Children.Add(pLogText);
        return pRoot;
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
        Dispatcher.Invoke(() =>
        {
            if (string.IsNullOrEmpty(pEntry))
            {
                pLogText.Clear();
                return;
            }

            pLogText.AppendText(pEntry);
            pLogText.ScrollToEnd();
        });
    }
}
