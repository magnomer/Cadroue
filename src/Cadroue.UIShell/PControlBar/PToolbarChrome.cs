using System.Windows;
using Cadroue.UIShell;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIShell.PControlBar;

public partial class PToolbar
{
    private const double PChromeButtonHeight = 56;

    private const double PChromeButtonWidth = 48;

    private void PChromeButtonsApply()
    {
        PChromeButtonApply(pChromeMinimizeButton, false);
        PChromeButtonApply(pChromeMaximizeButton, false);
        PChromeButtonApply(pChromeCloseButton, true);
    }

    private static void PChromeButtonApply(Button pChromeButton, bool pChromeClose)
    {
        pChromeButton.Width = PChromeButtonWidth;
        pChromeButton.Height = PChromeButtonHeight;
        pChromeButton.Style = PMainWindow.PButton.PButtonChromeCreate(pChromeClose);
    }

    private void PChromeMinimizeHandle(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)!.WindowState = WindowState.Minimized;
    }

    private void PChromeMaximizeHandle(object sender, RoutedEventArgs e)
    {
        PChromeMaximizeToggle();
    }

    private void PChromeMaximizeToggle()
    {
        var pWindow = Window.GetWindow(this);
        if (pWindow is null)
        {
            return;
        }

        pWindow.WindowState = pWindow.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void PChromeCloseHandle(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)!.Close();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (PToolbarButtonFind(e.OriginalSource as DependencyObject)
            || PToolbarTabFind(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            PChromeMaximizeToggle();
            e.Handled = true;
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            Window.GetWindow(this)?.DragMove();
        }
    }
}
