using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector : PPanel
{
    private static readonly FontFamily pInspectorFontFamily = new("Segoe UI");
    private static readonly Brush pInspectorTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pInspectorMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));

    private const double PInspectorLabelWidth = 58;
    private const double PInspectorFieldHeight = 26;

    public const double PInspectorStripWidth = 48;

    public event Action<bool>? PInspectorMinimizeChange;

    private readonly TextBlock pInspectorTitleLabel;
    private readonly TextBlock pInspectorEmptyNotice;
    private readonly UIElement pInspectorPersistentRow;
    private readonly UIElement pInspectorFullBody;
    private readonly UIElement pInspectorStripBody;
    private bool pInspectorMinimized;

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

        Button pMinimizeButton = PInspectorButtonBuild(
            "/PAssets/PPanels/PListMinimize.svg", "Hide the Inspector panel", () => PInspectorMinimizeSet(true));
        pMinimizeButton.HorizontalAlignment = HorizontalAlignment.Right;

        var pHeaderGrid = new Grid();
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pMinimizeButton, 1);
        pHeaderGrid.Children.Add(pInspectorTitleLabel);
        pHeaderGrid.Children.Add(pMinimizeButton);

        var pHeader = new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
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
        pBody.Children.Add(PInspectorVolumeBodyBuild());
        pBody.Children.Add(PInspectorNormalizeBodyBuild());
        pBody.Children.Add(PInspectorNoiseBodyBuild());
        pBody.Children.Add(PInspectorHighPassBodyBuild());
        pBody.Children.Add(PInspectorLowPassBodyBuild());

        var pScroll = new ScrollViewer
        {
            Content = pBody,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        pInspectorPersistentRow = PInspectorPersistentBuild();

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pRoot.Children.Add(pHeader);
        DockPanel.SetDock(pInspectorPersistentRow, Dock.Bottom);
        pRoot.Children.Add(pInspectorPersistentRow);
        pRoot.Children.Add(pScroll);

        pInspectorFullBody = pRoot;
        pInspectorStripBody = PInspectorStripBuild();
        pInspectorStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pInspectorFullBody);
        pBodyHost.Children.Add(pInspectorStripBody);

        FocusVisualStyle = null;
        Content = PPanelBorderBuild(pBodyHost);
        PInspectorRatioUpdate();
    }

    public bool PInspectorMinimizedCheck() => pInspectorMinimized;

    public void PInspectorMinimizeSet(bool pInspectorMinimizeRequest)
    {
        if (pInspectorMinimized == pInspectorMinimizeRequest)
        {
            return;
        }

        pInspectorMinimized = pInspectorMinimizeRequest;
        pInspectorFullBody.Visibility = pInspectorMinimized ? Visibility.Collapsed : Visibility.Visible;
        pInspectorStripBody.Visibility = pInspectorMinimized ? Visibility.Visible : Visibility.Collapsed;
        PInspectorMinimizeChange?.Invoke(pInspectorMinimized);
    }

    private UIElement PInspectorStripBuild()
    {
        Button pMaximizeButton = PInspectorButtonBuild(
            "/PAssets/PPanels/PListMaximize.svg", "Show the Inspector panel", () => PInspectorMinimizeSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    private static Button PInspectorButtonBuild(string pIconPath, string pTooltip, Action pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, pInspectorIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => pClick();
        return pButton;
    }

    public void PInspectorStepShow(string? pStepName)
    {
        bool pCropSelected = pStepName == "Crop";
        bool pVolumeSelected = pStepName == "Volume";
        bool pNormalizeSelected = pStepName == "Normalize";
        bool pNoiseSelected = pStepName == "Noise Reduction";
        bool pHighPassSelected = pStepName == "High Pass";
        bool pLowPassSelected = pStepName == "Low Pass";
        bool pKnownSelected = pCropSelected || pVolumeSelected || pNormalizeSelected
            || pNoiseSelected || pHighPassSelected || pLowPassSelected;

        pInspectorTitleLabel.Text = pStepName switch
        {
            "Crop" => "Crop",
            "Volume" => "Volume",
            "Normalize" => "Normalize",
            "Noise Reduction" => "Noise Reduction",
            "High Pass" => "High Pass",
            "Low Pass" => "Low Pass",
            _ => "Inspector"
        };
        pInspectorCropBody.Visibility = pCropSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorVolumeBody.Visibility = pVolumeSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorNormalizeBody.Visibility = pNormalizeSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorNoiseBody.Visibility = pNoiseSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorHighPass.PInspectorPassBody.Visibility = pHighPassSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorLowPass.PInspectorPassBody.Visibility = pLowPassSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorPersistentRow.Visibility = pCropSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorEmptyNotice.Visibility = pKnownSelected ? Visibility.Collapsed : Visibility.Visible;

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
