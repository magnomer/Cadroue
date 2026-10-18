using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIDeportment;

public static class QNameplateTip
{
    private static readonly DependencyProperty QNameplateTipOwned =
        DependencyProperty.RegisterAttached(
            "QNameplateTipOwned",
            typeof(bool),
            typeof(QNameplateTip),
            new PropertyMetadata(false));

    public static void QNameplateTipAttach()
    {
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement),
            UIElement.MouseEnterEvent,
            new MouseEventHandler(QNameplateTipClaim));
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement),
            ToolTipService.ToolTipOpeningEvent,
            new ToolTipEventHandler(QNameplateTipOpen));
    }

    private static void QNameplateTipClaim(object lSender, MouseEventArgs lEvent)
    {
        var lElement = (FrameworkElement)lSender;
        bool lOwned = QNameplate.QNameplateOwnedResolve(
            lElement.ToolTip,
            lElement.GetValue(QNameplateTipOwned) as bool?);
        lElement.SetValue(QNameplateTipOwned, lOwned);
        QNameplateTipShow(lElement, lOwned);
    }

    private static void QNameplateTipOpen(object lSender, ToolTipEventArgs lEvent)
    {
        var lElement = (FrameworkElement)lSender;
        bool? lOwned = lElement.GetValue(QNameplateTipOwned) as bool?;
        lEvent.Handled = QNameplate.QNameplateTipCheck(lOwned);
        QNameplateTipShow(lElement, lOwned);
    }

    private static void QNameplateTipShow(FrameworkElement lElement, bool? lOwned) =>
        lElement.SetCurrentValue(
            FrameworkElement.ToolTipProperty,
            QNameplate.QNameplateTipResolve(
                lOwned,
                lElement.ToolTip,
                () => QNameplateTree.QNameplateTreeRead(lElement)));
}
