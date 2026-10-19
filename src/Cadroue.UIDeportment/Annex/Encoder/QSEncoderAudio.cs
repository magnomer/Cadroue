using Cadroue.Conduct;
using Cadroue.Core;

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

    public LCapabilityCodec QSEncoderAudioCodec =>
        LCapability.LCapabilityAudioRead(LCapability.LCapabilityNameRead(QSEncoderAudioEncoder));

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
