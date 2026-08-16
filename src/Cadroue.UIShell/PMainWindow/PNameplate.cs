using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PMainWindow;

internal static class PNameplate
{
    private static readonly DependencyProperty PNameplateOwnedProperty =
        DependencyProperty.RegisterAttached(
            "PNameplateOwned",
            typeof(bool),
            typeof(PNameplate),
            new PropertyMetadata(false));

    internal static void PNameplateAttach()
    {
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement),
            FrameworkElement.MouseEnterEvent,
            new System.Windows.Input.MouseEventHandler(PNameplateEnterHandle));
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement),
            ToolTipService.ToolTipOpeningEvent,
            new ToolTipEventHandler(PNameplateTipHandle));
    }

    private static void PNameplateEnterHandle(object pSender, System.Windows.Input.MouseEventArgs pEvent)
    {
        if (!Cadroue.Application.LPreference.LPreferenceStateCurrent.LPreferenceDeveloperActive)
        {
            return;
        }

        if (pSender is not FrameworkElement pElement)
        {
            return;
        }

        if (pElement.ToolTip is not null && !(bool)pElement.GetValue(PNameplateOwnedProperty))
        {
            return;
        }

        pElement.SetValue(PNameplateOwnedProperty, true);
        pElement.ToolTip = PNameplateResolve(pElement);
    }

    internal static void PNameplateTipHandle(object pSender, ToolTipEventArgs pEvent)
    {
        if (pSender is not FrameworkElement pElement || !(bool)pElement.GetValue(PNameplateOwnedProperty))
        {
            return;
        }

        if (!Cadroue.Application.LPreference.LPreferenceStateCurrent.LPreferenceDeveloperActive)
        {
            pEvent.Handled = true;
            return;
        }

        pElement.ToolTip = PNameplateResolve(pElement);
    }

    internal static string PNameplateResolve(DependencyObject pStart)
    {
        if (pStart is FrameworkElement pStartFramework && !string.IsNullOrEmpty(pStartFramework.Name))
        {
            return pStartFramework.Name;
        }

        string pType = pStart.GetType().Name;

        DependencyObject? pElement = VisualTreeHelper.GetParent(pStart);
        while (pElement is not null)
        {
            if (pElement is FrameworkElement pFramework && !string.IsNullOrEmpty(pFramework.Name))
            {
                return pFramework.Name + " › " + pType;
            }

            if (PNameplateOwnedCheck(pElement))
            {
                return pElement.GetType().Name + " › " + pType;
            }

            pElement = VisualTreeHelper.GetParent(pElement);
        }

        return pType;
    }

    private static bool PNameplateOwnedCheck(DependencyObject pElement)
    {
        string? pNamespace = pElement.GetType().Namespace;
        return pNamespace is not null && pNamespace.StartsWith("Cadroue", System.StringComparison.Ordinal);
    }
}
