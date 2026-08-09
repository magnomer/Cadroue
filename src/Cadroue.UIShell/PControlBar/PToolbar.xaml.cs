using System.Windows;
using Cadroue.Core;
using Cadroue.UIShell;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;

namespace Cadroue.UIShell.PControlBar;

public partial class PToolbar : UserControl
{
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
        pRailHeaderColumn.Width = new GridLength(pVertical ? PRail.PRailWidth - 56 : 0);
        pTitleCenter.Background = pVertical ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Transparent;
        pChromeHost.Background = pVertical ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Transparent;
        pRailHeaderDivider.Visibility = pVertical ? Visibility.Visible : Visibility.Collapsed;
        pTabHost.Visibility = pVertical ? Visibility.Collapsed : Visibility.Visible;
        pSceneHost.Visibility = pVertical ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PToolbarOptionsShow()
    {
        PSOptions.PSOptionsShow(Window.GetWindow(this)!, PToolbarOptionsApply);
    }

    public void PToolbarShortcutShow()
    {
        PSKeymap.PSKeymapShow(Window.GetWindow(this)!, PToolbarOptionsApply);
    }
}
