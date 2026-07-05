using System.Windows;
using Cadroue.UIShell;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PControlBar;

public partial class PControlBar : UserControl
{
    private LTabSelect? lTabSelect;
    private PTabRecord? pTabDragRecord;
    private Point pTabDragStartPoint;
    private bool pTabDragActive;

    public event Action<LPreferenceState>? PPreferenceApplyRequest;

    public PControlBar()
    {
        InitializeComponent();
    }

    public void PControlBarTabSelectSet(LTabSelect lTabSelectValue)
    {
        lTabSelect = lTabSelectValue;
        DataContext = lTabSelect;
    }

    private void PTabButtonMouseLeftButtonDownHandle(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PTabRecord pTabRecord })
        {
            return;
        }

        pTabDragRecord = pTabRecord;
        pTabDragStartPoint = e.GetPosition(this);
        pTabDragActive = false;
        lTabSelect?.LTabSelectRequest(pTabRecord);
        Mouse.Capture(sender as IInputElement);
        e.Handled = true;
    }

    private void PTabButtonMouseMoveHandle(object sender, MouseEventArgs e)
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

        int pTabTargetIndex = PTabTargetIndexResolve(e.GetPosition(pTabItemsControl));
        lTabSelect?.LTabMoveRequest(pTabDragRecord, pTabTargetIndex);
        e.Handled = true;
    }

    private void PTabButtonMouseLeftButtonUpHandle(object sender, MouseButtonEventArgs e)
    {
        PTabDragClear();
        e.Handled = true;
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

    private int PTabTargetIndexResolve(Point pTabMousePoint)
    {
        if (lTabSelect is null || lTabSelect.PTabRecords.Count == 0)
        {
            return 0;
        }

        int pTargetIndex = 0;
        for (int index = 0; index < lTabSelect.PTabRecords.Count; index++)
        {
            PTabRecord pTabRecord = lTabSelect.PTabRecords[index];
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

        return Math.Clamp(pTargetIndex, 0, lTabSelect.PTabRecords.Count - 1);
    }

    private void PLogoButtonClickHandle(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement pLogoButton)
        {
            return;
        }

        var pLogoMenu = new ContextMenu
        {
            PlacementTarget = pLogoButton,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom
        };

        PLogoMenuItemAppend(pLogoMenu, "Preferences", "/PAssets/PMenus/PMenuPreferences.png");
        PLogoMenuItemAppend(pLogoMenu, "Shortcuts", "/PAssets/PMenus/PMenuShortcuts.png");
        PLogoMenuItemAppend(pLogoMenu, "About", "/PAssets/PMenus/PMenuAbout.png");

        pLogoMenu.IsOpen = true;
        e.Handled = true;
    }

    private void PLogoMenuItemAppend(ContextMenu pLogoMenu, string pLogoMenuText, string pLogoMenuIconPath)
    {
        var pLogoMenuItem = new MenuItem
        {
            Header = pLogoMenuText,
            Icon = new Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(pLogoMenuIconPath, UriKind.Relative)),
                Width = 20,
                Height = 20,
                Stretch = Stretch.Uniform
            },
            Foreground = System.Windows.Media.Brushes.Black
        };
        if (pLogoMenuText == "Preferences")
        {
            pLogoMenuItem.Click += (_, _) => PControlBarPreferenceDialogShow();
        }
        else if (pLogoMenuText == "Shortcuts")
        {
            pLogoMenuItem.Click += (_, _) => PControlBarShortcutDialogShow();
        }

        pLogoMenu.Items.Add(pLogoMenuItem);
    }

    private void PControlBarPreferenceDialogShow()
    {
        PSPreference.PSPreferenceShow(Window.GetWindow(this)!, PPreferenceApplyRequest);
    }

    public void PControlBarShortcutDialogShow()
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
            Content = PControlBarShortcutDialogContentBuild()
        };
        pShortcutWindow.ShowDialog();
    }

    private static ScrollViewer PControlBarShortcutDialogContentBuild()
    {
        var pPanel = new StackPanel { Margin = new Thickness(18) };
        pPanel.Children.Add(new TextBlock { Text = "Shortcuts", FontSize = 22, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 14) });
        foreach ((string pAction, string pShortcut, string pScope) in PControlBarShortcutRows())
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

    private static IEnumerable<(string Action, string Shortcut, string Scope)> PControlBarShortcutRows()
    {
        yield return ("Show shortcuts", "Ctrl+/", "Global");
        yield return ("Play / Pause", "Space", "Global");
        yield return ("Zoom in", "C", "Active flow");
        yield return ("Zoom out", "V", "Active flow");
        yield return ("Add section at cursor", "Q", "Split tab");
        yield return ("Set section start to cursor", "D", "Split tab");
        yield return ("Split section at cursor", "S", "Split tab");
        yield return ("Set section end to cursor", "F", "Split tab");
        yield return ("Delete selected section", "Delete", "Split tab");
        yield return ("Move to previous keyframe", "E", "Active flow");
        yield return ("Move to nearest keyframe", "W", "Active flow");
        yield return ("Move to next keyframe", "R", "Active flow");
    }

    private void PTabAddButtonClickHandle(object sender, RoutedEventArgs e)
    {
        if (sender is not Button pTabAddButton)
        {
            return;
        }

        var pTabAddMenu = new ContextMenu
        {
            PlacementTarget = pTabAddButton,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom
        };

        PTabAddMenuItemAppend(pTabAddMenu, "Split", "/PAssets/PTabs/PSplitButton.png");
        PTabAddMenuItemAppend(pTabAddMenu, "Edit", "/PAssets/PTabs/PEditButton.png");
        PTabAddMenuItemAppend(pTabAddMenu, "Audio", "/PAssets/PTabs/PAudioButton.png");
        PTabAddMenuItemAppend(pTabAddMenu, "Convert", "/PAssets/PTabs/PConvertButton.png");
        PTabAddMenuItemAppend(pTabAddMenu, "Merge", "/PAssets/PTabs/PMergeButton.png");
        PTabAddMenuItemAppend(pTabAddMenu, "Worklist", "/PAssets/PCompass/PActionAddList.png");

        pTabAddMenu.IsOpen = true;
        e.Handled = true;
    }

    private void PTabAddMenuItemAppend(ContextMenu pTabAddMenu, string pTabLayoutKey, string pTabIconPath)
    {
        var pTabAddMenuItem = new MenuItem
        {
            Header = pTabLayoutKey,
            Icon = new Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(pTabIconPath, UriKind.Relative)),
                Width = 20,
                Height = 20,
                Stretch = Stretch.Uniform
            },
            Foreground = System.Windows.Media.Brushes.Black
        };

        pTabAddMenuItem.Click += (_, _) => PTabAddRequestHandle(pTabLayoutKey);
        pTabAddMenu.Items.Add(pTabAddMenuItem);
    }

    private void PTabAddRequestHandle(string pTabLayoutKey)
    {
        var pTabRecord = lTabSelect?.LTabAddRequest(pTabLayoutKey);
        if (pTabRecord is not null)
        {
            lTabSelect?.LTabSelectRequest(pTabRecord);
        }
    }

    private void PTabCloseButtonClickHandle(object sender, MouseButtonEventArgs e)
    {
        PTabDragClear();
        e.Handled = true;
        if (sender is FrameworkElement { DataContext: PTabRecord pTabRecord })
        {
            lTabSelect?.LTabCloseRequest(pTabRecord);
        }
    }

    private void PWindowMinimizeButtonClickHandle(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)!.WindowState = WindowState.Minimized;
    }

    private void PWindowMaximizeButtonClickHandle(object sender, RoutedEventArgs e)
    {
        var pWindow = Window.GetWindow(this)!;
        pWindow.WindowState = pWindow.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void PWindowCloseButtonClickHandle(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)!.Close();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (PControlBarButtonFind(e.OriginalSource as DependencyObject)
            || PControlBarTabFind(e.OriginalSource as DependencyObject))
        {
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

    private static bool PControlBarButtonFind(DependencyObject? pControlBarSource)
    {
        while (pControlBarSource is not null)
        {
            if (pControlBarSource is Button)
            {
                return true;
            }

            pControlBarSource = VisualTreeHelper.GetParent(pControlBarSource);
        }

        return false;
    }

    private static bool PControlBarTabFind(DependencyObject? pControlBarSource)
    {
        while (pControlBarSource is not null)
        {
            if (pControlBarSource is FrameworkElement { DataContext: PTabRecord })
            {
                return true;
            }

            pControlBarSource = VisualTreeHelper.GetParent(pControlBarSource);
        }

        return false;
    }
}
