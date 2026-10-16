using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPorch;

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

    private static readonly IReadOnlyDictionary<bool, Action<PToolbar>> pChromeLogos =
        new Dictionary<bool, Action<PToolbar>>
        {
            [true] = pToolbar => pToolbar.PLogoMenuShow(pToolbar.pLogoHost),
            [false] = pToolbar => { },
        };

    private static readonly IReadOnlyDictionary<string, Action<PToolbar>> pLogoActions =
        new Dictionary<string, Action<PToolbar>>
        {
            ["Options"] = pToolbar => pToolbar.PToolbarOptionsShow(),
            ["Shortcuts"] = pToolbar => pToolbar.PToolbarShortcutShow(),
            ["Log"] = pToolbar =>
                PLogWindow.PLogWindowShow(Window.GetWindow(pToolbar)!, pToolbar.qLogoMenu!.QLogoLogCreate),
            ["About"] = pToolbar =>
                PSAbout.PSAboutShow(Window.GetWindow(pToolbar)!, pToolbar.qLogoMenu!.QLogoAboutCreate()),
            ["Exit"] = pToolbar => Window.GetWindow(pToolbar)!.Close(),
        };

    private QChrome lChrome = null!;

    private QLogoMenu? qLogoMenu;

    public event Action? PToolbarOptionsApplied;

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

    public void PToolbarLogoSet(QLogoMenu qLogoMenuOwner)
    {
        qLogoMenu = qLogoMenuOwner;
    }

    public void PToolbarChromeSet(QChrome lChromeOwner)
    {
        lChrome = lChromeOwner;
    }

    private void PToolbarOptionsShow()
    {
        PSOptions.PSOptionsShow(Window.GetWindow(this)!, qLogoMenu!.QLogoOptionsCreate(), PToolbarOptionsApplied);
    }

    public void PToolbarShortcutShow()
    {
        PSKeymap.PSKeymapShow(Window.GetWindow(this)!, qLogoMenu!.QLogoKeymapCreate(), PToolbarOptionsApplied);
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
        PSash.PSashDragMove(Window.GetWindow(this)!, e, PChromeButtonHeight);
        lChrome.QChromeReset();
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        pChromeLogos[lChrome.QChromeReleaseResolve(PChromeLogoCheck(e.OriginalSource as DependencyObject))](this);
    }

    private bool PChromeCaptionCheck(DependencyObject? pChromeSource) =>
        QChrome.QChromeCaptionResolve(
            PSash.PSashInteractiveCheck(pChromeSource),
            QWalk.QWalkParentCheck(pChromeSource, QSender.QSenderItemCheck<QStripTab>),
            QWalk.QWalkParentCheck(pChromeSource, PChromeSelfMatch));

    private bool PChromeSelfMatch(DependencyObject pChromeNode) => ReferenceEquals(pChromeNode, this);

    private bool PChromeLogoMatch(DependencyObject pChromeNode) => ReferenceEquals(pChromeNode, pLogoHost);

    private bool PChromeLogoCheck(DependencyObject? pChromeSource) =>
        QWalk.QWalkParentCheck(pChromeSource, PChromeLogoMatch);

    private void PLogoMenuShow(FrameworkElement pLogoButton)
    {
        ContextMenu pLogoMenu = QMenu.QMenuBuild(pLogoButton, QMenuSide.QMenuSideBelow);

        PLogoItemAppend(pLogoMenu, "Options", "Chrome.Menu.Options", "/PAsset/PMenu/PMenuPreferences.svg");
        PLogoItemAppend(pLogoMenu, "Shortcuts", "Chrome.Menu.Shortcuts", "/PAsset/PMenu/PMenuShortcuts.svg");
        PLogoItemAppend(pLogoMenu, "Log", "Chrome.Menu.Log", "/PAsset/PMenu/PMenuLog.svg");
        PLogoItemAppend(pLogoMenu, "About", "Chrome.Menu.About", "/PAsset/PMenu/PMenuAbout.svg");
        MenuItem pDebugItem = PMenu.PMenuItemCreate("Debug");
        pDebugItem.Visibility = PLook.PLookVisible[QChrome.QChromeDebugVisible];
        pDebugItem.Click += (_, _) => PSDebug.PSDebugShow(Window.GetWindow(this)!, qLogoMenu!);
        pLogoMenu.Items.Add(pDebugItem);
        PLogoItemAppend(pLogoMenu, "Exit", "Chrome.Menu.Exit", "/PAsset/PMenu/PMenuExit.svg");

        pLogoMenu.IsOpen = true;
    }

    private void PLogoItemAppend(
        ContextMenu pLogoMenu,
        string pLogoMenuToken,
        string pLogoMenuKey,
        string pLogoMenuIconPath)
    {
        MenuItem pLogoMenuItem = PMenu.PMenuItemCreate(
            QLocalizationCatalog.QLocalizationTextRead(pLogoMenuKey),
            PMenu.PMenuIconRead(pLogoMenuIconPath));
        pLogoMenuItem.Click += (_, _) => pLogoActions[pLogoMenuToken](this);
        pLogoMenu.Items.Add(pLogoMenuItem);
    }
}
