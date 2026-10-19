using Cadroue.Conduct;
using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

// The encoder window's audio codec: the chosen encoder and rate, the encoders a container offers, and their trial.
public sealed class QSEncoderAudio
{
    private readonly CSEncoderAudio qsEncoderAudioGate;
    private IReadOnlyList<QSVerdictRow> qsEncoderAudioResults = [];

    internal QSEncoderAudio(CSEncoderAudio lAudio)
    {
        qsEncoderAudioGate = lAudio;
        lAudio.CSEncoderAudioChanged += _ => QSEncoderAudioChanged?.Invoke();
    }

    // Raised once per kept change of encoder or rate.
    public event Action? QSEncoderAudioChanged;

    public string QSEncoderAudioEncoder => qsEncoderAudioGate.CSEncoderAudioState.CSEncoderAudioEncoder;

    public string QSEncoderAudioRate => qsEncoderAudioGate.CSEncoderAudioState.CSEncoderAudioRate;

    public string[] QSEncoderAudioModes => qsEncoderAudioGate.CSEncoderAudioState.CSEncoderAudioModes.ToArray();

    public QSEncoderAudioCodec QSEncoderAudioRead()
    {
        CSEncoderAudioCodec codec = qsEncoderAudioGate.CSEncoderAudioRead();
        return new QSEncoderAudioCodec(
            codec.CSEncoderAudioMode,
            codec.CSEncoderAudioQuality is { } quality ? QSCodecQuality.QSCodecQualityRead(quality) : null,
            codec.CSEncoderAudioSpeed is { } speed ? QSCodecSpeed.QSCodecSpeedRead(speed) : null,
            codec.CSEncoderAudioExtras,
            codec.CSEncoderAudioCaution);
    }

    public static ComboBox QSEncoderAudioBuild(
        string selected, IReadOnlyList<(string QSEncoderChoiceToken, string QSEncoderChoiceLabel)> values,
        double width, double height)
    {
        QSEncoderChoice[] items = values
            .Select(value => new QSEncoderChoice(value.QSEncoderChoiceToken, value.QSEncoderChoiceLabel))
            .ToArray();
        var combo = new ComboBox
        {
            ItemsSource = items,
            SelectedItem = items.Where(item => string.Equals(
                    item.QSEncoderChoiceToken, selected, StringComparison.Ordinal))
                .Concat(items).FirstOrDefault(),
            MinWidth = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        QDropdown.QDropdownAttach(combo);
        return combo;
    }

    public static string QSEncoderAudioRead(ComboBox? combo) =>
        QSEncoderChoice.QSEncoderChoiceRead(combo?.SelectedItem);

    public IReadOnlyList<QSVerdictRow> QSEncoderAudioResults => qsEncoderAudioResults;

    public QSVerdict QSEncoderVerdictCreate() =>
        new(QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.AudioTitle"), qsEncoderAudioResults);

    // The gate keeps the encoder and its resolved rate and answers whether the choice changed.
    public bool QSEncoderAudioSet(string lEncoder, string lRate) =>
        qsEncoderAudioGate.CSEncoderAudioSet(lEncoder, lRate);

    public string[] QSEncoderAudioRead(string lContainer, string lKeep) =>
        qsEncoderAudioGate.CSEncoderAudioRead(lContainer, lKeep);

    public bool QSEncoderAudioCheck(string lText) => qsEncoderAudioGate.CSEncoderAudioCheck(lText);

    public async Task<IReadOnlyList<string>> QSEncoderAudioScan(IProgress<double> lFeed)
    {
        IReadOnlyList<string> lAvailable = await qsEncoderAudioGate.CSEncoderAudioRun(lFeed);
        qsEncoderAudioResults = qsEncoderAudioGate.CSEncoderAudioRows.Select(QSEncoderVerdictRead).ToArray();
        return lAvailable;
    }

    private static QSVerdictRow QSEncoderVerdictRead(CSEncoderRow lEntry) =>
        new(
            lEntry.CSEncoderRowFamily,
            lEntry.CSEncoderRowEncoder,
            lEntry.CSEncoderRowSuccess,
            lEntry.CSEncoderRowMessage);
}
