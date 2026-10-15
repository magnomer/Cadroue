using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public static class QCheckbox
{
    private const string QCheckboxStyle = "PCheckbox";

    public static void QCheckboxAttach(CheckBox lBox)
    {
        ArgumentNullException.ThrowIfNull(lBox);

        lBox.Style = QContract.QContractSheetFind<Style>(QCheckboxStyle);
        QLook.QLookAttach(lBox, QCheckboxStyle);
    }
}
