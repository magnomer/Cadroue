using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The encoder window's size driver: it relays tier picks and typed sizes to the Conduct size gate.
public sealed class QSEncoderSize
{
    public const int QSEncoderDimensionMost = CSEncoderDimension.CSEncoderDimensionMost;

    private readonly CSEncoderSize qsEncoderSizeGate;

    internal QSEncoderSize(CSEncoderSize lSize)
    {
        qsEncoderSizeGate = lSize;
        lSize.CSEncoderSizeChanged += _ => QSEncoderSizeChanged?.Invoke();
    }

    public event Action? QSEncoderSizeChanged;

    public static IReadOnlyList<string> QSEncoderTierLabels => CSEncoderSize.CSEncoderTierLabels;

    public int QSEncoderSizeTier => qsEncoderSizeGate.CSEncoderSizeValue.CSEncoderDimensionTier;

    public int QSEncoderWidth => qsEncoderSizeGate.CSEncoderSizeValue.CSEncoderDimensionWidth;

    public int QSEncoderHeight => qsEncoderSizeGate.CSEncoderSizeValue.CSEncoderDimensionHeight;

    public bool QSEncoderTierSelect(int lTier) => qsEncoderSizeGate.CSEncoderSizeSelect(lTier);

    public bool QSEncoderSizeSet(int lWidth, int lHeight) => qsEncoderSizeGate.CSEncoderSizeSet(lWidth, lHeight);
}
