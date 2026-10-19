using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The kept mode's quality row: its label, the text it starts with, its range and step, whether higher is better,
// whether it is a bitrate, and the option line under it.
public sealed record QSEncoderQuality(
    string QSEncoderQualityLabel,
    string QSEncoderQualityText,
    double? QSEncoderQualityMinimum,
    double? QSEncoderQualityMaximum,
    double QSEncoderQualityStep,
    bool QSEncoderQualityAscending,
    bool QSEncoderQualityBitrate,
    string QSEncoderQualityRange)
{
    internal static QSEncoderQuality QSEncoderQualityRead(CSEncoderQuality lCodec) =>
        new(
            lCodec.CSEncoderQualityLabel,
            lCodec.CSEncoderQualityText,
            lCodec.CSEncoderQualityMinimum,
            lCodec.CSEncoderQualityMaximum,
            lCodec.CSEncoderQualityStep,
            lCodec.CSEncoderQualityAscending,
            lCodec.CSEncoderQualityBitrate,
            string.IsNullOrEmpty(lCodec.CSEncoderQualityRange)
                ? QLocalizationCatalog.QLocalizationFormat(
                    "Encoder.Video.FFmpegOption", lCodec.CSEncoderQualityOption)
                : QLocalizationCatalog.QLocalizationFormat(
                    "Encoder.Video.FFmpegOptionRange",
                    lCodec.CSEncoderQualityOption,
                    lCodec.CSEncoderQualityRange));
}

// The codec's speed row: its label, the place it starts at, and the label and value of each place.
public sealed record QSEncoderSpeed(
    string QSEncoderSpeedLabel,
    int QSEncoderSpeedIndex,
    IReadOnlyList<string> QSEncoderSpeedLabels,
    IReadOnlyList<string> QSEncoderSpeedValues)
{
    internal static QSEncoderSpeed QSEncoderSpeedRead(CSEncoderSpeed lCodec) =>
        new(
            lCodec.CSEncoderSpeedLabel,
            lCodec.CSEncoderSpeedIndex,
            lCodec.CSEncoderSpeedLabels,
            lCodec.CSEncoderSpeedValues);
}

// One extra option row: its label and option, the value it starts with, and the values it offers.
public sealed record QSEncoderExtra(
    string QSEncoderExtraLabel,
    string QSEncoderExtraOption,
    int QSEncoderExtraIndex,
    IReadOnlyList<QSEncoderChoice> QSEncoderExtraChoices)
{
    internal static QSEncoderExtra QSEncoderExtraRead(CSEncoderExtra lCodec) =>
        new(
            lCodec.CSEncoderExtraLabel,
            lCodec.CSEncoderExtraOption,
            lCodec.CSEncoderExtraIndex,
            lCodec.CSEncoderExtraChoices.Select(QSEncoderChoice.QSEncoderChoiceCreate).ToArray());
}

public sealed record QSEncoderChoice(string QSEncoderChoiceToken, string QSEncoderChoiceLabel)
{
    internal static QSEncoderChoice QSEncoderChoiceCreate(CSEncoderChoice lChoice) =>
        new(lChoice.CSEncoderChoiceValue, lChoice.CSEncoderChoiceLabel);

    public static string QSEncoderChoiceRead(object? choice) =>
        choice is QSEncoderChoice entry
            ? entry.QSEncoderChoiceToken
            : LLocalizationChoice.LLocalizationChoiceRead(choice);

    public override string ToString() => QSEncoderChoiceLabel ?? QSEncoderChoiceToken;
}

public sealed class QSEncoderVideo
{
    private readonly CSEncoderVideo qsEncoderVideoGate;
    private readonly CSEncoderTrial qsEncoderVideoTrial;

    internal QSEncoderVideo(CSEncoderVideo lVideo, CSEncoderTrial lTrial)
    {
        qsEncoderVideoGate = lVideo;
        qsEncoderVideoTrial = lTrial;
        lVideo.CSEncoderVideoChanged += _ => QSEncoderVideoChanged?.Invoke();
    }

    // Raised once per kept change of encoder or rate.
    public event Action? QSEncoderVideoChanged;

    public string QSEncoderVideoEncoder => qsEncoderVideoGate.CSEncoderVideoState.CSEncoderStateEncoder;

    public string QSEncoderVideoRate => qsEncoderVideoGate.CSEncoderVideoState.CSEncoderStateRate;

    public string[] QSEncoderVideoModes =>
        qsEncoderVideoGate.CSEncoderVideoState.CSEncoderStateCodec.CSEncoderCodecModes.ToArray();

    public string QSEncoderVideoCaution =>
        qsEncoderVideoGate.CSEncoderVideoState.CSEncoderStateCodec.CSEncoderCodecNotice;

    // Whether a trial has kept rows for the verdict window.
    public bool QSEncoderVideoTried => qsEncoderVideoTrial.CSEncoderTrialRows.Count > 0;

    public string[] QSEncoderVideoRead(string lMedia, string lChoice) =>
        qsEncoderVideoGate.CSEncoderVideoRead(lMedia, lChoice);

    public bool QSEncoderVideoCheck(string lEncoder) => qsEncoderVideoGate.CSEncoderVideoCheck(lEncoder);

    // The notice key a video mode shows.
    public string QSEncoderNoticeResolve(string lMode) => qsEncoderVideoGate.CSEncoderVideoResolve(lMode);

    // An empty encoder or rate is no choice; the gate keeps any other and answers whether it changed.
    public bool QSEncoderVideoSet(string lEncoder, string lMode) =>
        lEncoder.Length > 0 && lMode.Length > 0 && qsEncoderVideoGate.CSEncoderVideoSet(lEncoder, lMode);

    // A cancelled trial throws and keeps nothing.
    public Task<IReadOnlyList<string>> QSEncoderVideoScan(IProgress<double> lSender) =>
        qsEncoderVideoTrial.CSEncoderTrialRun(lSender);

    // The video trial's verdict window: its title and the last trial's rows.
    public QSVerdict QSEncoderVerdictCreate() =>
        new(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.VideoTitle"),
            qsEncoderVideoTrial.CSEncoderTrialRows.Select(QSEncoderVerdictRead).ToArray());

    // The quality row starts with the stored text while the stored rate is kept, else with the mode's default.
    public QSEncoderQuality? QSEncoderVideoQuality =>
        qsEncoderVideoGate.CSEncoderVideoState.CSEncoderStateCodec.CSEncoderCodecQuality is { } lCodec
            ? QSEncoderQuality.QSEncoderQualityRead(lCodec)
            : null;

    // The speed row starts at the stored preset while the stored rate is kept, else at the codec's default; a value
    // the codec does not offer starts at the first place.
    public QSEncoderSpeed? QSEncoderVideoSpeed =>
        qsEncoderVideoGate.CSEncoderVideoState.CSEncoderStateCodec.CSEncoderCodecSpeed is { } lCodec
            ? QSEncoderSpeed.QSEncoderSpeedRead(lCodec)
            : null;

    // Each extra row starts with the stored value when the row offers it, else with its default.
    public IReadOnlyList<QSEncoderExtra> QSEncoderVideoExtras =>
        qsEncoderVideoGate.CSEncoderVideoState.CSEncoderStateCodec.CSEncoderCodecExtras
            .Select(QSEncoderExtra.QSEncoderExtraRead)
            .ToArray();

    private static QSVerdictRow QSEncoderVerdictRead(CSEncoderRow lEntry) =>
        new(
            lEntry.CSEncoderRowFamily,
            lEntry.CSEncoderRowEncoder,
            lEntry.CSEncoderRowSuccess,
            lEntry.CSEncoderRowMessage);
}
