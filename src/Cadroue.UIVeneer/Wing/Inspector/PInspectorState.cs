using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

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

    internal static void PInspectorSectionApply(
        CheckBox pBox,
        CheckBox pPersistent,
        StackPanel pStack,
        StackPanel pBody,
        QInspectorTip pTip)
    {
        PInspectorEnabledApply(pBox, pTip.QInspectorTipEnabled);
        PInspectorEnabledApply(pPersistent, pTip.QInspectorTipEnabled);
        PInspectorSectionUpdate(pStack, pTip.QInspectorTipEnabled);
        PInspectorTipApply(pBox, pPersistent, pBody, pTip);
    }

    private static void PInspectorTipApply(CheckBox pBox, CheckBox pPersistent, StackPanel pBody, QInspectorTip pTip)
    {
        pBody.ToolTip = pTip.QInspectorTipNotice;
        pBox.ToolTip = pTip.QInspectorTipBox;
        pPersistent.ToolTip = pTip.QInspectorTipPersistent;
        ToolTipService.SetShowOnDisabled(pBody, true);
        ToolTipService.SetShowOnDisabled(pBox, true);
        ToolTipService.SetShowOnDisabled(pPersistent, true);
    }
}
