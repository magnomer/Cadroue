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
    private QCurveSection qInspectorCurveSection = null!;
    private QWhitebalance qInspectorWhitebalance = null!;
    private QLoudnessSection qLoudnessSection = null!;
    private QNoiseSection qNoiseSection = null!;
    private QFilterSection qFilterSectionHighpass = null!;
    private QFilterSection qFilterSectionLowpass = null!;
    private QEqualizerSection qEqualizerSection = null!;
    private QSkipSection qSkipSection = null!;
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
            FontFamily = QTool.QToolFontRead(),
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
            FontFamily = QTool.QToolFontRead(),
            Foreground = QTool.QToolMutedRead(),
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
            QInspector, () => qInspectorWhitebalance.QWhitebalanceToolCancel());
        pBody.Children.Add(pInspectorCrop.PInspectorCropBody);
        var qTone = new QTone(QInspector);
        pBody.Children.Add(qTone.QToneBrightnessBody);
        pBody.Children.Add(qTone.QToneContrastBody);
        pBody.Children.Add(qTone.QToneSaturationBody);
        var pGamma = new QGamma(QInspector);
        pBody.Children.Add(pGamma.QGammaBody);
        var pExposure = new QExposure(QInspector);
        pBody.Children.Add(pExposure.QExposureBody);
        qInspectorCurveSection = new QCurveSection(QInspector.QInspectorVideo.QInspectorCurve);
        pBody.Children.Add(qInspectorCurveSection.QCurveSectionBody);
        qInspectorWhitebalance = new QWhitebalance(QInspector);
        pBody.Children.Add(qInspectorWhitebalance.QWhitebalanceBody);
        var pVolume = new QVolume(QInspector);
        pBody.Children.Add(pVolume.QVolumeBody);
        qLoudnessSection = new QLoudnessSection(lAudio.QInspectorLoudness);
        pBody.Children.Add(qLoudnessSection.QLoudnessSectionBody);
        qNoiseSection = new QNoiseSection(lAudio.QInspectorNoise);
        pBody.Children.Add(qNoiseSection.QNoiseSectionBody);
        qFilterSectionHighpass = new QFilterSection(lAudio.QInspectorHighpass, "Inspector.Pass.HighApply");
        pBody.Children.Add(qFilterSectionHighpass.QFilterSectionBody);
        qFilterSectionLowpass = new QFilterSection(lAudio.QInspectorLowpass, "Inspector.Pass.LowApply");
        pBody.Children.Add(qFilterSectionLowpass.QFilterSectionBody);
        qEqualizerSection = new QEqualizerSection(lAudio.QInspectorEqualizer);
        pBody.Children.Add(qEqualizerSection.QEqualizerSectionBody);
        qSkipSection = new QSkipSection(QInspector.QInspectorSkip);
        pBody.Children.Add(qSkipSection.QSkipSectionBody);
        pInspectorBlank = new PBlank(
            QInspector.QInspectorBlank, qInspectorWhitebalance.QWhitebalanceWheel);
        pBody.Children.Add(pInspectorBlank.PBlankBody);
        pInspectorSections = PInspectorSectionsCreate(qTone, pGamma, pExposure, pVolume);
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
        qInspectorCurveSection.QCurveSectionAttach();
        qInspectorWhitebalance.QWhitebalanceChangeAttach();
        qLoudnessSection.QLoudnessSectionAttach();
        qNoiseSection.QNoiseSectionAttach();
        qFilterSectionHighpass.QFilterSectionAttach();
        qFilterSectionLowpass.QFilterSectionAttach();
        qEqualizerSection.QEqualizerSectionAttach();
        qSkipSection.QSkipSectionAttach();
    }

    private void PInspectorStepUpdate()
    {
        qInspectorCurveSection.QCurveSectionUpdate();
        qInspectorWhitebalance.QWhitebalanceUpdate();
        qLoudnessSection.QLoudnessSectionUpdate();
        qNoiseSection.QNoiseSectionUpdate();
        qFilterSectionHighpass.QFilterSectionUpdate();
        qFilterSectionLowpass.QFilterSectionUpdate();
        qEqualizerSection.QEqualizerSectionUpdate();
        qSkipSection.QSkipSectionUpdate();
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
        QTone qTone, QGamma pGamma, QExposure pExposure, QVolume pVolume) =>
    [
        new("Crop", pInspectorCrop.PInspectorCropBody, pInspectorCrop.PInspectorPersistentBox),
        new("Brightness", qTone.QToneBrightnessBody, qTone.QToneBrightnessPersistent),
        new("Contrast", qTone.QToneContrastBody, qTone.QToneContrastPersistent),
        new("Saturation", qTone.QToneSaturationBody, qTone.QToneSaturationPersistent),
        new("Gamma", pGamma.QGammaBody, pGamma.QGammaPersistent),
        new("Exposure", pExposure.QExposureBody, pExposure.QExposurePersistent),
        new("Curve", qInspectorCurveSection.QCurveSectionBody, qInspectorCurveSection.QCurveSectionPersistent),
        new("Whitebalance", qInspectorWhitebalance.QWhitebalanceBody, qInspectorWhitebalance.QWhitebalancePersistent),
        new("Volume", pVolume.QVolumeBody, pVolume.QVolumePersistent),
        new("Normalize", qLoudnessSection.QLoudnessSectionBody, qLoudnessSection.QLoudnessSectionPersistent),
        new("Noise Reduction", qNoiseSection.QNoiseSectionBody, qNoiseSection.QNoiseSectionPersistent),
        new("High Pass", qFilterSectionHighpass.QFilterSectionBody, qFilterSectionHighpass.QFilterSectionPersistent),
        new("Low Pass", qFilterSectionLowpass.QFilterSectionBody, qFilterSectionLowpass.QFilterSectionPersistent),
        new("Equalizer", qEqualizerSection.QEqualizerSectionBody, qEqualizerSection.QEqualizerSectionPersistent),
        new("No Processing", qSkipSection.QSkipSectionBody, qSkipSection.QSkipSectionPersistent)
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
