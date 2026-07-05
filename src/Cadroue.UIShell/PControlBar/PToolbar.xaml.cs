using System.Windows;
using Cadroue.UIShell;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PControlBar;

public partial class PToolbar : UserControl
{
    private LTabset? lTabset;
    private PTabRecord? pTabDragRecord;
    private Point pTabDragStartPoint;
    private bool pTabDragActive;

    public event Action<LPreferenceState>? PToolbarPreferenceApply;

    public PToolbar()
    {
        InitializeComponent();
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

        var pLogoMenu = new ContextMenu
        {
            PlacementTarget = pLogoButton,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom
        };

        PLogoItemAppend(pLogoMenu, "Preferences", "/PAssets/PMenus/PMenuPreferences.png");
        PLogoItemAppend(pLogoMenu, "Shortcuts", "/PAssets/PMenus/PMenuShortcuts.png");
        PLogoItemAppend(pLogoMenu, "About", "/PAssets/PMenus/PMenuAbout.png");

        pLogoMenu.IsOpen = true;
        e.Handled = true;
    }

    private void PLogoItemAppend(ContextMenu pLogoMenu, string pLogoMenuText, string pLogoMenuIconPath)
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
            pLogoMenuItem.Click += (_, _) => PToolbarPreferenceShow();
        }
        else if (pLogoMenuText == "Shortcuts")
        {
            pLogoMenuItem.Click += (_, _) => PToolbarShortcutShow();
        }

        pLogoMenu.Items.Add(pLogoMenuItem);
    }

    private void PToolbarPreferenceShow()
    {
        PSOptions.PSOptionsShow(Window.GetWindow(this)!, PToolbarPreferenceApply);
    }

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

    private void PTabMenuHandle(object sender, RoutedEventArgs e)
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

        PTabMenuAppend(pTabAddMenu, "Split", "/PAssets/PTabs/PSplitButton.png");
        PTabMenuAppend(pTabAddMenu, "Edit", "/PAssets/PTabs/PEditButton.png");
        PTabMenuAppend(pTabAddMenu, "Audio", "/PAssets/PTabs/PAudioButton.png");
        PTabMenuAppend(pTabAddMenu, "Convert", "/PAssets/PTabs/PConvertButton.png");
        PTabMenuAppend(pTabAddMenu, "Merge", "/PAssets/PTabs/PMergeButton.png");
        PTabMenuAppend(pTabAddMenu, "Worklist", "/PAssets/PCompass/PActionAddList.png");

        pTabAddMenu.IsOpen = true;
        e.Handled = true;
    }

    private void PTabMenuAppend(ContextMenu pTabAddMenu, string pTabLayoutKey, string pTabIconPath)
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
        var pWindow = Window.GetWindow(this)!;
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
