using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.Application;
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
            ["Log"] = pToolbar => PLogWindow.PLogWindowShow(Window.GetWindow(pToolbar)!),
            ["About"] = pToolbar => PSAbout.PSAboutShow(Window.GetWindow(pToolbar)!),
            ["Exit"] = pToolbar => Window.GetWindow(pToolbar)!.Close(),
        };

    private readonly LChrome lChrome = new();

    public event Action<LPreferenceState>? PToolbarOptionsApply;

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

    private void PToolbarOptionsShow()
    {
        PSOptions.PSOptionsShow(Window.GetWindow(this)!, PToolbarOptionsApply);
    }

    public void PToolbarShortcutShow()
    {
        PSKeymap.PSKeymapShow(Window.GetWindow(this)!, PToolbarOptionsApply);
    }

    private void PChromeButtonsApply()
    {
        PChromeButtonApply(pChromeMinimizeButton, false, new CornerRadius(0, 0, 0, 9));
        PChromeButtonApply(pChromeMaximizeButton, false, new CornerRadius(0));
        PChromeButtonApply(pChromeCloseButton, true, new CornerRadius(0, 9, 9, 0));
    }

    private static void PChromeButtonApply(Button pChromeButton, bool pChromeClose, CornerRadius pChromeCornerRadius)
    {
        pChromeButton.Width = PChromeButtonWidth;
        pChromeButton.Height = PChromeButtonHeight;
        pChromeButton.Style = PButton.PButtonChromeCreate(pChromeClose, pChromeCornerRadius);
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
        bool pToggle = LChrome.LChromeDoubleCheck(
            e.ClickCount, PChromeCaptionCheck(e.OriginalSource as DependencyObject));
        pChromeToggles[pToggle](this);
        e.Handled = pToggle;
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        Point pChromePoint = e.GetPosition(this);
        lChrome.LChromePressHandle(
            PChromeCaptionCheck(e.OriginalSource as DependencyObject), pChromePoint.X, pChromePoint.Y);
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        Point pChromePoint = e.GetPosition(this);
        bool pDrag = lChrome.LChromeMoveResolve(
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
        lChrome.LChromeReset();
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        pChromeLogos[lChrome.LChromeReleaseResolve(PChromeLogoCheck(e.OriginalSource as DependencyObject))](this);
    }

    private bool PChromeCaptionCheck(DependencyObject? pChromeSource) =>
        LChrome.LChromeCaptionResolve(
            PSash.PSashInteractiveCheck(pChromeSource),
            PWalk.PWalkParentCheck(pChromeSource, PWalk.PWalkItemCheck<LStripTab>),
            PWalk.PWalkParentCheck(pChromeSource, PChromeSelfMatch));

    private bool PChromeSelfMatch(DependencyObject pChromeNode) => ReferenceEquals(pChromeNode, this);

    private bool PChromeLogoMatch(DependencyObject pChromeNode) => ReferenceEquals(pChromeNode, pLogoHost);

    private bool PChromeLogoCheck(DependencyObject? pChromeSource) =>
        PWalk.PWalkParentCheck(pChromeSource, PChromeLogoMatch);

    private void PLogoMenuShow(FrameworkElement pLogoButton)
    {
        ContextMenu pLogoMenu = PMenu.PMenuCreate(pLogoButton);

        PLogoItemAppend(pLogoMenu, "Options", "Chrome.Menu.Options", "/PAsset/PMenu/PMenuPreferences.svg");
        PLogoItemAppend(pLogoMenu, "Shortcuts", "Chrome.Menu.Shortcuts", "/PAsset/PMenu/PMenuShortcuts.svg");
        PLogoItemAppend(pLogoMenu, "Log", "Chrome.Menu.Log", "/PAsset/PMenu/PMenuLog.svg");
        PLogoItemAppend(pLogoMenu, "About", "Chrome.Menu.About", "/PAsset/PMenu/PMenuAbout.svg");
        MenuItem pDebugItem = PMenu.PMenuItemCreate("Debug");
        pDebugItem.Visibility = PLook.PLookVisible[LChrome.LChromeDebugVisible];
        pDebugItem.Click += (_, _) => PSDebug.PSDebugShow(Window.GetWindow(this)!);
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
            LLocalization.LLocalizationTextRead(pLogoMenuKey),
            PMenu.PMenuIconRead(pLogoMenuIconPath));
        pLogoMenuItem.Click += (_, _) => pLogoActions[pLogoMenuToken](this);
        pLogoMenu.Items.Add(pLogoMenuItem);
    }
}
