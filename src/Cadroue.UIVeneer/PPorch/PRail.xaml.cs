using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPorch;

public partial class PRail : UserControl
{
    public const double PRailWidth = 180;

    private static readonly IReadOnlyDictionary<bool, double> pRailWidths = new Dictionary<bool, double>
    {
        [true] = PRailWidth,
        [false] = double.NaN,
    };

    private static readonly IReadOnlyDictionary<bool, double> pRailHeights = new Dictionary<bool, double>
    {
        [true] = double.NaN,
        [false] = 56,
    };

    private static readonly IReadOnlyDictionary<bool, Brush> pRailBackgrounds = new Dictionary<bool, Brush>
    {
        [true] = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFC)),
        [false] = Brushes.Transparent,
    };

    private static readonly IReadOnlyDictionary<bool, Thickness> pRailBorders = new Dictionary<bool, Thickness>
    {
        [true] = new Thickness(0, 0, 1, 0),
        [false] = new Thickness(0),
    };

    private static readonly IReadOnlyDictionary<bool, ScrollBarVisibility> pRailScrollbars =
        new Dictionary<bool, ScrollBarVisibility>
        {
            [true] = ScrollBarVisibility.Auto,
            [false] = ScrollBarVisibility.Disabled,
        };

    private static readonly IReadOnlyDictionary<bool, Orientation> pRailOrientations = new Dictionary<bool, Orientation>
    {
        [true] = Orientation.Vertical,
        [false] = Orientation.Horizontal,
    };

    private static readonly IReadOnlyDictionary<bool, string> pRailPanelKeys = new Dictionary<bool, string>
    {
        [true] = "pVerticalItemsPanel",
        [false] = "pHorizontalItemsPanel",
    };

    private static readonly IReadOnlyDictionary<bool, string> pRailTemplateKeys = new Dictionary<bool, string>
    {
        [true] = "pVerticalTabTemplate",
        [false] = "pHorizontalTabTemplate",
    };

    private static readonly IReadOnlyDictionary<bool, string> pRailAddKeys = new Dictionary<bool, string>
    {
        [true] = "pTabAddVerticalStyle",
        [false] = "pTabAddHorizontalStyle",
    };

    private static readonly IReadOnlyDictionary<Key, bool> pRailEnterKeys = new Dictionary<Key, bool>
    {
        [Key.Enter] = true,
    };

    private static readonly IReadOnlyDictionary<Key, bool> pRailEscapeKeys = new Dictionary<Key, bool>
    {
        [Key.Escape] = true,
    };

    private static readonly IReadOnlyDictionary<bool, Func<FrameworkElement, IInputElement?>> pRailCaptures =
        new Dictionary<bool, Func<FrameworkElement, IInputElement?>>
        {
            [true] = pElement => pElement,
            [false] = pElement => null,
        };

    private static readonly IReadOnlyDictionary<bool, Action<PRail, MouseEventArgs>> pRailMoves =
        new Dictionary<bool, Action<PRail, MouseEventArgs>>
        {
            [true] = (pRail, pEvent) => pRail.PTabMoveRun(pEvent),
            [false] = (pRail, pEvent) => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<PRail, FrameworkElement?>> pRailGhosts =
        new Dictionary<bool, Action<PRail, FrameworkElement?>>
        {
            [true] = (pRail, pElement) => pRail.PTabGhostShow(pElement!),
            [false] = (pRail, pElement) => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<TextBox>> pRailVisibles =
        new Dictionary<bool, Action<TextBox>>
        {
            [true] = PTabNamePrepare,
            [false] = pNameBox => { },
        };

    private static readonly IReadOnlyDictionary<LRailRelayOutcome, Action<Window>> pRailOutcomes =
        new Dictionary<LRailRelayOutcome, Action<Window>>
        {
            [LRailRelayOutcome.LRailRelaySkipped] = pWindow => { },
            [LRailRelayOutcome.LRailRelayBusy] = PTabBusyShow,
            [LRailRelayOutcome.LRailRelayRelayed] = pWindow => { },
            [LRailRelayOutcome.LRailRelayCopied] = pWindow => { },
            [LRailRelayOutcome.LRailRelayKept] = PTabKeptShow,
        };

    private readonly PStrip pStrip;
    private readonly LRail lRail;

    public PRail(PStrip pStripOwner)
    {
        pStrip = pStripOwner;
        lRail = new LRail(pStripOwner.LStrip);
        InitializeComponent();
        DataContext = pStripOwner.LStrip;
        Loaded += PRailLoadHandle;
        Unloaded += PRailUnloadHandle;
        PRailApply(false);
    }

    public void PRailApply(bool pVertical)
    {
        lRail.LStrip.LStripVerticalSet(pVertical);
        Width = pRailWidths[pVertical];
        Height = pRailHeights[pVertical];
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        pTabRoot.Background = pRailBackgrounds[pVertical];
        pTabRoot.BorderThickness = pRailBorders[pVertical];
        pTabViewport.VerticalScrollBarVisibility = pRailScrollbars[pVertical];
        pTabStack.Orientation = pRailOrientations[pVertical];
        pTabItemsControl.ItemsPanel = (ItemsPanelTemplate)FindResource(pRailPanelKeys[pVertical]);
        pTabItemsControl.ItemTemplate = (DataTemplate)FindResource(pRailTemplateKeys[pVertical]);
        pTabAddButton.Style = (Style)FindResource(pRailAddKeys[pVertical]);
    }

    private void PRailLoadHandle(object sender, RoutedEventArgs e) =>
        Window.GetWindow(this)!.PreviewMouseDown += PTabOutsideHandle;

    private void PRailUnloadHandle(object sender, RoutedEventArgs e) =>
        Window.GetWindow(this)!.PreviewMouseDown -= PTabOutsideHandle;

    private void PTabEnterHandle(object sender, MouseEventArgs e) =>
        lRail.LRailHoverSet(PSender.PSenderItemRead<LStripTab>(sender));

    private void PTabLeaveHandle(object sender, MouseEventArgs e) =>
        lRail.LRailHoverClear(PSender.PSenderItemRead<LStripTab>(sender));

    private void PTabPressHandle(object sender, MouseButtonEventArgs e)
    {
        var pTabElement = (FrameworkElement)sender;
        Point pRailPoint = e.GetPosition(this);
        Point pOffset = e.GetPosition(pTabElement);
        bool pDrag = lRail.LRailPressHandle(
            PSender.PSenderItemRead<LStripTab>(sender),
            e.ClickCount,
            PTabHitCheck(pTabElement, e),
            pRailPoint.X,
            pRailPoint.Y,
            pOffset.X,
            pOffset.Y);
        Mouse.Capture(pRailCaptures[pDrag](pTabElement));
        e.Handled = true;
    }

    private static bool PTabHitCheck(FrameworkElement pTabElement, MouseButtonEventArgs e)
    {
        var pPresenter = (ContentPresenter)pTabElement.TemplatedParent!;
        var pNameText = (FrameworkElement)pPresenter.ContentTemplate.FindName("pTabNameText", pPresenter);
        Point pPoint = e.GetPosition(pNameText);
        return LRail.LRailNameCheck(
            pNameText.IsVisible, pPoint.X, pPoint.Y, pNameText.ActualWidth, pNameText.ActualHeight);
    }

    private void PTabMoveHandle(object sender, MouseEventArgs e) =>
        pRailMoves[lRail.LRailMoveCheck(PLook.PLookPressed[e.LeftButton])](this, e);

    private void PTabMoveRun(MouseEventArgs e)
    {
        Point pRailPoint = e.GetPosition(this);
        FrameworkElement? pTabElement = Mouse.Captured as FrameworkElement;
        bool pStart = lRail.LRailDragResolve(
            pRailPoint.X,
            pRailPoint.Y,
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance);
        pRailGhosts[pStart](this, pTabElement);
        PGhost.PGhostSync(pTabElement);
        Point pItemsPoint = e.GetPosition(pTabItemsControl);
        lRail.LRailDragMove(
            lRail.LRailPointerResolve(pItemsPoint.X, pItemsPoint.Y),
            lRail.LStrip.LStripTabs.Select(PTabCenterRead).ToList());
        e.Handled = true;
    }

    private void PTabGhostShow(FrameworkElement pTabElement)
    {
        pTabElement.Opacity = 0.72;
        PGhost.PGhostShow(pTabElement, new Point(lRail.LRailOffsetX, lRail.LRailOffsetY));
    }

    private double PTabCenterRead(LStripTab lStripTab)
    {
        var pItemElement = (FrameworkElement)pTabItemsControl.ItemContainerGenerator.ContainerFromItem(lStripTab);
        Point pItemPoint = pItemElement.TransformToAncestor(pTabItemsControl).Transform(new Point(0, 0));
        return LRail.LRailCenterResolve(
            lRail.LStrip.LStripVertical,
            pItemPoint.X,
            pItemPoint.Y,
            pItemElement.ActualWidth,
            pItemElement.ActualHeight);
    }

    private void PTabReleaseHandle(object sender, MouseButtonEventArgs e)
    {
        LStripTab? lReleased = lRail.LRailReleaseResolve();
        Point pScreenPoint = PointToScreen(e.GetPosition(this));
        PTabDragClear(Mouse.Captured as FrameworkElement);
        e.Handled = true;
        PTabRelayRun(lReleased, pScreenPoint);
    }

    private async void PTabRelayRun(LStripTab? lStripTab, Point pDevicePoint)
    {
        Window pWindow = Window.GetWindow(this)!;
        Point pWindowPoint = pWindow.PointFromScreen(pDevicePoint);
        bool pInside = LRail.LRailInsideCheck(
            PLook.PLookMinimized[pWindow.WindowState],
            pWindowPoint.X,
            pWindowPoint.Y,
            pWindow.ActualWidth,
            pWindow.ActualHeight);
        Point pDipPoint = PSash.PSashDipRead(pWindow).Transform(pDevicePoint);
        LRailRelayOutcome lOutcome = await lRail.LRailRelayRun(
            lStripTab, pInside, pDipPoint.X, pDipPoint.Y, pDevicePoint.X, pDevicePoint.Y);
        pRailOutcomes[lOutcome](pWindow);
    }

    private static void PTabKeptShow(Window pWindow) =>
        PSWarning.PSWarningShow(
            pWindow,
            LLocalization.LLocalizationTextRead("Tab.Relay.KeptTitle"),
            LLocalization.LLocalizationTextRead("Tab.Relay.KeptMessage"));

    private static void PTabBusyShow(Window pWindow) =>
        PSAnnouncement.PSAnnouncementShow(
            pWindow,
            LLocalization.LLocalizationTextRead("Tab.Relay.BusyTitle"),
            LLocalization.LLocalizationTextRead("Tab.Relay.BusyMessage"));

    private void PTabLoadHandle(object sender, RoutedEventArgs e)
    {
        var pNameBox = (TextBox)sender;
        System.Windows.Automation.AutomationProperties.SetName(
            pNameBox, LLocalization.LLocalizationTextRead("Tab.Rename.Name"));
        pNameBox.IsVisibleChanged += PTabVisibleHandle;
    }

    private void PTabOutsideHandle(object sender, MouseButtonEventArgs e)
    {
        var pNameBox = Keyboard.FocusedElement as TextBox;
        lRail.LRailOutsideHandle(
            PSender.PSenderItemRead<LStripTab>(pNameBox),
            PWalk.PWalkParentCheck(e.OriginalSource as DependencyObject, pNode => ReferenceEquals(pNode, pNameBox)),
            pNameBox?.Text);
    }

    private static void PTabVisibleHandle(object sender, DependencyPropertyChangedEventArgs e)
    {
        var pNameBox = (TextBox)sender;
        pRailVisibles[pNameBox.IsVisible](pNameBox);
    }

    private static void PTabNamePrepare(TextBox pNameBox) =>
        pNameBox.Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() =>
            {
                pNameBox.Text = PSender.PSenderItemRead<LStripTab>(pNameBox)!.LStripTabTitle;
                pNameBox.SelectAll();
                pNameBox.Focus();
            }));

    private void PTabKeyHandle(object sender, KeyEventArgs e)
    {
        var pNameBox = (TextBox)sender;
        e.Handled = lRail.LRailKeyHandle(
            PSender.PSenderItemRead<LStripTab>(sender),
            pRailEnterKeys.GetValueOrDefault(e.Key),
            pRailEscapeKeys.GetValueOrDefault(e.Key),
            pNameBox.Text);
    }

    private void PTabLeaveHandle(object sender, RoutedEventArgs e) =>
        lRail.LRailLeaveHandle(PSender.PSenderItemRead<LStripTab>(sender), ((TextBox)sender).Text);

    private static void PTabDragClear(FrameworkElement? pTabElement)
    {
        pTabElement?.SetValue(OpacityProperty, 1.0);
        PGhost.PGhostClear(pTabElement);
        Mouse.Capture(null);
    }

    private void PTabMenuHandle(object sender, RoutedEventArgs e)
    {
        ContextMenu pTabAddMenu = PMenu.PMenuCreate((Button)sender);
        LStrip.LStripKeysRead().ToList().ForEach(pTabLayoutKey => PTabMenuAppend(pTabAddMenu, pTabLayoutKey));
        pTabAddMenu.IsOpen = true;
        e.Handled = true;
    }

    private void PTabMenuAppend(ContextMenu pTabAddMenu, string pTabLayoutKey)
    {
        MenuItem pTabAddMenuItem = PMenu.PMenuItemCreate(
            LStrip.LStripTitleResolve(pTabLayoutKey),
            PTabIcon.PTabIconRead(pTabLayoutKey));
        pTabAddMenuItem.Click += (_, _) => PTabLayoutAdd(pTabLayoutKey);
        pTabAddMenu.Items.Add(pTabAddMenuItem);
    }

    private void PTabLayoutAdd(string pTabLayoutKey) =>
        lRail.LStrip.LStripSelect(pStrip.PStripAdd(pTabLayoutKey));

    private void PTabCloseHandle(object sender, MouseButtonEventArgs e)
    {
        PTabDragClear(Mouse.Captured as FrameworkElement);
        e.Handled = true;
        lRail.LRailCloseHandle(PSender.PSenderItemRead<LStripTab>(sender));
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        lRail.LRailDragClear();
        PTabDragClear(e.OriginalSource as FrameworkElement);
    }
}
