using System.Windows;
using System.Windows.Input;

namespace Cadroue.UIVeneer.PHouse;

public static class PSender
{
    public static PSenderItem? PSenderItemRead<PSenderItem>(object? sender) where PSenderItem : class =>
        (sender as FrameworkElement)?.DataContext as PSenderItem;

    public static string? PSenderTagRead(object? sender) =>
        (sender as FrameworkElement)?.Tag as string;

    public static PSenderParameter? PSenderParameterRead<PSenderParameter>(ExecutedRoutedEventArgs e)
        where PSenderParameter : class =>
        e.Parameter as PSenderParameter;
}
