using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public static class QCheckbox
{
    private const string QCheckboxStyle = "PCheckbox";

    internal static void QCheckboxHook() =>
        EventManager.RegisterClassHandler(
            typeof(CheckBox),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((lSender, _) => QCheckboxLoad((CheckBox)lSender)),
            true);

    public static void QCheckboxAttach(CheckBox lBox)
    {
        ArgumentNullException.ThrowIfNull(lBox);

        lBox.Style = QContract.QContractSheetFind<Style>(QCheckboxStyle);
        QCheckboxLoad(lBox);
        QLook.QLookAttach(lBox);
    }

    // Every checkbox of this style records its clicks.
    private static void QCheckboxLoad(CheckBox lBox)
    {
        if (QLook.QLookStyleResolve(lBox, [QCheckboxStyle]) is not null)
        {
            QInteractionClick.QInteractionClickAttach(lBox);
        }
    }
}
