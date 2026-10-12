using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QTone
{
    private LWorkVideoStep qToneBrightness = LWorkVideoStep.LWorkBrightnessCreate(false, 0);
    private LWorkVideoStep qToneContrast = LWorkVideoStep.LWorkContrastCreate(false, 100);
    private LWorkVideoStep qToneSaturation = LWorkVideoStep.LWorkSaturationCreate(false, 100);
    private bool qToneBrightnessPersistent;
    private bool qToneContrastPersistent;
    private bool qToneSaturationPersistent;
    private bool qToneCapable = true;

    public event Action? QToneChanged;

    public bool QToneCapable => qToneCapable;

    public bool QToneNoticeShown =>
        !LFlyleaf.LFlyleafActive && LRenderer.LRendererEngineRead() != LPreviewEngine.LPreviewEngineMpv;

    public LWorkVideoStep QToneStepRead(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindContrast => qToneContrast,
        LColorKind.LColorKindSaturation => qToneSaturation,
        _ => qToneBrightness
    };

    public bool QTonePersistentRead(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindContrast => qToneContrastPersistent,
        LColorKind.LColorKindSaturation => qToneSaturationPersistent,
        _ => qToneBrightnessPersistent
    };

    public void QToneStepSet(LWorkVideoStep lStep)
    {
        LWorkVideoStep lNormal = QToneNormalize(lStep.LWorkStepKind, lStep.LWorkStepActive, lStep.LWorkStepValue);
        if (QToneStepRead(lNormal.LWorkStepKind) == lNormal)
        {
            return;
        }

        switch (lNormal.LWorkStepKind)
        {
            case LColorKind.LColorKindContrast:
                qToneContrast = lNormal;
                break;
            case LColorKind.LColorKindSaturation:
                qToneSaturation = lNormal;
                break;
            default:
                qToneBrightness = lNormal;
                break;
        }

        QToneChanged?.Invoke();
    }

    public void QToneActiveSet(LColorKind lKind, bool lActive) =>
        QToneStepSet(QToneNormalize(lKind, lActive, QToneStepRead(lKind).LWorkStepValue));

    public void QToneValueSet(LColorKind lKind, double lValue) =>
        QToneStepSet(QToneNormalize(lKind, QToneStepRead(lKind).LWorkStepActive, lValue));

    public void QTonePersistentSet(LColorKind lKind, bool lPersistent)
    {
        if (QTonePersistentRead(lKind) == lPersistent)
        {
            return;
        }

        switch (lKind)
        {
            case LColorKind.LColorKindContrast:
                qToneContrastPersistent = lPersistent;
                break;
            case LColorKind.LColorKindSaturation:
                qToneSaturationPersistent = lPersistent;
                break;
            default:
                qToneBrightnessPersistent = lPersistent;
                break;
        }

        QToneChanged?.Invoke();
    }

    public void QToneCapableSet(bool lCapable)
    {
        if (qToneCapable == lCapable)
        {
            return;
        }

        qToneCapable = lCapable;
        QToneChanged?.Invoke();
    }

    private static LWorkVideoStep QToneNormalize(LColorKind lKind, bool lActive, double lValue) => lKind switch
    {
        LColorKind.LColorKindContrast => LWorkVideoStep.LWorkContrastCreate(lActive, lValue),
        LColorKind.LColorKindSaturation => LWorkVideoStep.LWorkSaturationCreate(lActive, lValue),
        _ => LWorkVideoStep.LWorkBrightnessCreate(lActive, lValue)
    };
}
