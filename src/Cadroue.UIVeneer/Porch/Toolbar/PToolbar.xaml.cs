using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public partial class PToolbar : UserControl
{
    private const double PChromeButtonHeight = 56;

    private const double PChromeButtonWidth = 48;

    private static readonly IReadOnlyDictionary<bool, Action<PToolbar, MouseEventArgs>> pChromeDrags =
        new Dictionary<bool, Action<PToolbar, MouseEventArgs>>
        {
            [true] = (pToolbar, pEvent) => pToolbar.PChromeDragRun(pEvent),
            [false] = (pToolbar, pEvent) => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<PToolbar>> pChromeToggles =
        new Dictionary<bool, Action<PToolbar>>
        {
            [true] = pToolbar => pToolbar.PChromeMaximizeToggle(),
            [false] = pToolbar => { },
        };

    private QChrome lChrome = null!;

    public PToolbar()
    {
        InitializeComponent();
        PChromeButtonsApply();
    }

    public void PToolbarTabSet(UIElement? pTabs)
    {
        pTabHost.Content = pTabs;
    }

    public void PToolbarSceneSet(UIElement? pSceneControls)
    {
        pSceneHost.Content = pSceneControls;
    }

    public void PToolbarVerticalSet(bool pVertical)
    {
        pRailHeaderColumn.Width = PLook.PLookRailHeader[pVertical];
        pToolbarCenter.Background = PLook.PLookWhite[pVertical];
        pChromeHost.Background = PLook.PLookWhite[pVertical];
        pRailHeaderDivider.Visibility = PLook.PLookVisible[pVertical];
        pTabHost.Visibility = PLook.PLookVisible[!pVertical];
        pSceneHost.Visibility = PLook.PLookVisible[pVertical];
    }

    public void PToolbarChromeSet(QChrome lChromeOwner)
    {
        lChrome = lChromeOwner;
    }

    private void PChromeButtonsApply()
    {
        PChromeButtonApply(pChromeMinimizeButton, QButtonLook.QButtonLookFirst);
        PChromeButtonApply(pChromeMaximizeButton, QButtonLook.QButtonLookChrome);
        PChromeButtonApply(pChromeCloseButton, QButtonLook.QButtonLookLast);
    }

    private static void PChromeButtonApply(Button pChromeButton, QButtonLook pChromeLook)
    {
        pChromeButton.Width = PChromeButtonWidth;
        pChromeButton.Height = PChromeButtonHeight;
        QButton.QButtonAttach(pChromeButton, pChromeLook);
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
        Window pWindow = Window.GetWindow(this)!;
        pWindow.WindowState = PLook.PLookMaximizeOpposite[pWindow.WindowState];
    }

    private void PChromeCloseHandle(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)!.Close();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        bool pToggle = QChrome.QChromeDoubleCheck(
            e.ClickCount, PChromeCaptionCheck(e.OriginalSource as DependencyObject));
        pChromeToggles[pToggle](this);
        e.Handled = pToggle;
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        Point pChromePoint = e.GetPosition(this);
        lChrome.QChromePressHandle(
            PChromeCaptionCheck(e.OriginalSource as DependencyObject), pChromePoint.X, pChromePoint.Y);
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        Point pChromePoint = e.GetPosition(this);
        bool pDrag = lChrome.QChromeMoveResolve(
            pChromePoint.X,
            pChromePoint.Y,
            PLook.PLookPressed[e.LeftButton],
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance);
        pChromeDrags[pDrag](this, e);
    }

    private void PChromeDragRun(MouseEventArgs e)
    {
        Mouse.Capture(null);
        QSashDrag.QSashDragMove(Window.GetWindow(this)!, e, PChromeButtonHeight);
        lChrome.QChromeReset();
    }

    private bool PChromeCaptionCheck(DependencyObject? pChromeSource) =>
        QChrome.QChromeCaptionResolve(
            QSashGuard.QSashGuardCheck(pChromeSource),
            QWalk.QWalkParentCheck(pChromeSource, QSender.QSenderItemCheck<QStripTab>),
            QWalk.QWalkParentCheck(pChromeSource, PChromeSelfMatch));

    private bool PChromeSelfMatch(DependencyObject pChromeNode) => ReferenceEquals(pChromeNode, this);

}
