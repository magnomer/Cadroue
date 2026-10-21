using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

// The wiring every inspector tool section shares: its apply and persistent switches and its enabled look.
public static class QTool
{
    private const string QToolFontSheet = "PToolFont";

    private const string QToolMutedSheet = "PToolMuted";

    private const string QToolIconSheet = "PToolIcon";

    private const string QToolAccentSheet = "PToolAccent";

    private const string QToolDimmedSheet = "PToolDimmed";

    private const double QToolLit = 1.0;

    public static double QToolLabelWidth =>
        (double)QContract.QContractSheetFind<object>("PToolLabelWidth");

    public static double QToolFieldHeight =>
        (double)QContract.QContractSheetFind<object>("PToolFieldHeight");

    public static double QToolRowHeight =>
        (double)QContract.QContractSheetFind<object>("PToolRowHeight");

    public static FontFamily QToolFontRead() =>
        QContract.QContractSheetFind<FontFamily>(QToolFontSheet);

    public static Brush QToolMutedRead() => QContract.QContractSheetFind<Brush>(QToolMutedSheet);

    public static Brush QToolIconRead() => QContract.QContractSheetFind<Brush>(QToolIconSheet);

    public static Brush QToolPickRead(bool lPicked) =>
        lPicked
            ? QContract.QContractSheetFind<Brush>(QToolAccentSheet)
            : QToolIconRead();

    public static void QToolSwitchAttach(CheckBox lSwitch, Action<bool> lSet)
    {
        QInteractionClick.QInteractionClickAttach(lSwitch);
        QCheckbox.QCheckboxAttach(lSwitch);
        lSwitch.Checked += (_, _) => lSet(true);
        lSwitch.Unchecked += (_, _) => lSet(false);
    }

    public static void QToolSwitchUpdate(CheckBox lSwitch, bool lChecked) => lSwitch.IsChecked = lChecked;

    public static void QToolStackUpdate(FrameworkElement lStack, bool lEnabled)
    {
        QToolEnabledApply(lStack, lEnabled);
        QToolOpacityApply(lStack, QToolOpacityResolve(lEnabled));
    }

    public static void QToolTipApply(
        CheckBox lApply, CheckBox lPersistent, FrameworkElement lStack, FrameworkElement lBody, QInspectorTip lTip)
    {
        QToolEnabledApply(lApply, lTip.QInspectorTipEnabled);
        QToolEnabledApply(lPersistent, lTip.QInspectorTipEnabled);
        QToolStackUpdate(lStack, lTip.QInspectorTipEnabled);
        QToolHintApply(lApply, lPersistent, lBody, lTip);
    }

    private static void QToolEnabledApply(FrameworkElement lElement, bool lEnabled) => lElement.IsEnabled = lEnabled;

    private static void QToolOpacityApply(FrameworkElement lElement, double lOpacity) => lElement.Opacity = lOpacity;

    private static double QToolOpacityResolve(bool lEnabled) =>
        lEnabled
            ? QToolLit
            : (double)QContract.QContractSheetFind<object>(QToolDimmedSheet);

    private static void QToolHintApply(
        CheckBox lApply, CheckBox lPersistent, FrameworkElement lBody, QInspectorTip lTip)
    {
        lBody.ToolTip = lTip.QInspectorTipNotice;
        lApply.ToolTip = lTip.QInspectorTipBox;
        lPersistent.ToolTip = lTip.QInspectorTipPersistent;
        ToolTipService.SetShowOnDisabled(lBody, true);
        ToolTipService.SetShowOnDisabled(lApply, true);
        ToolTipService.SetShowOnDisabled(lPersistent, true);
    }
}
