using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PAssets;

namespace Cadroue.UIShell.PMainArea;

public sealed class PAction : UserControl
{
    private static readonly Brush pActionPositiveBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x64));
    private static readonly Brush pActionNegativeBrush = new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x45));
    public event Action<LWorkPriority>? PActionRun;

    public PAction()
    {
        var pPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Button pAddListButton = PActionButtonBuild("PActionAddList.svg", "Add List");
        Button pExecuteButton = PActionButtonBuild("PActionExecute.svg", "Execute");
        pAddListButton.Click += (_, _) => PActionRun?.Invoke(LWorkPriority.LWorkPriorityNormal);
        pExecuteButton.Click += (_, _) => PActionRun?.Invoke(LWorkPriority.LWorkPriorityHigh);
        pPanel.Children.Add(pAddListButton);
        pPanel.Children.Add(new Border { Width = 2 });
        pPanel.Children.Add(pExecuteButton);
        Content = new Border { Child = pPanel };
    }

    private static Button PActionButtonBuild(string pIconAssetName, string pLabelText)
    {
        var pStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        pStack.Children.Add(new Image
        {
            Source = PIcon.PIconRead($"/PAssets/PCompass/{pIconAssetName}", PActionAccentBrushRead(pLabelText)),
            Width = 24,
            Height = 24,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        pStack.Children.Add(new Border { Height = 1 });
        pStack.Children.Add(new TextBlock
        {
            Text = pLabelText,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(0x0D, 0x1B, 0x2F))
        });

        return new Button
        {
            Width = 58,
            Height = 58,
            Content = pStack,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
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
        pHoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Transparent, "pButtonBorder"));
        var pPressTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
        pPressTrigger.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Transparent, "pButtonBorder"));
        pTemplate.Triggers.Add(pHoverTrigger);
        pTemplate.Triggers.Add(pPressTrigger);
        return pTemplate;
    }

    private static Brush? PActionAccentBrushRead(string pLabelText) => pLabelText switch
    {
        "Add List" => pActionPositiveBrush,
        "Execute" => pActionNegativeBrush,
        _ => null
    };
}
