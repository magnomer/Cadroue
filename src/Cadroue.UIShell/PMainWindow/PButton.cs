using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PMainWindow;

public static class PButton
{
    private static readonly SolidColorBrush PButtonTextBrush = new(Color.FromRgb(0x11, 0x18, 0x27));

    private static readonly SolidColorBrush PButtonChromeGlyph = new(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly SolidColorBrush PButtonChromeHover = new(Color.FromRgb(0xD3, 0xE1, 0xF2));
    private static readonly SolidColorBrush PButtonChromePressed = new(Color.FromRgb(0xC2, 0xD4, 0xEA));
    private static readonly SolidColorBrush PButtonCloseHover = new(Color.FromRgb(0xE8, 0x11, 0x23));

    private static readonly SolidColorBrush PButtonClosePressed = new(Color.FromRgb(0xC5, 0x0F, 0x1F));
    private static readonly SolidColorBrush PButtonBorderBrush = new(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly SolidColorBrush PButtonGreyFill = new(Color.FromRgb(0xF8, 0xFA, 0xFC));
    private static readonly SolidColorBrush PButtonGreyHover = new(Color.FromRgb(0xF1, 0xF5, 0xF9));
    private static readonly SolidColorBrush PButtonGreyPressed = new(Color.FromRgb(0xE8, 0xEE, 0xF6));
    private static readonly SolidColorBrush PButtonCommandHover = new(Color.FromRgb(0xEE, 0xF4, 0xFC));
    private static readonly SolidColorBrush PButtonCommandPressed = new(Color.FromRgb(0xDC, 0xE8, 0xF7));
    private static readonly SolidColorBrush PButtonPanelText = new(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly SolidColorBrush PButtonPanelMuted = new(Color.FromRgb(0x62, 0x6F, 0x83));
    private static readonly SolidColorBrush PButtonWhiteFill = new(Colors.White);
    private static readonly SolidColorBrush PButtonWhiteHover = new(Color.FromRgb(0xF8, 0xFA, 0xFC));
    private static readonly SolidColorBrush PButtonWhitePressed = new(Color.FromRgb(0xF0, 0xF4, 0xFA));

    public static Style PButtonGreyCreate()
    {
        var pStyle = PButtonBaseCreate();
        pStyle.Setters.Add(new Setter(Control.MinHeightProperty, 38.0));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, PButtonGreyFill));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PButtonTextBrush));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, PButtonBorderBrush));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonTemplateCreate(PButtonGreyHover, PButtonGreyPressed)));
        return pStyle;
    }

    public static Style PButtonSourceCreate()
    {
        var pStyle = PButtonBaseCreate();
        pStyle.Setters.Add(new Setter(Control.WidthProperty, 38.0));
        pStyle.Setters.Add(new Setter(Control.HeightProperty, 38.0));
        pStyle.Setters.Add(new Setter(Control.MinWidthProperty, 38.0));
        pStyle.Setters.Add(new Setter(Control.MinHeightProperty, 38.0));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, PButtonGreyFill));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PButtonTextBrush));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, PButtonBorderBrush));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonTemplateCreate(PButtonGreyHover, PButtonGreyPressed)));
        return pStyle;
    }

    public static Style PButtonCommandCreate()
    {
        var pStyle = PButtonBaseCreate();
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonTemplateCreate(PButtonCommandHover, PButtonCommandPressed)));
        return pStyle;
    }

    public static Style PButtonPanelCreate()
    {
        var pStyle = PButtonBaseCreate();
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PButtonPanelText));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonPanelBuild()));
        return pStyle;
    }

    private static ControlTemplate PButtonPanelBuild()
    {
        var pTemplate = new ControlTemplate(typeof(Button));
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pBorder.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pBorder.AppendChild(pContent);
        pTemplate.VisualTree = pBorder;

        var pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHover.Setters.Add(new Setter(Control.BackgroundProperty, PButtonGreyHover));
        pTemplate.Triggers.Add(pHover);

        var pDisabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        pDisabled.Setters.Add(new Setter(Control.ForegroundProperty, PButtonPanelMuted));
        pTemplate.Triggers.Add(pDisabled);
        return pTemplate;
    }

    public static Style PButtonWhiteCreate()
    {
        var pStyle = PButtonWhitePrepare();
        pStyle.Setters.Add(new Setter(Control.HeightProperty, 42.0));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 4, 12, 4)));
        return pStyle;
    }

    public static Style PButtonLabelCreate()
    {
        return PButtonWhiteCreate();
    }

    public static Style PButtonIconCreate()
    {
        var pStyle = PButtonWhitePrepare();
        pStyle.Setters.Add(new Setter(Control.WidthProperty, 44.0));
        pStyle.Setters.Add(new Setter(Control.MinWidthProperty, 44.0));
        pStyle.Setters.Add(new Setter(Control.HeightProperty, 40.0));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        return pStyle;
    }

    private static Style PButtonWhitePrepare()
    {
        var pStyle = PButtonBaseCreate();
        pStyle.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, PButtonWhiteFill));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PButtonTextBrush));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, PButtonBorderBrush));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonTemplateCreate(PButtonWhiteHover, PButtonWhitePressed)));
        return pStyle;
    }

    public static Style PButtonChromeCreate(bool pButtonClose)
    {
        return PButtonChromeCreate(pButtonClose, new CornerRadius(0));
    }

    public static Style PButtonChromeCreate(bool pButtonClose, CornerRadius pCornerRadius)
    {
        var pStyle = new Style(typeof(Button));
        pStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PButtonChromeGlyph));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonChromeBuild(pButtonClose, pCornerRadius)));
        return pStyle;
    }

    private static ControlTemplate PButtonChromeBuild(bool pButtonClose, CornerRadius pCornerRadius)
    {
        var pTemplate = new ControlTemplate(typeof(Button));
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.Name = "pChromeFrame";
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pBorder.SetValue(Border.CornerRadiusProperty, pCornerRadius);

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pBorder.AppendChild(pContent);
        pTemplate.VisualTree = pBorder;

        Brush pHoverBrush = pButtonClose ? PButtonCloseHover : PButtonChromeHover;
        Brush pPressedBrush = pButtonClose ? PButtonClosePressed : PButtonChromePressed;

        var pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHover.Setters.Add(new Setter(Border.BackgroundProperty, pHoverBrush, "pChromeFrame"));
        if (pButtonClose)
        {
            pHover.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
        }

        pTemplate.Triggers.Add(pHover);

        var pPressed = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
        pPressed.Setters.Add(new Setter(Border.BackgroundProperty, pPressedBrush, "pChromeFrame"));
        if (pButtonClose)
        {
            pPressed.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
        }

        pTemplate.Triggers.Add(pPressed);
        return pTemplate;
    }

    public static Style PButtonToggleCreate()
    {
        var pStyle = new Style(typeof(ToggleButton));
        pStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
        pStyle.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PButtonPanelText));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonToggleBuild()));
        return pStyle;
    }

    private static ControlTemplate PButtonToggleBuild()
    {
        var pTemplate = new ControlTemplate(typeof(ToggleButton));
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.Name = "ToggleBorder";
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        pBorder.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pBorder.AppendChild(pContent);
        pTemplate.VisualTree = pBorder;

        var pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHover.Setters.Add(new Setter(Border.BackgroundProperty, PButtonGreyHover, "ToggleBorder"));
        pTemplate.Triggers.Add(pHover);

        var pChecked = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
        pChecked.Setters.Add(new Setter(Border.BackgroundProperty, PButtonCommandPressed, "ToggleBorder"));
        pTemplate.Triggers.Add(pChecked);
        return pTemplate;
    }

    private static Style PButtonBaseCreate()
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
