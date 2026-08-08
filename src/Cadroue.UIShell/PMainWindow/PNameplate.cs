using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PMainWindow;

internal static class PNameplate
{
    internal static void PNameplateToolTipHandle(object pSender, ToolTipEventArgs pEvent)
    {
        if (!Cadroue.Application.LPreference.LPreferenceStateCurrent.LPreferenceDeveloperActive)
        {
            return;
        }

        if (pSender is not FrameworkElement pElement)
        {
            return;
        }

        pElement.ToolTip = PNameplateResolve(pElement);
    }

    internal static string PNameplateResolve(DependencyObject pStart)
    {
        DependencyObject? pElement = pStart;
        while (pElement is not null)
        {
            if (pElement is FrameworkElement pFramework && !string.IsNullOrEmpty(pFramework.Name))
            {
                return pFramework.Name;
            }

            pElement = VisualTreeHelper.GetParent(pElement);
        }

        return pStart.GetType().Name;
    }
}
