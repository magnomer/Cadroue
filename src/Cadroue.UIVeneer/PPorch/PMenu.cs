using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPorch;

internal static class PMenu
{
    private static readonly Brush pMenuTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pMenuHighlightBrush = new SolidColorBrush(Color.FromRgb(0xEC, 0xF3, 0xFF));

    private const double PMenuIconSize = 20;

    static PMenu()
    {
        pMenuTextBrush.Freeze();
        pMenuHighlightBrush.Freeze();
    }

    internal static ImageSource PMenuIconRead(string pMenuIconPath)
        => QIcon.QIconRead(pMenuIconPath, pMenuTextBrush);

    internal static MenuItem PMenuItemCreate(string pMenuText) => new()
    {
        Header = pMenuText,
        Style = PMenuStyleCreate()
    };

    internal static MenuItem PMenuItemCreate(string pMenuText, ImageSource? pMenuIcon) => new()
    {
        Header = pMenuText,
        Style = PMenuStyleCreate(),
        Icon = pMenuIcon?.PMenuImageBuild()
    };

    internal static Image PMenuImageBuild(this ImageSource pMenuIcon) => new()
    {
        Source = pMenuIcon,
        Width = PMenuIconSize,
        Height = PMenuIconSize,
        Stretch = Stretch.Uniform
    };

    private static Style PMenuStyleCreate()
    {
        var pStyle = new Style(typeof(MenuItem));
        pStyle.Setters.Add(new EventSetter(
            MenuItem.ClickEvent,
            new RoutedEventHandler(QInteractionClick.QInteractionClickHandle))
        {
            HandledEventsToo = true
        });
        pStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
        pStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, pMenuTextBrush));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 7, 14, 7)));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PMenuItemBuild()));
        return pStyle;
    }

    private static ControlTemplate PMenuItemBuild()
    {
        var pTemplate = new ControlTemplate(typeof(MenuItem));
        var pRow = new FrameworkElementFactory(typeof(Border));
        pRow.Name = "pMenuRow";
        pRow.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pRow.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        pRow.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
        pRow.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

        var pRowContent = new FrameworkElementFactory(typeof(DockPanel));

        var pIcon = new FrameworkElementFactory(typeof(ContentPresenter));
        pIcon.Name = "pMenuIcon";
        pIcon.SetBinding(
            ContentPresenter.ContentProperty,
            new Binding("Icon") { RelativeSource = RelativeSource.TemplatedParent });
        pIcon.SetValue(DockPanel.DockProperty, Dock.Left);
        pIcon.SetValue(FrameworkElement.WidthProperty, PMenuIconSize);
        pIcon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 6, 0));
        pIcon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pRowContent.AppendChild(pIcon);

        var pHeader = new FrameworkElementFactory(typeof(ContentPresenter));
        pHeader.SetBinding(
            ContentPresenter.ContentProperty,
            new Binding("Header") { RelativeSource = RelativeSource.TemplatedParent });
        pHeader.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pRowContent.AppendChild(pHeader);

        pRow.AppendChild(pRowContent);
        pTemplate.VisualTree = pRow;

        var pHighlight = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
        pHighlight.Setters.Add(new Setter(Border.BackgroundProperty, pMenuHighlightBrush, "pMenuRow"));
        pTemplate.Triggers.Add(pHighlight);

        var pIconEmpty = new Trigger { Property = MenuItem.IconProperty, Value = null };
        pIconEmpty.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed, "pMenuIcon"));
        pTemplate.Triggers.Add(pIconEmpty);
        return pTemplate;
    }
}
