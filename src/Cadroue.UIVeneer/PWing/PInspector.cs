using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;
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

    public QInspector QInspector { get; }

    private readonly TextBlock pInspectorTitleLabel;
    private readonly TextBlock pInspectorEmptyNotice;
    private readonly List<PInspectorSection> pInspectorSections;
    private readonly UIElement pInspectorPersistentRow;
    private readonly UIElement pInspectorFullBody;
    private readonly UIElement pInspectorStripBody;
    private readonly ScrollViewer pInspectorSectionsHost;

    public PInspector(QInspector lInspector) : base("")
    {
        QInspector = lInspector;
        pInspectorTitleLabel = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Header.Title"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pInspectorTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PInspectorButtonBuild(
            "/PAsset/PPanel/PListMinimize.svg",
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Panel.HideTooltip"),
            () => QInspector.QInspectorMinimizedSet(true));
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
            Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Empty.Notice"),
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
        pSensorSections = QSensor.QSensorPlans.Select(PSensorBuild).ToList();
        pSensorSections.ForEach(pSection => pBody.Children.Add(pSection.PSensorBody));
        QSensor.QSensorChanged += PSensorUpdate;
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
        QInspector.QInspectorChanged += PInspectorUpdate;
        PInspectorAttach();
        PInspectorUpdate();
    }

    public QTone QTone => QInspector.QInspectorVideo.QInspectorTone;

    public QGamma QGamma => QInspector.QInspectorVideo.QInspectorGamma;

    public QExposure QExposure => QInspector.QInspectorVideo.QInspectorExposure;

    public QCurve QCurve => QInspector.QInspectorVideo.QInspectorCurve;

    public QWhitebalance QWhitebalance => QInspector.QInspectorVideo.QInspectorWhitebalance;

    public QVolume QVolume => QInspector.QInspectorAudio.QInspectorVolume;

    public QLoudness QLoudness => QInspector.QInspectorAudio.QInspectorLoudness;

    public QNoise QNoise => QInspector.QInspectorAudio.QInspectorNoise;

    public QFilter LFilterHigh => QInspector.QInspectorAudio.QInspectorHighpass;

    public QFilter LFilterLow => QInspector.QInspectorAudio.QInspectorLowpass;

    public QEqualizer QEqualizer => QInspector.QInspectorAudio.QInspectorEqualizer;

    public QSkip QSkip => QInspector.QInspectorSkip;

    private void PInspectorAttach()
    {
        QTone.QToneChanged += PToneUpdate;
        QGamma.QGammaChanged += PGammaUpdate;
        QExposure.QExposureChanged += PExposureUpdate;
        QCurve.QCurveChanged += PCurveUpdate;
        QWhitebalance.QWhitebalanceChanged += PWhitebalanceUpdate;
        QVolume.QVolumeChanged += PVolumeUpdate;
        QLoudness.QLoudnessChanged += PLoudnessUpdate;
        QNoise.QNoiseChanged += PNoiseUpdate;
        LFilterHigh.QFilterChanged += () => PFilterUpdate(pInspectorHighPass);
        LFilterLow.QFilterChanged += () => PFilterUpdate(pInspectorLowPass);
        QEqualizer.QEqualizerChanged += PEqualizerUpdate;
        QSkip.QSkipChanged += PSkipUpdate;
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

    public bool PInspectorMinimizedCheck() => QInspector.QInspectorMinimized;

    public void PInspectorMinimizeSet(bool pInspectorMinimizeRequest) =>
        QInspector.QInspectorMinimizedSet(pInspectorMinimizeRequest);

    public void PInspectorStepShow(string? pStepName) => QInspector.QInspectorStepSet(pStepName);

    private void PInspectorUpdate()
    {
        pInspectorFullBody.Visibility = PLook.PLookVisible[!QInspector.QInspectorMinimized];
        pInspectorStripBody.Visibility = PLook.PLookVisible[QInspector.QInspectorMinimized];
        pSensorSections.ForEach(PSensorSectionShow);
        pBlankBody.Visibility = PLook.PLookVisible[QInspector.QInspectorSensorCheck(QBlank.QBlankKind)];
        pInspectorSections.ForEach(PInspectorSectionShow);
        pInspectorTitleLabel.Text = QInspector.QInspectorTitleRead();
        pInspectorPersistentRow.Visibility = PLook.PLookVisible[QInspector.QInspectorPersistentShown];
        pInspectorEmptyNotice.Visibility = PLook.PLookVisible[QInspector.QInspectorEmptyShown];
        PInspectorCropUpdate();
    }

    private void PSensorSectionShow(PSensorSection pSection) => pSection.PSensorBody.Visibility =
        PLook.PLookVisible[QInspector.QInspectorSensorCheck(pSection.PSensorPlan.QSensorPlanKind)];

    private void PInspectorSectionShow(PInspectorSection pSection)
    {
        bool pShown = QInspector.QInspectorSectionCheck(pSection.PInspectorSectionKey);
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
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Panel.ShowTooltip"),
            () => QInspector.QInspectorMinimizedSet(false));
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
                Width = 20,
                Height = 20,
                Source = QIcon.QIconRead(pIconPath, pInspectorIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26
        };
        pButton.Click += (_, _) => pClick();
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
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
