using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPorch;

internal sealed class PLogoMenu
{
    private static readonly IReadOnlyDictionary<string, Action<PLogoMenu>> pLogoActions =
        new Dictionary<string, Action<PLogoMenu>>
        {
            ["Options"] = pLogo => pLogo.PToolbarOptionsShow(),
            ["Shortcuts"] = pLogo => pLogo.PToolbarShortcutShow(),
            ["Log"] = pLogo =>
                PLogWindow.PLogWindowShow(Window.GetWindow(pLogo.pLogoOwner)!, pLogo.qLogoMenu!.QLogoLogCreate),
            ["About"] = pLogo =>
                PSAbout.PSAboutShow(Window.GetWindow(pLogo.pLogoOwner)!, pLogo.qLogoMenu!.QLogoAboutCreate()),
            ["Exit"] = pLogo => Window.GetWindow(pLogo.pLogoOwner)!.Close(),
        };

    private readonly DependencyObject pLogoOwner;

    private QLogoMenu? qLogoMenu;

    internal PLogoMenu(DependencyObject pLogoOwnerElement) => pLogoOwner = pLogoOwnerElement;

    internal event Action? PToolbarOptionsApplied;

    internal void PLogoMenuSet(QLogoMenu qLogoMenuOwner)
    {
        qLogoMenu = qLogoMenuOwner;
    }

    private void PToolbarOptionsShow()
    {
        PSOptions.PSOptionsShow(Window.GetWindow(pLogoOwner)!, qLogoMenu!.QLogoOptionsCreate(), PToolbarOptionsApplied);
    }

    internal void PToolbarShortcutShow()
    {
        PSKeymap.PSKeymapShow(Window.GetWindow(pLogoOwner)!, qLogoMenu!.QLogoKeymapCreate(), PToolbarOptionsApplied);
    }

    internal void PLogoMenuShow(FrameworkElement pLogoButton)
    {
        ContextMenu pLogoMenu = QMenu.QMenuBuild(pLogoButton, QMenuSide.QMenuSideBelow);

        PLogoItemAppend(pLogoMenu, "Options", "Chrome.Menu.Options", "/PAsset/PMenu/PMenuPreferences.svg");
        PLogoItemAppend(pLogoMenu, "Shortcuts", "Chrome.Menu.Shortcuts", "/PAsset/PMenu/PMenuShortcuts.svg");
        PLogoItemAppend(pLogoMenu, "Log", "Chrome.Menu.Log", "/PAsset/PMenu/PMenuLog.svg");
        PLogoItemAppend(pLogoMenu, "About", "Chrome.Menu.About", "/PAsset/PMenu/PMenuAbout.svg");
        PLogoDebugAppend(pLogoMenu);
        PLogoItemAppend(pLogoMenu, "Exit", "Chrome.Menu.Exit", "/PAsset/PMenu/PMenuExit.svg");

        pLogoMenu.IsOpen = true;
    }

    private void PLogoDebugAppend(ContextMenu pLogoMenu)
    {
        MenuItem pDebugItem = QMenu.QMenuItemBuild("Debug");
        PLogoDebugShow(pDebugItem);
        PLogoDebugAttach(pDebugItem);
        pLogoMenu.Items.Add(pDebugItem);
    }

    private static void PLogoDebugShow(MenuItem pDebugItem) =>
        pDebugItem.Visibility = PLook.PLookVisible[QChrome.QChromeDebugVisible];

    private void PLogoDebugAttach(MenuItem pDebugItem) =>
        pDebugItem.Click += (_, _) => PSDebug.PSDebugShow(Window.GetWindow(pLogoOwner)!, qLogoMenu!);

    private void PLogoItemAppend(
        ContextMenu pLogoMenu,
        string pLogoMenuToken,
        string pLogoMenuKey,
        string pLogoMenuIconPath)
    {
        MenuItem pLogoMenuItem = QMenu.QMenuItemBuild(
            QLocalizationCatalog.QLocalizationTextRead(pLogoMenuKey),
            QMenu.QMenuIconRead(pLogoMenuIconPath));
        PLogoItemAttach(pLogoMenuItem, pLogoMenuToken);
        pLogoMenu.Items.Add(pLogoMenuItem);
    }

    private void PLogoItemAttach(MenuItem pLogoMenuItem, string pLogoMenuToken) =>
        pLogoMenuItem.Click += (_, _) => pLogoActions[pLogoMenuToken](this);
}
