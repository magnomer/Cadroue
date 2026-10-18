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

        QPickerStyleApply(lHost);
        (ToggleButton lArrow, Popup lPopup, Panel lList) = QPickerPartsBuild(lHost);
        QPickerPopupAttach(lArrow, lPopup);
        QPickerItemsSet(lHost, lItems);
        foreach (QPickerItem lItem in lItems)
        {
            lList.Children.Add(QPickerRowBuild(lHost, lItem, lEmptyText, lChanged));
        }

        QLook.QLookAttach(lHost, QPickerControlStyle);
        QPickerControlUpdate(lHost, lEmptyText);
    }

    private static CheckBox QPickerRowBuild(
        Control lHost,
        QPickerItem lItem,
        string lEmptyText,
        Action<IReadOnlyList<string>>? lChanged)
    {
        CheckBox lBox = QContract.QContractSheetFind<CheckBox>(QPickerControlRow);
        QPickerRowUpdate(lBox, lItem);
        QPickerRowAttach(lBox, lHost, lItem.QPickerItemToken, lEmptyText, lChanged);
        return lBox;
    }

    public static IReadOnlyList<string> QPickerControlRead(Control lHost)
    {
        ArgumentNullException.ThrowIfNull(lHost);

        return QPicker.QPickerSelectionRead(QPickerItemsRead(lHost));
    }

    private static void QPickerStyleApply(Control lHost)
    {
        QScrollbar.QScrollbarAttach(lHost);
        lHost.Style = QContract.QContractSheetFind<Style>(QPickerControlStyle);
    }

    private static (ToggleButton, Popup, Panel) QPickerPartsBuild(Control lHost)
    {
        _ = lHost.ApplyTemplate();

        ToggleButton lArrow = (ToggleButton)lHost.Template.FindName(QPickerControlArrow, lHost);
        Popup lPopup = (Popup)lHost.Template.FindName(QLook.QLookPopupPart, lHost);
        Panel lList = (Panel)lHost.Template.FindName(QPickerControlList, lHost);
        lPopup.PlacementTarget = lHost;
        _ = ((FrameworkElement)lHost.Template.FindName(QPickerControlSheet, lHost)).SetBinding(
            FrameworkElement.MinWidthProperty,
            new Binding(nameof(FrameworkElement.ActualWidth)) { Source = lHost });
        return (lArrow, lPopup, lList);
    }

    private static void QPickerPopupAttach(ToggleButton lArrow, Popup lPopup)
    {
        QInteractionClick.QInteractionClickAttach(lArrow);
        lArrow.Checked += (_, _) => lPopup.IsOpen = true;
        lArrow.Unchecked += (_, _) => lPopup.IsOpen = false;
        lPopup.Closed += (_, _) => lArrow.IsChecked = false;
    }

    private static void QPickerRowUpdate(CheckBox lBox, QPickerItem lItem)
    {
        lBox.Content = lItem.QPickerItemLabel;
        lBox.IsChecked = lItem.QPickerItemChecked;
    }

    private static void QPickerRowAttach(
        CheckBox lBox,
        Control lHost,
        string lToken,
        string lEmptyText,
        Action<IReadOnlyList<string>>? lChanged)
    {
        QInteractionClick.QInteractionClickAttach(lBox);
        QCheckbox.QCheckboxAttach(lBox);
        lBox.Checked += (_, _) => QPickerRowHandle(lHost, lToken, true, lEmptyText, lChanged);
        lBox.Unchecked += (_, _) => QPickerRowHandle(lHost, lToken, false, lEmptyText, lChanged);
    }

    private static void QPickerRowHandle(
        Control lHost,
        string lToken,
        bool lChecked,
        string lEmptyText,
        Action<IReadOnlyList<string>>? lChanged)
    {
        QPickerItemsSet(lHost, QPicker.QPickerItemSet(QPickerItemsRead(lHost), lToken, lChecked));
        QPickerControlUpdate(lHost, lEmptyText);
        lChanged?.Invoke(QPickerControlRead(lHost));
    }

    private static IReadOnlyList<QPickerItem> QPickerItemsRead(Control lHost) => (IReadOnlyList<QPickerItem>)lHost.Tag;

    private static void QPickerItemsSet(Control lHost, IReadOnlyList<QPickerItem> lItems) => lHost.Tag = lItems;

    private static void QPickerControlUpdate(Control lHost, string lEmptyText)
    {
        (string lText, bool lEmpty) = QPicker.QPickerSummaryRead(QPickerItemsRead(lHost), lEmptyText);
        TextBlock lSummary = (TextBlock)lHost.Template.FindName(QPickerControlSummary, lHost);
        QPickerSummaryUpdate(lSummary, lText);
        QPickerSummaryApply(lSummary, lEmpty);
    }

    private static void QPickerSummaryUpdate(TextBlock lSummary, string lText) => lSummary.Text = lText;

    private static void QPickerSummaryApply(TextBlock lSummary, bool lEmpty) =>
        lSummary.SetResourceReference(TextBlock.ForegroundProperty, QPickerControlBrushes[lEmpty]);
}
