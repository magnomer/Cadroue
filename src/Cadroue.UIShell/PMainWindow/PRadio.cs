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
    private static readonly Brush PRadioSoftBrush = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC));
    private static readonly Brush PRadioTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PRadioAccentBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));
    private static readonly Brush PRadioDeepBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x6B, 0xDB));
    private static readonly Brush PRadioMutedBrush = new SolidColorBrush(Color.FromRgb(0x9A, 0xA5, 0xB4));
    private static readonly Brush PRadioSegmentBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xF3, 0xFA));
    private static readonly Brush PRadioSegmentHover = new SolidColorBrush(Color.FromRgb(0xE6, 0xEC, 0xF6));

    internal static Border PRadioSegmentBuild(params RadioButton[] pRadios)
    {
        var pStrip = new StackPanel { Orientation = Orientation.Horizontal };
        var pHost = new Border
        {
            BorderBrush = PLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            SnapsToDevicePixels = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = pStrip
        };

        for (int pIndex = 0; pIndex < pRadios.Length; pIndex++)
        {
            RadioButton pRadio = pRadios[pIndex];
            pRadio.Foreground = PRadioTextBrush;
            pRadio.Cursor = System.Windows.Input.Cursors.Hand;
            pRadio.FocusVisualStyle = null;
            pRadio.Margin = new Thickness(0);
            pRadio.Template = PRadioBandBuild(
                pIndex == 0, pIndex == pRadios.Length - 1, pIndex != 0);
            pStrip.Children.Add(pRadio);
        }

        return pHost;
    }

    private static ControlTemplate PRadioBandBuild(bool pFirst, bool pLast, bool pDivider)
    {
        var pTemplate = new ControlTemplate(typeof(RadioButton));

        var pBox = new FrameworkElementFactory(typeof(Border));
        pBox.Name = "PRadioSegmentBox";
        pBox.SetValue(Border.BackgroundProperty, PRadioSegmentBrush);
        pBox.SetValue(Border.BorderBrushProperty, PLineBrush);
        pBox.SetValue(Border.BorderThicknessProperty, new Thickness(pDivider ? 1 : 0, 0, 0, 0));
        pBox.SetValue(Border.CornerRadiusProperty,
            new CornerRadius(pFirst ? 5 : 0, pLast ? 5 : 0, pLast ? 5 : 0, pFirst ? 5 : 0));
        pBox.SetValue(Border.PaddingProperty, new Thickness(16, 4, 16, 4));
        pBox.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

        var pLabel = new FrameworkElementFactory(typeof(ContentPresenter));
        pLabel.Name = "PRadioSegmentLabel";
        pLabel.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pLabel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pLabel.SetValue(TextElement.ForegroundProperty, PRadioTextBrush);
        pBox.AppendChild(pLabel);

        pTemplate.VisualTree = pBox;

        var pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHover.Setters.Add(new Setter(Border.BackgroundProperty, PRadioSegmentHover, "PRadioSegmentBox"));
        pTemplate.Triggers.Add(pHover);

        var pChecked = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
        pChecked.Setters.Add(new Setter(Border.BackgroundProperty, PRadioAccentBrush, "PRadioSegmentBox"));
        pChecked.Setters.Add(new Setter(TextElement.ForegroundProperty, Brushes.White, "PRadioSegmentLabel"));
        pTemplate.Triggers.Add(pChecked);

        var pCheckedHover = new MultiTrigger();
        pCheckedHover.Conditions.Add(new Condition(ToggleButton.IsCheckedProperty, true));
        pCheckedHover.Conditions.Add(new Condition(UIElement.IsMouseOverProperty, true));
        pCheckedHover.Setters.Add(new Setter(Border.BackgroundProperty, PRadioDeepBrush, "PRadioSegmentBox"));
        pTemplate.Triggers.Add(pCheckedHover);

        var pDisabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        pDisabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
        pTemplate.Triggers.Add(pDisabled);

        return pTemplate;
    }

    internal static void PRadioApply(RadioButton pRadioButton)
    {
        pRadioButton.Foreground = PRadioTextBrush;
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
        pLabel.SetValue(TextElement.ForegroundProperty, PRadioTextBrush);
        pRoot.AppendChild(pLabel);

        pTemplate.VisualTree = pRoot;
        PRadioTriggerAdd(pTemplate);
        return pTemplate;
    }

    private static void PRadioTriggerAdd(ControlTemplate pTemplate)
    {
        var pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHover.Setters.Add(new Setter(Border.BorderBrushProperty, PRadioAccentBrush, "PRadioRing"));
        pHover.Setters.Add(new Setter(Border.BackgroundProperty, PRadioSoftBrush, "PRadioRing"));
        pTemplate.Triggers.Add(pHover);

        var pChecked = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
        pChecked.Setters.Add(new Setter(Border.BackgroundProperty, PRadioAccentBrush, "PRadioRing"));
        pChecked.Setters.Add(new Setter(Border.BorderBrushProperty, PRadioAccentBrush, "PRadioRing"));
        pChecked.Setters.Add(new Setter(UIElement.OpacityProperty, 1.0, "PRadioDot"));
        pTemplate.Triggers.Add(pChecked);

        var pCheckedHover = new MultiTrigger();
        pCheckedHover.Conditions.Add(new Condition(ToggleButton.IsCheckedProperty, true));
        pCheckedHover.Conditions.Add(new Condition(UIElement.IsMouseOverProperty, true));
        pCheckedHover.Setters.Add(new Setter(Border.BackgroundProperty, PRadioDeepBrush, "PRadioRing"));
        pCheckedHover.Setters.Add(new Setter(Border.BorderBrushProperty, PRadioDeepBrush, "PRadioRing"));
        pTemplate.Triggers.Add(pCheckedHover);

        var pFocus = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
        pFocus.Setters.Add(new Setter(Border.BorderBrushProperty, PRadioAccentBrush, "PRadioRing"));
        pTemplate.Triggers.Add(pFocus);

        var pDisabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        pDisabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
        pDisabled.Setters.Add(new Setter(TextElement.ForegroundProperty, PRadioMutedBrush, "PRadioLabel"));
        pTemplate.Triggers.Add(pDisabled);
    }
}
