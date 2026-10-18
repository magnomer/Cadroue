using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

// The editable dropdown whose every row carries a remove button.
public static class QDropdownAction
{
    private const string QDropdownEditableStyle = "PDropdownEditable";

    private const string QDropdownActionStyle = "PDropdownRowAction";

    private const string QDropdownButtonStyle = "PDropdownRowButton";

    private const string QDropdownButtonPart = "PDropdownRowButton";

    private const string QDropdownMinusPart = "PDropdownRowMinus";

    private const string QDropdownTextKey = "PDropdownText";

    private const string QDropdownMinusIcon = "/PAsset/PPanel/PExportMinus.svg";

    public static void QDropdownActionAttach(ComboBox lCombo, string lActionTooltip)
    {
        ArgumentNullException.ThrowIfNull(lCombo);

        QDropdownActionApply(lCombo, lActionTooltip);
        _ = lCombo.ApplyTemplate();
        QDropdown.QDropdownPartAttach(
            lCombo, QDropdown.QDropdownArrowPart, ToggleButton.IsCheckedProperty, "IsDropDownOpen");
        QDropdown.QDropdownPopupAttach(lCombo);
        QLook.QLookAttach(lCombo, QDropdown.QDropdownStyle);
    }

    private static void QDropdownActionApply(ComboBox lCombo, string lActionTooltip)
    {
        lCombo.Style = QContract.QContractSheetFind<Style>(QDropdownEditableStyle);
        lCombo.ItemContainerStyle = QDropdown.QDropdownHookBuild(
            QContract.QContractSheetFind<Style>(QDropdownActionStyle),
            (lSender, _) => QDropdownActionLoad((ComboBoxItem)lSender, lActionTooltip));
    }

    private static void QDropdownActionLoad(ComboBoxItem lRow, string lActionTooltip)
    {
        QDropdownRowApply(lRow);
        QLook.QLookAttach(lRow, QDropdown.QDropdownRowStyle);

        Button lButton = (Button)lRow.Template.FindName(QDropdownButtonPart, lRow);
        QDropdownTooltipSet(lButton, lActionTooltip);
        QDropdownMinusApply(lRow);
        QLook.QLookAttach(lButton, QDropdownButtonStyle);
    }

    private static void QDropdownRowApply(ComboBoxItem lRow) =>
        lRow.Style = QContract.QContractSheetFind<Style>(QDropdownActionStyle);

    private static void QDropdownTooltipSet(Button lButton, string lActionTooltip)
    {
        lButton.ToolTip = lActionTooltip;
        AutomationProperties.SetName(lButton, lActionTooltip);
    }

    private static void QDropdownMinusApply(ComboBoxItem lRow) =>
        ((Image)lRow.Template.FindName(QDropdownMinusPart, lRow)).Source =
            QIcon.QIconRead(QDropdownMinusIcon, QContract.QContractSheetFind<Brush>(QDropdownTextKey));
}
