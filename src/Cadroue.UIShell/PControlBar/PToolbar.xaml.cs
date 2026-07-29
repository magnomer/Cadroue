using System.Windows;
using Cadroue.UIShell;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;

namespace Cadroue.UIShell.PControlBar;

public partial class PToolbar : UserControl
{
    private LTabset? lTabset;
    private PTabRecord? pTabDragRecord;
    private Point pTabDragStartPoint;
    private bool pTabDragActive;

    public event Action<LPreferenceState>? PToolbarOptionsApply;

    private const double PChromeButtonHeight = 56;

    private const double PChromeButtonWidth = 48;

    public PToolbar()
    {
        InitializeComponent();
        PChromeButtonsApply();
    }

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

    public void PToolbarTabsetSet(LTabset lTabsetValue)
    {
        lTabset = lTabsetValue;
        DataContext = lTabset;
    }

    private void PTabPressHandle(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PTabRecord pTabRecord })
        {
            return;
        }

        pTabDragRecord = pTabRecord;
        pTabDragStartPoint = e.GetPosition(this);
        pTabDragActive = false;
        lTabset?.LTabsetSelect(pTabRecord);
        Mouse.Capture(sender as IInputElement);
        e.Handled = true;
    }

    private void PTabMoveHandle(object sender, MouseEventArgs e)
    {
        if (pTabDragRecord is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point pCurrentPoint = e.GetPosition(this);
        if (!pTabDragActive)
        {
            double pHorizontalMove = Math.Abs(pCurrentPoint.X - pTabDragStartPoint.X);
            double pVerticalMove = Math.Abs(pCurrentPoint.Y - pTabDragStartPoint.Y);
            if (pHorizontalMove < SystemParameters.MinimumHorizontalDragDistance
                && pVerticalMove < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            pTabDragActive = true;
        }

        int pTabTargetIndex = PTabIndexResolve(e.GetPosition(pTabItemsControl));
        lTabset?.LTabsetMove(pTabDragRecord, pTabTargetIndex);
        e.Handled = true;
    }

    private void PTabReleaseHandle(object sender, MouseButtonEventArgs e)
    {
        PTabRecord? pReleasedRecord = pTabDragActive ? pTabDragRecord : null;
        Point pReleasedScreenPoint = PointToScreen(e.GetPosition(this));
        PTabDragClear();
        e.Handled = true;

        if (pReleasedRecord is not null)
        {
            PTabRelayCheck(pReleasedRecord, pReleasedScreenPoint);
        }
    }

    private void PTabDragClear()
    {
        pTabDragRecord = null;
        pTabDragActive = false;
        if (Mouse.Captured is not null)
        {
            Mouse.Capture(null);
        }
    }

    private int PTabIndexResolve(Point pTabMousePoint)
    {
        if (lTabset is null || lTabset.PTabsetRecords.Count == 0)
        {
            return 0;
        }

        int pTargetIndex = 0;
        for (int index = 0; index < lTabset.PTabsetRecords.Count; index++)
        {
            PTabRecord pTabRecord = lTabset.PTabsetRecords[index];
            if (pTabItemsControl.ItemContainerGenerator.ContainerFromItem(pTabRecord) is not FrameworkElement pItemElement)
            {
                continue;
            }

            Point pItemPoint = pItemElement.TransformToAncestor(pTabItemsControl).Transform(new Point(0, 0));
            double pItemCenterX = pItemPoint.X + pItemElement.ActualWidth / 2;
            if (pTabMousePoint.X > pItemCenterX)
            {
                pTargetIndex = index + 1;
            }
        }

        return Math.Clamp(pTargetIndex, 0, lTabset.PTabsetRecords.Count - 1);
    }

    private void PLogoClickHandle(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement pLogoButton)
        {
            return;
        }

        ContextMenu pLogoMenu = PMenu.PMenuCreate(pLogoButton);

        PLogoItemAppend(pLogoMenu, "Options", "/PAssets/PMenus/PMenuPreferences.svg");
        PLogoItemAppend(pLogoMenu, "Shortcuts", "/PAssets/PMenus/PMenuShortcuts.svg");
        PLogoItemAppend(pLogoMenu, "Log", "/PAssets/PMenus/PMenuLog.svg");
        PLogoItemAppend(pLogoMenu, "About", "/PAssets/PMenus/PMenuAbout.svg");

        pLogoMenu.IsOpen = true;
        e.Handled = true;
    }

    private void PLogoItemAppend(ContextMenu pLogoMenu, string pLogoMenuText, string pLogoMenuIconPath)
    {
        MenuItem pLogoMenuItem = PMenu.PMenuItemCreate(pLogoMenuText, PMenu.PMenuIconRead(pLogoMenuIconPath));
        if (pLogoMenuText == "Options")
        {
            pLogoMenuItem.Click += (_, _) => PToolbarOptionsShow();
        }
        else if (pLogoMenuText == "Shortcuts")
        {
            pLogoMenuItem.Click += (_, _) => PToolbarShortcutShow();
        }
        else if (pLogoMenuText == "Log")
        {
            pLogoMenuItem.Click += (_, _) => PLogWindow.PLogWindowShow(Window.GetWindow(this));
        }

        pLogoMenu.Items.Add(pLogoMenuItem);
    }

    private void PToolbarOptionsShow()
    {
        PSOptions.PSOptionsShow(Window.GetWindow(this)!, PToolbarOptionsApply);
    }

    private const string PToolbarShortcutPlacementKey = "Shortcut";

    public void PToolbarShortcutShow()
    {
        var pShortcutWindow = new Window
        {
            Title = "Shortcuts",
            Width = 640,
            Height = 520,
            MinWidth = 520,
            MinHeight = 360,
            Owner = Window.GetWindow(this),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = PShortcutContentBuild()
        };
        PSShared.PSGrabber.PSGrabberPlacementRestore(pShortcutWindow, PToolbarShortcutPlacementKey);
        pShortcutWindow.Closed += (_, _) =>
            PSShared.PSGrabber.PSGrabberPlacementSave(pShortcutWindow, PToolbarShortcutPlacementKey);
        pShortcutWindow.ShowDialog();
    }

    private static ScrollViewer PShortcutContentBuild()
    {
        var pPanel = new StackPanel { Margin = new Thickness(18) };
        pPanel.Children.Add(new TextBlock { Text = "Shortcuts", FontSize = 22, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 14) });
        foreach ((string pAction, string pShortcut, string pScope) in PShortcutRowBuild())
        {
            pPanel.Children.Add(new TextBlock
            {
                Text = $"{pShortcut,-10}  {pAction}  —  {pScope}",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 7),
                Foreground = System.Windows.Media.Brushes.Black
            });
        }

        return new ScrollViewer { Content = pPanel };
    }

    private static IEnumerable<(string Action, string Shortcut, string Scope)> PShortcutRowBuild()
    {
        yield return ("Show shortcuts", "Ctrl+/", "Global");
        yield return ("Undo", "Ctrl+Z", "Active tab");
        yield return ("Redo", "Ctrl+Y", "Active tab");
        yield return ("Play / Pause", "Space", "Global");
        yield return ("Close the media file", "F4", "Active tab");
        yield return ("Zoom in", "C", "Active flow");
        yield return ("Zoom out", "V", "Active flow");
        yield return ("Add section at cursor", "Q", "Split tab");
        yield return ("Set section start to cursor", "D", "Split tab");
        yield return ("Split section at cursor", "S", "Split tab");
        yield return ("Set section end to cursor", "F", "Split tab");
        yield return ("Delete selected section", "Delete", "Split tab");
        yield return ("Rename selected section", "A", "Split tab");
        yield return ("Move to previous keyframe", "E", "Active flow");
        yield return ("Move to nearest keyframe", "W", "Active flow");
        yield return ("Move to next keyframe", "R", "Active flow");
    }

    private void PTabMenuHandle(object sender, RoutedEventArgs e)
    {
        if (sender is not Button pTabAddButton)
        {
            return;
        }

        ContextMenu pTabAddMenu = PMenu.PMenuCreate(pTabAddButton);

        PTabMenuAppend(pTabAddMenu, "Split", PIcon.PIconRead("/PAssets/PTabs/PSplitButton.svg"));
        PTabMenuAppend(pTabAddMenu, "Edit", PIcon.PIconRead("/PAssets/PTabs/PEditButton.svg"));
        PTabMenuAppend(pTabAddMenu, "Audio", PIcon.PIconRead("/PAssets/PTabs/PAudioButton.svg"));
        PTabMenuAppend(pTabAddMenu, "Convert", PIcon.PIconRead("/PAssets/PTabs/PConvertButton.svg"));
        PTabMenuAppend(pTabAddMenu, "Merge", PIcon.PIconRead("/PAssets/PTabs/PMergeButton.svg"));
        PTabMenuAppend(pTabAddMenu, "Worklist", PIcon.PIconRead("/PAssets/PTabs/PWorklistButton.svg"));

        pTabAddMenu.IsOpen = true;
        e.Handled = true;
    }

    private void PTabMenuAppend(ContextMenu pTabAddMenu, string pTabLayoutKey, ImageSource pTabIconSource)
    {
        MenuItem pTabAddMenuItem = PMenu.PMenuItemCreate(pTabLayoutKey, pTabIconSource);
        pTabAddMenuItem.Click += (_, _) => PTabLayoutAdd(pTabLayoutKey);
        pTabAddMenu.Items.Add(pTabAddMenuItem);
    }

    private void PTabLayoutAdd(string pTabLayoutKey)
    {
        var pTabRecord = lTabset?.LTabsetAdd(pTabLayoutKey);
        if (pTabRecord is not null)
        {
            lTabset?.LTabsetSelect(pTabRecord);
        }
    }

    private void PTabCloseHandle(object sender, MouseButtonEventArgs e)
    {
        PTabDragClear();
        e.Handled = true;
        if (sender is FrameworkElement { DataContext: PTabRecord pTabRecord })
        {
            lTabset?.LTabsetClose(pTabRecord);
        }
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

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        PTabDragClear();
    }

    private static bool PToolbarButtonFind(DependencyObject? pToolbarSource)
    {
        while (pToolbarSource is not null)
        {
            if (pToolbarSource is Button)
            {
                return true;
            }

            pToolbarSource = VisualTreeHelper.GetParent(pToolbarSource);
        }

        return false;
    }

    private static bool PToolbarTabFind(DependencyObject? pToolbarSource)
    {
        while (pToolbarSource is not null)
        {
            if (pToolbarSource is FrameworkElement { DataContext: PTabRecord })
            {
                return true;
            }

            pToolbarSource = VisualTreeHelper.GetParent(pToolbarSource);
        }

        return false;
    }
}
