using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Cadroue.UIVeneer.PHouse;

internal static class PRadio
{
    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PRadioTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PRadioAccentBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));
    private static readonly Brush PRadioDeepBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x6B, 0xDB));
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

        pRadios.ToList().ForEach(pRadio => PRadioSegmentApply(pRadio, pRadios, pStrip));

        return pHost;
    }

    private static void PRadioSegmentApply(RadioButton pRadio, RadioButton[] pRadios, StackPanel pStrip)
    {
        pRadio.Foreground = PRadioTextBrush;
        pRadio.Cursor = System.Windows.Input.Cursors.Hand;
        pRadio.FocusVisualStyle = null;
        pRadio.Margin = new Thickness(0);
        pRadio.Template = PRadioBandBuild(
            pRadios.Take(1).Contains(pRadio),
            pRadios.TakeLast(1).Contains(pRadio));
        pStrip.Children.Add(pRadio);
    }

    private static readonly IReadOnlyDictionary<bool, Thickness> PRadioDividerThickness =
        new Dictionary<bool, Thickness>
        {
            [true] = new Thickness(0),
            [false] = new Thickness(1, 0, 0, 0),
        };

    private static readonly IReadOnlyDictionary<(bool, bool), CornerRadius> PRadioCorners =
        new Dictionary<(bool, bool), CornerRadius>
        {
            [(true, true)] = new CornerRadius(5),
            [(true, false)] = new CornerRadius(5, 0, 0, 5),
            [(false, true)] = new CornerRadius(0, 5, 5, 0),
            [(false, false)] = new CornerRadius(0),
        };

    private static ControlTemplate PRadioBandBuild(bool pFirst, bool pLast)
    {
        var pTemplate = new ControlTemplate(typeof(RadioButton));

        var pBox = new FrameworkElementFactory(typeof(Border));
        pBox.Name = "PRadioSegmentBox";
        pBox.SetValue(Border.BackgroundProperty, PRadioSegmentBrush);
        pBox.SetValue(Border.BorderBrushProperty, PLineBrush);
        pBox.SetValue(Border.BorderThicknessProperty, PRadioDividerThickness[pFirst]);
        pBox.SetValue(Border.CornerRadiusProperty, PRadioCorners[(pFirst, pLast)]);
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
}
