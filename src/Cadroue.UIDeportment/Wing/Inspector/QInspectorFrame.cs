using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public sealed class QInspectorFrame
{
    public QInspectorFrame(QInspector lInspector)
    {
        QInspector = lInspector;
    }

    public QInspector QInspector { get; }

    private FrameworkElement PInspector { get; set; } = null!;
    private TextBlock PInspectorTitle { get; set; } = null!;
    private TextBlock PInspectorVacancy { get; set; } = null!;
    private Grid PInspectorSections { get; set; } = null!;
    private ScrollViewer PInspectorScroller { get; set; } = null!;
    private StackPanel PInspectorPersistence { get; set; } = null!;
    private Button PInspectorMinimizer { get; set; } = null!;
    private Button PInspectorMaximizer { get; set; } = null!;
    private ContentControl PInspectorExecution { get; set; } = null!;
    private FrameworkElement PInspectorNarrow { get; set; } = null!;
    private FrameworkElement PInspectorExpanded { get; set; } = null!;

    public void QInspectorFrameBuild(FrameworkElement lSurface)
    {
        if (ReferenceEquals(PInspector, lSurface))
        {
            QInspectorFrameUpdate();
            return;
        }

        if (PInspector is not null)
        {
            PInspector.Loaded -= QInspectorFrameAttach;
            PInspector.Unloaded -= QInspectorFrameDetach;
            QInspectorFrameDetach();
        }

        PInspector = lSurface;
        PInspectorTitle = QContract.QContractFind<TextBlock>(lSurface, nameof(PInspectorTitle));
        PInspectorVacancy = QContract.QContractFind<TextBlock>(lSurface, nameof(PInspectorVacancy));
        PInspectorSections = QContract.QContractFind<Grid>(lSurface, nameof(PInspectorSections));
        PInspectorScroller = QContract.QContractFind<ScrollViewer>(lSurface, nameof(PInspectorScroller));
        PInspectorPersistence = QContract.QContractFind<StackPanel>(lSurface, nameof(PInspectorPersistence));
        PInspectorMinimizer = QContract.QContractFind<Button>(lSurface, nameof(PInspectorMinimizer));
        PInspectorMaximizer = QContract.QContractFind<Button>(lSurface, nameof(PInspectorMaximizer));
        PInspectorExecution = QContract.QContractFind<ContentControl>(lSurface, nameof(PInspectorExecution));
        PInspectorNarrow = QContract.QContractFind<FrameworkElement>(lSurface, nameof(PInspectorNarrow));
        PInspectorExpanded = QContract.QContractFind<FrameworkElement>(lSurface, nameof(PInspectorExpanded));

        PInspectorTitle.Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Header.Title");
        PInspectorVacancy.Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Empty.Notice");
        PInspectorMinimizer.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Panel.HideTooltip");
        PInspectorMaximizer.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Panel.ShowTooltip");
        ((Image)PInspectorMinimizer.Content).Source = QIcon.QIconRead(
            "/PAsset/PPanel/PListMinimize.svg", QTool.QToolIconRead());
        ((Image)PInspectorMaximizer.Content).Source = QIcon.QIconRead(
            "/PAsset/PPanel/PListMaximize.svg", QTool.QToolIconRead());
        QButton.QButtonAttach(PInspectorMinimizer, QButtonLook.QButtonLookPanel);
        QButton.QButtonAttach(PInspectorMaximizer, QButtonLook.QButtonLookPanel);

        foreach ((ContentControl lHost, QSensorPlan lPlan) in PInspectorSections.Children
                     .OfType<ContentControl>()
                     .Zip(QInspector.QInspectorSensor.QSensorPlans))
        {
            QInspector.QInspectorSensor.QSensorBuild(lPlan.QSensorPlanKind, (StackPanel)lHost.Content);
        }

        QInspectorAudio lAudio = QInspector.QInspectorAudio;
        QInspectorCrop lCrop = QInspector.QInspectorCrop;
        lCrop.QInspectorCropBuild();
        QInspectorFrameSet("Crop", lCrop.PInspectorCropBody, lCrop.PInspectorPersistentBox);
        var qTone = new QTone(QInspector);
        QInspectorFrameSet(
            "Brightness", qTone.QToneBrightnessBody, qTone.QToneBrightnessPersistent);
        QInspectorFrameSet("Contrast", qTone.QToneContrastBody, qTone.QToneContrastPersistent);
        QInspectorFrameSet(
            "Saturation", qTone.QToneSaturationBody, qTone.QToneSaturationPersistent);
        var qGamma = new QGamma(QInspector);
        QInspectorFrameSet("Gamma", qGamma.QGammaBody, qGamma.QGammaPersistent);
        var qExposure = new QExposure(QInspector);
        QInspectorFrameSet("Exposure", qExposure.QExposureBody, qExposure.QExposurePersistent);
        var lCurve = new QCurveSection(QInspector.QInspectorVideo.QInspectorCurve);
        QInspectorFrameSet(
            "Curve", lCurve.QCurveSectionBody, lCurve.QCurveSectionPersistent);
        var lWhitebalance = new QWhitebalance(QInspector);
        QInspectorFrameSet(
            "Whitebalance",
            lWhitebalance.QWhitebalanceBody,
            lWhitebalance.QWhitebalancePersistent);
        var lVolume = new QVolume(QInspector);
        QInspectorFrameSet("Volume", lVolume.QVolumeBody, lVolume.QVolumePersistent);
        var lLoudness = new QLoudnessSection(lAudio.QInspectorLoudness);
        QInspectorFrameSet(
            "Normalize", lLoudness.QLoudnessSectionBody, lLoudness.QLoudnessSectionPersistent);
        var lNoise = new QNoiseSection(lAudio.QInspectorNoise);
        QInspectorFrameSet(
            "Noise Reduction", lNoise.QNoiseSectionBody, lNoise.QNoiseSectionPersistent);
        var lHighpass = new QFilterSection(lAudio.QInspectorHighpass, "Inspector.Pass.HighApply");
        QInspectorFrameSet(
            "High Pass",
            lHighpass.QFilterSectionBody,
            lHighpass.QFilterSectionPersistent);
        var lLowpass = new QFilterSection(lAudio.QInspectorLowpass, "Inspector.Pass.LowApply");
        QInspectorFrameSet(
            "Low Pass",
            lLowpass.QFilterSectionBody,
            lLowpass.QFilterSectionPersistent);
        var lEqualizer = new QEqualizerSection(lAudio.QInspectorEqualizer);
        QInspectorFrameSet(
            "Equalizer", lEqualizer.QEqualizerSectionBody, lEqualizer.QEqualizerSectionPersistent);
        var lSkip = new QSkipSection(QInspector.QInspectorSkip);
        QInspectorFrameSet("No Processing", lSkip.QSkipSectionBody, lSkip.QSkipSectionPersistent);

        QInspector.QInspectorBlank.QBlankBuild(lWhitebalance.QWhitebalanceWheel);
        PInspectorSections.Children.Add(QInspector.QInspectorBlank.QBlankBody);
        foreach (ContentControl lHost in PInspectorSections.Children.OfType<ContentControl>().ToArray())
        {
            PInspectorSections.Children.Remove(lHost);
            PInspectorSections.Children.Add(lHost);
        }

        QInspector.QInspectorSensor.QSensorSweep.QSensorSweepBuild();
        PInspectorExecution.Content = QInspector.QInspectorSensor.QSensorSweep.QSensorSweepBody;
        var lLifecycles = new List<(Action, Action)>
        {
            (lCrop.QInspectorCropAttach, lCrop.QInspectorCropDetach),
            (qTone.QToneAttach, qTone.QToneDetach),
            (qGamma.QGammaAttach, qGamma.QGammaDetach),
            (qExposure.QExposureAttach, qExposure.QExposureDetach),
            (lCurve.QCurveSectionAttach, lCurve.QCurveSectionDetach),
            (lWhitebalance.QWhitebalanceChangeAttach, lWhitebalance.QWhitebalanceChangeDetach),
            (lVolume.QVolumeHandlersAttach, lVolume.QVolumeHandlersDetach),
            (lLoudness.QLoudnessSectionAttach, lLoudness.QLoudnessSectionDetach),
            (lNoise.QNoiseSectionAttach, lNoise.QNoiseSectionDetach),
            (lHighpass.QFilterSectionAttach, lHighpass.QFilterSectionDetach),
            (lLowpass.QFilterSectionAttach, lLowpass.QFilterSectionDetach),
            (lEqualizer.QEqualizerSectionAttach, lEqualizer.QEqualizerSectionDetach),
            (lSkip.QSkipSectionAttach, lSkip.QSkipSectionDetach),
            (QInspector.QInspectorBlank.QBlankAttach, QInspector.QInspectorBlank.QBlankDetach),
            (QInspector.QInspectorSensor.QSensorAttach, QInspector.QInspectorSensor.QSensorDetach),
            (
                QInspector.QInspectorSensor.QSensorSweep.QSensorSweepAttach,
                QInspector.QInspectorSensor.QSensorSweep.QSensorSweepClose),
        };
        Action lUpdate = () =>
        {
            lCrop.QInspectorCropUpdate();
            lCurve.QCurveSectionUpdate();
            lWhitebalance.QWhitebalanceUpdate();
            lLoudness.QLoudnessSectionUpdate();
            lNoise.QNoiseSectionUpdate();
            lHighpass.QFilterSectionUpdate();
            lLowpass.QFilterSectionUpdate();
            lEqualizer.QEqualizerSectionUpdate();
            lSkip.QSkipSectionUpdate();
        };
        PInspector.Resources[typeof(QInspectorFrame)] = (lLifecycles, lUpdate);

        PInspector.Loaded += QInspectorFrameAttach;
        PInspector.Unloaded += QInspectorFrameDetach;
        QInspectorFrameAttach();
        QInspectorFrameUpdate();
    }

    private void QInspectorFrameSet(string lKey, StackPanel lBody, CheckBox lPersistent)
    {
        lBody.Tag = lKey;
        PInspectorSections.Children.Add(lBody);
        lPersistent.Tag = lKey;
        lPersistent.Margin = new Thickness(12, 0, 12, 12);
        lPersistent.Visibility = Visibility.Collapsed;
        PInspectorPersistence.Children.Add(lPersistent);
    }

    public void QInspectorFrameAttach()
    {
        if (PInspector.Resources[typeof(QInspectorFrame)]
            is ValueTuple<List<(Action, Action)>, Action> lFrame)
        {
            foreach ((Action lAttach, _) in lFrame.Item1)
            {
                lAttach();
            }
        }

        QInspector.QInspectorChanged -= QInspectorFrameUpdate;
        QInspector.QInspectorChanged += QInspectorFrameUpdate;
        PInspectorMinimizer.Click -= QInspectorFrameSet;
        PInspectorMinimizer.Click += QInspectorFrameSet;
        PInspectorMaximizer.Click -= QInspectorFrameSet;
        PInspectorMaximizer.Click += QInspectorFrameSet;
    }

    private void QInspectorFrameAttach(object _, RoutedEventArgs __)
    {
        QInspectorFrameAttach();
        QInspectorFrameUpdate();
    }

    public void QInspectorFrameDetach()
    {
        QInspector.QInspectorChanged -= QInspectorFrameUpdate;
        PInspectorMinimizer.Click -= QInspectorFrameSet;
        PInspectorMaximizer.Click -= QInspectorFrameSet;
        if (PInspector.Resources[typeof(QInspectorFrame)]
            is ValueTuple<List<(Action, Action)>, Action> lFrame)
        {
            foreach ((_, Action lDetach) in lFrame.Item1)
            {
                lDetach();
            }
        }
    }

    private void QInspectorFrameDetach(object _, RoutedEventArgs __) => QInspectorFrameDetach();

    public void QInspectorFrameUpdate()
    {
        PInspectorExpanded.Visibility = QInspector.QInspectorMinimized ? Visibility.Collapsed : Visibility.Visible;
        PInspectorNarrow.Visibility = QInspector.QInspectorMinimized ? Visibility.Visible : Visibility.Collapsed;
        QInspector.QInspectorSensor.QSensorUpdate(QInspector.QInspectorSensorCheck());
        QInspector.QInspectorBlank.QBlankBody.Visibility = QInspector.QInspectorSensorCheck(
            QInspector.QInspectorBlank.QBlankKind) ? Visibility.Visible : Visibility.Collapsed;

        foreach (StackPanel lBody in PInspectorSections.Children.OfType<StackPanel>()
                     .Where(lBody => lBody.Tag is string))
        {
            bool lShown = QInspector.QInspectorSectionCheck((string)lBody.Tag);
            lBody.Visibility = lShown ? Visibility.Visible : Visibility.Collapsed;
            PInspectorPersistence.Children.OfType<CheckBox>().Single(lBox => Equals(lBox.Tag, lBody.Tag)).Visibility =
                lShown ? Visibility.Visible : Visibility.Collapsed;
        }

        PInspectorTitle.Text = QInspector.QInspectorTitleRead();
        PInspectorPersistence.Visibility = QInspector.QInspectorPersistentShown
            ? Visibility.Visible
            : Visibility.Collapsed;
        PInspectorVacancy.Visibility = QInspector.QInspectorEmptyShown ? Visibility.Visible : Visibility.Collapsed;
        if (PInspector.Resources[typeof(QInspectorFrame)]
            is ValueTuple<List<(Action, Action)>, Action> lFrame)
        {
            lFrame.Item2();
        }
    }

    public void QInspectorFrameSet(string? lStep) => QInspector.QInspectorStepSet(lStep);

    public void QInspectorFrameSet(bool lMinimized) => QInspector.QInspectorMinimizedSet(lMinimized);

    public void QInspectorFrameSet(bool lLocked, bool lExecution)
    {
        PInspectorScroller.IsEnabled = !lLocked;
        if (PInspectorExecution.Content is FrameworkElement lBody)
        {
            lBody.Visibility = lExecution ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public void QInspectorFrameSet(double lProgress) =>
        QInspector.QInspectorSensor.QSensorSweep.QSensorSweepUpdate(lProgress);

    private void QInspectorFrameSet(object lSender, RoutedEventArgs __) =>
        QInspector.QInspectorMinimizedSet(ReferenceEquals(lSender, PInspectorMinimizer));
}
