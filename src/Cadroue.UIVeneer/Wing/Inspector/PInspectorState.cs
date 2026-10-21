using System.Windows;

namespace Cadroue.UIVeneer;

internal static class PInspectorState
{
    internal static void PInspectorSectionUpdate(FrameworkElement pStack, bool pEnabled)
    {
        PInspectorEnabledApply(pStack, pEnabled);
        PInspectorOpacityApply(pStack, pEnabled);
    }

    private static void PInspectorEnabledApply(FrameworkElement pElement, bool pEnabled) =>
        pElement.IsEnabled = pEnabled;

    private static void PInspectorOpacityApply(FrameworkElement pElement, bool pEnabled) =>
        pElement.Opacity = PLook.PLookOpacity[pEnabled];
}
