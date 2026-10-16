using System.Windows;

namespace Cadroue.UIDeportment;

public static class QSender
{
    public static QSenderItem? QSenderItemRead<QSenderItem>(object? lSender) where QSenderItem : class =>
        (lSender as FrameworkElement)?.DataContext as QSenderItem;

    public static bool QSenderItemCheck<QSenderItem>(DependencyObject lNode) where QSenderItem : class =>
        QSenderItemRead<QSenderItem>(lNode) is not null;
}
