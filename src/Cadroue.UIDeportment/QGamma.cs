using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QGamma
{
    private static readonly double[] qGammaLeast = { -100, -100, -100, -100, 0 };

    private LWorkVideoStep qGammaStep = LWorkVideoStep.LWorkGammaCreate(false, 0);
    private bool qGammaPersistent;
    private bool qGammaCapable;
    private bool qGammaPreview;

    public event Action? QGammaChanged;

    public LWorkVideoStep QGammaStep => qGammaStep;

    public LWorkGammaSettings QGammaValue => qGammaStep.LWorkGammaRead();

    public bool QGammaPersistent => qGammaPersistent;

    public bool QGammaCapable => qGammaCapable;

    public bool QGammaPreview => qGammaPreview;

    public double QGammaLeastRead(int lIndex) => qGammaLeast[lIndex];

    public double QGammaValueRead(int lIndex) => lIndex switch
    {
        1 => QGammaValue.LWorkGammaRed,
        2 => QGammaValue.LWorkGammaGreen,
        3 => QGammaValue.LWorkGammaBlue,
        4 => QGammaValue.LWorkGammaHighlight,
        _ => QGammaValue.LWorkGammaGlobal
    };

    public void QGammaStepSet(LWorkVideoStep lStep)
    {
        LWorkGammaSettings lValue = lStep.LWorkGammaRead();
        LWorkVideoStep lNormal = LWorkVideoStep.LWorkGammaCreate(
            lStep.LWorkStepActive,
            lValue.LWorkGammaGlobal,
            lValue.LWorkGammaRed,
            lValue.LWorkGammaGreen,
            lValue.LWorkGammaBlue,
            lValue.LWorkGammaHighlight);
        if (qGammaStep == lNormal)
        {
            return;
        }

        qGammaStep = lNormal;
        QGammaChanged?.Invoke();
    }

    public void QGammaActiveSet(bool lActive) =>
        QGammaStepSet(qGammaStep with { LWorkStepActive = lActive });

    public void QGammaValueSet(int lIndex, double lValue)
    {
        LWorkGammaSettings lCurrent = QGammaValue;
        QGammaStepSet(LWorkVideoStep.LWorkGammaCreate(
            qGammaStep.LWorkStepActive,
            lIndex == 0 ? lValue : lCurrent.LWorkGammaGlobal,
            lIndex == 1 ? lValue : lCurrent.LWorkGammaRed,
            lIndex == 2 ? lValue : lCurrent.LWorkGammaGreen,
            lIndex == 3 ? lValue : lCurrent.LWorkGammaBlue,
            lIndex == 4 ? lValue : lCurrent.LWorkGammaHighlight));
    }

    public void QGammaReset() =>
        QGammaStepSet(LWorkVideoStep.LWorkGammaCreate(qGammaStep.LWorkStepActive, 0));

    public void QGammaPersistentSet(bool lPersistent)
    {
        if (qGammaPersistent == lPersistent)
        {
            return;
        }

        qGammaPersistent = lPersistent;
        QGammaChanged?.Invoke();
    }

    public void QGammaCapableSet(bool lCapable, bool lPreview)
    {
        bool lPreviewShown = lCapable && lPreview;
        if (qGammaCapable == lCapable && qGammaPreview == lPreviewShown)
        {
            return;
        }

        qGammaCapable = lCapable;
        qGammaPreview = lPreviewShown;
        QGammaChanged?.Invoke();
    }
}
