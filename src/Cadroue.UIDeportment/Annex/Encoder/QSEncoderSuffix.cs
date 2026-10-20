using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The encoder window's name-clash policy: each policy keeps its own suffix in the draft while another is shown.
public sealed class QSEncoderSuffix
{
    private readonly CSEncoderSuffix qsEncoderSuffixGate;

    internal QSEncoderSuffix(CSEncoderSuffix lSuffix)
    {
        qsEncoderSuffixGate = lSuffix;
    }

    public static string? QSEncoderSuffixResolve(string lPolicy) => CSEncoderSuffix.CSEncoderSuffixResolve(lPolicy);

    public string QSEncoderSuffixRead(string lPolicy) => qsEncoderSuffixGate.CSEncoderSuffixRead(lPolicy);

    public string QSEncoderSuffixSelect(string lPolicy, string lSuffixShown) =>
        qsEncoderSuffixGate.CSEncoderSuffixSelect(lPolicy, lSuffixShown);

    public string QSEncoderSuffixNormalize(string lPolicy, string lSuffixShown) =>
        qsEncoderSuffixGate.CSEncoderSuffixNormalize(lPolicy, lSuffixShown);
}
