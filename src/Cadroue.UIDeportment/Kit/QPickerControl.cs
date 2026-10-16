using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Cadroue.UIDeportment;

public static class QPickerControl
{
    private const string QPickerControlStyle = "PPicker";

    private const string QPickerControlRow = "PPickerRow";

    private const string QPickerControlArrow = "PPickerArrow";

    private const string QPickerControlSummary = "PPickerSummary";

    private const string QPickerControlSheet = "PPickerSheet";

    private const string QPickerControlList = "PPickerList";

    private static readonly IReadOnlyDictionary<bool, string> QPickerControlBrushes = new Dictionary<bool, string>
    {
        [true] = "PPickerMuted",
        [false] = "PPickerText",
    };

    public static void QPickerControlAttach(
        Control lHost,
        IReadOnlyList<QPickerItem> lItems,
        string lEmptyText,
        Action<IReadOnlyList<string>>? lChanged = null)
    {
        ArgumentNullException.ThrowIfNull(lHost);
        ArgumentNullException.ThrowIfNull(lItems);

        QScrollbar.QScrollbarAttach(lHost);
        lHost.Style = QContract.QContractSheetFind<Style>(QPickerControlStyle);
        _ = lHost.ApplyTemplate();

        ToggleButton lArrow = (ToggleButton)lHost.Template.FindName(QPickerControlArrow, lHost);
        Popup lPopup = (Popup)lHost.Template.FindName(QLook.QLookPopupPart, lHost);
        Panel lList = (Panel)lHost.Template.FindName(QPickerControlList, lHost);
        QInteractionClick.QInteractionClickAttach(lArrow);
        lArrow.Checked += (_, _) => lPopup.IsOpen = true;
        lArrow.Unchecked += (_, _) => lPopup.IsOpen = false;
        lPopup.PlacementTarget = lHost;
        lPopup.Closed += (_, _) => lArrow.IsChecked = false;
        _ = ((FrameworkElement)lHost.Template.FindName(QPickerControlSheet, lHost)).SetBinding(
            FrameworkElement.MinWidthProperty,
            new Binding(nameof(FrameworkElement.ActualWidth)) { Source = lHost });

        lHost.Tag = lItems;
        Action<string, bool> lToggle = (lToken, lChecked) =>
        {
            lHost.Tag = QPicker.QPickerItemSet((IReadOnlyList<QPickerItem>)lHost.Tag, lToken, lChecked);
            QPickerControlUpdate(lHost, lEmptyText);
            lChanged?.Invoke(QPickerControlRead(lHost));
        };
        foreach (QPickerItem lItem in lItems)
        {
            CheckBox lBox = QContract.QContractSheetFind<CheckBox>(QPickerControlRow);
            lBox.Content = lItem.QPickerItemLabel;
            lBox.IsChecked = lItem.QPickerItemChecked;
            QInteractionClick.QInteractionClickAttach(lBox);
            QCheckbox.QCheckboxAttach(lBox);
            lBox.Checked += (_, _) => lToggle(lItem.QPickerItemToken, true);
            lBox.Unchecked += (_, _) => lToggle(lItem.QPickerItemToken, false);
            lList.Children.Add(lBox);
        }

        QLook.QLookAttach(lHost, QPickerControlStyle);
        QPickerControlUpdate(lHost, lEmptyText);
    }

    public static IReadOnlyList<string> QPickerControlRead(Control lHost)
    {
        ArgumentNullException.ThrowIfNull(lHost);

        return QPicker.QPickerSelectionRead((IReadOnlyList<QPickerItem>)lHost.Tag);
    }

    private static void QPickerControlUpdate(Control lHost, string lEmptyText)
    {
        (string lText, bool lEmpty) = QPicker.QPickerSummaryRead((IReadOnlyList<QPickerItem>)lHost.Tag, lEmptyText);
        TextBlock lSummary = (TextBlock)lHost.Template.FindName(QPickerControlSummary, lHost);
        lSummary.Text = lText;
        lSummary.SetResourceReference(TextBlock.ForegroundProperty, QPickerControlBrushes[lEmpty]);
    }
}
