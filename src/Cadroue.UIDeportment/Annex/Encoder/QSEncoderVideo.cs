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
    string QSEncoderQualityRange);

// The codec's speed row: its label, the place it starts at, and the label and value of each place.
public sealed record QSEncoderSpeed(
    string QSEncoderSpeedLabel,
    int QSEncoderSpeedIndex,
    IReadOnlyList<string> QSEncoderSpeedLabels,
    IReadOnlyList<string> QSEncoderSpeedValues);

// One extra option row: its label and option, the value it starts with, and the values it offers.
public sealed record QSEncoderExtra(
    string QSEncoderExtraLabel,
    string QSEncoderExtraOption,
    string QSEncoderExtraSelected,
    LLocalizationChoice[] QSEncoderExtraChoices);

// The encoder window's video driver: it answers the video sheet from the video and trial gates, hands the sheet's
// choice and trial to them, and decides what each rate row starts with from the codec and the stored draft.
public sealed class QSEncoderVideo
{
    private readonly CSEncoderVideo qsEncoderVideoGate;
    private readonly CSEncoderTrial qsEncoderVideoTrial;
    private readonly LPreset qsEncoderVideoDraft;

    internal QSEncoderVideo(CSEncoderVideo lVideo, CSEncoderTrial lTrial, LPreset lPreset)
    {
        qsEncoderVideoGate = lVideo;
        qsEncoderVideoTrial = lTrial;
        qsEncoderVideoDraft = lPreset;
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
            ? new QSEncoderQuality(
                lCodec.CSEncoderQualityLabel,
                QSEncoderStoredRead(qsEncoderVideoDraft.LPresetVideo.LPresetQuality, lCodec.CSEncoderQualityDefault),
                lCodec.CSEncoderQualityMinimum,
                lCodec.CSEncoderQualityMaximum,
                lCodec.CSEncoderQualityStep,
                lCodec.CSEncoderQualityAscending,
                lCodec.CSEncoderQualityBitrate,
                QSEncoderRangeFormat(lCodec))
            : null;

    // The speed row starts at the stored preset while the stored rate is kept, else at the codec's default; a value
    // the codec does not offer starts at the first place.
    public QSEncoderSpeed? QSEncoderVideoSpeed =>
        qsEncoderVideoGate.CSEncoderVideoState.CSEncoderStateCodec.CSEncoderCodecSpeed is { } lCodec
            ? QSEncoderSpeedRead(lCodec)
            : null;

    // Each extra row starts with the stored value when the row offers it, else with its default.
    public IReadOnlyList<QSEncoderExtra> QSEncoderVideoExtras =>
        qsEncoderVideoGate.CSEncoderVideoState.CSEncoderStateCodec.CSEncoderCodecExtras
            .Select(QSEncoderExtraRead)
            .ToArray();

    private static QSVerdictRow QSEncoderVerdictRead(CSEncoderRow lEntry) =>
        new(
            lEntry.CSEncoderRowFamily,
            lEntry.CSEncoderRowEncoder,
            lEntry.CSEncoderRowSuccess,
            lEntry.CSEncoderRowMessage);

    private string QSEncoderStoredRead(string lChoice, string lMode) =>
        string.Equals(
                QSEncoderVideoRate, qsEncoderVideoDraft.LPresetVideo.LPresetRateControl, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(lChoice)
            ? lChoice
            : lMode;

    private static string QSEncoderRangeFormat(CSEncoderQuality lCodec) =>
        string.IsNullOrEmpty(lCodec.CSEncoderQualityRange)
            ? QLocalizationCatalog.QLocalizationFormat("Encoder.Video.FFmpegOption", lCodec.CSEncoderQualityOption)
            : QLocalizationCatalog.QLocalizationFormat(
                "Encoder.Video.FFmpegOptionRange",
                lCodec.CSEncoderQualityOption,
                lCodec.CSEncoderQualityRange);

    private QSEncoderSpeed QSEncoderSpeedRead(CSEncoderSpeed lCodec)
    {
        string[] lEntry = lCodec.CSEncoderSpeedValues.Select(QSEncoderValueRead).ToArray();
        string lChoice = QSEncoderStoredRead(
            qsEncoderVideoDraft.LPresetVideo.LPresetSpeedPreset, lCodec.CSEncoderSpeedDefault);
        return new QSEncoderSpeed(
            lCodec.CSEncoderSpeedLabel,
            Math.Max(0, Array.IndexOf(lEntry, lChoice)),
            lCodec.CSEncoderSpeedValues.Select(QSEncoderLabelRead).ToArray(),
            lEntry);
    }

    private QSEncoderExtra QSEncoderExtraRead(CSEncoderExtra lCodec)
    {
        string? lChoice = qsEncoderVideoDraft.LPresetVideo.LPresetExtras.TryGetValue(
            lCodec.CSEncoderExtraOption, out string? lEntry)
            ? lEntry
            : null;
        return new QSEncoderExtra(
            lCodec.CSEncoderExtraLabel,
            lCodec.CSEncoderExtraOption,
            lChoice is not null && lCodec.CSEncoderExtraValues.Select(QSEncoderValueRead).Contains(lChoice)
                ? lChoice
                : lCodec.CSEncoderExtraDefault,
            lCodec.CSEncoderExtraValues.Select(QSEncoderChoiceRead).ToArray());
    }

    private static LLocalizationChoice QSEncoderChoiceRead(CSEncoderChoice lChoice) =>
        new(lChoice.CSEncoderChoiceValue, string.Empty, lChoice.CSEncoderChoiceLabel);

    private static string QSEncoderValueRead(CSEncoderChoice lChoice) => lChoice.CSEncoderChoiceValue;

    private static string QSEncoderLabelRead(CSEncoderChoice lChoice) => lChoice.CSEncoderChoiceLabel;
}
