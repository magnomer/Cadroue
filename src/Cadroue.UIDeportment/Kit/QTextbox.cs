using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Cadroue.UIDeportment;

public static class QTextbox
{
    private const string QTextboxStyle = "PTextbox";

    private const string QTextboxHostPart = "PART_ContentHost";

    internal static void QTextboxHook() =>
        EventManager.RegisterClassHandler(
            typeof(TextBox),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((lSender, _) => QTextboxLoad((TextBox)lSender)),
            true);

    public static void QTextboxAttach(TextBox lBox)
    {
        ArgumentNullException.ThrowIfNull(lBox);

        QTextboxStyleApply(lBox);
        QTextboxLoad(lBox);
        QLook.QLookAttach(lBox);
    }

    private static void QTextboxStyleApply(TextBox lBox) =>
        lBox.Style = QContract.QContractSheetFind<Style>(QTextboxStyle);

    private static void QTextboxLoad(TextBox lBox)
    {
        if (QLook.QLookStyleResolve(lBox, [QTextboxStyle]) is not null)
        {
            QTextboxHostBuild(lBox);
        }
    }

    private static void QTextboxHostBuild(TextBox lBox)
    {
        _ = lBox.ApplyTemplate();
        _ = ((FrameworkElement)lBox.Template.FindName(QTextboxHostPart, lBox)).SetBinding(
            FrameworkElement.MarginProperty,
            new Binding(nameof(Control.Padding)) { Source = lBox });
    }
}
