using System.Windows;
using System.Windows.Controls;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QSEncoderRate
{
    private readonly UserControl qsEncoderRateSurface;
    private readonly QSEncoderVideo qsEncoderRateVideo;
    private readonly List<ComboBox> qsEncoderRateOptions = [];
    private Slider? qsEncoderRateScale;
    private CSEncoderCodec? qsEncoderRateCodec;

    public QSEncoderRate(UserControl lSurface, QSEncoderVideo lVideo)
    {
        qsEncoderRateSurface = lSurface;
        qsEncoderRateVideo = lVideo;
    }

    private UserControl QSEncoderRateFader =>
        QContract.QContractFind<UserControl>(qsEncoderRateSurface, "psEncoderRateFader");

    private StackPanel QSEncoderRateRows =>
        QContract.QContractFind<StackPanel>(qsEncoderRateSurface, "psEncoderRateRows");

    internal TextBox? QSEncoderRateQuality => QSFader.QSFaderRead(QSEncoderRateFader);

    public string QSEncoderRateSpeed =>
        qsEncoderRateCodec?.CSEncoderCodecSpeed is { } lSpeed && qsEncoderRateScale is not null
            ? lSpeed.CSEncoderSpeedRead(qsEncoderRateScale.Value)
            : string.Empty;

    public Dictionary<string, string> QSEncoderRateExtras
    {
        get
        {
            var lSelected = new List<int>();
            foreach (ComboBox lOption in qsEncoderRateOptions)
            {
                lSelected.Add(lOption.SelectedIndex);
            }

            return qsEncoderRateCodec is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : qsEncoderRateCodec.CSEncoderCodecResolve(lSelected);
        }
    }

    public void QSEncoderRateRebuild()
    {
        QSEncoderRateClear();
        CSEncoderCodec lCodec = qsEncoderRateVideo.QSEncoderVideoCodec;
        qsEncoderRateCodec = lCodec;
        QSEncoderRateAdd(lCodec.CSEncoderCodecQuality);
        QSEncoderRateAdd(lCodec.CSEncoderCodecNotice);
        QSEncoderRateAdd(lCodec.CSEncoderCodecSpeed);
        foreach (CSEncoderExtra lExtra in lCodec.CSEncoderCodecExtras)
        {
            QSEncoderRateAdd(lExtra);
        }
    }

    private void QSEncoderRateClear()
    {
        QSFader.QSFaderClear(QSEncoderRateFader);
        QSEncoderRateRows.Children.Clear();
        qsEncoderRateScale = null;
        qsEncoderRateOptions.Clear();
    }

    private void QSEncoderRateAdd(CSEncoderQuality? lQuality)
    {
        if (lQuality is null)
        {
            return;
        }

        _ = QSFader.QSFaderBuild(
            QSEncoderRateFader,
            lQuality.CSEncoderQualityLabel,
            lQuality.CSEncoderQualityText,
            lQuality.CSEncoderQualityMinimum,
            lQuality.CSEncoderQualityMaximum,
            lQuality.CSEncoderQualityStep,
            lQuality.CSEncoderQualityAscending,
            lQuality.CSEncoderQualityBitrate,
            QSEncoderRateDescribe(lQuality));
    }

    private static string QSEncoderRateDescribe(CSEncoderQuality lQuality) =>
        string.IsNullOrEmpty(lQuality.CSEncoderQualityRange)
            ? QLocalizationCatalog.QLocalizationFormat("Encoder.Video.FFmpegOption", lQuality.CSEncoderQualityOption)
            : QLocalizationCatalog.QLocalizationFormat(
                "Encoder.Video.FFmpegOptionRange",
                lQuality.CSEncoderQualityOption,
                lQuality.CSEncoderQualityRange);

    private void QSEncoderRateAdd(string lCaution)
    {
        if (string.IsNullOrWhiteSpace(lCaution))
        {
            return;
        }

        var lNotice = (TextBlock)QContract.QContractSheetFind<DataTemplate>(
            qsEncoderRateSurface, "psEncoderRateCaution").LoadContent();
        lNotice.Text = lCaution;
        QSEncoderRateRows.Children.Add(lNotice);
    }

    private void QSEncoderRateAdd(CSEncoderSpeed? lSpeed)
    {
        if (lSpeed is null)
        {
            return;
        }

        var lRow = (FrameworkElement)QContract.QContractSheetFind<DataTemplate>(
            qsEncoderRateSurface, "psEncoderRateSpeed").LoadContent();
        Slider lScale = QContract.QContractFind<Slider>(lRow, "psEncoderRateScale");
        lScale.Maximum = lSpeed.CSEncoderSpeedMost;
        lScale.Value = lSpeed.CSEncoderSpeedOrigin;
        QSlider.QSliderAttach(lScale);
        qsEncoderRateScale = lScale;
        TextBlock lReadout = QContract.QContractFind<TextBlock>(lRow, "psEncoderRateReadout");
        lReadout.Text = lSpeed.CSEncoderSpeedShown;
        lScale.ValueChanged += (_, _) => lReadout.Text = lSpeed.CSEncoderSpeedFormat(lScale.Value);
        QContract.QContractFind<TextBlock>(lRow, "psEncoderRateCaption").Text = lSpeed.CSEncoderSpeedLabel;
        QSEncoderRateRows.Children.Add(lRow);
    }

    private void QSEncoderRateAdd(CSEncoderExtra lExtra)
    {
        var lRow = (FrameworkElement)QContract.QContractSheetFind<DataTemplate>(
            qsEncoderRateSurface, "psEncoderRateExtra").LoadContent();
        ComboBox lOption = QContract.QContractFind<ComboBox>(lRow, "psEncoderRateOption");
        lOption.ItemsSource = lExtra.CSEncoderExtraLabels;
        lOption.SelectedIndex = lExtra.CSEncoderExtraIndex;
        QDropdown.QDropdownAttach(lOption);
        qsEncoderRateOptions.Add(lOption);
        QContract.QContractFind<TextBlock>(lRow, "psEncoderRateCaption").Text = lExtra.CSEncoderExtraLabel;
        QSEncoderRateRows.Children.Add(lRow);
    }
}
