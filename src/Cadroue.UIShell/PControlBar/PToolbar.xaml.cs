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
    private LTabset? lTabset;

    public event Action<LPreferenceState>? PToolbarOptionsApply;

    public PToolbar()
    {
        InitializeComponent();
        PChromeButtonsApply();
    }

    public void PToolbarTabsetSet(LTabset lTabsetValue)
    {
        lTabset = lTabsetValue;
        DataContext = lTabset;
    }

    private void PToolbarOptionsShow()
    {
        PSOptions.PSOptionsShow(Window.GetWindow(this)!, PToolbarOptionsApply);
    }

    public void PToolbarShortcutShow()
    {
        PSKeymap.PSKeymapShow(Window.GetWindow(this)!, PToolbarOptionsApply);
    }

    private static bool PToolbarButtonFind(DependencyObject? pToolbarSource)
    {
        while (pToolbarSource is not null)
        {
            if (pToolbarSource is Button)
            {
                return true;
            }

            pToolbarSource = VisualTreeHelper.GetParent(pToolbarSource);
        }

        return false;
    }

    private static bool PToolbarTabFind(DependencyObject? pToolbarSource)
    {
        while (pToolbarSource is not null)
        {
            if (pToolbarSource is FrameworkElement { DataContext: PTabRecord })
            {
                return true;
            }

            pToolbarSource = VisualTreeHelper.GetParent(pToolbarSource);
        }

        return false;
    }
}
