using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The encoder window's driver: it holds the draft preset, hands each row group its piece, and copies the draft back.
public sealed class QSEncoder
{
    public const string QSEncoderSourceMode = "Same as source";

    private readonly LPreset qsEncoderSource;
    private readonly LPreset qsEncoderDraft;
    private Action? qsEncoderHelpHandler;

    internal QSEncoder(CUsher lUsher, LPreset lSource, bool lSmart)
    {
        qsEncoderSource = lSource;
        qsEncoderDraft = lSource.LPresetClone();
        QSEncoderVideo = new QSEncoderVideo(qsEncoderDraft, lSmart);
        QSEncoderSize = new QSEncoderSize(qsEncoderDraft);
        QSEncoderAudio = new QSEncoderAudio(qsEncoderDraft);
        QSEncoderSuffix = new QSEncoderSuffix(qsEncoderDraft);
        QSEncoderLocation = new QSEncoderLocation(lUsher, qsEncoderDraft);
        QSEncoderInventoryAttach();
    }

    public LPreset QSEncoderDraft => qsEncoderDraft;

    public QSEncoderVideo QSEncoderVideo { get; }

    public QSEncoderSize QSEncoderSize { get; }

    public QSEncoderAudio QSEncoderAudio { get; }

    public QSEncoderSuffix QSEncoderSuffix { get; }

    public QSEncoderLocation QSEncoderLocation { get; }

    public QToken QSEncoderTokenCreate() => new();

    // The rate a codec keeps: the asked rate when the codec offers it, else the codec's first mode.
    internal static string QSEncoderRateResolve(string[] lModes, string lRate) =>
        lModes.Contains(lRate) ? lRate : lModes[0];

    internal static double QSEncoderProgressResolve(int lDone, int lTotal) =>
        lTotal == 0 ? 1 : (double)lDone / lTotal;

    public void QSEncoderClose()
    {
        QSEncoderHelpDetach();
        QSEncoderVideo.QSEncoderVideoCancel();
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

    public void QSEncoderApply()
    {
        string lDisplay = QSEncoderDisplayNormalize(qsEncoderDraft.LPresetDisplay);
        QSEncoderDraftUpdate(lDisplay);
        qsEncoderSource.LPresetCopy(qsEncoderDraft);
    }

    private static string QSEncoderDisplayNormalize(string lDisplay) =>
        string.IsNullOrWhiteSpace(lDisplay) ? "{OriginalName}_export" : lDisplay.Trim();

    private void QSEncoderDraftUpdate(string lDisplay)
    {
        qsEncoderDraft.LPresetDisplay = lDisplay;
        qsEncoderDraft.LPresetVideo.LPresetStream = "Include";
        qsEncoderDraft.LPresetVideo.LPresetEncoder = QSEncoderVideo.QSEncoderVideoEncoder;
        qsEncoderDraft.LPresetVideo.LPresetRateControl = QSEncoderVideo.QSEncoderVideoRate;
        qsEncoderDraft.LPresetVideo.LPresetSize = QSEncoderSize.QSEncoderSizeFormat();
        qsEncoderDraft.LPresetAudio.LPresetEncoder = QSEncoderAudio.QSEncoderAudioEncoder;
        qsEncoderDraft.LPresetAudio.LPresetRateControl = QSEncoderAudio.QSEncoderAudioRate;
    }
}
