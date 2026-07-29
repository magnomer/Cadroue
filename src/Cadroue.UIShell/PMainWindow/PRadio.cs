using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Cadroue.UIShell.PMainWindow;

internal static class PRadio
{
    private const double PRadioSize = 18;
    private const double PRadioDotSize = 8;
    private const double PRadioGap = 10;

    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PSoftBrush = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC));
    private static readonly Brush PTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PAccentBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));
    private static readonly Brush PAccentDeepBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x6B, 0xDB));
    private static readonly Brush PMutedBrush = new SolidColorBrush(Color.FromRgb(0x9A, 0xA5, 0xB4));

    internal static void PRadioApply(RadioButton pRadioButton)
    {
        pRadioButton.Foreground = PTextBrush;
        pRadioButton.Background = Brushes.White;
        pRadioButton.BorderBrush = PLineBrush;
        pRadioButton.Cursor = System.Windows.Input.Cursors.Hand;
        pRadioButton.FocusVisualStyle = null;
        pRadioButton.VerticalContentAlignment = VerticalAlignment.Center;
        pRadioButton.Template = PRadioTemplateBuild();
    }

    private static ControlTemplate PRadioTemplateBuild()
    {
        var pTemplate = new ControlTemplate(typeof(RadioButton));

        var pRoot = new FrameworkElementFactory(typeof(StackPanel));
        pRoot.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        pRoot.SetValue(Panel.BackgroundProperty, Brushes.Transparent);
        pRoot.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        var pRing = new FrameworkElementFactory(typeof(Border));
        pRing.Name = "PRadioRing";
        pRing.SetValue(FrameworkElement.WidthProperty, PRadioSize);
        pRing.SetValue(FrameworkElement.HeightProperty, PRadioSize);
        pRing.SetValue(Border.BackgroundProperty, Brushes.White);
        pRing.SetValue(Border.BorderBrushProperty, PLineBrush);
        pRing.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        pRing.SetValue(Border.CornerRadiusProperty, new CornerRadius(PRadioSize / 2));
        pRing.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pRing.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

        var pDot = new FrameworkElementFactory(typeof(Ellipse));
        pDot.Name = "PRadioDot";
        pDot.SetValue(FrameworkElement.WidthProperty, PRadioDotSize);
        pDot.SetValue(FrameworkElement.HeightProperty, PRadioDotSize);
        pDot.SetValue(Shape.FillProperty, Brushes.White);
        pDot.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pDot.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pDot.SetValue(UIElement.OpacityProperty, 0.0);
        pRing.AppendChild(pDot);

        pRoot.AppendChild(pRing);

        var pLabel = new FrameworkElementFactory(typeof(ContentPresenter));
        pLabel.Name = "PRadioLabel";
        pLabel.SetValue(FrameworkElement.MarginProperty, new Thickness(PRadioGap, 0, 0, 0));
        pLabel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pLabel.SetValue(TextElement.ForegroundProperty, PTextBrush);
        pRoot.AppendChild(pLabel);

        pTemplate.VisualTree = pRoot;
        PRadioTriggerAdd(pTemplate);
        return pTemplate;
    }

    private static void PRadioTriggerAdd(ControlTemplate pTemplate)
    {
        var pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHover.Setters.Add(new Setter(Border.BorderBrushProperty, PAccentBrush, "PRadioRing"));
        pHover.Setters.Add(new Setter(Border.BackgroundProperty, PSoftBrush, "PRadioRing"));
        pTemplate.Triggers.Add(pHover);

        var pChecked = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
        pChecked.Setters.Add(new Setter(Border.BackgroundProperty, PAccentBrush, "PRadioRing"));
        pChecked.Setters.Add(new Setter(Border.BorderBrushProperty, PAccentBrush, "PRadioRing"));
        pChecked.Setters.Add(new Setter(UIElement.OpacityProperty, 1.0, "PRadioDot"));
        pTemplate.Triggers.Add(pChecked);

        var pCheckedHover = new MultiTrigger();
        pCheckedHover.Conditions.Add(new Condition(ToggleButton.IsCheckedProperty, true));
        pCheckedHover.Conditions.Add(new Condition(UIElement.IsMouseOverProperty, true));
        pCheckedHover.Setters.Add(new Setter(Border.BackgroundProperty, PAccentDeepBrush, "PRadioRing"));
        pCheckedHover.Setters.Add(new Setter(Border.BorderBrushProperty, PAccentDeepBrush, "PRadioRing"));
        pTemplate.Triggers.Add(pCheckedHover);

        var pFocus = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
        pFocus.Setters.Add(new Setter(Border.BorderBrushProperty, PAccentBrush, "PRadioRing"));
        pTemplate.Triggers.Add(pFocus);

        var pDisabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        pDisabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
        pDisabled.Setters.Add(new Setter(TextElement.ForegroundProperty, PMutedBrush, "PRadioLabel"));
        pTemplate.Triggers.Add(pDisabled);
    }
}
