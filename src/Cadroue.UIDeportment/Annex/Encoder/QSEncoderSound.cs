using System.Windows;
using System.Windows.Controls;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QSEncoderSound
{
    private readonly UserControl qsEncoderSoundSurface;
    private readonly UserControl qsEncoderSoundSample;
    private readonly UserControl qsEncoderSoundChannel;
    private readonly QSEncoderAudio qsEncoderSoundAudio;
    private readonly List<ComboBox> qsEncoderSoundOptions = [];
    private ComboBox? qsEncoderSoundSpeed;
    private Slider? qsEncoderSoundScale;

    public QSEncoderSound(UserControl lSurface, UserControl lSample, UserControl lChannel, QSEncoderAudio lAudio)
    {
        qsEncoderSoundSurface = lSurface;
        qsEncoderSoundSample = lSample;
        qsEncoderSoundChannel = lChannel;
        qsEncoderSoundAudio = lAudio;
    }

    private UserControl QSEncoderSoundFader =>
        QContract.QContractFind<UserControl>(qsEncoderSoundSurface, "psEncoderRateFader");

    private StackPanel QSEncoderSoundRows =>
        QContract.QContractFind<StackPanel>(qsEncoderSoundSurface, "psEncoderRateRows");

    public void QSEncoderSoundRebuild()
    {
        QSEncoderSoundClear();
        (_, _, _, string? lSample, double? lChannel) = QSEncoderSoundRead();
        qsEncoderSoundAudio.QSEncoderAudioRead(lSample, lChannel, QSEncoderSoundAdd);
    }

    private void QSEncoderSoundAdd(CSEncoderSound lSound)
    {
        QSEncoderSoundAdd(lSound.CSEncoderSoundCodec.CSEncoderCodecQuality, lSound.CSEncoderSoundOption);
        QSEncoderSoundAdd(lSound.CSEncoderSoundCodec.CSEncoderCodecSpeed);
        foreach (CSEncoderExtra lExtra in lSound.CSEncoderSoundCodec.CSEncoderCodecExtras)
        {
            QSEncoderSoundAdd(lExtra);
        }

        QSEncoderSoundAdd(lSound.CSEncoderSoundCodec.CSEncoderCodecNotice);
        QSFader.QSFaderClear(qsEncoderSoundSample);
        _ = QSFader.QSFaderBuild(
            qsEncoderSoundSample,
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Field.SampleRate"),
            lSound.CSEncoderSoundSample.Item4,
            lSound.CSEncoderSoundSample.Item1,
            lSound.CSEncoderSoundSample.Item2,
            lSound.CSEncoderSoundSample.Item3,
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Sample.Source"),
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Notice.SampleSource"));
        QSFader.QSFaderClear(qsEncoderSoundChannel);
        qsEncoderSoundScale = QSFader.QSFaderBuild(
            qsEncoderSoundChannel,
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Field.Channels"),
            [
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Sample.Source"),
                .. lSound.CSEncoderSoundChannel.Item1
            ],
            lSound.CSEncoderSoundChannel.Item2,
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Notice.ChannelSource"));
    }

    public void QSEncoderSoundSave(string lStream, string lMode)
    {
        (string? lQuality, int lSpeed, IReadOnlyList<int> lExtras, string? lSample, double? lChannel) =
            QSEncoderSoundRead();
        qsEncoderSoundAudio.QSEncoderAudioSave(
            lStream, lMode, lQuality, lSpeed, lExtras, lSample, lChannel);
    }

    private (string? QSEncoderSoundQuality, int QSEncoderSoundSpeed, IReadOnlyList<int> QSEncoderSoundExtras,
        string? QSEncoderSoundSample, double? QSEncoderSoundChannel) QSEncoderSoundRead()
    {
        var lSelected = new List<int>();
        foreach (ComboBox lOption in qsEncoderSoundOptions)
        {
            lSelected.Add(lOption.SelectedIndex);
        }

        return (
            QSFader.QSFaderRead(QSEncoderSoundFader)?.Text,
            qsEncoderSoundSpeed?.SelectedIndex ?? -1,
            lSelected,
            QSFader.QSFaderRead(qsEncoderSoundSample)?.Text,
            qsEncoderSoundScale?.Value);
    }

    private void QSEncoderSoundClear()
    {
        QSFader.QSFaderClear(QSEncoderSoundFader);
        QSEncoderSoundRows.Children.Clear();
        qsEncoderSoundSpeed = null;
        qsEncoderSoundOptions.Clear();
    }

    private void QSEncoderSoundAdd(CSEncoderQuality? lQuality, string lOptionKey)
    {
        if (lQuality is null)
        {
            return;
        }

        _ = QSFader.QSFaderBuild(
            QSEncoderSoundFader,
            lQuality.CSEncoderQualityLabel,
            lQuality.CSEncoderQualityText,
            lQuality.CSEncoderQualityMinimum,
            lQuality.CSEncoderQualityMaximum,
            lQuality.CSEncoderQualityStep,
            lQuality.CSEncoderQualityAscending,
            lQuality.CSEncoderQualityBitrate,
            QSEncoderSoundDescribe(lQuality, lOptionKey));
    }

    private static string QSEncoderSoundDescribe(CSEncoderQuality lQuality, string lOptionKey) =>
        QLocalizationCatalog.QLocalizationFormat(
            lOptionKey, lQuality.CSEncoderQualityOption, lQuality.CSEncoderQualityRange);

    private void QSEncoderSoundAdd(CSEncoderSpeed? lSpeed)
    {
        if (lSpeed is null)
        {
            return;
        }

        qsEncoderSoundSpeed = QSEncoderSoundAdd(
            lSpeed.CSEncoderSpeedLabel, lSpeed.CSEncoderSpeedLabels, lSpeed.CSEncoderSpeedIndex);
    }

    private void QSEncoderSoundAdd(CSEncoderExtra lExtra) =>
        qsEncoderSoundOptions.Add(QSEncoderSoundAdd(
            lExtra.CSEncoderExtraLabel, lExtra.CSEncoderExtraLabels, lExtra.CSEncoderExtraIndex));

    private ComboBox QSEncoderSoundAdd(string lCaption, IReadOnlyList<string> lLabels, int lIndex)
    {
        var lRow = (FrameworkElement)QContract.QContractSheetFind<DataTemplate>(
            qsEncoderSoundSurface, "psEncoderRateExtra").LoadContent();
        ComboBox lOption = QContract.QContractFind<ComboBox>(lRow, "psEncoderRateOption");
        lOption.ItemsSource = lLabels;
        lOption.SelectedIndex = lIndex;
        QDropdown.QDropdownAttach(lOption);
        QContract.QContractFind<TextBlock>(lRow, "psEncoderRateCaption").Text = lCaption;
        QSEncoderSoundRows.Children.Add(lRow);
        return lOption;
    }

    private void QSEncoderSoundAdd(string lCaution)
    {
        if (string.IsNullOrWhiteSpace(lCaution))
        {
            return;
        }

        var lNotice = (TextBlock)QContract.QContractSheetFind<DataTemplate>(
            qsEncoderSoundSurface, "psEncoderRateCaution").LoadContent();
        lNotice.Text = lCaution;
        QSEncoderSoundRows.Children.Add(lNotice);
    }

}
