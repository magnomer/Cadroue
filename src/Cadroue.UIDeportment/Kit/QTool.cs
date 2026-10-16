using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

// The wiring every inspector tool section shares: its apply and persistent switches and its enabled look.
public static class QTool
{
    private const string QToolDimmedSheet = "PToolDimmed";

    private const double QToolLit = 1.0;

    public static void QToolSwitchAttach(CheckBox lSwitch, Action<bool> lSet)
    {
        QInteractionClick.QInteractionClickAttach(lSwitch);
        QCheckbox.QCheckboxAttach(lSwitch);
        lSwitch.Checked += (_, _) => lSet(true);
        lSwitch.Unchecked += (_, _) => lSet(false);
    }

    public static void QToolSwitchUpdate(CheckBox lSwitch, bool lChecked) => lSwitch.IsChecked = lChecked;

    public static void QToolTipApply(
        CheckBox lApply, CheckBox lPersistent, FrameworkElement lStack, FrameworkElement lBody, QInspectorTip lTip)
    {
        lApply.IsEnabled = lTip.QInspectorTipEnabled;
        lPersistent.IsEnabled = lTip.QInspectorTipEnabled;
        lStack.IsEnabled = lTip.QInspectorTipEnabled;
        lStack.Opacity = lTip.QInspectorTipEnabled
            ? QToolLit
            : (double)QContract.QContractSheetFind<object>(QToolDimmedSheet);
        lBody.ToolTip = lTip.QInspectorTipNotice;
        lApply.ToolTip = lTip.QInspectorTipBox;
        lPersistent.ToolTip = lTip.QInspectorTipPersistent;
        ToolTipService.SetShowOnDisabled(lBody, true);
        ToolTipService.SetShowOnDisabled(lApply, true);
        ToolTipService.SetShowOnDisabled(lPersistent, true);
    }
}
