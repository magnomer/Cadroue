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

    // The picker's items and their checked state; the picker's rows write it back as they are checked.
    internal static readonly DependencyProperty QPickerItemsProperty = DependencyProperty.RegisterAttached(
        "QPickerItems",
        typeof(IReadOnlyList<QPickerItem>),
        typeof(QPickerControl),
        new FrameworkPropertyMetadata(null));

    // The summary the picker shows while nothing is checked.
    internal static readonly DependencyProperty QPickerHintProperty = DependencyProperty.RegisterAttached(
        "QPickerHint",
        typeof(string),
        typeof(QPickerControl),
        new FrameworkPropertyMetadata(string.Empty));

    // Receives the checked tokens after each row is checked or unchecked.
    internal static readonly DependencyProperty QPickerHandlerProperty = DependencyProperty.RegisterAttached(
        "QPickerHandler",
        typeof(Action<IReadOnlyList<string>>),
        typeof(QPickerControl),
        new FrameworkPropertyMetadata(null));

    internal static void QPickerControlHook() =>
        EventManager.RegisterClassHandler(
            typeof(Control),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((lSender, _) => QPickerControlLoad((Control)lSender)),
            true);

    public static void QPickerControlAttach(
        Control lHost,
        IReadOnlyList<QPickerItem> lItems,
        string lEmptyText,
        Action<IReadOnlyList<string>>? lChanged = null)
    {
        ArgumentNullException.ThrowIfNull(lHost);
        ArgumentNullException.ThrowIfNull(lItems);

        QPickerItemsSet(lHost, lItems);
        lHost.SetValue(QPickerHintProperty, lEmptyText);
        lHost.SetValue(QPickerHandlerProperty, lChanged);
        lHost.Style = QContract.QContractSheetFind<Style>(QPickerControlStyle);
        QPickerControlLoad(lHost);
        QLook.QLookAttach(lHost);
    }

    // Builds a row per item the picker holds when it loads; items set later are not shown.
    private static void QPickerControlLoad(Control lHost)
    {
        if (QLook.QLookStyleResolve(lHost, [QPickerControlStyle]) is null)
        {
            return;
        }

        QScrollbar.QScrollbarAttach(lHost);
        (ToggleButton lArrow, Popup lPopup, Panel lList) = QPickerPartsBuild(lHost);
        QPickerPopupAttach(lArrow, lPopup);
        if (QPickerItemsRead(lHost) is not { } lItems)
        {
            return;
        }

        foreach (QPickerItem lItem in lItems)
        {
            lList.Children.Add(QPickerRowBuild(lHost, lItem));
        }

        QPickerControlUpdate(lHost);
    }

    private static CheckBox QPickerRowBuild(Control lHost, QPickerItem lItem)
    {
        CheckBox lBox = QContract.QContractSheetFind<CheckBox>(QPickerControlRow);
        QPickerRowUpdate(lBox, lItem);
        QPickerRowAttach(lBox, lHost, lItem.QPickerItemToken);
        return lBox;
    }

    public static IReadOnlyList<string> QPickerControlRead(Control lHost)
    {
        ArgumentNullException.ThrowIfNull(lHost);

        return QPicker.QPickerSelectionRead(QPickerItemsRead(lHost)!);
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

    // The checkbox style records the row's clicks.
    private static void QPickerRowAttach(CheckBox lBox, Control lHost, string lToken)
    {
        QCheckbox.QCheckboxAttach(lBox);
        lBox.Checked += (_, _) => QPickerRowHandle(lHost, lToken, true);
        lBox.Unchecked += (_, _) => QPickerRowHandle(lHost, lToken, false);
    }

    private static void QPickerRowHandle(Control lHost, string lToken, bool lChecked)
    {
        QPickerItemsSet(lHost, QPicker.QPickerItemSet(QPickerItemsRead(lHost)!, lToken, lChecked));
        QPickerControlUpdate(lHost);
        (lHost.GetValue(QPickerHandlerProperty) as Action<IReadOnlyList<string>>)?.Invoke(QPickerControlRead(lHost));
    }

    private static IReadOnlyList<QPickerItem>? QPickerItemsRead(Control lHost) =>
        (IReadOnlyList<QPickerItem>?)lHost.GetValue(QPickerItemsProperty);

    private static void QPickerItemsSet(Control lHost, IReadOnlyList<QPickerItem> lItems) =>
        lHost.SetValue(QPickerItemsProperty, lItems);

    private static void QPickerControlUpdate(Control lHost)
    {
        (string lText, bool lEmpty) = QPicker.QPickerSummaryRead(
            QPickerItemsRead(lHost)!, (string)lHost.GetValue(QPickerHintProperty));
        TextBlock lSummary = (TextBlock)lHost.Template.FindName(QPickerControlSummary, lHost);
        QPickerSummaryUpdate(lSummary, lText);
        QPickerSummaryApply(lSummary, lEmpty);
    }

    private static void QPickerSummaryUpdate(TextBlock lSummary, string lText) => lSummary.Text = lText;

    private static void QPickerSummaryApply(TextBlock lSummary, bool lEmpty) =>
        lSummary.SetResourceReference(TextBlock.ForegroundProperty, QPickerControlBrushes[lEmpty]);
}
