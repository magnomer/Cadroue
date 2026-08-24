using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PControlBar;

internal static class PMenu
{
    private static readonly Brush pMenuLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pMenuTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pMenuHighlightBrush = new SolidColorBrush(Color.FromRgb(0xEC, 0xF3, 0xFF));

    private const double PMenuIconSize = 16;

    static PMenu()
    {
        pMenuLineBrush.Freeze();
        pMenuTextBrush.Freeze();
        pMenuHighlightBrush.Freeze();
    }

    internal static ContextMenu PMenuCreate(UIElement pMenuTarget) => new()
    {
        PlacementTarget = pMenuTarget,
        Placement = PlacementMode.Bottom,
        VerticalOffset = 4,

        HasDropShadow = true,
        Background = Brushes.Transparent,
        BorderThickness = new Thickness(0),
        Template = PMenuTemplateCreate()
    };

    internal static ContextMenu PMenuContextCreate() => new()
    {
        HasDropShadow = true,
        Background = Brushes.Transparent,
        BorderThickness = new Thickness(0),
        Template = PMenuTemplateCreate()
    };

    internal static ImageSource PMenuIconRead(string pMenuIconPath)
        => PAssets.PIcon.PIconRead(pMenuIconPath, pMenuTextBrush);

    internal static MenuItem PMenuItemCreate(string pMenuText, ImageSource? pMenuIcon)
    {
        var pMenuItem = new MenuItem
        {
            Header = pMenuText,
            Style = PMenuStyleCreate()
        };
        if (pMenuIcon is not null)
        {
            pMenuItem.Icon = new Image
            {
                Source = pMenuIcon,
                Width = PMenuIconSize,
                Height = PMenuIconSize,
                Stretch = Stretch.Uniform
            };
        }

        return pMenuItem;
    }

    private static ControlTemplate PMenuTemplateCreate()
    {
        var pTemplate = new ControlTemplate(typeof(ContextMenu));
        var pCard = new FrameworkElementFactory(typeof(Border));
        pCard.SetValue(Border.BackgroundProperty, Brushes.White);
        pCard.SetValue(Border.BorderBrushProperty, pMenuLineBrush);
        pCard.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        pCard.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
        pCard.SetValue(Border.PaddingProperty, new Thickness(4));
        pCard.SetValue(UIElement.SnapsToDevicePixelsProperty, true);
        pCard.AppendChild(new FrameworkElementFactory(typeof(ItemsPresenter)));
        pTemplate.VisualTree = pCard;
        return pTemplate;
    }

    private static Style PMenuStyleCreate()
    {
        var pStyle = new Style(typeof(MenuItem));
        pStyle.Setters.Add(new EventSetter(
            MenuItem.ClickEvent,
            new RoutedEventHandler(PInteraction.PInteractionMenuHandle))
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
        pIcon.SetBinding(ContentPresenter.ContentProperty, new Binding("Icon") { RelativeSource = RelativeSource.TemplatedParent });
        pIcon.SetValue(DockPanel.DockProperty, Dock.Left);
        pIcon.SetValue(FrameworkElement.WidthProperty, PMenuIconSize);
        pIcon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 0));
        pIcon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pRowContent.AppendChild(pIcon);

        var pHeader = new FrameworkElementFactory(typeof(ContentPresenter));
        pHeader.SetBinding(ContentPresenter.ContentProperty, new Binding("Header") { RelativeSource = RelativeSource.TemplatedParent });
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
