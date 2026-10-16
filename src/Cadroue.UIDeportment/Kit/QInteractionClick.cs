using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Cadroue.UIDeportment;

public static class QInteractionClick
{
    public static void QInteractionClickAttach(ButtonBase lButton)
    {
        ArgumentNullException.ThrowIfNull(lButton);

        lButton.AddHandler(
            ButtonBase.ClickEvent,
            new RoutedEventHandler(QInteractionClickHandle),
            handledEventsToo: true);
    }

    public static void QInteractionClickHandle(object lSender, RoutedEventArgs lEvent)
    {
        ArgumentNullException.ThrowIfNull(lEvent);

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
