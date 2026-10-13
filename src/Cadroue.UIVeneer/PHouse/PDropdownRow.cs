using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIVeneer.PAsset;

namespace Cadroue.UIVeneer.PHouse;

internal static class PDropdownRow
{
    private static readonly Brush PDropdownHighlightBrush = new SolidColorBrush(Color.FromRgb(0xEC, 0xF3, 0xFF));

    internal static Style PDropdownStyleBuild()
    {
        return PDropdownStyleBuild(PDropdownRowBuild());
    }

    private static Style PDropdownStyleBuild(ControlTemplate pRowTemplate)
    {
        var pStyle = new Style(typeof(ComboBoxItem));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, pRowTemplate));
        return pStyle;
    }

    internal static Style PDropdownStyleBuild(string pActionTooltip)
    {
        return PDropdownStyleBuild(PDropdownActionBuild(pActionTooltip));
    }

    private static ControlTemplate PDropdownRowBuild()
    {
        var pTemplate = new ControlTemplate(typeof(ComboBoxItem));
        var pBorder = PDropdownBorderBuild(new Thickness(12, 6, 12, 6));

        var pDock = new FrameworkElementFactory(typeof(DockPanel));
        pBorder.AppendChild(pDock);
        pDock.AppendChild(PDropdownCheckBuild());

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        pContent.SetValue(ContentPresenter.MarginProperty, new Thickness(0, 0, 8, 0));
        pDock.AppendChild(pContent);

        pTemplate.VisualTree = pBorder;
        PDropdownRowAdd(pTemplate);
        return pTemplate;
    }

    private static ControlTemplate PDropdownActionBuild(string pActionTooltip)
    {
        var pTemplate = new ControlTemplate(typeof(ComboBoxItem));
        var pBorder = PDropdownBorderBuild(new Thickness(12, 6, 2, 6));

        var pDock = new FrameworkElementFactory(typeof(DockPanel));
        pBorder.AppendChild(pDock);

        var pAction = new FrameworkElementFactory(typeof(Button));
        pAction.SetValue(DockPanel.DockProperty, Dock.Right);
        pAction.SetValue(FrameworkElement.WidthProperty, 13.0);
        pAction.SetValue(FrameworkElement.HeightProperty, 13.0);
        pAction.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pAction.SetValue(Control.PaddingProperty, new Thickness(0));
        pAction.SetValue(FrameworkElement.StyleProperty, PButton.PButtonChromeCreate(false));
        pAction.SetValue(FrameworkElement.ToolTipProperty, pActionTooltip);
        pAction.SetValue(AutomationProperties.NameProperty, pActionTooltip);

        var pMinus = new FrameworkElementFactory(typeof(Image));
        pMinus.SetValue(
            Image.SourceProperty,
            PIcon.PIconRead("/PAsset/PPanel/PExportMinus.svg", PDropdown.PDropdownTextBrush));
        pMinus.SetValue(FrameworkElement.WidthProperty, 8.0);
        pMinus.SetValue(FrameworkElement.HeightProperty, 8.0);
        pMinus.SetValue(Image.StretchProperty, Stretch.Uniform);
        pAction.AppendChild(pMinus);
        pDock.AppendChild(pAction);

        FrameworkElementFactory pCheck = PDropdownCheckBuild();
        pCheck.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0));
        pDock.AppendChild(pCheck);

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        pContent.SetValue(ContentPresenter.MarginProperty, new Thickness(0, 0, 8, 0));
        pDock.AppendChild(pContent);

        pTemplate.VisualTree = pBorder;
        PDropdownRowAdd(pTemplate);
        return pTemplate;
    }

    private static FrameworkElementFactory PDropdownBorderBuild(Thickness pPadding)
    {
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.Name = "ItemBorder";
        pBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        pBorder.SetValue(FrameworkElement.MarginProperty, new Thickness(4));
        pBorder.SetValue(Border.PaddingProperty, pPadding);
        return pBorder;
    }

    private static FrameworkElementFactory PDropdownCheckBuild()
    {
        var pCheck = new FrameworkElementFactory(typeof(System.Windows.Shapes.Path));
        pCheck.Name = "CheckPath";
        pCheck.SetValue(DockPanel.DockProperty, Dock.Right);
        pCheck.SetValue(System.Windows.Shapes.Path.StrokeProperty, PDropdown.PDropdownAccentBrush);
        pCheck.SetValue(System.Windows.Shapes.Path.StrokeThicknessProperty, 1.8);
        pCheck.SetValue(System.Windows.Shapes.Path.StrokeStartLineCapProperty, PenLineCap.Round);
        pCheck.SetValue(System.Windows.Shapes.Path.StrokeEndLineCapProperty, PenLineCap.Round);
        pCheck.SetValue(System.Windows.Shapes.Path.DataProperty, Geometry.Parse("M 3 7 L 6 10 L 11 4"));
        pCheck.SetValue(System.Windows.Shapes.Path.WidthProperty, 12.0);
        pCheck.SetValue(System.Windows.Shapes.Path.HeightProperty, 12.0);
        pCheck.SetValue(System.Windows.Shapes.Path.StretchProperty, Stretch.Fill);
        pCheck.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
        pCheck.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        return pCheck;
    }

    private static void PDropdownRowAdd(ControlTemplate pTemplate)
    {
        var pHighlightTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHighlightTrigger.Setters.Add(new Setter(Border.BackgroundProperty, PDropdownHighlightBrush, "ItemBorder"));
        pTemplate.Triggers.Add(pHighlightTrigger);

        var pSelectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        pSelectedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, PDropdownHighlightBrush, "ItemBorder"));
        pSelectedTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "CheckPath"));
        pTemplate.Triggers.Add(pSelectedTrigger);
    }
}
