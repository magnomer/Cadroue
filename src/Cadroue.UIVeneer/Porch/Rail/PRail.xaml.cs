using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

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

    private readonly QRail lRail;
    private readonly PRailName pRailName;
    private readonly PRailMenu pRailMenu;
    private readonly PRailDrag pRailDrag;

    public PRail(PStrip pStripOwner)
    {
        lRail = pStripOwner.QStrip.QStripRail;
        pRailName = new PRailName(lRail);
        pRailMenu = new PRailMenu(pStripOwner, lRail);
        InitializeComponent();
        pRailDrag = new PRailDrag(lRail, this, pTabItemsControl);
        DataContext = pStripOwner.QStrip;
        PRailAttach();
        PRailApply(false);
    }

    private void PRailAttach()
    {
        Loaded += PRailLoadHandle;
        Unloaded += PRailUnloadHandle;
    }

    public void PRailApply(bool pVertical)
    {
        lRail.QStrip.QStripSeparator.QStripVerticalSet(pVertical);
        PRailLookApply(pVertical);
        PRailTemplateApply(pVertical);
    }

    private void PRailLookApply(bool pVertical)
    {
        Width = pRailWidths[pVertical];
        Height = pRailHeights[pVertical];
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        pTabRoot.Background = pRailBackgrounds[pVertical];
        pTabRoot.BorderThickness = pRailBorders[pVertical];
        pTabViewport.VerticalScrollBarVisibility = pRailScrollbars[pVertical];
        pTabStack.Orientation = pRailOrientations[pVertical];
    }

    private void PRailTemplateApply(bool pVertical)
    {
        pTabItemsControl.ItemsPanel = (ItemsPanelTemplate)FindResource(pRailPanelKeys[pVertical]);
        pTabItemsControl.ItemTemplate = (DataTemplate)FindResource(pRailTemplateKeys[pVertical]);
        pTabAddButton.Style = (Style)FindResource(pRailAddKeys[pVertical]);
    }

    private void PRailLoadHandle(object sender, RoutedEventArgs e) =>
        Window.GetWindow(this)!.PreviewMouseDown += pRailName.PTabOutsideHandle;

    private void PRailUnloadHandle(object sender, RoutedEventArgs e) =>
        Window.GetWindow(this)!.PreviewMouseDown -= pRailName.PTabOutsideHandle;

    private void PTabEnterHandle(object sender, MouseEventArgs e) =>
        lRail.QRailHoverSet(QSender.QSenderItemRead<QStripTab>(sender));

    private void PTabLeaveHandle(object sender, MouseEventArgs e) =>
        lRail.QRailHoverClear(QSender.QSenderItemRead<QStripTab>(sender));

    private void PTabPressHandle(object sender, MouseButtonEventArgs e) => pRailDrag.PTabPressHandle(sender, e);

    private void PTabMoveHandle(object sender, MouseEventArgs e) => pRailDrag.PTabMoveHandle(e);

    private void PTabReleaseHandle(object sender, MouseButtonEventArgs e) => pRailDrag.PTabReleaseHandle(e);

    private void PTabLoadHandle(object sender, RoutedEventArgs e) => PRailName.PTabLoadHandle(sender);

    private void PTabKeyHandle(object sender, KeyEventArgs e) => pRailName.PTabKeyHandle(sender, e);

    private void PTabLeaveHandle(object sender, RoutedEventArgs e) => pRailName.PTabLeaveHandle(sender);

    private void PTabMenuHandle(object sender, RoutedEventArgs e) => pRailMenu.PTabMenuHandle(sender, e);

    private void PTabCloseHandle(object sender, MouseButtonEventArgs e)
    {
        PRailDrag.PTabDragClear(Mouse.Captured as FrameworkElement);
        e.Handled = true;
        lRail.QRailCloseHandle(QSender.QSenderItemRead<QStripTab>(sender));
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        lRail.QRailDragClear();
        PRailDrag.PTabDragClear(e.OriginalSource as FrameworkElement);
    }
}
