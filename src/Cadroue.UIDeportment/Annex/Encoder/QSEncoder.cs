using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The encoder window's driver: it holds the draft and its gate, hands each row group its piece, and relays apply.
public sealed class QSEncoder
{
    private readonly LSEncoderDraft qsEncoderPreset;
    private readonly CSEncoderDraft qsEncoderGate;
    private Action? qsEncoderHelpHandler;

    internal QSEncoder(CUsher lUsher, CSEncoder lEncoder, LPreset lSource, bool lSmart)
    {
        qsEncoderPreset = new LSEncoderDraft(lSource);
        qsEncoderGate = lEncoder.CSEncoderDraftCreate(
            qsEncoderPreset.LSEncoderDraftRead,
            qsEncoderPreset.LSEncoderDraftSave,
            qsEncoderPreset.LSEncoderRateRead,
            qsEncoderPreset.LSEncoderSizeRead,
            lSmart);
        QSEncoderVideo = new QSEncoderVideo(
            qsEncoderGate.CSEncoderDraftVideo, qsEncoderGate.CSEncoderDraftTrial);
        QSEncoderSize = new QSEncoderSize(qsEncoderGate.CSEncoderDraftSize);
        QSEncoderAudio = new QSEncoderAudio(qsEncoderPreset.LSEncoderDraftPreset);
        QSEncoderSuffix = new QSEncoderSuffix(qsEncoderPreset.LSEncoderDraftPreset);
        QSEncoderLocation = new QSEncoderLocation(lUsher, qsEncoderPreset.LSEncoderDraftPreset);
        QSEncoderInventoryAttach();
    }

    public LPreset QSEncoderDraft => qsEncoderPreset.LSEncoderDraftPreset;

    public QSEncoderVideo QSEncoderVideo { get; }

    public QSEncoderSize QSEncoderSize { get; }

    public QSEncoderAudio QSEncoderAudio { get; }

    public QSEncoderSuffix QSEncoderSuffix { get; }

    public QSEncoderLocation QSEncoderLocation { get; }

    public QToken QSEncoderTokenCreate() => new();

    public void QSEncoderClose()
    {
        QSEncoderHelpDetach();
        qsEncoderGate.CSEncoderDraftClose();
        QSEncoderAudio.QSEncoderAudioCancel();
    }

    public void QSEncoderHelpAttach(Action lHandler) => qsEncoderHelpHandler = lHandler;

    private void QSEncoderHelpHandle(string lEncoder) => qsEncoderHelpHandler?.Invoke();

    private void QSEncoderInventoryAttach() => LInventoryHelp.LInventoryHelpReady += QSEncoderHelpHandle;

    private void QSEncoderHelpDetach()
    {
        LInventoryHelp.LInventoryHelpReady -= QSEncoderHelpHandle;
        qsEncoderHelpHandler = null;
    }

    public void QSEncoderApply() =>
        qsEncoderGate.CSEncoderDraftApply(
            QSEncoderAudio.QSEncoderAudioEncoder,
            QSEncoderAudio.QSEncoderAudioRate);
}
