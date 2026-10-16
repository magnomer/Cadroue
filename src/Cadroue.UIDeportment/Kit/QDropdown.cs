using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public static class QDropdown
{
    private const string QDropdownStyle = "PDropdown";

    private const string QDropdownEditableStyle = "PDropdownEditable";

    private const string QDropdownRowStyle = "PDropdownRow";

    private const string QDropdownActionStyle = "PDropdownRowAction";

    private const string QDropdownButtonStyle = "PDropdownRowButton";

    private const string QDropdownArrowPart = "PDropdownArrow";

    private const string QDropdownSurfacePart = "PDropdownSurface";

    private const string QDropdownFacePart = "PDropdownFace";

    private const string QDropdownPopupPart = "PART_Popup";

    private const string QDropdownSheetPart = "PDropdownSheet";

    private const string QDropdownButtonPart = "PDropdownRowButton";

    private const string QDropdownMinusPart = "PDropdownRowMinus";

    private const string QDropdownTextKey = "PDropdownText";

    private const string QDropdownMinusIcon = "/PAsset/PPanel/PExportMinus.svg";

    public static void QDropdownAttach(ComboBox lCombo)
    {
        ArgumentNullException.ThrowIfNull(lCombo);

        lCombo.Style = QContract.QContractSheetFind<Style>(QDropdownStyle);
        lCombo.ItemContainerStyle = QDropdownHookBuild(
            QContract.QContractSheetFind<Style>(QDropdownRowStyle),
            (lSender, _) => QDropdownRowLoad((ComboBoxItem)lSender));
        _ = lCombo.ApplyTemplate();
        QDropdownPartAttach(lCombo, QDropdownArrowPart, ToggleButton.IsCheckedProperty, "IsDropDownOpen");
        QDropdownPartAttach(lCombo, QDropdownSurfacePart, ToggleButton.IsCheckedProperty, "IsDropDownOpen");
        QDropdownPartAttach(lCombo, QDropdownFacePart, ContentPresenter.ContentProperty, "SelectionBoxItem");
        QDropdownPartAttach(
            lCombo, QDropdownFacePart, ContentPresenter.ContentTemplateProperty, "SelectionBoxItemTemplate");
        QDropdownPartAttach(
            lCombo, QDropdownFacePart, ContentPresenter.ContentStringFormatProperty, "SelectionBoxItemStringFormat");
        QDropdownPopupAttach(lCombo);
        QLook.QLookAttach(lCombo, QDropdownStyle);
    }

    public static void QDropdownActionAttach(ComboBox lCombo, string lActionTooltip)
    {
        ArgumentNullException.ThrowIfNull(lCombo);

        lCombo.Style = QContract.QContractSheetFind<Style>(QDropdownEditableStyle);
        lCombo.ItemContainerStyle = QDropdownHookBuild(
            QContract.QContractSheetFind<Style>(QDropdownActionStyle),
            (lSender, _) => QDropdownActionLoad((ComboBoxItem)lSender, lActionTooltip));
        _ = lCombo.ApplyTemplate();
        QDropdownPartAttach(lCombo, QDropdownArrowPart, ToggleButton.IsCheckedProperty, "IsDropDownOpen");
        QDropdownPopupAttach(lCombo);
        QLook.QLookAttach(lCombo, QDropdownStyle);
    }

    private static Style QDropdownHookBuild(Style lRowStyle, RoutedEventHandler lLoad)
    {
        return new Style(typeof(ComboBoxItem), lRowStyle)
        {
            Setters = { new EventSetter(FrameworkElement.LoadedEvent, lLoad) },
        };
    }

    private static void QDropdownPopupAttach(ComboBox lCombo)
    {
        QDropdownPartAttach(lCombo, QDropdownPopupPart, Popup.IsOpenProperty, "IsDropDownOpen");
        QDropdownPartAttach(lCombo, QDropdownSheetPart, FrameworkElement.MinWidthProperty, "ActualWidth");
    }

    private static void QDropdownPartAttach(
        ComboBox lCombo, string lPart, DependencyProperty lProperty, string lSource)
    {
        FrameworkElement lTarget = (FrameworkElement)lCombo.Template.FindName(lPart, lCombo);
        _ = lTarget.SetBinding(lProperty, new Binding(lSource) { Source = lCombo });
    }

    private static void QDropdownRowLoad(ComboBoxItem lRow)
    {
        lRow.Style = QContract.QContractSheetFind<Style>(QDropdownRowStyle);
        QLook.QLookAttach(lRow, QDropdownRowStyle);
    }

    private static void QDropdownActionLoad(ComboBoxItem lRow, string lActionTooltip)
    {
        lRow.Style = QContract.QContractSheetFind<Style>(QDropdownActionStyle);
        QLook.QLookAttach(lRow, QDropdownRowStyle);

        Button lButton = (Button)lRow.Template.FindName(QDropdownButtonPart, lRow);
        lButton.ToolTip = lActionTooltip;
        AutomationProperties.SetName(lButton, lActionTooltip);
        ((Image)lRow.Template.FindName(QDropdownMinusPart, lRow)).Source =
            QIcon.QIconRead(QDropdownMinusIcon, QContract.QContractSheetFind<Brush>(QDropdownTextKey));
        QLook.QLookAttach(lButton, QDropdownButtonStyle);
    }
}
