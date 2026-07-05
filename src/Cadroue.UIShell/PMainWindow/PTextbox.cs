using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PMainWindow;

internal static class PTextbox
{
    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PAccentBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));

    internal static void PTextboxApply(TextBox pTextBox)
    {
        pTextBox.Background = Brushes.White;
        pTextBox.Foreground = PTextBrush;
        pTextBox.BorderBrush = PLineBrush;
        pTextBox.BorderThickness = new Thickness(1);
        pTextBox.FontSize = 14;
        pTextBox.Padding = new Thickness(10, 0, 10, 0);
        pTextBox.HorizontalContentAlignment = HorizontalAlignment.Left;
        pTextBox.VerticalContentAlignment = VerticalAlignment.Center;
        pTextBox.TextAlignment = TextAlignment.Left;
        pTextBox.SelectionBrush = PAccentBrush;
        pTextBox.FocusVisualStyle = null;
        ScrollViewer.SetHorizontalScrollBarVisibility(pTextBox, ScrollBarVisibility.Hidden);
        ScrollViewer.SetVerticalScrollBarVisibility(pTextBox, ScrollBarVisibility.Hidden);
        pTextBox.Template = PTextboxTemplateBuild();
    }

    private static ControlTemplate PTextboxTemplateBuild()
    {
        var pTemplate = new ControlTemplate(typeof(TextBox));
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.Name = "OuterBorder";
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        pBorder.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));

        var pGrid = new FrameworkElementFactory(typeof(Grid));
        var pContent = new FrameworkElementFactory(typeof(ScrollViewer));
        pContent.Name = "PART_ContentHost";
        pContent.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
        pContent.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        pContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pGrid.AppendChild(pContent);
        pBorder.AppendChild(pGrid);

        pTemplate.VisualTree = pBorder;

        var pFocusTrigger = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
        pFocusTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, PAccentBrush, "OuterBorder"));
        pTemplate.Triggers.Add(pFocusTrigger);
        return pTemplate;
    }
}
