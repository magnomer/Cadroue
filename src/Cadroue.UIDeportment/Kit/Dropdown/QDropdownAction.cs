using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

// The editable dropdown whose every row carries a remove button.
public static class QDropdownAction
{
    private const string QDropdownActionStyle = "PDropdownRowAction";

    private const string QDropdownButtonPart = "PDropdownRowButton";

    private const string QDropdownMinusPart = "PDropdownRowMinus";

    private const string QDropdownTextKey = "PDropdownText";

    private const string QDropdownMinusIcon = "/PAsset/PPanel/PExportMinus.svg";

    // Set on the dropdown; its rows' remove buttons show it as their tooltip and automation name.
    internal static readonly DependencyProperty QDropdownTooltipProperty = DependencyProperty.RegisterAttached(
        "QDropdownTooltip",
        typeof(string),
        typeof(QDropdownAction),
        new FrameworkPropertyMetadata(null));

    internal static void QDropdownActionHook() =>
        EventManager.RegisterClassHandler(
            typeof(ComboBoxItem),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((lSender, _) => QDropdownActionLoad((ComboBoxItem)lSender)),
            true);

    public static void QDropdownActionAttach(ComboBox lCombo, string lActionTooltip)
    {
        ArgumentNullException.ThrowIfNull(lCombo);

        lCombo.SetValue(QDropdownTooltipProperty, lActionTooltip);
        QDropdownActionApply(lCombo);
        QDropdown.QDropdownLoad(lCombo);
        QLook.QLookAttach(lCombo);
    }

    private static void QDropdownActionApply(ComboBox lCombo)
    {
        lCombo.Style = QContract.QContractSheetFind<Style>(QDropdown.QDropdownEditableStyle);
        lCombo.ItemContainerStyle = QDropdown.QDropdownHookBuild(
            QContract.QContractSheetFind<Style>(QDropdownActionStyle),
            (lSender, _) => QDropdownRowApply((ComboBoxItem)lSender));
    }

    private static void QDropdownActionLoad(ComboBoxItem lRow)
    {
        if (QLook.QLookStyleResolve(lRow, [QDropdownActionStyle]) is null)
        {
            return;
        }

        Button lButton = (Button)lRow.Template.FindName(QDropdownButtonPart, lRow);
        if (ItemsControl.ItemsControlFromItemContainer(lRow)?.GetValue(QDropdownTooltipProperty) is string lTooltip)
        {
            QDropdownTooltipSet(lButton, lTooltip);
        }

        QDropdownMinusApply(lRow);
        QLook.QLookAttach(lButton);
    }

    private static void QDropdownRowApply(ComboBoxItem lRow)
    {
        lRow.Style = QContract.QContractSheetFind<Style>(QDropdownActionStyle);
        QLook.QLookAttach(lRow);
    }

    private static void QDropdownTooltipSet(Button lButton, string lActionTooltip)
    {
        lButton.ToolTip = lActionTooltip;
        AutomationProperties.SetName(lButton, lActionTooltip);
    }

    private static void QDropdownMinusApply(ComboBoxItem lRow) =>
        ((Image)lRow.Template.FindName(QDropdownMinusPart, lRow)).Source =
            QIcon.QIconRead(QDropdownMinusIcon, QContract.QContractSheetFind<Brush>(QDropdownTextKey));
}
