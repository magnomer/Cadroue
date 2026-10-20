using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QSEncoderPicture
{
    private readonly UserControl qsEncoderPictureSurface;
    private readonly QSEncoderVideo qsEncoderPictureVideo;
    private readonly QSEncoderResolution qsEncoderPictureResolution;
    private readonly QSEncoderFramerate qsEncoderPictureFramerate;
    private readonly QSEncoderRate qsEncoderPictureRate;
    private readonly Action<QSVerdict> qsEncoderPictureVerdict;

    public QSEncoderPicture(
        UserControl lSurface,
        QSEncoderVideo lVideo,
        QSEncoderSize lSize,
        Action<QSVerdict> lVerdict)
    {
        qsEncoderPictureSurface = lSurface;
        qsEncoderPictureVideo = lVideo;
        qsEncoderPictureVerdict = lVerdict;
        QSEncoderChip.QSEncoderChipApply(QSEncoderPictureCodec);
        QSEncoderPictureLoad();
        QSEncoderPictureShow();
        QSEncoderPictureReset(QSEncoderPictureCodec, lVideo.QSEncoderVideoOffer);
        QDropdown.QDropdownAttach(QSEncoderPictureCodec);
        QSEncoderPictureSync();
        QDropdown.QDropdownAttach(QSEncoderPictureControl);
        qsEncoderPictureResolution = new QSEncoderResolution(
            QContract.QContractFind<UserControl>(lSurface, "psEncoderPictureResolution"),
            lSize,
            lVideo.QSEncoderVideoReactive);
        qsEncoderPictureFramerate = new QSEncoderFramerate(
            QContract.QContractFind<UserControl>(lSurface, "psEncoderPictureFramerate"), lVideo.QSEncoderVideoFps);
        QSEncoderPictureReset(
            QSEncoderPictureLayout, lVideo.QSEncoderLayoutRead(QLocalizationCatalog.QLocalizationTextRead));
        QDropdown.QDropdownAttach(QSEncoderPictureLayout);
        qsEncoderPictureRate = new QSEncoderRate(
            QContract.QContractFind<UserControl>(lSurface, "psEncoderPictureRate"), lVideo);
        lVideo.QSEncoderVideoChanged += QSEncoderPictureApply;
        lSize.QSEncoderSizeChanged += qsEncoderPictureResolution.QSEncoderResolutionApply;
    }

    private StackPanel QSEncoderPictureBar =>
        QContract.QContractFind<StackPanel>(qsEncoderPictureSurface, "psEncoderModeBar");

    private StackPanel QSEncoderPictureRows =>
        QContract.QContractFind<StackPanel>(qsEncoderPictureSurface, "psEncoderPictureRows");

    private TextBlock QSEncoderPictureHint =>
        QContract.QContractFind<TextBlock>(qsEncoderPictureSurface, "psEncoderModeHint");

    private ComboBox QSEncoderPictureCodec =>
        QContract.QContractFind<ComboBox>(qsEncoderPictureSurface, "psEncoderCodecSelection");

    private TextBlock QSEncoderPictureError =>
        QContract.QContractFind<TextBlock>(qsEncoderPictureSurface, "psEncoderCodecError");

    private ComboBox QSEncoderPictureControl =>
        QContract.QContractFind<ComboBox>(qsEncoderPictureSurface, "psEncoderControlSelection");

    private ComboBox QSEncoderPictureLayout =>
        QContract.QContractFind<ComboBox>(qsEncoderPictureSurface, "psEncoderPixelSelection");

    private Brush QSEncoderPictureAccent =>
        QContract.QContractSheetFind<Brush>(qsEncoderPictureSurface, "psEncoderPictureAccent");

    private Brush QSEncoderPictureInactive =>
        QContract.QContractSheetFind<Brush>(qsEncoderPictureSurface, "psEncoderPictureInactive");

    private Brush QSEncoderPictureInk =>
        QContract.QContractSheetFind<Brush>(qsEncoderPictureSurface, "psEncoderPictureInk");

    private Brush QSEncoderPictureBright =>
        QContract.QContractSheetFind<Brush>(qsEncoderPictureSurface, "psEncoderPictureBright");

    public UserControl QSEncoderPictureBuild()
    {
        Button lVerification = QButton.QButtonBuild(
            QContract.QContractFind<Button>(qsEncoderPictureSurface, "psEncoderCodecVerification"),
            QButtonLook.QButtonLookWhite);
        lVerification.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify");
        Button lResult = QButton.QButtonBuild(
            QContract.QContractFind<Button>(qsEncoderPictureSurface, "psEncoderCodecResult"),
            QButtonLook.QButtonLookWhite);
        lResult.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Result");
        QSEncoderPictureSync(lResult);
        QSEncoderPictureAttach(
            lVerification,
            lResult,
            QContract.QContractFind<ProgressBar>(qsEncoderPictureSurface, "psEncoderCodecProgress"));
        QContract.QContractFind<TextBlock>(qsEncoderPictureSurface, "psEncoderCodecCaption").Text =
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.Encoder");
        QContract.QContractFind<TextBlock>(qsEncoderPictureSurface, "psEncoderControlCaption").Text =
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.RateControl");
        qsEncoderPictureResolution.QSEncoderResolutionBuild();
        qsEncoderPictureFramerate.QSEncoderFramerateBuild();
        QContract.QContractFind<TextBlock>(qsEncoderPictureSurface, "psEncoderPixelCaption").Text =
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.PixelFormat");
        QContract.QContractFind<TextBlock>(qsEncoderPictureSurface, "psEncoderModeCaption").Text =
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.Mode");
        QSEncoderPictureRebuild();
        QSEncoderPictureToggle();
        QSEncoderPictureUpdate();
        return qsEncoderPictureSurface;
    }

    public void QSEncoderPictureHandle(string lContainer)
    {
        QSEncoderPictureReset(
            QSEncoderPictureCodec,
            qsEncoderPictureVideo.QSEncoderVideoRead(
                lContainer, QSEncoderChoice.QSEncoderChoiceRead(QSEncoderPictureCodec.SelectedItem)));
        QSEncoderPictureUpdate();
    }

    public void QSEncoderPictureCommit() =>
        qsEncoderPictureVideo.QSEncoderDraftSet(
            qsEncoderPictureRate.QSEncoderRateQuality?.Text ?? string.Empty,
            qsEncoderPictureRate.QSEncoderRateSpeed,
            qsEncoderPictureResolution.QSEncoderResolutionOrientation.IsChecked,
            qsEncoderPictureFramerate.QSEncoderFramerateRead(),
            QSEncoderPictureLayout.SelectedIndex,
            qsEncoderPictureRate.QSEncoderRateExtras);

    private static void QSEncoderPictureReset(ComboBox lCombo, (IReadOnlyList<string>, int) lOffer)
    {
        lCombo.ItemsSource = lOffer.Item1;
        lCombo.SelectedIndex = lOffer.Item2;
    }

    private void QSEncoderPictureLoad()
    {
        foreach ((string lLabel, double lEdge, double lLead, double lTrail) in
            CSEncoderVideo.CSEncoderFacetRead(QLocalizationCatalog.QLocalizationTextRead))
        {
            var lFacet = (Border)QContract.QContractSheetFind<DataTemplate>(
                qsEncoderPictureSurface, "psEncoderModeFacet").LoadContent();
            lFacet.BorderThickness = new Thickness(lEdge, 0, 0, 0);
            lFacet.CornerRadius = new CornerRadius(lLead, lTrail, lTrail, lLead);
            QContract.QContractFind<TextBlock>(lFacet, "psEncoderModeLabel").Text = lLabel;
            lFacet.MouseLeftButtonUp += (lSender, _) => QSEncoderPictureSelect(lSender);
            QSEncoderPictureBar.Children.Add(lFacet);
        }
    }

    private void QSEncoderPictureSelect(object lFacet)
    {
        qsEncoderPictureVideo.QSEncoderModeSet(QSEncoderPictureBar.Children.IndexOf((UIElement)lFacet));
        QSEncoderPictureShow();
        QSEncoderPictureToggle();
    }

    private void QSEncoderPictureShow()
    {
        for (int lIndex = 0; lIndex < QSEncoderPictureBar.Children.Count; lIndex++)
        {
            var lFacet = (Border)QSEncoderPictureBar.Children[lIndex];
            TextBlock lLabel = QContract.QContractFind<TextBlock>(lFacet, "psEncoderModeLabel");
            if (qsEncoderPictureVideo.QSEncoderModeCheck(lIndex))
            {
                lFacet.Background = QSEncoderPictureAccent;
                lLabel.Foreground = QSEncoderPictureBright;
            }
            else
            {
                lFacet.Background = QSEncoderPictureInactive;
                lLabel.Foreground = QSEncoderPictureInk;
            }
        }
    }

    private void QSEncoderPictureToggle()
    {
        if (qsEncoderPictureVideo.QSEncoderVideoEncoded)
        {
            QSEncoderPictureRows.Visibility = Visibility.Visible;
            QSEncoderPictureHint.Visibility = Visibility.Collapsed;
        }
        else
        {
            QSEncoderPictureRows.Visibility = Visibility.Collapsed;
            QSEncoderPictureHint.Visibility = Visibility.Visible;
        }

        QSEncoderPictureHint.Text =
            QLocalizationCatalog.QLocalizationTextRead(qsEncoderPictureVideo.QSEncoderNoticeResolve());
    }

    private void QSEncoderPictureSync(Button lResult) => lResult.IsEnabled = qsEncoderPictureVideo.QSEncoderVideoTried;

    private void QSEncoderPictureSync()
    {
        if (qsEncoderPictureVideo.QSEncoderVideoSync(QSEncoderPictureControl.ItemsSource) is { } lModes)
        {
            QSEncoderPictureControl.ItemsSource = lModes;
        }

        QSEncoderPictureControl.SelectedItem = qsEncoderPictureVideo.QSEncoderVideoRate;
    }

    private void QSEncoderPictureAttach(Button lVerification, Button lResult, ProgressBar lProgress)
    {
        var lFeed = new Progress<double>(lValue => lProgress.Value = lValue);
        lVerification.Click += async (_, _) => await QSEncoderPictureRun(lVerification, lResult, lProgress, lFeed);
        lResult.Click += (_, _) => qsEncoderPictureVerdict(qsEncoderPictureVideo.QSEncoderVerdictCreate());
        QSEncoderPictureCodec.SelectionChanged += (_, _) => QSEncoderPictureSet();
        QSEncoderPictureControl.SelectionChanged += (_, _) => QSEncoderPictureSet();
    }

    private async Task QSEncoderPictureRun(
        Button lVerification, Button lResult, ProgressBar lProgress, IProgress<double> lFeed)
    {
        lProgress.Value = 0;
        lProgress.Visibility = Visibility.Visible;
        try
        {
            await QSEncoderPictureValidate(lVerification, lFeed);
        }
        finally
        {
            lProgress.Visibility = Visibility.Collapsed;
        }

        QSEncoderPictureSync(lResult);
    }

    private async Task QSEncoderPictureValidate(Button lVerification, IProgress<double> lFeed)
    {
        lVerification.IsEnabled = false;
        lVerification.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.Checking");
        if (await QSEncoderPictureScan(lFeed) is not { } lOffer)
        {
            return;
        }

        QSEncoderPictureReset(QSEncoderPictureCodec, lOffer);
        QSEncoderPictureUpdate();
        lVerification.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify");
        lVerification.IsEnabled = true;
    }

    private async Task<(IReadOnlyList<string>, int)?> QSEncoderPictureScan(IProgress<double> lFeed)
    {
        try
        {
            return await qsEncoderPictureVideo.QSEncoderVideoScan(lFeed);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private void QSEncoderPictureSet() =>
        qsEncoderPictureVideo.QSEncoderVideoSet(
            QSEncoderChoice.QSEncoderChoiceRead(QSEncoderPictureCodec.SelectedItem),
            QSEncoderChoice.QSEncoderChoiceRead(QSEncoderPictureControl.SelectedItem));

    private void QSEncoderPictureApply()
    {
        QSEncoderPictureSync();
        QSEncoderPictureRebuild();
        QSEncoderPictureUpdate();
    }

    private void QSEncoderPictureRebuild() => qsEncoderPictureRate.QSEncoderRateRebuild();

    private void QSEncoderPictureUpdate()
    {
        if (qsEncoderPictureVideo.QSEncoderVideoCheck(
                QSEncoderChoice.QSEncoderChoiceRead(QSEncoderPictureCodec.SelectedItem)))
        {
            QSEncoderPictureError.Visibility = Visibility.Collapsed;
        }
        else
        {
            QSEncoderPictureError.Visibility = Visibility.Visible;
            QSEncoderPictureError.Text =
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Notice.Unavailable");
        }
    }
}
