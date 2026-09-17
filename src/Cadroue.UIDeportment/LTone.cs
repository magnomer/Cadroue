using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LTone
{
    private LWorkVideoStep lToneBrightness = LWorkVideoStep.LWorkBrightnessCreate(false, 0);
    private LWorkVideoStep lToneContrast = LWorkVideoStep.LWorkContrastCreate(false, 100);
    private LWorkVideoStep lToneSaturation = LWorkVideoStep.LWorkSaturationCreate(false, 100);
    private bool lToneBrightnessPersistent;
    private bool lToneContrastPersistent;
    private bool lToneSaturationPersistent;
    private bool lToneCapable = true;

    public event Action? LToneChange;

    public bool LToneCapable => lToneCapable;

    public LWorkVideoStep LToneStepRead(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindContrast => lToneContrast,
        LColorKind.LColorKindSaturation => lToneSaturation,
        _ => lToneBrightness
    };

    public bool LTonePersistentRead(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindContrast => lToneContrastPersistent,
        LColorKind.LColorKindSaturation => lToneSaturationPersistent,
        _ => lToneBrightnessPersistent
    };

    public void LToneStepSet(LWorkVideoStep lStep)
    {
        LWorkVideoStep lNormal = LToneNormalize(lStep.LWorkStepKind, lStep.LWorkStepActive, lStep.LWorkStepValue);
        if (LToneStepRead(lNormal.LWorkStepKind) == lNormal)
        {
            return;
        }

        switch (lNormal.LWorkStepKind)
        {
            case LColorKind.LColorKindContrast:
                lToneContrast = lNormal;
                break;
            case LColorKind.LColorKindSaturation:
                lToneSaturation = lNormal;
                break;
            default:
                lToneBrightness = lNormal;
                break;
        }

        LToneChange?.Invoke();
    }

    public void LToneActiveSet(LColorKind lKind, bool lActive) =>
        LToneStepSet(LToneNormalize(lKind, lActive, LToneStepRead(lKind).LWorkStepValue));

    public void LToneValueSet(LColorKind lKind, double lValue) =>
        LToneStepSet(LToneNormalize(lKind, LToneStepRead(lKind).LWorkStepActive, lValue));

    public void LTonePersistentSet(LColorKind lKind, bool lPersistent)
    {
        if (LTonePersistentRead(lKind) == lPersistent)
        {
            return;
        }

        switch (lKind)
        {
            case LColorKind.LColorKindContrast:
                lToneContrastPersistent = lPersistent;
                break;
            case LColorKind.LColorKindSaturation:
                lToneSaturationPersistent = lPersistent;
                break;
            default:
                lToneBrightnessPersistent = lPersistent;
                break;
        }

        LToneChange?.Invoke();
    }

    public void LToneCapableSet(bool lCapable)
    {
        if (lToneCapable == lCapable)
        {
            return;
        }

        lToneCapable = lCapable;
        LToneChange?.Invoke();
    }

    private static LWorkVideoStep LToneNormalize(LColorKind lKind, bool lActive, double lValue) => lKind switch
    {
        LColorKind.LColorKindContrast => LWorkVideoStep.LWorkContrastCreate(lActive, lValue),
        LColorKind.LColorKindSaturation => LWorkVideoStep.LWorkSaturationCreate(lActive, lValue),
        _ => LWorkVideoStep.LWorkBrightnessCreate(lActive, lValue)
    };
}
