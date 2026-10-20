using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The encoder window's size driver: it relays tier picks and typed sizes to the Conduct size gate.
public sealed class QSEncoderSize
{
    private readonly CSEncoderSize qsEncoderSizeGate;

    internal QSEncoderSize(CSEncoderSize lSize)
    {
        qsEncoderSizeGate = lSize;
        lSize.CSEncoderSizeChanged += _ => QSEncoderSizeChanged?.Invoke();
    }

    public event Action? QSEncoderSizeChanged;

    public CSEncoderDimension QSEncoderSizeValue => qsEncoderSizeGate.CSEncoderSizeValue;

    public bool QSEncoderTierSelect(double lKnob) => qsEncoderSizeGate.CSEncoderSizeSelect(lKnob);

    public bool QSEncoderSizeSet(string lWidth, string lHeight) => qsEncoderSizeGate.CSEncoderSizeSet(lWidth, lHeight);
}
