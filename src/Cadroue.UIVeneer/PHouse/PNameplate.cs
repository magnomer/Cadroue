using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PHouse;

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
        var pElement = (FrameworkElement)pSender;
        bool pOwned = LNameplate.LNameplateOwnedResolve(pElement.ToolTip, PNameplateOwnedRead(pElement));
        pElement.SetValue(PNameplateOwnedProperty, pOwned);
        pElement.SetCurrentValue(
            FrameworkElement.ToolTipProperty,
            LNameplate.LNameplateTipResolve(pOwned, pElement.ToolTip, () => PNameplateResolve(pElement)));
    }

    internal static void PNameplateTipHandle(object pSender, ToolTipEventArgs pEvent)
    {
        var pElement = (FrameworkElement)pSender;
        bool? pOwned = PNameplateOwnedRead(pElement);
        pEvent.Handled = LNameplate.LNameplateTipCheck(pOwned);
        pElement.SetCurrentValue(
            FrameworkElement.ToolTipProperty,
            LNameplate.LNameplateTipResolve(pOwned, pElement.ToolTip, () => PNameplateResolve(pElement)));
    }

    internal static string PNameplateResolve(DependencyObject pStart)
    {
        DependencyObject? pOwner = PWalk.PWalkParentFind(VisualTreeHelper.GetParent(pStart), PNameplateOwnerMatch);
        return LNameplate.LNameplateResolve(
            (pStart as FrameworkElement)?.Name,
            pStart.GetType().Name,
            (pOwner as FrameworkElement)?.Name,
            pOwner?.GetType().Name);
    }

    private static bool? PNameplateOwnedRead(FrameworkElement pElement) =>
        pElement.GetValue(PNameplateOwnedProperty) as bool?;

    private static bool PNameplateOwnerMatch(DependencyObject pNode) =>
        LNameplate.LNameplateOwnerCheck((pNode as FrameworkElement)?.Name, pNode.GetType().Namespace);
}
