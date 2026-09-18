using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed partial class LSEncoder
{
    public const string LSEncoderSourceMode = "Same as source";

    private readonly LPreset lsEncoderSource;
    private readonly LPreset lsEncoderDraft;
    private readonly bool lsEncoderSmart;
    private string? lsEncoderSuffixMode;
    private string? lsEncoderLocationMode;
    private CancellationTokenSource? lsEncoderVideoTrial;
    private CancellationTokenSource? lsEncoderAudioTrial;

    public LSEncoder(LPreset lSource, bool lSmart)
    {
        lsEncoderSource = lSource;
        lsEncoderDraft = lSource.LPresetClone();
        lsEncoderSmart = lSmart;
        LSEncoderVideoPrepare();
        LSEncoderAudioPrepare();
    }

    public LPreset LSEncoderDraft => lsEncoderDraft;

    private CancellationToken LSEncoderScanStart(LTrialKind lKind)
    {
        ref CancellationTokenSource? lSlot = ref lKind == LTrialKind.LTrialKindAudio
            ? ref lsEncoderAudioTrial
            : ref lsEncoderVideoTrial;
        lSlot?.Cancel();
        lSlot = new CancellationTokenSource();
        return lSlot.Token;
    }

    public void LSEncoderScanCancel()
    {
        lsEncoderVideoTrial?.Cancel();
        lsEncoderAudioTrial?.Cancel();
    }

    public bool LSEncoderSmart => lsEncoderSmart;

    public string? LSEncoderSuffixMode => lsEncoderSuffixMode;

    public string? LSEncoderLocationMode => lsEncoderLocationMode;

    public static bool LSEncoderSuffixCheck(string lPolicy) =>
        string.Equals(lPolicy, "Rename output", StringComparison.Ordinal)
        || string.Equals(lPolicy, "Rename existing", StringComparison.Ordinal);

    public static string LSEncoderSuffixResolve(string lPolicy) =>
        string.Equals(lPolicy, "Rename existing", StringComparison.Ordinal)
            ? "Encoder.Field.Output.SuffixSource"
            : "Encoder.Field.Output.SuffixOutput";

    public string LSEncoderSuffixSelect(string lPolicy, string lSuffixShown)
    {
        if (lsEncoderSuffixMode is not null
            && LSEncoderSuffixCheck(lsEncoderSuffixMode)
            && !string.Equals(lsEncoderSuffixMode, lPolicy, StringComparison.Ordinal))
        {
            lsEncoderDraft.LPresetSuffixSet(lsEncoderSuffixMode, lSuffixShown.Trim());
        }

        lsEncoderSuffixMode = lPolicy;
        return LSEncoderSuffixCheck(lPolicy) ? lsEncoderDraft.LPresetSuffixRead(lPolicy) : lSuffixShown;
    }

    public string LSEncoderSuffixNormalize(string lPolicy, string lSuffixShown)
    {
        if (!string.IsNullOrEmpty(lSuffixShown.Trim()))
        {
            return lSuffixShown;
        }

        lsEncoderDraft.LPresetSuffixSet(lPolicy, string.Empty);
        return lsEncoderDraft.LPresetSuffixRead(lPolicy);
    }

    public static bool LSEncoderFolderCheck(string lMode) =>
        !string.Equals(lMode, LSEncoderSourceMode, StringComparison.Ordinal);

    public static bool LSEncoderCustomCheck(string lMode) =>
        string.Equals(lMode, "Custom location", StringComparison.Ordinal);

    public static string LSEncoderFolderResolve(string lMode) => lMode switch
    {
        "Sibling" => "Encoder.Location.Sibling",
        "Custom location" => "Encoder.Location.Custom",
        _ => "Encoder.Location.Subfolder"
    };

    public static string LSEncoderStatusResolve(string lMode) => lMode switch
    {
        "Subfolder" => "Encoder.Location.SubfolderStatus",
        "Sibling" => "Encoder.Location.SiblingStatus",
        "Custom location" => "Encoder.Location.CustomStatus",
        _ => "Encoder.Location.Source"
    };

    public string LSEncoderLocationSelect(string lMode, string lFolderShown)
    {
        string lFolder = lFolderShown;
        if (lsEncoderLocationMode is not null && !string.Equals(lsEncoderLocationMode, lMode, StringComparison.Ordinal))
        {
            lsEncoderDraft.LPresetLocationSet(lsEncoderLocationMode, lFolderShown.Trim());
            lFolder = lsEncoderDraft.LPresetLocationRead(lMode);
        }

        lsEncoderLocationMode = lMode;
        return lFolder;
    }

    public string LSEncoderNoticeResolve(string lMode)
    {
        if (lMode != "Smart")
        {
            return "Encoder.Video.Notice.Copied";
        }

        return lsEncoderSmart ? "Encoder.Video.Notice.Smart" : "Encoder.Video.Notice.SmartFull";
    }

    public void LSEncoderApply()
    {
        lsEncoderDraft.LPresetDisplay = string.IsNullOrWhiteSpace(lsEncoderDraft.LPresetDisplay)
            ? "{OriginalName}_export"
            : lsEncoderDraft.LPresetDisplay.Trim();
        lsEncoderDraft.LPresetVideo.LPresetStream = "Include";
        lsEncoderDraft.LPresetVideo.LPresetEncoder = lsEncoderVideoEncoder;
        lsEncoderDraft.LPresetVideo.LPresetRateControl = lsEncoderVideoRate;
        lsEncoderDraft.LPresetVideo.LPresetSize = LSEncoderSizeFormat();
        lsEncoderDraft.LPresetAudio.LPresetEncoder = lsEncoderAudioEncoder;
        lsEncoderDraft.LPresetAudio.LPresetRateControl = lsEncoderAudioRate;
        lsEncoderSource.LPresetCopy(lsEncoderDraft);
    }
}
