using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Cadroue.UIShell.PMainWindow;

internal static class PCheckbox
{
    private const double PCheckboxSize = 18;
    private const double PCheckboxCorner = 5;
    private const double PCheckboxGap = 10;

    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PSoftBrush = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC));
    private static readonly Brush PTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PAccentBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));
    private static readonly Brush PAccentDeepBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x6B, 0xDB));
    private static readonly Brush PMutedBrush = new SolidColorBrush(Color.FromRgb(0x9A, 0xA5, 0xB4));

    internal static void PCheckboxApply(CheckBox pCheckBox)
    {
        pCheckBox.Foreground = PTextBrush;
        pCheckBox.Background = Brushes.White;
        pCheckBox.BorderBrush = PLineBrush;
        pCheckBox.Cursor = System.Windows.Input.Cursors.Hand;
        pCheckBox.FocusVisualStyle = null;
        pCheckBox.VerticalContentAlignment = VerticalAlignment.Center;
        pCheckBox.Template = PCheckboxTemplateBuild();
    }

    private static ControlTemplate PCheckboxTemplateBuild()
    {
        var pTemplate = new ControlTemplate(typeof(CheckBox));

        var pRoot = new FrameworkElementFactory(typeof(StackPanel));
        pRoot.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        pRoot.SetValue(Panel.BackgroundProperty, Brushes.Transparent);
        pRoot.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

        var pBox = new FrameworkElementFactory(typeof(Border));
        pBox.Name = "PCheckboxBox";
        pBox.SetValue(FrameworkElement.WidthProperty, PCheckboxSize);
        pBox.SetValue(FrameworkElement.HeightProperty, PCheckboxSize);
        pBox.SetValue(Border.BackgroundProperty, Brushes.White);
        pBox.SetValue(Border.BorderBrushProperty, PLineBrush);
        pBox.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        pBox.SetValue(Border.CornerRadiusProperty, new CornerRadius(PCheckboxCorner));
        pBox.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pBox.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

        var pTick = new FrameworkElementFactory(typeof(Path));
        pTick.Name = "PCheckboxTick";
        pTick.SetValue(Path.DataProperty, Geometry.Parse("M 0,4.2 L 3.2,7.4 L 8.6,1.2"));
        pTick.SetValue(Shape.StrokeProperty, Brushes.White);
        pTick.SetValue(Shape.StrokeThicknessProperty, 1.9);
        pTick.SetValue(Shape.StrokeStartLineCapProperty, PenLineCap.Round);
        pTick.SetValue(Shape.StrokeEndLineCapProperty, PenLineCap.Round);
        pTick.SetValue(Shape.StrokeLineJoinProperty, PenLineJoin.Round);
        pTick.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pTick.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pTick.SetValue(UIElement.OpacityProperty, 0.0);

        var pDash = new FrameworkElementFactory(typeof(Border));
        pDash.Name = "PCheckboxDash";
        pDash.SetValue(FrameworkElement.WidthProperty, 9.0);
        pDash.SetValue(FrameworkElement.HeightProperty, 2.0);
        pDash.SetValue(Border.BackgroundProperty, Brushes.White);
        pDash.SetValue(Border.CornerRadiusProperty, new CornerRadius(1));
        pDash.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pDash.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pDash.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);

        var pGlyphGrid = new FrameworkElementFactory(typeof(Grid));
        pGlyphGrid.AppendChild(pTick);
        pGlyphGrid.AppendChild(pDash);
        pBox.AppendChild(pGlyphGrid);

        pRoot.AppendChild(pBox);

        var pLabel = new FrameworkElementFactory(typeof(ContentPresenter));
        pLabel.Name = "PCheckboxLabel";
        pLabel.SetValue(FrameworkElement.MarginProperty, new Thickness(PCheckboxGap, 0, 0, 0));
        pLabel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pLabel.SetValue(TextElement.ForegroundProperty, PTextBrush);
        pRoot.AppendChild(pLabel);

        pTemplate.VisualTree = pRoot;
        PCheckboxTriggerAdd(pTemplate);
        return pTemplate;
    }

    private static void PCheckboxTriggerAdd(ControlTemplate pTemplate)
    {
        var pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHover.Setters.Add(new Setter(Border.BorderBrushProperty, PAccentBrush, "PCheckboxBox"));
        pHover.Setters.Add(new Setter(Border.BackgroundProperty, PSoftBrush, "PCheckboxBox"));
        pTemplate.Triggers.Add(pHover);

        var pChecked = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
        pChecked.Setters.Add(new Setter(Border.BackgroundProperty, PAccentBrush, "PCheckboxBox"));
        pChecked.Setters.Add(new Setter(Border.BorderBrushProperty, PAccentBrush, "PCheckboxBox"));
        pChecked.Setters.Add(new Setter(UIElement.OpacityProperty, 1.0, "PCheckboxTick"));
        pTemplate.Triggers.Add(pChecked);

        var pCheckedHover = new MultiTrigger();
        pCheckedHover.Conditions.Add(new Condition(ToggleButton.IsCheckedProperty, true));
        pCheckedHover.Conditions.Add(new Condition(UIElement.IsMouseOverProperty, true));
        pCheckedHover.Setters.Add(new Setter(Border.BackgroundProperty, PAccentDeepBrush, "PCheckboxBox"));
        pCheckedHover.Setters.Add(new Setter(Border.BorderBrushProperty, PAccentDeepBrush, "PCheckboxBox"));
        pTemplate.Triggers.Add(pCheckedHover);

        var pIndeterminate = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = null };
        pIndeterminate.Setters.Add(new Setter(Border.BackgroundProperty, PAccentBrush, "PCheckboxBox"));
        pIndeterminate.Setters.Add(new Setter(Border.BorderBrushProperty, PAccentBrush, "PCheckboxBox"));
        pIndeterminate.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "PCheckboxDash"));
        pTemplate.Triggers.Add(pIndeterminate);

        var pFocus = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
        pFocus.Setters.Add(new Setter(Border.BorderBrushProperty, PAccentBrush, "PCheckboxBox"));
        pTemplate.Triggers.Add(pFocus);

        var pDisabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        pDisabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
        pDisabled.Setters.Add(new Setter(TextElement.ForegroundProperty, PMutedBrush, "PCheckboxLabel"));
        pTemplate.Triggers.Add(pDisabled);
    }
}
