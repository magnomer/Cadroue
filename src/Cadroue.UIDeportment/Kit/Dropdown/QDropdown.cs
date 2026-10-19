using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Cadroue.UIDeportment;

public static class QDropdown
{
    internal const string QDropdownStyle = "PDropdown";

    internal const string QDropdownEditableStyle = "PDropdownEditable";

    internal const string QDropdownRowStyle = "PDropdownRow";

    private const string QDropdownArrowPart = "PDropdownArrow";

    private const string QDropdownSurfacePart = "PDropdownSurface";

    private const string QDropdownFacePart = "PDropdownFace";

    private const string QDropdownPopupPart = "PART_Popup";

    private const string QDropdownSheetPart = "PDropdownSheet";

    internal static void QDropdownHook() =>
        EventManager.RegisterClassHandler(
            typeof(ComboBox),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((lSender, _) => QDropdownLoad((ComboBox)lSender)),
            true);

    public static void QDropdownAttach(ComboBox lCombo)
    {
        ArgumentNullException.ThrowIfNull(lCombo);

        QDropdownStyleApply(lCombo);
        QDropdownLoad(lCombo);
        QLook.QLookAttach(lCombo);
    }

    private static void QDropdownStyleApply(ComboBox lCombo)
    {
        lCombo.Style = QContract.QContractSheetFind<Style>(QDropdownStyle);
        lCombo.ItemContainerStyle = QDropdownHookBuild(
            QContract.QContractSheetFind<Style>(QDropdownRowStyle),
            (lSender, _) => QDropdownRowLoad((ComboBoxItem)lSender));
    }

    internal static Style QDropdownHookBuild(Style lRowStyle, RoutedEventHandler lLoad)
    {
        return new Style(typeof(ComboBoxItem), lRowStyle)
        {
            Setters = { new EventSetter(FrameworkElement.LoadedEvent, lLoad) },
        };
    }

    // The editable template is BasedOn the plain one and holds neither its surface nor its face.
    internal static void QDropdownLoad(ComboBox lCombo)
    {
        if (QLook.QLookStyleResolve(lCombo, [QDropdownEditableStyle, QDropdownStyle]) is not string lStyle)
        {
            return;
        }

        _ = lCombo.ApplyTemplate();
        QDropdownPartAttach(lCombo, QDropdownArrowPart, ToggleButton.IsCheckedProperty, "IsDropDownOpen");
        if (lStyle == QDropdownStyle)
        {
            QDropdownPartAttach(lCombo, QDropdownSurfacePart, ToggleButton.IsCheckedProperty, "IsDropDownOpen");
            QDropdownPartAttach(lCombo, QDropdownFacePart, ContentPresenter.ContentProperty, "SelectionBoxItem");
            QDropdownPartAttach(
                lCombo, QDropdownFacePart, ContentPresenter.ContentTemplateProperty, "SelectionBoxItemTemplate");
            QDropdownPartAttach(
                lCombo,
                QDropdownFacePart,
                ContentPresenter.ContentStringFormatProperty,
                "SelectionBoxItemStringFormat");
        }

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
        QLook.QLookAttach(lRow);
    }
}
