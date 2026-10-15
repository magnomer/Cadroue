using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed partial class QSEncoder
{
    public const string QSEncoderSourceMode = "Same as source";

    private readonly CUsher qsEncoderUsher;
    private readonly LPreset qsEncoderSource;
    private readonly LPreset qsEncoderDraft;
    private readonly bool qsEncoderSmart;
    private string? qsEncoderSuffixMode;
    private string? qsEncoderLocationMode;
    private CancellationTokenSource? qsEncoderVideoTrial;
    private CancellationTokenSource? qsEncoderAudioTrial;

    internal QSEncoder(CUsher lUsher, LPreset lSource, bool lSmart)
    {
        qsEncoderUsher = lUsher;
        qsEncoderSource = lSource;
        qsEncoderDraft = lSource.LPresetClone();
        qsEncoderSmart = lSmart;
        QSEncoderVideoPrepare();
        QSEncoderAudioPrepare();
        LInventory.LInventoryHelpReady += QSEncoderHelpHandle;
    }

    public LPreset QSEncoderDraft => qsEncoderDraft;

    public QToken QSEncoderTokenCreate() => new();

    public QSVerdict QSEncoderVerdictCreate(bool lAudio) => lAudio
        ? new(QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.AudioTitle"), QSEncoderAudioResults)
        : new(QLocalizationCatalog.QLocalizationTextRead("Encoder.Verification.VideoTitle"), QSEncoderVideoResults);

    public string QSEncoderInitialResolve(string lCurrent) =>
        qsEncoderUsher.CUsherFolderExist(lCurrent) ? lCurrent : string.Empty;

    private CancellationToken QSEncoderScanStart(LTrialKind lKind)
    {
        ref CancellationTokenSource? lSlot = ref lKind == LTrialKind.LTrialKindAudio
            ? ref qsEncoderAudioTrial
            : ref qsEncoderVideoTrial;
        lSlot?.Cancel();
        lSlot = new CancellationTokenSource();
        return lSlot.Token;
    }

    public void QSEncoderClose()
    {
        LInventory.LInventoryHelpReady -= QSEncoderHelpHandle;
        qsEncoderHelpHandler = null;
        qsEncoderVideoTrial?.Cancel();
        qsEncoderAudioTrial?.Cancel();
    }

    public bool QSEncoderSmart => qsEncoderSmart;

    public string? QSEncoderSuffixMode => qsEncoderSuffixMode;

    public string? QSEncoderLocationMode => qsEncoderLocationMode;

    public static bool QSEncoderSuffixCheck(string lPolicy) =>
        string.Equals(lPolicy, "Rename output", StringComparison.Ordinal)
        || string.Equals(lPolicy, "Rename existing", StringComparison.Ordinal);

    public static string QSEncoderSuffixResolve(string lPolicy) =>
        string.Equals(lPolicy, "Rename existing", StringComparison.Ordinal)
            ? "Encoder.Field.Output.SuffixSource"
            : "Encoder.Field.Output.SuffixOutput";

    public string QSEncoderSuffixSelect(string lPolicy, string lSuffixShown)
    {
        if (qsEncoderSuffixMode is not null
            && QSEncoderSuffixCheck(qsEncoderSuffixMode)
            && !string.Equals(qsEncoderSuffixMode, lPolicy, StringComparison.Ordinal))
        {
            qsEncoderDraft.LPresetSuffixSet(qsEncoderSuffixMode, lSuffixShown.Trim());
        }

        qsEncoderSuffixMode = lPolicy;
        return QSEncoderSuffixCheck(lPolicy) ? qsEncoderDraft.LPresetSuffixRead(lPolicy) : lSuffixShown;
    }

    public string QSEncoderSuffixNormalize(string lPolicy, string lSuffixShown)
    {
        if (!string.IsNullOrEmpty(lSuffixShown.Trim()))
        {
            return lSuffixShown;
        }

        qsEncoderDraft.LPresetSuffixSet(lPolicy, string.Empty);
        return qsEncoderDraft.LPresetSuffixRead(lPolicy);
    }

    public static bool QSEncoderFolderCheck(string lMode) =>
        !string.Equals(lMode, QSEncoderSourceMode, StringComparison.Ordinal);

    public static bool QSEncoderCustomCheck(string lMode) =>
        string.Equals(lMode, "Custom location", StringComparison.Ordinal);

    public static string QSEncoderFolderResolve(string lMode) => lMode switch
    {
        "Sibling" => "Encoder.Location.Sibling",
        "Custom location" => "Encoder.Location.Custom",
        _ => "Encoder.Location.Subfolder"
    };

    public static string QSEncoderStatusResolve(string lMode) => lMode switch
    {
        "Subfolder" => "Encoder.Location.SubfolderStatus",
        "Sibling" => "Encoder.Location.SiblingStatus",
        "Custom location" => "Encoder.Location.CustomStatus",
        _ => "Encoder.Location.Source"
    };

    public string QSEncoderLocationSelect(string lMode, string lFolderShown)
    {
        string lFolder = lFolderShown;
        if (qsEncoderLocationMode is not null && !string.Equals(qsEncoderLocationMode, lMode, StringComparison.Ordinal))
        {
            qsEncoderDraft.LPresetLocationSet(qsEncoderLocationMode, lFolderShown.Trim());
            lFolder = qsEncoderDraft.LPresetLocationRead(lMode);
        }

        qsEncoderLocationMode = lMode;
        return lFolder;
    }

    public string QSEncoderNoticeResolve(string lMode)
    {
        if (lMode != "Smart")
        {
            return "Encoder.Video.Notice.Copied";
        }

        return qsEncoderSmart ? "Encoder.Video.Notice.Smart" : "Encoder.Video.Notice.SmartFull";
    }

    public void QSEncoderApply()
    {
        qsEncoderDraft.LPresetDisplay = string.IsNullOrWhiteSpace(qsEncoderDraft.LPresetDisplay)
            ? "{OriginalName}_export"
            : qsEncoderDraft.LPresetDisplay.Trim();
        qsEncoderDraft.LPresetVideo.LPresetStream = "Include";
        qsEncoderDraft.LPresetVideo.LPresetEncoder = qsEncoderVideoEncoder;
        qsEncoderDraft.LPresetVideo.LPresetRateControl = qsEncoderVideoRate;
        qsEncoderDraft.LPresetVideo.LPresetSize = QSEncoderSizeFormat();
        qsEncoderDraft.LPresetAudio.LPresetEncoder = qsEncoderAudioEncoder;
        qsEncoderDraft.LPresetAudio.LPresetRateControl = qsEncoderAudioRate;
        qsEncoderSource.LPresetCopy(qsEncoderDraft);
    }
}
