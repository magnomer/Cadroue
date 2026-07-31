using System.Windows;
using Cadroue.UIShell;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIShell.PControlBar;

public partial class PToolbar
{
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
        PLogoItemAppend(pLogoMenu, "Exit", "Chrome.Menu.Exit", "/PAssets/PMenus/PMenuExit.svg");

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
