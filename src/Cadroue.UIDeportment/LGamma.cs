using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LGamma
{
    private static readonly double[] lGammaLeast = { -100, -100, -100, -100, 0 };

    private LWorkVideoStep lGammaStep = LWorkVideoStep.LWorkGammaCreate(false, 0);
    private bool lGammaPersistent;
    private bool lGammaCapable;
    private bool lGammaPreview;

    public event Action? LGammaChange;

    public LWorkVideoStep LGammaStep => lGammaStep;

    public LWorkGammaSettings LGammaValue => lGammaStep.LWorkGammaRead();

    public bool LGammaPersistent => lGammaPersistent;

    public bool LGammaCapable => lGammaCapable;

    public bool LGammaPreview => lGammaPreview;

    public double LGammaLeastRead(int lIndex) => lGammaLeast[lIndex];

    public double LGammaValueRead(int lIndex) => lIndex switch
    {
        1 => LGammaValue.LWorkGammaRed,
        2 => LGammaValue.LWorkGammaGreen,
        3 => LGammaValue.LWorkGammaBlue,
        4 => LGammaValue.LWorkGammaHighlight,
        _ => LGammaValue.LWorkGammaGlobal
    };

    public void LGammaStepSet(LWorkVideoStep lStep)
    {
        LWorkGammaSettings lValue = lStep.LWorkGammaRead();
        LWorkVideoStep lNormal = LWorkVideoStep.LWorkGammaCreate(
            lStep.LWorkStepActive,
            lValue.LWorkGammaGlobal,
            lValue.LWorkGammaRed,
            lValue.LWorkGammaGreen,
            lValue.LWorkGammaBlue,
            lValue.LWorkGammaHighlight);
        if (lGammaStep == lNormal)
        {
            return;
        }

        lGammaStep = lNormal;
        LGammaChange?.Invoke();
    }

    public void LGammaActiveSet(bool lActive) =>
        LGammaStepSet(lGammaStep with { LWorkStepActive = lActive });

    public void LGammaValueSet(int lIndex, double lValue)
    {
        LWorkGammaSettings lCurrent = LGammaValue;
        LGammaStepSet(LWorkVideoStep.LWorkGammaCreate(
            lGammaStep.LWorkStepActive,
            lIndex == 0 ? lValue : lCurrent.LWorkGammaGlobal,
            lIndex == 1 ? lValue : lCurrent.LWorkGammaRed,
            lIndex == 2 ? lValue : lCurrent.LWorkGammaGreen,
            lIndex == 3 ? lValue : lCurrent.LWorkGammaBlue,
            lIndex == 4 ? lValue : lCurrent.LWorkGammaHighlight));
    }

    public void LGammaReset() =>
        LGammaStepSet(LWorkVideoStep.LWorkGammaCreate(lGammaStep.LWorkStepActive, 0));

    public void LGammaPersistentSet(bool lPersistent)
    {
        if (lGammaPersistent == lPersistent)
        {
            return;
        }

        lGammaPersistent = lPersistent;
        LGammaChange?.Invoke();
    }

    public void LGammaCapableSet(bool lCapable, bool lPreview)
    {
        bool lPreviewShown = lCapable && lPreview;
        if (lGammaCapable == lCapable && lGammaPreview == lPreviewShown)
        {
            return;
        }

        lGammaCapable = lCapable;
        lGammaPreview = lPreviewShown;
        LGammaChange?.Invoke();
    }
}
