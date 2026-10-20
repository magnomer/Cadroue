using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The encoder window's driver: it holds the draft and its gate, hands each row group its piece, and relays apply.
public sealed class QSEncoder
{
    private readonly CSEncoderDraft qsEncoderGate;

    internal QSEncoder(CUsher lUsher, CSEncoder lEncoder, string lPreset, bool lSmart)
    {
        qsEncoderGate = lEncoder.CSEncoderDraftCreate(lPreset, lSmart);
        QSEncoderVideo = new QSEncoderVideo(
            qsEncoderGate.CSEncoderDraftVideo, qsEncoderGate.CSEncoderDraftTrial, qsEncoderGate);
        QSEncoderSize = new QSEncoderSize(qsEncoderGate.CSEncoderDraftSize);
        QSEncoderAudio = new QSEncoderAudio(qsEncoderGate.CSEncoderDraftAudio);
        QSEncoderSuffix = new QSEncoderSuffix(qsEncoderGate.CSEncoderDraftSuffix);
        QSEncoderLocation = new QSEncoderLocation(lUsher, qsEncoderGate.CSEncoderDraftLocation);
    }

    public QSEncoderVideo QSEncoderVideo { get; }

    public QSEncoderSize QSEncoderSize { get; }

    public QSEncoderAudio QSEncoderAudio { get; }

    public QSEncoderSuffix QSEncoderSuffix { get; }

    public QSEncoderLocation QSEncoderLocation { get; }

    public (string, string, string, string, string) QSEncoderOutputRead() =>
        qsEncoderGate.CSEncoderDraftOutput.CSEncoderOutputRead();

    public IReadOnlyList<string> QSEncoderOutputRead(string lContainer) =>
        qsEncoderGate.CSEncoderDraftOutput.CSEncoderOutputRead(lContainer);

    public void QSEncoderOutputSet(
        string lDisplay,
        string lContainer,
        string lExtension,
        string lCollision,
        string lSuffix,
        string lLocation,
        string lFolder) =>
        qsEncoderGate.CSEncoderDraftOutput.CSEncoderOutputSet(
            lDisplay, lContainer, lExtension, lCollision, lSuffix, lLocation, lFolder);

    public QToken QSEncoderTokenCreate() => new();

    public void QSEncoderClose() => qsEncoderGate.CSEncoderDraftClose();

    public void QSEncoderHelpAttach(Action lHandler) => qsEncoderGate.CSEncoderDraftReady += lHandler;

    public void QSEncoderApply() => qsEncoderGate.CSEncoderDraftApply();

    public void QSEncoderSave() => qsEncoderGate.CSEncoderDraftSave();
}
