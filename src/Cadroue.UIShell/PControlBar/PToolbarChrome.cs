using System;
using System.Windows;
using Cadroue.UIShell;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PSShared;

namespace Cadroue.UIShell.PControlBar;

public partial class PToolbar
{
    private const double PChromeButtonHeight = 56;

    private const double PChromeButtonWidth = 48;

    private Point pChromePressPoint;

    private bool pChromePressArmed;

    private bool pChromeDragActive;

    private void PChromeButtonsApply()
    {
        PChromeButtonApply(pChromeMinimizeButton, false, new CornerRadius(0, 0, 0, 9));
        PChromeButtonApply(pChromeMaximizeButton, false, new CornerRadius(0));
        PChromeButtonApply(pChromeCloseButton, true, new CornerRadius(0, 9, 9, 0));
    }

    private static void PChromeButtonApply(Button pChromeButton, bool pChromeClose, CornerRadius pChromeCornerRadius)
    {
        pChromeButton.Width = PChromeButtonWidth;
        pChromeButton.Height = PChromeButtonHeight;
        pChromeButton.Style = PMainWindow.PButton.PButtonChromeCreate(pChromeClose, pChromeCornerRadius);
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

        if (e.ClickCount == 2 && PChromeCaptionCheck(e.OriginalSource as DependencyObject))
        {
            PChromeMaximizeToggle();
            e.Handled = true;
        }
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        pChromePressArmed = false;
        pChromeDragActive = false;
        if (!PChromeCaptionCheck(e.OriginalSource as DependencyObject))
        {
            return;
        }

        pChromePressPoint = e.GetPosition(this);
        pChromePressArmed = true;
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        if (!pChromePressArmed || pChromeDragActive || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point pChromePoint = e.GetPosition(this);
        if (Math.Abs(pChromePoint.X - pChromePressPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(pChromePoint.Y - pChromePressPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        pChromeDragActive = true;
        if (Mouse.Captured is not null)
        {
            Mouse.Capture(null);
        }

        if (Window.GetWindow(this) is { } pWindow)
        {
            PSWindowManagement.PSWindowDragMove(pWindow, e);
        }
        pChromePressArmed = false;
        pChromeDragActive = false;
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        bool pChromeClicked = pChromePressArmed && !pChromeDragActive;
        pChromePressArmed = false;
        pChromeDragActive = false;
        if (pChromeClicked && PChromeLogoCheck(e.OriginalSource as DependencyObject))
        {
            PLogoMenuShow(pLogoHost);
        }
    }

    private bool PChromeCaptionCheck(DependencyObject? pChromeSource)
    {
        if (PSWindowManagement.PSWindowInteractiveCheck(pChromeSource))
        {
            return false;
        }

        while (pChromeSource is not null)
        {
            if (pChromeSource is FrameworkElement { DataContext: PTabRecord })
            {
                return false;
            }

            if (ReferenceEquals(pChromeSource, this))
            {
                return true;
            }

            pChromeSource = VisualTreeHelper.GetParent(pChromeSource);
        }

        return false;
    }

    private bool PChromeLogoCheck(DependencyObject? pChromeSource)
    {
        while (pChromeSource is not null)
        {
            if (ReferenceEquals(pChromeSource, pLogoHost))
            {
                return true;
            }

            pChromeSource = VisualTreeHelper.GetParent(pChromeSource);
        }

        return false;
    }
}
