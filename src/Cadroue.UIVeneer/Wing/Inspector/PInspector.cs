using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PInspector : PPanel
{
    private static readonly Brush pInspectorTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));

    public const double PInspectorStripWidth = 48;

    public QInspector QInspector { get; }

    private TextBlock pInspectorTitleLabel = null!;
    private TextBlock pInspectorEmptyNotice = null!;
    private List<PInspectorSection> pInspectorSections = null!;
    private UIElement pInspectorPersistentRow = null!;
    private UIElement pInspectorFullBody = null!;
    private UIElement pInspectorStripBody = null!;
    private ScrollViewer pInspectorSectionsHost = null!;

    private PInspectorCrop pInspectorCrop = null!;
    private PInspectorCurve pInspectorCurve = null!;
    private PInspectorWhitebalance pInspectorWhitebalance = null!;
    private PInspectorLoudness pInspectorLoudness = null!;
    private PInspectorNoise pInspectorNoise = null!;
    private PInspectorPass pInspectorHighPass = null!;
    private PInspectorPass pInspectorLowPass = null!;
    private PInspectorEqualizer pInspectorEqualizer = null!;
    private PInspectorSkip pInspectorSkip = null!;
    private PBlank pInspectorBlank = null!;
    private readonly PSensor pInspectorSensor;

    public PInspector(QInspector lInspector) : base("")
    {
        QInspector = lInspector;
        (Border pHeader, Grid pBody) = PInspectorBodyBuild();
        pInspectorSensor = new PSensor(QInspector.QInspectorSensor);
        PInspectorFrameBuild(pHeader, pBody);
        PInspectorChangeAttach();
        PInspectorAttach();
        PInspectorUpdate();
    }

    public PSensorSweep PInspectorSweep { get; private set; } = null!;

    private (Border, Grid) PInspectorBodyBuild()
    {
        pInspectorTitleLabel = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Header.Title"),
            FontSize = 12,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pInspectorTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PInspectorButton.PInspectorButtonBuild(
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
            BorderBrush = QPanel.QPanelLineRead(),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
        };

        pInspectorEmptyNotice = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Empty.Notice"),
            FontSize = 12,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            Foreground = PInspectorLook.PInspectorMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 24, 16, 16)
        };

        return (pHeader, PInspectorSectionsBuild());
    }

    // The sections in panel order; the crop tool reaches the white balance pickers only when it is clicked.
    private Grid PInspectorSectionsBuild()
    {
        QInspectorAudio lAudio = QInspector.QInspectorAudio;
        var pBody = new Grid();
        pBody.Children.Add(pInspectorEmptyNotice);
        pInspectorCrop = new PInspectorCrop(
            QInspector, () => pInspectorWhitebalance.QWhitebalance.QWhitebalanceToolCancel());
        pBody.Children.Add(pInspectorCrop.PInspectorCropBody);
        var pTone = new PInspectorTone(QInspector);
        pBody.Children.Add(pTone.PInspectorBrightnessBody);
        pBody.Children.Add(pTone.PInspectorContrastBody);
        pBody.Children.Add(pTone.PInspectorSaturationBody);
        var pGamma = new QGamma(QInspector);
        pBody.Children.Add(pGamma.QGammaBody);
        var pExposure = new QExposure(QInspector);
        pBody.Children.Add(pExposure.QExposureBody);
        pInspectorCurve = new PInspectorCurve(QInspector.QInspectorVideo.QInspectorCurve);
        pBody.Children.Add(pInspectorCurve.PCurveBody);
        pInspectorWhitebalance = new PInspectorWhitebalance(QInspector);
        pBody.Children.Add(pInspectorWhitebalance.PWhitebalanceBody);
        var pVolume = new QVolume(QInspector);
        pBody.Children.Add(pVolume.QVolumeBody);
        pInspectorLoudness = new PInspectorLoudness(lAudio.QInspectorLoudness);
        pBody.Children.Add(pInspectorLoudness.PLoudnessBody);
        pInspectorNoise = new PInspectorNoise(lAudio.QInspectorNoise);
        pBody.Children.Add(pInspectorNoise.PNoiseBody);
        pInspectorHighPass = new PInspectorPass(
            lAudio.QInspectorHighpass, QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.HighApply"));
        pBody.Children.Add(pInspectorHighPass.PInspectorPassBody);
        pInspectorLowPass = new PInspectorPass(
            lAudio.QInspectorLowpass, QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.LowApply"));
        pBody.Children.Add(pInspectorLowPass.PInspectorPassBody);
        pInspectorEqualizer = new PInspectorEqualizer(lAudio.QInspectorEqualizer);
        pBody.Children.Add(pInspectorEqualizer.PEqualizerBody);
        pInspectorSkip = new PInspectorSkip(QInspector.QInspectorSkip);
        pBody.Children.Add(pInspectorSkip.PSkipBody);
        pInspectorBlank = new PBlank(
            QInspector.QInspectorBlank, pInspectorWhitebalance.QWhitebalance.QWhitebalanceWheel);
        pBody.Children.Add(pInspectorBlank.PBlankBody);
        pInspectorSections = PInspectorSectionsCreate(pTone, pGamma, pExposure, pVolume);
        return pBody;
    }

    private void PInspectorFrameBuild(Border pHeader, Grid pBody)
    {
        pInspectorSensor.PSensorSections.ForEach(pSection => pBody.Children.Add(pSection.PSensorBody));

        var pScroll = new ScrollViewer
        {
            Content = pBody,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        pInspectorSectionsHost = pScroll;

        pInspectorPersistentRow = PInspectorPersistentBuild();

        PInspectorSweep = new PSensorSweep(QInspector.QInspectorSensor.QSensorSweep);
        UIElement pRunRow = PInspectorSweep.PSensorRunRow;

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
    }

    private void PInspectorChangeAttach()
    {
        QInspector.QInspectorSensor.QSensorChanged += pInspectorSensor.PSensorUpdate;
        QInspector.QInspectorChanged += PInspectorUpdate;
    }

    private void PInspectorAttach()
    {
        PInspectorStepAttach();
        PInspectorStepUpdate();
        pInspectorCrop.PInspectorCropAttach();
    }

    private void PInspectorStepAttach()
    {
        pInspectorCurve.PCurveChangeAttach();
        pInspectorWhitebalance.PWhitebalanceChangeAttach();
        pInspectorLoudness.PLoudnessChangeAttach();
        pInspectorNoise.PNoiseChangeAttach();
        pInspectorHighPass.PFilterChangeAttach();
        pInspectorLowPass.PFilterChangeAttach();
        pInspectorEqualizer.PEqualizerChangeAttach();
        pInspectorSkip.PSkipChangeAttach();
    }

    private void PInspectorStepUpdate()
    {
        pInspectorCurve.PCurveUpdate();
        pInspectorWhitebalance.PWhitebalanceUpdate();
        pInspectorLoudness.PLoudnessUpdate();
        pInspectorNoise.PNoiseUpdate();
        pInspectorHighPass.PFilterUpdate();
        pInspectorLowPass.PFilterUpdate();
        pInspectorEqualizer.PEqualizerUpdate();
        pInspectorSkip.PSkipUpdate();
    }

    public bool PInspectorMinimizedCheck() => QInspector.QInspectorMinimized;

    public void PInspectorMinimizeSet(bool pInspectorMinimizeRequest) =>
        QInspector.QInspectorMinimizedSet(pInspectorMinimizeRequest);

    public void PInspectorStepShow(string? pStepName) => QInspector.QInspectorStepSet(pStepName);

    public void PSensorLockSet(bool pSensorLocked) => pInspectorSectionsHost.IsEnabled = !pSensorLocked;

    private void PInspectorUpdate()
    {
        pInspectorFullBody.Visibility = PLook.PLookVisible[!QInspector.QInspectorMinimized];
        pInspectorStripBody.Visibility = PLook.PLookVisible[QInspector.QInspectorMinimized];
        pInspectorSensor.PSensorSections.ForEach(PSensorSectionShow);
        pInspectorBlank.PBlankBody.Visibility =
            PLook.PLookVisible[QInspector.QInspectorSensorCheck(QInspector.QInspectorBlank.QBlankKind)];
        pInspectorSections.ForEach(PInspectorSectionShow);
        pInspectorTitleLabel.Text = QInspector.QInspectorTitleRead();
        pInspectorPersistentRow.Visibility = PLook.PLookVisible[QInspector.QInspectorPersistentShown];
        pInspectorEmptyNotice.Visibility = PLook.PLookVisible[QInspector.QInspectorEmptyShown];
        pInspectorCrop.PInspectorCropUpdate();
    }

    private void PSensorSectionShow(PSensorSection pSection) => pSection.PSensorBody.Visibility =
        PLook.PLookVisible[QInspector.QInspectorSensorCheck(pSection.PSensorPlan.QSensorPlanKind)];

    private void PInspectorSectionShow(PInspectorSection pSection)
    {
        bool pShown = QInspector.QInspectorSectionCheck(pSection.PInspectorSectionKey);
        pSection.PInspectorSectionBody.Visibility = PLook.PLookVisible[pShown];
        pSection.PInspectorSectionPersistent.Visibility = PLook.PLookVisible[pShown];
    }

    private List<PInspectorSection> PInspectorSectionsCreate(
        PInspectorTone pTone, QGamma pGamma, QExposure pExposure, QVolume pVolume) =>
    [
        new("Crop", pInspectorCrop.PInspectorCropBody, pInspectorCrop.PInspectorPersistentBox),
        new("Brightness", pTone.PInspectorBrightnessBody, pTone.PInspectorBrightnessPersistent),
        new("Contrast", pTone.PInspectorContrastBody, pTone.PInspectorContrastPersistent),
        new("Saturation", pTone.PInspectorSaturationBody, pTone.PInspectorSaturationPersistent),
        new("Gamma", pGamma.QGammaBody, pGamma.QGammaPersistent),
        new("Exposure", pExposure.QExposureBody, pExposure.QExposurePersistent),
        new("Curve", pInspectorCurve.PCurveBody, pInspectorCurve.PCurvePersistent),
        new("Whitebalance", pInspectorWhitebalance.PWhitebalanceBody, pInspectorWhitebalance.PWhitebalancePersistent),
        new("Volume", pVolume.QVolumeBody, pVolume.QVolumePersistent),
        new("Normalize", pInspectorLoudness.PLoudnessBody, pInspectorLoudness.PLoudnessPersistent),
        new("Noise Reduction", pInspectorNoise.PNoiseBody, pInspectorNoise.PNoisePersistent),
        new("High Pass", pInspectorHighPass.PInspectorPassBody, pInspectorHighPass.PInspectorPassPersistent),
        new("Low Pass", pInspectorLowPass.PInspectorPassBody, pInspectorLowPass.PInspectorPassPersistent),
        new("Equalizer", pInspectorEqualizer.PEqualizerBody, pInspectorEqualizer.PEqualizerPersistent),
        new("No Processing", pInspectorSkip.PSkipBody, pInspectorSkip.PSkipPersistentBox)
    ];

    private UIElement PInspectorPersistentBuild()
    {
        var pPersistentPanel = new StackPanel { Visibility = Visibility.Collapsed };
        pPersistentPanel.Children.Add(new Border
        {
            Height = 1,
            Background = QPanel.QPanelLineRead(),
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
        Button pMaximizeButton = PInspectorButton.PInspectorButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Panel.ShowTooltip"),
            () => QInspector.QInspectorMinimizedSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }
}

internal sealed record PInspectorSection(
    string PInspectorSectionKey,
    StackPanel PInspectorSectionBody,
    CheckBox PInspectorSectionPersistent);
