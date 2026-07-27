using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PMainWindow;

public static class PButton
{
    private static readonly SolidColorBrush PButtonTextBrush = new(Color.FromRgb(0x11, 0x18, 0x27));

    private static readonly SolidColorBrush PButtonChromeGlyphBrush = new(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly SolidColorBrush PButtonChromeHoverBrush = new(Color.FromRgb(0xD3, 0xE1, 0xF2));
    private static readonly SolidColorBrush PButtonChromePressedBrush = new(Color.FromRgb(0xC2, 0xD4, 0xEA));
    private static readonly SolidColorBrush PButtonCloseHoverBrush = new(Color.FromRgb(0xE8, 0x11, 0x23));

    private static readonly SolidColorBrush PButtonClosePressedBrush = new(Color.FromRgb(0xC5, 0x0F, 0x1F));
    private static readonly SolidColorBrush PButtonBorderBrush = new(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly SolidColorBrush PButtonNormalGreyBackgroundBrush = new(Color.FromRgb(0xF8, 0xFA, 0xFC));
    private static readonly SolidColorBrush PButtonNormalGreyHoverBrush = new(Color.FromRgb(0xF1, 0xF5, 0xF9));
    private static readonly SolidColorBrush PButtonNormalGreyPressedBrush = new(Color.FromRgb(0xE8, 0xEE, 0xF6));
    private static readonly SolidColorBrush PButtonCommandHoverBrush = new(Color.FromRgb(0xEE, 0xF4, 0xFC));
    private static readonly SolidColorBrush PButtonCommandPressedBrush = new(Color.FromRgb(0xDC, 0xE8, 0xF7));
    private static readonly SolidColorBrush PButtonPanelTextBrush = new(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly SolidColorBrush PButtonPanelMutedBrush = new(Color.FromRgb(0x62, 0x6F, 0x83));
    private static readonly SolidColorBrush PButtonNormalWhiteBackgroundBrush = new(Colors.White);
    private static readonly SolidColorBrush PButtonNormalWhiteHoverBrush = new(Color.FromRgb(0xF8, 0xFA, 0xFC));
    private static readonly SolidColorBrush PButtonNormalWhitePressedBrush = new(Color.FromRgb(0xF0, 0xF4, 0xFA));

    public static Style PButtonGreyCreate()
    {
        var pStyle = PButtonBaseCreate();
        pStyle.Setters.Add(new Setter(Control.MinHeightProperty, 38.0));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, PButtonNormalGreyBackgroundBrush));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PButtonTextBrush));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, PButtonBorderBrush));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonTemplateCreate(PButtonNormalGreyHoverBrush, PButtonNormalGreyPressedBrush)));
        return pStyle;
    }

    public static Style PButtonSourceCreate()
    {
        var pStyle = PButtonBaseCreate();
        pStyle.Setters.Add(new Setter(Control.WidthProperty, 38.0));
        pStyle.Setters.Add(new Setter(Control.HeightProperty, 38.0));
        pStyle.Setters.Add(new Setter(Control.MinWidthProperty, 38.0));
        pStyle.Setters.Add(new Setter(Control.MinHeightProperty, 38.0));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, PButtonNormalGreyBackgroundBrush));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PButtonTextBrush));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, PButtonBorderBrush));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonTemplateCreate(PButtonNormalGreyHoverBrush, PButtonNormalGreyPressedBrush)));
        return pStyle;
    }

    public static Style PButtonCommandCreate()
    {
        var pStyle = PButtonBaseCreate();
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonTemplateCreate(PButtonCommandHoverBrush, PButtonCommandPressedBrush)));
        return pStyle;
    }

    public static Style PButtonPanelCreate()
    {
        var pStyle = PButtonBaseCreate();
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PButtonPanelTextBrush));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonPanelTemplateCreate()));
        return pStyle;
    }

    private static ControlTemplate PButtonPanelTemplateCreate()
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
        pHover.Setters.Add(new Setter(Control.BackgroundProperty, PButtonNormalGreyHoverBrush));
        pTemplate.Triggers.Add(pHover);

        var pDisabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        pDisabled.Setters.Add(new Setter(Control.ForegroundProperty, PButtonPanelMutedBrush));
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
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, PButtonNormalWhiteBackgroundBrush));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PButtonTextBrush));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, PButtonBorderBrush));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        pStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonTemplateCreate(PButtonNormalWhiteHoverBrush, PButtonNormalWhitePressedBrush)));
        return pStyle;
    }

    public static Style PButtonChromeCreate(bool pButtonClose)
    {
        var pStyle = new Style(typeof(Button));
        pStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PButtonChromeGlyphBrush));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonChromeTemplateCreate(pButtonClose)));
        return pStyle;
    }

    private static ControlTemplate PButtonChromeTemplateCreate(bool pButtonClose)
    {
        var pTemplate = new ControlTemplate(typeof(Button));
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.Name = "pChromeFrame";
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pBorder.AppendChild(pContent);
        pTemplate.VisualTree = pBorder;

        Brush pHoverBrush = pButtonClose ? PButtonCloseHoverBrush : PButtonChromeHoverBrush;
        Brush pPressedBrush = pButtonClose ? PButtonClosePressedBrush : PButtonChromePressedBrush;

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
        pStyle.Setters.Add(new Setter(Control.ForegroundProperty, PButtonPanelTextBrush));
        pStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
        pStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        pStyle.Setters.Add(new Setter(Control.TemplateProperty, PButtonToggleTemplateCreate()));
        return pStyle;
    }

    private static ControlTemplate PButtonToggleTemplateCreate()
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
        pHover.Setters.Add(new Setter(Border.BackgroundProperty, PButtonNormalGreyHoverBrush, "ToggleBorder"));
        pTemplate.Triggers.Add(pHover);

        var pChecked = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
        pChecked.Setters.Add(new Setter(Border.BackgroundProperty, PButtonCommandPressedBrush, "ToggleBorder"));
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
