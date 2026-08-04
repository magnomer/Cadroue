using System.Windows;
using Cadroue.UIShell;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;

namespace Cadroue.UIShell.PControlBar;

public partial class PTabNavigator : UserControl
{
    public const double PTabRailWidth = 180;

    private LTabset? lTabset;
    private PTabRecord? pTabDragItem;
    private Point pTabDragPoint;
    private bool pTabDragActive;
    private bool pTabVertical;

    public PTabNavigator()
    {
        InitializeComponent();
        PTabNavigatorVerticalSet(false);
    }

    public void PTabNavigatorTabsetSet(LTabset pTabset)
    {
        lTabset = pTabset;
        DataContext = pTabset;
    }

    public void PTabNavigatorVerticalSet(bool pVertical)
    {
        pTabVertical = pVertical;
        Width = pVertical ? PTabRailWidth : double.NaN;
        Height = pVertical ? double.NaN : 56;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        pTabRoot.Background = pVertical
            ? new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFC))
            : Brushes.Transparent;
        pTabRoot.BorderThickness = pVertical ? new Thickness(0, 0, 1, 0) : new Thickness(0);
        pTabScroll.VerticalScrollBarVisibility = pVertical ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
        pTabStack.Orientation = pVertical ? Orientation.Vertical : Orientation.Horizontal;
        pTabItemsControl.ItemsPanel = (ItemsPanelTemplate)FindResource(
            pVertical ? "pVerticalItemsPanel" : "pHorizontalItemsPanel");
        pTabItemsControl.ItemTemplate = (DataTemplate)FindResource(
            pVertical ? "pVerticalTabTemplate" : "pHorizontalTabTemplate");
        pTabAddButton.Style = (Style)FindResource(
            pVertical ? "pTabAddVerticalStyle" : "pTabAddHorizontalStyle");
    }

    private void PTabPressHandle(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PTabRecord pTabRecord })
        {
            return;
        }

        if (e.ClickCount >= 2 && PTabHitCheck(sender, e))
        {
            PTabDragClear();
            pTabRecord.PTabNameActive = true;
            e.Handled = true;
            return;
        }

        pTabDragItem = pTabRecord;
        pTabDragPoint = e.GetPosition(this);
        pTabDragActive = false;
        lTabset?.LTabsetSelect(pTabRecord);
        Mouse.Capture(sender as IInputElement);
        e.Handled = true;
    }

    private static bool PTabHitCheck(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DependencyObject pTabFrame
            || PTabElementFind(pTabFrame) is not { IsVisible: true } pNameText)
        {
            return false;
        }

        return pNameText.InputHitTest(e.GetPosition(pNameText)) is not null;
    }

    private static FrameworkElement? PTabElementFind(DependencyObject pTabFrame)
    {
        int pChildCount = VisualTreeHelper.GetChildrenCount(pTabFrame);
        for (int pChildIndex = 0; pChildIndex < pChildCount; pChildIndex++)
        {
            DependencyObject pChild = VisualTreeHelper.GetChild(pTabFrame, pChildIndex);
            if (pChild is FrameworkElement { Tag: "pTabNameText" } pNameText)
            {
                return pNameText;
            }

            if (PTabElementFind(pChild) is { } pFound)
            {
                return pFound;
            }
        }

        return null;
    }

    private void PTabMoveHandle(object sender, MouseEventArgs e)
    {
        if (pTabDragItem is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point pCurrentPoint = e.GetPosition(this);
        if (!pTabDragActive)
        {
            double pHorizontalMove = Math.Abs(pCurrentPoint.X - pTabDragPoint.X);
            double pVerticalMove = Math.Abs(pCurrentPoint.Y - pTabDragPoint.Y);
            if (pHorizontalMove < SystemParameters.MinimumHorizontalDragDistance
                && pVerticalMove < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            pTabDragActive = true;
        }

        int pTabTargetIndex = PTabIndexResolve(e.GetPosition(pTabItemsControl));
        lTabset?.LTabsetMove(pTabDragItem, pTabTargetIndex);
        e.Handled = true;
    }

    private void PTabReleaseHandle(object sender, MouseButtonEventArgs e)
    {
        PTabRecord? pReleasedRecord = pTabDragActive ? pTabDragItem : null;
        Point pReleasedScreenPoint = PointToScreen(e.GetPosition(this));
        PTabDragClear();
        e.Handled = true;

        if (pReleasedRecord is not null)
        {
            PTabRelayCheck(pReleasedRecord, pReleasedScreenPoint);
        }
    }

    private void PTabLoadHandle(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox pNameBox)
        {
            return;
        }

        System.Windows.Automation.AutomationProperties.SetName(
            pNameBox, LLocalization.LLocalizationTextRead("Tab.Rename.Name"));
        pNameBox.IsVisibleChanged += PTabVisibleHandle;

        Window? pTabWindow = null;
        MouseButtonEventHandler pOutsideHandle = (_, pDownEvent) =>
        {
            if (pNameBox.DataContext is not PTabRecord pTabRecord || !pTabRecord.PTabNameActive)
            {
                return;
            }

            if (PTabInsideCheck(pDownEvent.OriginalSource as DependencyObject, pNameBox))
            {
                return;
            }

            PTabNameCommit(pTabRecord, pNameBox.Text);
        };
        pNameBox.IsVisibleChanged += (_, _) =>
        {
            if (pNameBox.IsVisible)
            {
                pTabWindow = Window.GetWindow(pNameBox);
                if (pTabWindow is not null)
                {
                    pTabWindow.PreviewMouseDown += pOutsideHandle;
                }
            }
            else if (pTabWindow is not null)
            {
                pTabWindow.PreviewMouseDown -= pOutsideHandle;
            }
        };
    }

    private static bool PTabInsideCheck(DependencyObject? pSource, DependencyObject pTarget)
    {
        while (pSource is not null)
        {
            if (ReferenceEquals(pSource, pTarget))
            {
                return true;
            }

            pSource = pSource is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(pSource)
                : LogicalTreeHelper.GetParent(pSource);
        }

        return false;
    }

    private static void PTabVisibleHandle(object sender, DependencyPropertyChangedEventArgs e)
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

    private void PTabKeyHandle(object sender, KeyEventArgs e)
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

    private void PTabLeaveHandle(object sender, RoutedEventArgs e)
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
        pTabDragItem = null;
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
            double pItemCenter = pTabVertical
                ? pItemPoint.Y + pItemElement.ActualHeight / 2
                : pItemPoint.X + pItemElement.ActualWidth / 2;
            double pPointer = pTabVertical ? pTabMousePoint.Y : pTabMousePoint.X;
            if (pPointer > pItemCenter)
            {
                pTargetIndex = index + 1;
            }
        }

        return Math.Clamp(pTargetIndex, 0, lTabset.PTabsetRecords.Count - 1);
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
        PTabMenuAppend(pTabAddMenu, "Funnel", "Tab.Funnel", PIcon.PIconRead("/PAssets/PTabs/PFunnelButton.svg"));
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

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        PTabDragClear();
    }
}
