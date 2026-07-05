using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Cadroue.UIShell.LWork;

namespace Cadroue.UIShell.PMainArea;

public sealed class PAction : UserControl
{
    public event Action<LWorkPriority>? PActionRun;

    public PAction()
    {
        var pPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Button pAddListButton = PActionButtonBuild("PActionAddList.png", "Add List");
        Button pExecuteButton = PActionButtonBuild("PActionExecute.png", "Execute");
        pAddListButton.Click += (_, _) => PActionRun?.Invoke(LWorkPriority.LWorkPriorityNormal);
        pExecuteButton.Click += (_, _) => PActionRun?.Invoke(LWorkPriority.LWorkPriorityHigh);
        pPanel.Children.Add(pAddListButton);
        pPanel.Children.Add(new Border { Width = 10 });
        pPanel.Children.Add(pExecuteButton);
        Content = new Border { Child = pPanel };
    }

    private static Button PActionButtonBuild(string pIconAssetName, string pLabelText)
    {
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(new Image
        {
            Source = new BitmapImage(new Uri($"pack://application:,,,/PAssets/PCompass/{pIconAssetName}", UriKind.Absolute)),
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        pStack.Children.Add(new Border { Height = 2 });
        pStack.Children.Add(new TextBlock
        {
            Text = pLabelText,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontWeight = FontWeights.Medium,
            Foreground = new SolidColorBrush(Color.FromRgb(0x0D, 0x1B, 0x2F))
        });

        return new Button
        {
            Width = 82,
            Height = 58,
            Content = pStack,
            Background = new SolidColorBrush(Color.FromRgb(0xFB, 0xFC, 0xFE)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD7, 0xDF, 0xEA)),
            BorderThickness = new Thickness(1),
            FocusVisualStyle = null,
            Padding = new Thickness(0),
            Cursor = Cursors.Hand,
            ToolTip = pLabelText,
            Template = PActionTemplateBuild()
        };
    }

    private static ControlTemplate PActionTemplateBuild()
    {
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.Name = "pButtonBorder";
        pBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        pBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
        pBorder.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        pBorder.SetValue(Border.SnapsToDevicePixelsProperty, true);

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        pContent.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        pBorder.AppendChild(pContent);

        var pTemplate = new ControlTemplate(typeof(Button)) { VisualTree = pBorder };
        var pHoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xF4, 0xF7, 0xFB)), "pButtonBorder"));
        var pPressTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
        pPressTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xEA, 0xF1, 0xFF)), "pButtonBorder"));
        pTemplate.Triggers.Add(pHoverTrigger);
        pTemplate.Triggers.Add(pPressTrigger);
        return pTemplate;
    }
}
