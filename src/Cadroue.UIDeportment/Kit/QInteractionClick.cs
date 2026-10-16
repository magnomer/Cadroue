using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Cadroue.UIDeportment;

public static class QInteractionClick
{
    private static readonly DependencyProperty QInteractionClickRecorded =
        DependencyProperty.RegisterAttached(
            "QInteractionClickRecorded",
            typeof(bool),
            typeof(QInteractionClick),
            new PropertyMetadata(false));

    // A class handler runs before every instance handler, so the record leads
    // the caller's Click handlers whatever order the caller attached them in.
    static QInteractionClick() =>
        EventManager.RegisterClassHandler(
            typeof(ButtonBase),
            ButtonBase.ClickEvent,
            new RoutedEventHandler(QInteractionClickHandle),
            handledEventsToo: true);

    public static void QInteractionClickAttach(ButtonBase lButton)
    {
        ArgumentNullException.ThrowIfNull(lButton);

        lButton.SetValue(QInteractionClickRecorded, true);
    }

    // A sender that is no ButtonBase (a MenuItem style setter) is always recorded.
    private static bool QInteractionClickCheck(object lSender) =>
        lSender is not ButtonBase lButton || (bool)lButton.GetValue(QInteractionClickRecorded);

    public static void QInteractionClickHandle(object lSender, RoutedEventArgs lEvent)
    {
        ArgumentNullException.ThrowIfNull(lEvent);

        if (!QInteractionClickCheck(lSender))
        {
            return;
        }

        (FrameworkElement lElement, object? lContent, string lType) = lSender switch
        {
            MenuItem lItem => ((FrameworkElement)lItem, (object?)lItem.Header, nameof(MenuItem)),
            _ => ((ButtonBase)lSender, ((ButtonBase)lSender).Content, lSender.GetType().Name),
        };
        QInteraction.QInteractionRecord(
            ReferenceEquals(lElement, lEvent.OriginalSource),
            lType,
            () => QInteractionFact.QInteractionFactRead(lElement, lContent),
            () => QNameplateTree.QNameplateTreeRead(lElement),
            typeof(ToggleButton).IsInstanceOfType(lElement),
            (lElement as ToggleButton)?.IsChecked);
    }
}
