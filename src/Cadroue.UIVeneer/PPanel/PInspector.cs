using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

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

    public LInspector LInspector { get; } = new();

    private readonly TextBlock pInspectorTitleLabel;
    private readonly TextBlock pInspectorEmptyNotice;
    private readonly UIElement pInspectorPersistentRow;
    private readonly UIElement pInspectorFullBody;
    private readonly UIElement pInspectorStripBody;
    private readonly ScrollViewer pInspectorSectionsHost;

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
            "/PAsset/PPanel/PListMinimize.svg",
            LLocalization.LLocalizationTextRead("Inspector.Panel.HideTooltip"),
            () => LInspector.LInspectorMinimizedSet(true));
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
        pBody.Children.Add(PCurveBuild());
        pBody.Children.Add(PWhitebalanceBuild());
        pBody.Children.Add(PVolumeBodyBuild());
        pBody.Children.Add(PLoudnessBodyBuild());
        pBody.Children.Add(PNoiseBodyBuild());
        pBody.Children.Add(PFilterHighBuild());
        pBody.Children.Add(PFilterLowBuild());
        pBody.Children.Add(PEqualizerBodyBuild());
        pBody.Children.Add(PSkipBodyBuild());
        pBody.Children.Add(PSensorBuild(LDetectorKind.LDetectorKindBlank));
        pBody.Children.Add(PSensorBuild(LDetectorKind.LDetectorKindScene));
        pBody.Children.Add(PSensorBuild(LDetectorKind.LDetectorKindStill));
        pBody.Children.Add(PSensorBuild(LDetectorKind.LDetectorKindLuminance));
        pBody.Children.Add(PSensorBuild(LDetectorKind.LDetectorKindSilence));
        pBody.Children.Add(PSensorBuild(LDetectorKind.LDetectorKindVolume));

        var pScroll = new ScrollViewer
        {
            Content = pBody,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        pInspectorSectionsHost = pScroll;

        pInspectorPersistentRow = PInspectorPersistentBuild();

        UIElement pRunRow = PSensorRunBuild();

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pRoot.Children.Add(pHeader);
        DockPanel.SetDock(pRunRow, Dock.Bottom);
        pRoot.Children.Add(pRunRow);
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
        LInspector.LInspectorChange += PInspectorUpdate;
        PInspectorAttach();
        PInspectorUpdate();
    }

    private void PInspectorAttach()
    {
        PInspectorVideoAttach();
        PInspectorAudioAttach();
        PInspectorCropAttach();
    }

    public bool PInspectorMinimizedCheck() => LInspector.LInspectorMinimized;

    public void PInspectorMinimizeSet(bool pInspectorMinimizeRequest) =>
        LInspector.LInspectorMinimizedSet(pInspectorMinimizeRequest);

    public void PInspectorStepShow(string? pStepName) => LInspector.LInspectorStepSet(pStepName);

    private void PInspectorUpdate()
    {
        bool pMinimized = LInspector.LInspectorMinimized;
        bool pFlipped = pInspectorFullBody.Visibility != (pMinimized ? Visibility.Collapsed : Visibility.Visible);
        pInspectorFullBody.Visibility = pMinimized ? Visibility.Collapsed : Visibility.Visible;
        pInspectorStripBody.Visibility = pMinimized ? Visibility.Visible : Visibility.Collapsed;
        PInspectorSectionsUpdate();
        PInspectorCropUpdate();
        if (pMinimized || LInspector.LInspectorStep != "Whitebalance")
        {
            LWhitebalance.LWhitebalanceToolSet(false, LWhitebalance.LWhitebalanceTarget);
        }

        if (LInspector.LInspectorStep != "Crop")
        {
            LInspector.LInspectorToolSet(false);
        }

        if (pFlipped)
        {
            PInspectorMinimizeChange?.Invoke(pMinimized);
        }
    }

    private void PInspectorSectionsUpdate()
    {
        string? pStepName = LInspector.LInspectorStep;
        LDetectorKind? pDetectorKind = PSensorKindRead(pStepName);
        foreach (KeyValuePair<LDetectorKind, PSensorSection> pSensorEntry in pSensorSections)
        {
            pSensorEntry.Value.PSensorBody.Visibility =
                pSensorEntry.Key == pDetectorKind ? Visibility.Visible : Visibility.Collapsed;
        }

        bool pDetectorSelected = pDetectorKind is not null;
        bool pKnownSelected = false;
        foreach ((string pKey, StackPanel pSection, CheckBox pPersistent) in PInspectorSectionsRead())
        {
            bool pSelected = pStepName == pKey && !(pKey == "Volume" && pDetectorSelected);
            pKnownSelected |= pSelected;
            pSection.Visibility = pSelected ? Visibility.Visible : Visibility.Collapsed;
            pPersistent.Visibility = pSelected ? Visibility.Visible : Visibility.Collapsed;
        }

        pInspectorTitleLabel.Text = pDetectorKind is { } pDetectorTitleKind
            ? LLocalization.LLocalizationTextRead(PSensorTitleRead(pDetectorTitleKind))
            : LLocalization.LLocalizationTextRead(PInspectorTitleRead(pStepName));
        pInspectorPersistentRow.Visibility = pKnownSelected ? Visibility.Visible : Visibility.Collapsed;
        pInspectorEmptyNotice.Visibility = pKnownSelected || pDetectorSelected
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private IEnumerable<(string, StackPanel, CheckBox)> PInspectorSectionsRead()
    {
        yield return ("Crop", pInspectorCropBody, pInspectorPersistentBox);
        yield return ("Brightness", pInspectorBrightnessBody, pInspectorBrightnessPersistent);
        yield return ("Contrast", pInspectorContrastBody, pInspectorContrastPersistent);
        yield return ("Saturation", pInspectorSaturationBody, pInspectorSaturationPersistent);
        yield return ("Gamma", pGammaBody, pGammaPersistent);
        yield return ("Exposure", pExposureBody, pExposurePersistent);
        yield return ("Curve", pCurveBody, pCurvePersistent);
        yield return ("Whitebalance", pWhitebalanceBody, pWhitebalancePersistent);
        yield return ("Volume", pInspectorVolumeBody, pInspectorVolumePersistent);
        yield return ("Normalize", pLoudnessBody, pLoudnessPersistent);
        yield return ("Noise Reduction", pNoiseBody, pNoisePersistent);
        yield return ("High Pass", pInspectorHighPass.PInspectorPassBody, pInspectorHighPass.PInspectorPassPersistent);
        yield return ("Low Pass", pInspectorLowPass.PInspectorPassBody, pInspectorLowPass.PInspectorPassPersistent);
        yield return ("Equalizer", pEqualizerBody, pEqualizerPersistent);
        yield return ("No Processing", pSkipBody, pSkipPersistentBox);
    }

    private static string PInspectorTitleRead(string? pStepName) => pStepName switch
    {
        "Crop" => "Inspector.Step.Crop",
        "Brightness" => "Inspector.Step.Brightness",
        "Contrast" => "Inspector.Step.Contrast",
        "Saturation" => "Inspector.Step.Saturation",
        "Gamma" => "Inspector.Step.Gamma",
        "Exposure" => "Inspector.Step.Exposure",
        "Curve" => "Inspector.Step.Curve",
        "Whitebalance" => "Inspector.Step.Whitebalance",
        "Volume" => "Inspector.Step.Volume",
        "Normalize" => "Inspector.Step.Normalize",
        "Noise Reduction" => "Inspector.Step.NoiseReduction",
        "High Pass" => "Inspector.Step.HighPass",
        "Low Pass" => "Inspector.Step.LowPass",
        "Equalizer" => "Inspector.Step.Equalizer",
        "No Processing" => "Inspector.Step.NoProcessing",
        _ => "Inspector.Header.Title"
    };

    private UIElement PInspectorStripBuild()
    {
        Button pMaximizeButton = PInspectorButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            LLocalization.LLocalizationTextRead("Inspector.Panel.ShowTooltip"),
            () => LInspector.LInspectorMinimizedSet(false));
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

    private static UIElement PInspectorFieldBuild(
        string pFieldLabel, FrameworkElement pFieldContent, bool pFieldCenter)
    {
        var pFieldGrid = new Grid
        {
            Height = PInspectorRowHeight,
            Margin = new Thickness(0, 0, 0, 8)
        };
        TextBlock pFieldTag = PInspectorLabelBuild(pFieldLabel);
        pFieldTag.HorizontalAlignment = HorizontalAlignment.Left;
        pFieldContent.HorizontalAlignment = pFieldCenter
            ? HorizontalAlignment.Center
            : HorizontalAlignment.Left;
        pFieldGrid.Children.Add(pFieldTag);
        pFieldGrid.Children.Add(pFieldContent);
        return pFieldGrid;
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
