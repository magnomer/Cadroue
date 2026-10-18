using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSList
{
    private const double PSListHeight = 300;

    internal static ListBox PSListBuild(string pSelected, params LLocalizationChoice[] pItems)
    {
        ListBox pList = PSListControlBuild(pItems);
        PSListSelectedSet(pList, PSListSelectedResolve(pSelected, pItems));
        return pList;
    }

    private static ListBox PSListControlBuild(LLocalizationChoice[] pItems)
    {
        var pList = new ListBox
        {
            ItemsSource = pItems,
            MinWidth = 260,
            Height = PSListHeight,
            HorizontalAlignment = HorizontalAlignment.Left,
            BorderBrush = PSField.PSFieldLine,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            Padding = new Thickness(3),
            ItemContainerStyle = PSListStyleCreate()
        };
        QScrollbar.QScrollbarAttach(pList);
        return pList;
    }

    private static LLocalizationChoice? PSListSelectedResolve(string pSelected, LLocalizationChoice[] pItems) =>
        pItems.FirstOrDefault(
            pItem => string.Equals(pItem.LLocalizationChoiceToken, pSelected, StringComparison.Ordinal))
            ?? pItems.FirstOrDefault();

    private static void PSListSelectedSet(ListBox pList, LLocalizationChoice? pItem) => pList.SelectedItem = pItem;

    private static Style PSListStyleCreate()
    {
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        pBorder.SetValue(Border.PaddingProperty, new Thickness(10, 6, 10, 6));
        pBorder.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));

        var pStyle = new Style(typeof(ListBoxItem));
        pStyle.Setters.Add(new Setter(
            Control.TemplateProperty,
            new ControlTemplate(typeof(ListBoxItem)) { VisualTree = pBorder }));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PSField.PSFieldText));
        pStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(1)));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));

        var pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHover.Setters.Add(new Setter(Control.BackgroundProperty, PSField.PSFieldSoft));
        pStyle.Triggers.Add(pHover);

        var pSelected = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        pSelected.Setters.Add(new Setter(Control.BackgroundProperty, PSField.PSFieldAccent));
        pSelected.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
        pStyle.Triggers.Add(pSelected);

        return pStyle;
    }
}
