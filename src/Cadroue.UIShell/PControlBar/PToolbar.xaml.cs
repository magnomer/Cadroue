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

        if (e.ClickCount >= 2 && PTabNameHitCheck(sender, e))
        {
            PTabDragClear();
            pTabRecord.PTabNameActive = true;
            e.Handled = true;
            return;
        }

        pTabDragRecord = pTabRecord;
        pTabDragStartPoint = e.GetPosition(this);
        pTabDragActive = false;
        lTabset?.LTabsetSelect(pTabRecord);
        Mouse.Capture(sender as IInputElement);
        e.Handled = true;
    }

    private static bool PTabNameHitCheck(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DependencyObject pTabFrame
            || PTabNameElementFind(pTabFrame) is not { IsVisible: true } pNameText)
        {
            return false;
        }

        return pNameText.InputHitTest(e.GetPosition(pNameText)) is not null;
    }

    private static FrameworkElement? PTabNameElementFind(DependencyObject pTabFrame)
    {
        int pChildCount = VisualTreeHelper.GetChildrenCount(pTabFrame);
        for (int pChildIndex = 0; pChildIndex < pChildCount; pChildIndex++)
        {
            DependencyObject pChild = VisualTreeHelper.GetChild(pTabFrame, pChildIndex);
            if (pChild is FrameworkElement { Tag: "pTabNameText" } pNameText)
            {
                return pNameText;
            }

            if (PTabNameElementFind(pChild) is { } pFound)
            {
                return pFound;
            }
        }

        return null;
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

    private void PTabNameLoadHandle(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox pNameBox)
        {
            return;
        }

        System.Windows.Automation.AutomationProperties.SetName(
            pNameBox, LLocalization.LLocalizationTextRead("Tab.Rename.Name"));
        pNameBox.IsVisibleChanged += PTabNameVisibleHandle;
    }

    private static void PTabNameVisibleHandle(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not TextBox { DataContext: PTabRecord pTabRecord } pNameBox
            || !pNameBox.IsVisible)
        {
            return;
        }

        pNameBox.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Input,
            new Action(() =>
            {
                pNameBox.Text = pTabRecord.PTabTitle;
                pNameBox.SelectAll();
                pNameBox.Focus();
            }));
    }

    private void PTabNameKeyHandle(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: PTabRecord pTabRecord } pNameBox)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            PTabNameCommit(pTabRecord, pNameBox.Text);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            pTabRecord.PTabNameActive = false;
            e.Handled = true;
        }
    }

    private void PTabNameLeaveHandle(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: PTabRecord pTabRecord } pNameBox && pTabRecord.PTabNameActive)
        {
            PTabNameCommit(pTabRecord, pNameBox.Text);
        }
    }

    private void PTabNameCommit(PTabRecord pTabRecord, string pTabName)
    {
        pTabRecord.PTabNameActive = false;
        lTabset?.LTabsetNameSet(pTabRecord, pTabName);
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

        PLogoItemAppend(pLogoMenu, "Options", "Chrome.Menu.Options", "/PAssets/PMenus/PMenuPreferences.svg");
        PLogoItemAppend(pLogoMenu, "Shortcuts", "Chrome.Menu.Shortcuts", "/PAssets/PMenus/PMenuShortcuts.svg");
        PLogoItemAppend(pLogoMenu, "Log", "Chrome.Menu.Log", "/PAssets/PMenus/PMenuLog.svg");
        PLogoItemAppend(pLogoMenu, "About", "Chrome.Menu.About", "/PAssets/PMenus/PMenuAbout.svg");

        pLogoMenu.IsOpen = true;
        e.Handled = true;
    }

    private void PLogoItemAppend(ContextMenu pLogoMenu, string pLogoMenuToken, string pLogoMenuKey, string pLogoMenuIconPath)
    {
        MenuItem pLogoMenuItem = PMenu.PMenuItemCreate(LLocalization.LLocalizationTextRead(pLogoMenuKey), PMenu.PMenuIconRead(pLogoMenuIconPath));
        if (pLogoMenuToken == "Options")
        {
            pLogoMenuItem.Click += (_, _) => PToolbarOptionsShow();
        }
        else if (pLogoMenuToken == "Shortcuts")
        {
            pLogoMenuItem.Click += (_, _) => PToolbarShortcutShow();
        }
        else if (pLogoMenuToken == "Log")
        {
            pLogoMenuItem.Click += (_, _) => PLogWindow.PLogWindowShow(Window.GetWindow(this));
        }

        pLogoMenu.Items.Add(pLogoMenuItem);
    }

    private void PToolbarOptionsShow()
    {
        PSOptions.PSOptionsShow(Window.GetWindow(this)!, PToolbarOptionsApply);
    }

    public void PToolbarShortcutShow()
    {
        PSKeymap.PSKeymapShow(Window.GetWindow(this)!, PToolbarOptionsApply);
    }

    private void PTabMenuHandle(object sender, RoutedEventArgs e)
    {
        if (sender is not Button pTabAddButton)
        {
            return;
        }

        ContextMenu pTabAddMenu = PMenu.PMenuCreate(pTabAddButton);

        PTabMenuAppend(pTabAddMenu, "Split", "Tab.Split", PIcon.PIconRead("/PAssets/PTabs/PSplitButton.svg"));
        PTabMenuAppend(pTabAddMenu, "Edit", "Tab.Edit", PIcon.PIconRead("/PAssets/PTabs/PEditButton.svg"));
        PTabMenuAppend(pTabAddMenu, "Audio", "Tab.Audio", PIcon.PIconRead("/PAssets/PTabs/PAudioButton.svg"));
        PTabMenuAppend(pTabAddMenu, "Convert", "Tab.Convert", PIcon.PIconRead("/PAssets/PTabs/PConvertButton.svg"));
        PTabMenuAppend(pTabAddMenu, "Merge", "Tab.Merge", PIcon.PIconRead("/PAssets/PTabs/PMergeButton.svg"));
        PTabMenuAppend(pTabAddMenu, "Worklist", "Tab.Worklist", PIcon.PIconRead("/PAssets/PTabs/PWorklistButton.svg"));

        pTabAddMenu.IsOpen = true;
        e.Handled = true;
    }

    private void PTabMenuAppend(ContextMenu pTabAddMenu, string pTabLayoutKey, string pTabTitleKey, ImageSource pTabIconSource)
    {
        MenuItem pTabAddMenuItem = PMenu.PMenuItemCreate(LLocalization.LLocalizationTextRead(pTabTitleKey), pTabIconSource);
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
