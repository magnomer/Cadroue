using Cadroue.Application;

namespace Cadroue.UIDeportment;

// The encoder window's name-clash policy: each policy keeps its own suffix in the draft while another is shown.
public sealed class QSEncoderSuffix
{
    private readonly LPreset qsEncoderDraft;
    private string? qsEncoderSuffixMode;

    internal QSEncoderSuffix(LPreset lDraft)
    {
        qsEncoderDraft = lDraft;
    }

    public string? QSEncoderSuffixMode => qsEncoderSuffixMode;

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
}
