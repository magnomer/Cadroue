using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector : PPanel
{
    private static readonly FontFamily pInspectorFontFamily = new("Segoe UI");
    private static readonly Brush pInspectorTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pInspectorMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));

    private const double PInspectorLabelWidth = 58;
    private const double PInspectorFieldHeight = 26;

    private readonly TextBlock pInspectorTitleLabel;
    private readonly TextBlock pInspectorEmptyNotice;

    public PInspector() : base("")
    {
        pInspectorTitleLabel = new TextBlock
        {
            Text = "Inspector",
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pInspectorTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        var pHeader = new Border
        {
            Padding = new Thickness(12, 10, 12, 10),
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pInspectorTitleLabel
        };

        pInspectorEmptyNotice = new TextBlock
        {
            Text = "Select a processing step to edit its settings.",
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 24, 16, 16)
        };

        var pBody = new Grid();
        pBody.Children.Add(pInspectorEmptyNotice);
        pBody.Children.Add(PInspectorCropBodyBuild());

        var pScroll = new ScrollViewer
        {
            Content = pBody,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pScroll);

        FocusVisualStyle = null;
        Content = PPanelBorderBuild(pRoot);
        PInspectorRatioUpdate();
    }

    public void PInspectorStepShow(string? pStepName)
    {
        bool pCropSelected = pStepName == "Crop";
        pInspectorTitleLabel.Text = pCropSelected ? "Crop" : "Inspector";
        pInspectorCropBody.Visibility = pCropSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorEmptyNotice.Visibility = pCropSelected ? Visibility.Collapsed : Visibility.Visible;

        if (!pCropSelected && pInspectorCropTool.IsChecked == true)
        {
            PInspectorToolDisarm();
        }
    }

    private static UIElement PInspectorFieldBuild(string pFieldLabel, UIElement pFieldContent)
    {
        var pFieldPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 8)
        };
        pFieldPanel.Children.Add(PInspectorLabelBuild(pFieldLabel));
        pFieldPanel.Children.Add(pFieldContent);
        return pFieldPanel;
    }

    private static TextBlock PInspectorLabelBuild(string pFieldLabel) => new()
    {
        Text = pFieldLabel,
        Width = PInspectorLabelWidth,
        FontSize = 12,
        FontFamily = pInspectorFontFamily,
        Foreground = PPanelTextBrush,
        VerticalAlignment = VerticalAlignment.Center
    };
}
