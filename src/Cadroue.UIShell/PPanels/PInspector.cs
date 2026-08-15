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

    private const double PInspectorLabelWidth = 80;
    private const double PInspectorFieldHeight = 26;
    private const double PInspectorRowHeight = 34;

    public const double PInspectorStripWidth = 48;

    public event Action<bool>? PInspectorMinimizeChange;
    public event Action? PInspectorPlanChange;

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
            Text = LLocalization.LLocalizationTextRead("Inspector.Header.Title"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pInspectorTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PInspectorButtonBuild(
            "/PAssets/PPanels/PListMinimize.svg", LLocalization.LLocalizationTextRead("Inspector.Panel.HideTooltip"), () => PInspectorMinimizeSet(true));
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
            Text = LLocalization.LLocalizationTextRead("Inspector.Empty.Notice"),
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
        pBody.Children.Add(PCropBodyBuild());
        pBody.Children.Add(PToneBrightnessBuild());
        pBody.Children.Add(PToneContrastBuild());
        pBody.Children.Add(PToneSaturationBuild());
        pBody.Children.Add(PGammaBuild());
        pBody.Children.Add(PExposureBuild());
        pBody.Children.Add(PWhitebalanceBuild());
        pBody.Children.Add(PVolumeBodyBuild());
        pBody.Children.Add(PLoudnessBodyBuild());
        pBody.Children.Add(PNoiseBodyBuild());
        pBody.Children.Add(PFilterHighBuild());
        pBody.Children.Add(PFilterLowBuild());
        pBody.Children.Add(PEqualizerBodyBuild());
        pBody.Children.Add(PSkipBodyBuild());

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
        if (pInspectorMinimized)
        {
            PWhitebalanceToolReset();
        }

        pInspectorFullBody.Visibility = pInspectorMinimized ? Visibility.Collapsed : Visibility.Visible;
        pInspectorStripBody.Visibility = pInspectorMinimized ? Visibility.Visible : Visibility.Collapsed;
        PInspectorMinimizeChange?.Invoke(pInspectorMinimized);
    }

    private UIElement PInspectorStripBuild()
    {
        Button pMaximizeButton = PInspectorButtonBuild(
            "/PAssets/PPanels/PListMaximize.svg", LLocalization.LLocalizationTextRead("Inspector.Panel.ShowTooltip"), () => PInspectorMinimizeSet(false));
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
        bool pBrightnessSelected = pStepName == "Brightness";
        bool pContrastSelected = pStepName == "Contrast";
        bool pSaturationSelected = pStepName == "Saturation";
        bool pGammaSelected = pStepName == "Gamma";
        bool pExposureSelected = pStepName == "Exposure";
        bool pWhitebalanceSelected = pStepName == "Whitebalance";
        bool pVolumeSelected = pStepName == "Volume";
        bool pNormalizeSelected = pStepName == "Normalize";
        bool pNoiseSelected = pStepName == "Noise Reduction";
        bool pHighPassSelected = pStepName == "High Pass";
        bool pLowPassSelected = pStepName == "Low Pass";
        bool pEqualizerSelected = pStepName == "Equalizer";
        bool pSkipSelected = pStepName == "No Processing";
        bool pKnownSelected = pCropSelected || pBrightnessSelected || pContrastSelected || pSaturationSelected || pGammaSelected || pExposureSelected || pWhitebalanceSelected || pVolumeSelected || pNormalizeSelected
            || pNoiseSelected || pHighPassSelected || pLowPassSelected || pEqualizerSelected || pSkipSelected;

        pInspectorTitleLabel.Text = pStepName switch
        {
            "Crop" => LLocalization.LLocalizationTextRead("Inspector.Step.Crop"),
            "Brightness" => LLocalization.LLocalizationTextRead("Inspector.Step.Brightness"),
            "Contrast" => LLocalization.LLocalizationTextRead("Inspector.Step.Contrast"),
            "Saturation" => LLocalization.LLocalizationTextRead("Inspector.Step.Saturation"),
            "Gamma" => LLocalization.LLocalizationTextRead("Inspector.Step.Gamma"),
            "Exposure" => LLocalization.LLocalizationTextRead("Inspector.Step.Exposure"),
            "Whitebalance" => LLocalization.LLocalizationTextRead("Inspector.Step.Whitebalance"),
            "Volume" => LLocalization.LLocalizationTextRead("Inspector.Step.Volume"),
            "Normalize" => LLocalization.LLocalizationTextRead("Inspector.Step.Normalize"),
            "Noise Reduction" => LLocalization.LLocalizationTextRead("Inspector.Step.NoiseReduction"),
            "High Pass" => LLocalization.LLocalizationTextRead("Inspector.Step.HighPass"),
            "Low Pass" => LLocalization.LLocalizationTextRead("Inspector.Step.LowPass"),
            "Equalizer" => LLocalization.LLocalizationTextRead("Inspector.Step.Equalizer"),
            "No Processing" => LLocalization.LLocalizationTextRead("Inspector.Step.NoProcessing"),
            _ => LLocalization.LLocalizationTextRead("Inspector.Header.Title")
        };
        pInspectorCropBody.Visibility = pCropSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorBrightnessBody.Visibility = pBrightnessSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorContrastBody.Visibility = pContrastSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorSaturationBody.Visibility = pSaturationSelected ? Visibility.Visible : Visibility.Collapsed;
        pGammaBody.Visibility = pGammaSelected ? Visibility.Visible : Visibility.Collapsed;
        pExposureBody.Visibility = pExposureSelected ? Visibility.Visible : Visibility.Collapsed;
        pWhitebalanceBody.Visibility = pWhitebalanceSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorVolumeBody.Visibility = pVolumeSelected ? Visibility.Visible : Visibility.Collapsed;
        pLoudnessBody.Visibility = pNormalizeSelected ? Visibility.Visible : Visibility.Collapsed;
        pNoiseBody.Visibility = pNoiseSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorHighPass.PInspectorPassBody.Visibility = pHighPassSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorLowPass.PInspectorPassBody.Visibility = pLowPassSelected ? Visibility.Visible : Visibility.Collapsed;
        pEqualizerBody.Visibility = pEqualizerSelected ? Visibility.Visible : Visibility.Collapsed;
        pSkipBody.Visibility = pSkipSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorPersistentRow.Visibility = pKnownSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorPersistentBox.Visibility = pCropSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorBrightnessPersistent.Visibility = pBrightnessSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorContrastPersistent.Visibility = pContrastSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorSaturationPersistent.Visibility = pSaturationSelected ? Visibility.Visible : Visibility.Collapsed;
        pGammaPersistent.Visibility = pGammaSelected ? Visibility.Visible : Visibility.Collapsed;
        pExposurePersistent.Visibility = pExposureSelected ? Visibility.Visible : Visibility.Collapsed;
        pWhitebalancePersistent.Visibility = pWhitebalanceSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorVolumePersistent.Visibility = pVolumeSelected ? Visibility.Visible : Visibility.Collapsed;
        pLoudnessPersistent.Visibility = pNormalizeSelected ? Visibility.Visible : Visibility.Collapsed;
        pNoisePersistent.Visibility = pNoiseSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorHighPass.PInspectorPassPersistent.Visibility = pHighPassSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorLowPass.PInspectorPassPersistent.Visibility = pLowPassSelected ? Visibility.Visible : Visibility.Collapsed;
        pEqualizerPersistent.Visibility = pEqualizerSelected ? Visibility.Visible : Visibility.Collapsed;
        pSkipPersistentBox.Visibility = pSkipSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorEmptyNotice.Visibility = pKnownSelected ? Visibility.Collapsed : Visibility.Visible;

        if (!pCropSelected && pInspectorCropTool.IsChecked == true)
        {
            PInspectorToolReset();
        }

        if (!pWhitebalanceSelected)
        {
            PWhitebalanceToolReset();
        }
    }

    private static UIElement PInspectorFieldBuild(string pFieldLabel, UIElement pFieldContent)
    {
        var pFieldPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Height = PInspectorRowHeight,
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
