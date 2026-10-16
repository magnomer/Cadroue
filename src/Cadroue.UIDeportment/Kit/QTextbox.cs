using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Cadroue.UIDeportment;

public static class QTextbox
{
    private const string QTextboxStyle = "PTextbox";

    private const string QTextboxHostPart = "PART_ContentHost";

    public static void QTextboxAttach(TextBox lBox)
    {
        ArgumentNullException.ThrowIfNull(lBox);

        lBox.Style = QContract.QContractSheetFind<Style>(QTextboxStyle);
        _ = lBox.ApplyTemplate();
        _ = ((FrameworkElement)lBox.Template.FindName(QTextboxHostPart, lBox)).SetBinding(
            FrameworkElement.MarginProperty,
            new Binding(nameof(Control.Padding)) { Source = lBox });
        QLook.QLookAttach(lBox, QTextboxStyle);
    }
}
