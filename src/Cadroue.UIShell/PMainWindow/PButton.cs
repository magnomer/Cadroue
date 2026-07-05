using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PMainWindow;

public static class PMainButton
{
    private static readonly SolidColorBrush PButtonTextBrush = new(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly SolidColorBrush PButtonBorderBrush = new(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly SolidColorBrush PButtonNormalGreyBackgroundBrush = new(Color.FromRgb(0xF8, 0xFA, 0xFC));
    private static readonly SolidColorBrush PButtonNormalGreyHoverBrush = new(Color.FromRgb(0xF1, 0xF5, 0xF9));
    private static readonly SolidColorBrush PButtonNormalGreyPressedBrush = new(Color.FromRgb(0xE8, 0xEE, 0xF6));
    private static readonly SolidColorBrush PButtonNormalWhiteBackgroundBrush = new(Colors.White);
    private static readonly SolidColorBrush PButtonNormalWhiteHoverBrush = new(Color.FromRgb(0xF8, 0xFA, 0xFC));
    private static readonly SolidColorBrush PButtonNormalWhitePressedBrush = new(Color.FromRgb(0xF0, 0xF4, 0xFA));

    public static Style PButtonNormalGreyCreate()
    {
        var pStyle = PButtonBaseStyleCreate();
        pStyle.Setters.Add(new Setter(Control.MinHeightProperty, 38.0));
        pStyle.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, PButtonNormalGreyBackgroundBrush));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PButtonTextBrush));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, PButtonBorderBrush));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonTemplateCreate(PButtonNormalGreyHoverBrush, PButtonNormalGreyPressedBrush)));
        return pStyle;
    }

    public static Style PButtonNormalWhiteCreate()
    {
        var pStyle = PButtonNormalWhiteBaseStyleCreate();
        pStyle.Setters.Add(new Setter(Control.HeightProperty, 42.0));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 4, 12, 4)));
        return pStyle;
    }

    public static Style PButtonNormalIconWhiteCreate()
    {
        return PButtonNormalWhiteCreate();
    }

    public static Style PButtonIconWhiteCreate()
    {
        var pStyle = PButtonNormalWhiteBaseStyleCreate();
        pStyle.Setters.Add(new Setter(Control.WidthProperty, 44.0));
        pStyle.Setters.Add(new Setter(Control.MinWidthProperty, 44.0));
        pStyle.Setters.Add(new Setter(Control.HeightProperty, 40.0));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        return pStyle;
    }

    private static Style PButtonNormalWhiteBaseStyleCreate()
    {
        var pStyle = PButtonBaseStyleCreate();
        pStyle.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
        pStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, PButtonNormalWhiteBackgroundBrush));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PButtonTextBrush));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, PButtonBorderBrush));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonTemplateCreate(PButtonNormalWhiteHoverBrush, PButtonNormalWhitePressedBrush)));
        return pStyle;
    }

    private static Style PButtonBaseStyleCreate()
    {
        var pStyle = new Style(typeof(Button));
        pStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
        pStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));
        return pStyle;
    }

    private static ControlTemplate PButtonTemplateCreate(Brush pHoverBrush, Brush pPressedBrush)
    {
        var pTemplate = new ControlTemplate(typeof(Button));
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        pBorder.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        pBorder.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pBorder.AppendChild(pContent);
        pTemplate.VisualTree = pBorder;

        var pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHover.Setters.Add(new Setter(Control.BackgroundProperty, pHoverBrush));
        pTemplate.Triggers.Add(pHover);

        var pPressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
        pPressed.Setters.Add(new Setter(Control.BackgroundProperty, pPressedBrush));
        pTemplate.Triggers.Add(pPressed);
        return pTemplate;
    }
}
