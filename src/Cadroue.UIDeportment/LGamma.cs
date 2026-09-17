using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LGamma
{
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

    public bool LGammaDefaultCheck() =>
        LGammaValue == new LWorkGammaSettings(0, 0, 0, 0, 0);

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
