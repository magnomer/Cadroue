using System.Windows;
using Cadroue.Application;
using Cadroue.UIVeneer;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIVeneer.PPorch;

public partial class PToolbar
{
    private void PLogoMenuShow(FrameworkElement pLogoButton)
    {
        ContextMenu pLogoMenu = PMenu.PMenuCreate(pLogoButton);

        PLogoItemAppend(pLogoMenu, "Options", "Chrome.Menu.Options", "/PAsset/PMenu/PMenuPreferences.svg");
        PLogoItemAppend(pLogoMenu, "Shortcuts", "Chrome.Menu.Shortcuts", "/PAsset/PMenu/PMenuShortcuts.svg");
        PLogoItemAppend(pLogoMenu, "Log", "Chrome.Menu.Log", "/PAsset/PMenu/PMenuLog.svg");
        PLogoItemAppend(pLogoMenu, "About", "Chrome.Menu.About", "/PAsset/PMenu/PMenuAbout.svg");
        if (Cadroue.Application.LPreference.LPreferenceStateCurrent.LPreferenceDeveloperActive)
        {
            MenuItem pDebugItem = PMenu.PMenuItemCreate("Debug", null);
            pDebugItem.Click += (_, _) => PSDebug.PSDebugShow(Window.GetWindow(this)!);
            pLogoMenu.Items.Add(pDebugItem);
        }
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
            pLogoMenuItem.Click += (_, _) => PLogWindow.PLogWindowShow(Window.GetWindow(this)!);
        }
        else if (pLogoMenuToken == "About")
        {
            pLogoMenuItem.Click += (_, _) => PSAbout.PSAboutShow(Window.GetWindow(this)!);
        }
        else if (pLogoMenuToken == "Exit")
        {
            pLogoMenuItem.Click += (_, _) => Window.GetWindow(this)!.Close();
        }

        pLogoMenu.Items.Add(pLogoMenuItem);
    }
}
