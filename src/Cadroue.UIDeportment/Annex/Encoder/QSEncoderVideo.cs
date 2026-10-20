using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed record QSEncoderChoice(string QSEncoderChoiceToken, string QSEncoderChoiceLabel)
{
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
    private readonly CSEncoderDraft qsEncoderVideoDraft;

    internal QSEncoderVideo(CSEncoderVideo lVideo, CSEncoderTrial lTrial, CSEncoderDraft lDraft)
    {
        qsEncoderVideoGate = lVideo;
        qsEncoderVideoTrial = lTrial;
        qsEncoderVideoDraft = lDraft;
        lVideo.CSEncoderVideoChanged += _ => QSEncoderVideoChanged?.Invoke();
    }

    // Raised once per kept change of encoder or rate.
    public event Action? QSEncoderVideoChanged;

    public string QSEncoderVideoRate => qsEncoderVideoGate.CSEncoderVideoState.CSEncoderStateRate;

    // Whether a trial has kept rows for the verdict window.
    public bool QSEncoderVideoTried => qsEncoderVideoTrial.CSEncoderTrialRows.Count > 0;

    public bool QSEncoderVideoReactive => qsEncoderVideoDraft.CSEncoderDraftOrigin.Item2;

    public string QSEncoderVideoFps => qsEncoderVideoDraft.CSEncoderDraftOrigin.Item3;

    public bool QSEncoderVideoEncoded => qsEncoderVideoGate.CSEncoderVideoEncoded;

    public (IReadOnlyList<string>, int) QSEncoderVideoOffer => qsEncoderVideoGate.CSEncoderVideoOffer;

    public (IReadOnlyList<string>, int) QSEncoderVideoRead(string lMedia, string lChoice) =>
        qsEncoderVideoGate.CSEncoderVideoRead(lMedia, lChoice);

    public (IReadOnlyList<string>, int) QSEncoderLayoutRead(Func<string, string> lTranslate) =>
        qsEncoderVideoGate.CSEncoderLayoutRead(lTranslate);

    public string[]? QSEncoderVideoSync(object? lShown) =>
        qsEncoderVideoGate.CSEncoderVideoSync(lShown);

    public bool QSEncoderModeCheck(int lIndex) => qsEncoderVideoGate.CSEncoderModeCheck(lIndex);

    public void QSEncoderModeSet(int lIndex) => qsEncoderVideoGate.CSEncoderModeSet(lIndex);

    public void QSEncoderDraftSet(
        string lQuality,
        string lSpeed,
        bool? lReactive,
        string lFps,
        int lLayout,
        IReadOnlyDictionary<string, string> lExtras) =>
        qsEncoderVideoDraft.CSEncoderDraftSet(lQuality, lSpeed, lReactive, lFps, lLayout, lExtras);

    public bool QSEncoderVideoCheck(string lEncoder) => qsEncoderVideoGate.CSEncoderVideoCheck(lEncoder);

    public string QSEncoderNoticeResolve() => qsEncoderVideoGate.CSEncoderVideoResolve();

    // An empty encoder or rate is no choice; the gate keeps any other and answers whether it changed.
    public bool QSEncoderVideoSet(string lEncoder, string lMode) =>
        lEncoder.Length > 0 && lMode.Length > 0 && qsEncoderVideoGate.CSEncoderVideoSet(lEncoder, lMode);

    // A cancelled trial throws and keeps nothing.
    public Task<(IReadOnlyList<string>, int)> QSEncoderVideoScan(IProgress<double> lSender) =>
        qsEncoderVideoTrial.CSEncoderTrialRun(lSender);

    // The video trial's verdict window: its title and the last trial's rows.
    public QSVerdict QSEncoderVerdictCreate() =>
        new(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.VideoTitle"),
            qsEncoderVideoTrial.CSEncoderTrialRead(QSEncoderVerdictRead));

    internal CSEncoderCodec QSEncoderVideoCodec => qsEncoderVideoGate.CSEncoderVideoState.CSEncoderStateCodec;

    private static QSVerdictRow QSEncoderVerdictRead(string lFamily, string lEncoder, bool lSuccess, string lMessage) =>
        new(lFamily, lEncoder, lSuccess, lMessage);
}
