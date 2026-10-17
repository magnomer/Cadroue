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
        QSEncoderInventoryAttach();
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

    private void QSEncoderVerdictSet(LTrialKind lKind, IReadOnlyList<QSVerdictRow> lRows)
    {
        if (lKind == LTrialKind.LTrialKindAudio)
        {
            qsEncoderAudioResults = lRows;
            return;
        }

        qsEncoderVideoResults = lRows;
    }

    private static string QSEncoderRateResolve(string[] lModes, string lRate) =>
        lModes.Contains(lRate) ? lRate : lModes[0];

    private static double QSEncoderProgressResolve(int lDone, int lTotal) =>
        lTotal == 0 ? 1 : (double)lDone / lTotal;

    public void QSEncoderClose()
    {
        QSEncoderHelpDetach();
        QSEncoderTrialCancel();
    }

    private void QSEncoderInventoryAttach() => LInventory.LInventoryHelpReady += QSEncoderHelpHandle;

    private void QSEncoderHelpDetach()
    {
        LInventory.LInventoryHelpReady -= QSEncoderHelpHandle;
        qsEncoderHelpHandler = null;
    }

    private void QSEncoderTrialCancel()
    {
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
        if (QSEncoderSuffixFind(lPolicy) is { } lPrevious)
        {
            qsEncoderDraft.LPresetSuffixSet(lPrevious, lSuffixShown.Trim());
        }

        QSEncoderSuffixSet(lPolicy);
        if (!QSEncoderSuffixCheck(lPolicy))
        {
            return lSuffixShown;
        }

        return qsEncoderDraft.LPresetSuffixRead(lPolicy);
    }

    private string? QSEncoderSuffixFind(string lPolicy) =>
        qsEncoderSuffixMode is not null
        && QSEncoderSuffixCheck(qsEncoderSuffixMode)
        && !string.Equals(qsEncoderSuffixMode, lPolicy, StringComparison.Ordinal)
            ? qsEncoderSuffixMode
            : null;

    private void QSEncoderSuffixSet(string lPolicy) => qsEncoderSuffixMode = lPolicy;

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
        if (QSEncoderLocationFind(lMode) is { } lPrevious)
        {
            qsEncoderDraft.LPresetLocationSet(lPrevious, lFolderShown.Trim());
            lFolder = qsEncoderDraft.LPresetLocationRead(lMode);
        }

        QSEncoderLocationSet(lMode);
        return lFolder;
    }

    private string? QSEncoderLocationFind(string lMode) =>
        qsEncoderLocationMode is not null && !string.Equals(qsEncoderLocationMode, lMode, StringComparison.Ordinal)
            ? qsEncoderLocationMode
            : null;

    private void QSEncoderLocationSet(string lMode) => qsEncoderLocationMode = lMode;

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
        qsEncoderDraft.LPresetVideo.LPresetEncoder = qsEncoderVideoEncoder;
        qsEncoderDraft.LPresetVideo.LPresetRateControl = qsEncoderVideoRate;
        qsEncoderDraft.LPresetVideo.LPresetSize = QSEncoderSizeFormat();
        qsEncoderDraft.LPresetAudio.LPresetEncoder = qsEncoderAudioEncoder;
        qsEncoderDraft.LPresetAudio.LPresetRateControl = qsEncoderAudioRate;
    }
}
