using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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

    private static readonly IReadOnlyDictionary<QRailRelayOutcome, Action<Window>> pRailOutcomes =
        new Dictionary<QRailRelayOutcome, Action<Window>>
        {
            [QRailRelayOutcome.QRailRelaySkipped] = pWindow => { },
            [QRailRelayOutcome.QRailRelayBusy] = PTabBusyShow,
            [QRailRelayOutcome.QRailRelayRelayed] = pWindow => { },
            [QRailRelayOutcome.QRailRelayCopied] = pWindow => { },
            [QRailRelayOutcome.QRailRelayKept] = PTabKeptShow,
        };

    private readonly PStrip pStrip;
    private readonly QRail lRail;

    public PRail(PStrip pStripOwner)
    {
        pStrip = pStripOwner;
        lRail = pStripOwner.QStrip.QStripRail;
        InitializeComponent();
        DataContext = pStripOwner.QStrip;
        Loaded += PRailLoadHandle;
        Unloaded += PRailUnloadHandle;
        PRailApply(false);
    }

    public void PRailApply(bool pVertical)
    {
        lRail.QStrip.QStripSeparator.QStripVerticalSet(pVertical);
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
        lRail.QRailHoverSet(QSender.QSenderItemRead<QStripTab>(sender));

    private void PTabLeaveHandle(object sender, MouseEventArgs e) =>
        lRail.QRailHoverClear(QSender.QSenderItemRead<QStripTab>(sender));

    private void PTabPressHandle(object sender, MouseButtonEventArgs e)
    {
        var pTabElement = (FrameworkElement)sender;
        Point pRailPoint = e.GetPosition(this);
        Point pOffset = e.GetPosition(pTabElement);
        bool pDrag = lRail.QRailPressHandle(
            QSender.QSenderItemRead<QStripTab>(sender),
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
        return QRail.QRailNameCheck(
            pNameText.IsVisible, pPoint.X, pPoint.Y, pNameText.ActualWidth, pNameText.ActualHeight);
    }

    private void PTabMoveHandle(object sender, MouseEventArgs e) =>
        pRailMoves[lRail.QRailMoveCheck(PLook.PLookPressed[e.LeftButton])](this, e);

    private void PTabMoveRun(MouseEventArgs e)
    {
        Point pRailPoint = e.GetPosition(this);
        FrameworkElement? pTabElement = Mouse.Captured as FrameworkElement;
        bool pStart = lRail.QRailDragResolve(
            pRailPoint.X,
            pRailPoint.Y,
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance);
        pRailGhosts[pStart](this, pTabElement);
        QGhostLayer.QGhostLayerSync(pTabElement);
        Point pItemsPoint = e.GetPosition(pTabItemsControl);
        lRail.QRailDragMove(
            lRail.QRailPointerResolve(pItemsPoint.X, pItemsPoint.Y),
            lRail.QStrip.QStripTabs.Select(PTabCenterRead).ToList());
        e.Handled = true;
    }

    private void PTabGhostShow(FrameworkElement pTabElement)
    {
        pTabElement.Opacity = 0.72;
        QGhostLayer.QGhostLayerShow(pTabElement, lRail.QRailGhost);
    }

    private double PTabCenterRead(QStripTab lStripTab)
    {
        var pItemElement = (FrameworkElement)pTabItemsControl.ItemContainerGenerator.ContainerFromItem(lStripTab);
        Point pItemPoint = pItemElement.TransformToAncestor(pTabItemsControl).Transform(new Point(0, 0));
        return QRail.QRailCenterResolve(
            lRail.QStrip.QStripSeparator.QStripVertical,
            pItemPoint.X,
            pItemPoint.Y,
            pItemElement.ActualWidth,
            pItemElement.ActualHeight);
    }

    private void PTabReleaseHandle(object sender, MouseButtonEventArgs e)
    {
        QStripTab? lReleased = lRail.QRailReleaseResolve();
        Point pScreenPoint = PointToScreen(e.GetPosition(this));
        PTabDragClear(Mouse.Captured as FrameworkElement);
        e.Handled = true;
        PTabRelayRun(lReleased, pScreenPoint);
    }

    private async void PTabRelayRun(QStripTab? lStripTab, Point pDevicePoint)
    {
        Window pWindow = Window.GetWindow(this)!;
        Point pWindowPoint = pWindow.PointFromScreen(pDevicePoint);
        bool pInside = QRail.QRailInsideCheck(
            PLook.PLookMinimized[pWindow.WindowState],
            pWindowPoint.X,
            pWindowPoint.Y,
            pWindow.ActualWidth,
            pWindow.ActualHeight);
        Point pDipPoint = PSash.PSashDipRead(pWindow).Transform(pDevicePoint);
        QRailRelayOutcome lOutcome = await lRail.QRailRelayRun(
            lStripTab, pInside, pDipPoint.X, pDipPoint.Y, pDevicePoint.X, pDevicePoint.Y);
        pRailOutcomes[lOutcome](pWindow);
    }

    private static void PTabKeptShow(Window pWindow) =>
        PSWarning.PSWarningShow(
            pWindow,
            QLocalizationCatalog.QLocalizationTextRead("Tab.Relay.KeptTitle"),
            QLocalizationCatalog.QLocalizationTextRead("Tab.Relay.KeptMessage"));

    private static void PTabBusyShow(Window pWindow) =>
        PSAnnouncement.PSAnnouncementShow(
            pWindow,
            QLocalizationCatalog.QLocalizationTextRead("Tab.Relay.BusyTitle"),
            QLocalizationCatalog.QLocalizationTextRead("Tab.Relay.BusyMessage"));

    private void PTabLoadHandle(object sender, RoutedEventArgs e)
    {
        var pNameBox = (TextBox)sender;
        System.Windows.Automation.AutomationProperties.SetName(
            pNameBox, QLocalizationCatalog.QLocalizationTextRead("Tab.Rename.Name"));
        pNameBox.IsVisibleChanged += PTabVisibleHandle;
    }

    private void PTabOutsideHandle(object sender, MouseButtonEventArgs e)
    {
        var pNameBox = Keyboard.FocusedElement as TextBox;
        lRail.QRailOutsideHandle(
            QSender.QSenderItemRead<QStripTab>(pNameBox),
            QWalk.QWalkParentCheck(e.OriginalSource as DependencyObject, pNode => ReferenceEquals(pNode, pNameBox)),
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
                pNameBox.Text = QSender.QSenderItemRead<QStripTab>(pNameBox)!.QStripTabTitle;
                pNameBox.SelectAll();
                pNameBox.Focus();
            }));

    private void PTabKeyHandle(object sender, KeyEventArgs e)
    {
        var pNameBox = (TextBox)sender;
        e.Handled = lRail.QRailKeyHandle(
            QSender.QSenderItemRead<QStripTab>(sender),
            pRailEnterKeys.GetValueOrDefault(e.Key),
            pRailEscapeKeys.GetValueOrDefault(e.Key),
            pNameBox.Text);
    }

    private void PTabLeaveHandle(object sender, RoutedEventArgs e) =>
        lRail.QRailLeaveHandle(QSender.QSenderItemRead<QStripTab>(sender), ((TextBox)sender).Text);

    private static void PTabDragClear(FrameworkElement? pTabElement)
    {
        pTabElement?.SetValue(OpacityProperty, 1.0);
        QGhostLayer.QGhostLayerClear(pTabElement);
        Mouse.Capture(null);
    }

    private void PTabMenuHandle(object sender, RoutedEventArgs e)
    {
        ContextMenu pTabAddMenu = QMenu.QMenuBuild((Button)sender, QMenuSide.QMenuSideBelow);
        QStrip.QStripKeysRead().ToList().ForEach(pTabLayoutKey => PTabMenuAppend(pTabAddMenu, pTabLayoutKey));
        pTabAddMenu.IsOpen = true;
        e.Handled = true;
    }

    private void PTabMenuAppend(ContextMenu pTabAddMenu, string pTabLayoutKey)
    {
        MenuItem pTabAddMenuItem = PMenu.PMenuItemCreate(
            QStrip.QStripTitleResolve(pTabLayoutKey),
            PTabIcon.PTabIconRead(pTabLayoutKey));
        pTabAddMenuItem.Click += (_, _) => PTabLayoutAdd(pTabLayoutKey);
        pTabAddMenu.Items.Add(pTabAddMenuItem);
    }

    private void PTabLayoutAdd(string pTabLayoutKey) =>
        lRail.QStrip.QStripSelect(pStrip.PStripAdd(lRail.QStrip.QStripTabCreate(pTabLayoutKey)));

    private void PTabCloseHandle(object sender, MouseButtonEventArgs e)
    {
        PTabDragClear(Mouse.Captured as FrameworkElement);
        e.Handled = true;
        lRail.QRailCloseHandle(QSender.QSenderItemRead<QStripTab>(sender));
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        lRail.QRailDragClear();
        PTabDragClear(e.OriginalSource as FrameworkElement);
    }
}
