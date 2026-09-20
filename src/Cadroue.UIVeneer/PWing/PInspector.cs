using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector : PPanel
{
    private static readonly FontFamily pInspectorFontFamily = new("Segoe UI");
    private static readonly Brush pInspectorTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pInspectorMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));

    private const double PInspectorLabelWidth = 80;
    private const double PInspectorFieldHeight = 26;
    private const double PInspectorRowHeight = 34;

    public const double PInspectorStripWidth = 48;

    private static readonly IReadOnlyDictionary<bool, HorizontalAlignment> pInspectorFieldAligns =
        new Dictionary<bool, HorizontalAlignment>
        {
            [true] = HorizontalAlignment.Center,
            [false] = HorizontalAlignment.Left
        };

    public LInspector LInspector { get; } = new();

    private readonly TextBlock pInspectorTitleLabel;
    private readonly TextBlock pInspectorEmptyNotice;
    private readonly List<PInspectorSection> pInspectorSections;
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
        pBody.Children.Add(PBlankBuild());
        pSensorSections = LSensor.LSensorPlans.Select(PSensorBuild).ToList();
        pSensorSections.ForEach(pSection => pBody.Children.Add(pSection.PSensorBody));
        LSensor.LSensorChange += PSensorUpdate;
        pInspectorSections = PInspectorSectionsCreate();

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

    public LTone LTone => LInspector.LInspectorTone;

    public LGamma LGamma => LInspector.LInspectorGamma;

    public LExposure LExposure => LInspector.LInspectorExposure;

    public LCurve LCurve => LInspector.LInspectorCurve;

    public LWhitebalance LWhitebalance => LInspector.LInspectorWhitebalance;

    public LVolume LVolume => LInspector.LInspectorAudio.LInspectorVolume;

    public LLoudness LLoudness => LInspector.LInspectorAudio.LInspectorLoudness;

    public LNoise LNoise => LInspector.LInspectorAudio.LInspectorNoise;

    public LFilter LFilterHigh => LInspector.LInspectorAudio.LInspectorHighpass;

    public LFilter LFilterLow => LInspector.LInspectorAudio.LInspectorLowpass;

    public LEqualizer LEqualizer => LInspector.LInspectorAudio.LInspectorEqualizer;

    public LSkip LSkip => LInspector.LInspectorSkip;

    private void PInspectorAttach()
    {
        LTone.LToneChange += PToneUpdate;
        LGamma.LGammaChange += PGammaUpdate;
        LExposure.LExposureChange += PExposureUpdate;
        LCurve.LCurveChange += PCurveUpdate;
        LWhitebalance.LWhitebalanceChange += PWhitebalanceUpdate;
        LVolume.LVolumeChange += PVolumeUpdate;
        LLoudness.LLoudnessChange += PLoudnessUpdate;
        LNoise.LNoiseChange += PNoiseUpdate;
        LFilterHigh.LFilterChange += () => PFilterUpdate(pInspectorHighPass);
        LFilterLow.LFilterChange += () => PFilterUpdate(pInspectorLowPass);
        LEqualizer.LEqualizerChange += PEqualizerUpdate;
        LSkip.LSkipChange += PSkipUpdate;
        PToneUpdate();
        PGammaUpdate();
        PExposureUpdate();
        PCurveUpdate();
        PWhitebalanceUpdate();
        PVolumeUpdate();
        PLoudnessUpdate();
        PNoiseUpdate();
        PFilterUpdate(pInspectorHighPass);
        PFilterUpdate(pInspectorLowPass);
        PEqualizerUpdate();
        PSkipUpdate();
        PInspectorCropAttach();
    }

    public bool PInspectorMinimizedCheck() => LInspector.LInspectorMinimized;

    public void PInspectorMinimizeSet(bool pInspectorMinimizeRequest) =>
        LInspector.LInspectorMinimizedSet(pInspectorMinimizeRequest);

    public void PInspectorStepShow(string? pStepName) => LInspector.LInspectorStepSet(pStepName);

    private void PInspectorUpdate()
    {
        pInspectorFullBody.Visibility = PLook.PLookVisible[!LInspector.LInspectorMinimized];
        pInspectorStripBody.Visibility = PLook.PLookVisible[LInspector.LInspectorMinimized];
        pSensorSections.ForEach(PSensorSectionShow);
        pBlankBody.Visibility = PLook.PLookVisible[LInspector.LInspectorSensorCheck(LBlank.LBlankKind)];
        pInspectorSections.ForEach(PInspectorSectionShow);
        pInspectorTitleLabel.Text = LInspector.LInspectorTitleRead();
        pInspectorPersistentRow.Visibility = PLook.PLookVisible[LInspector.LInspectorPersistentShown];
        pInspectorEmptyNotice.Visibility = PLook.PLookVisible[LInspector.LInspectorEmptyShown];
        PInspectorCropUpdate();
    }

    private void PSensorSectionShow(PSensorSection pSection) => pSection.PSensorBody.Visibility =
        PLook.PLookVisible[LInspector.LInspectorSensorCheck(pSection.PSensorPlan.LSensorPlanKind)];

    private void PInspectorSectionShow(PInspectorSection pSection)
    {
        bool pShown = LInspector.LInspectorSectionCheck(pSection.PInspectorSectionKey);
        pSection.PInspectorSectionBody.Visibility = PLook.PLookVisible[pShown];
        pSection.PInspectorSectionPersistent.Visibility = PLook.PLookVisible[pShown];
    }

    private List<PInspectorSection> PInspectorSectionsCreate() =>
    [
        new("Crop", pInspectorCropBody, pInspectorPersistentBox),
        new("Brightness", pInspectorBrightnessBody, pInspectorBrightnessPersistent),
        new("Contrast", pInspectorContrastBody, pInspectorContrastPersistent),
        new("Saturation", pInspectorSaturationBody, pInspectorSaturationPersistent),
        new("Gamma", pGammaBody, pGammaPersistent),
        new("Exposure", pExposureBody, pExposurePersistent),
        new("Curve", pCurveBody, pCurvePersistent),
        new("Whitebalance", pWhitebalanceBody, pWhitebalancePersistent),
        new("Volume", pInspectorVolumeBody, pInspectorVolumePersistent),
        new("Normalize", pLoudnessBody, pLoudnessPersistent),
        new("Noise Reduction", pNoiseBody, pNoisePersistent),
        new("High Pass", pInspectorHighPass.PInspectorPassBody, pInspectorHighPass.PInspectorPassPersistent),
        new("Low Pass", pInspectorLowPass.PInspectorPassBody, pInspectorLowPass.PInspectorPassPersistent),
        new("Equalizer", pEqualizerBody, pEqualizerPersistent),
        new("No Processing", pSkipBody, pSkipPersistentBox)
    ];

    private UIElement PInspectorPersistentBuild()
    {
        var pPersistentPanel = new StackPanel { Visibility = Visibility.Collapsed };
        pPersistentPanel.Children.Add(new Border
        {
            Height = 1,
            Background = PPanelLineBrush,
            Margin = new Thickness(12, 0, 12, 12)
        });
        pInspectorSections.ForEach(pSection => PInspectorPersistentAdd(pPersistentPanel, pSection));
        return pPersistentPanel;
    }

    private static void PInspectorPersistentAdd(StackPanel pPersistentPanel, PInspectorSection pSection)
    {
        pSection.PInspectorSectionPersistent.Margin = new Thickness(12, 0, 12, 12);
        pSection.PInspectorSectionPersistent.Visibility = Visibility.Collapsed;
        pPersistentPanel.Children.Add(pSection.PInspectorSectionPersistent);
    }

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
        pFieldContent.HorizontalAlignment = pInspectorFieldAligns[pFieldCenter];
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

internal sealed record PInspectorSection(
    string PInspectorSectionKey,
    StackPanel PInspectorSectionBody,
    CheckBox PInspectorSectionPersistent);
