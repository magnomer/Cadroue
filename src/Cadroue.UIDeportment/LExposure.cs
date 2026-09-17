using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LExposure
{
    private LWorkVideoStep lExposureStep = LWorkVideoStep.LWorkExposureCreate(false, 0);
    private bool lExposurePersistent;
    private bool lExposureCapable;
    private bool lExposurePreview;

    public event Action? LExposureChange;

    public LWorkVideoStep LExposureStep => lExposureStep;

    public bool LExposurePersistent => lExposurePersistent;

    public bool LExposureCapable => lExposureCapable;

    public bool LExposurePreview => lExposurePreview;

    public void LExposureStepSet(LWorkVideoStep lStep)
    {
        LWorkVideoStep lNormal = LWorkVideoStep.LWorkExposureCreate(lStep.LWorkStepActive, lStep.LWorkStepValue);
        if (lExposureStep == lNormal)
        {
            return;
        }

        lExposureStep = lNormal;
        LExposureChange?.Invoke();
    }

    public void LExposureActiveSet(bool lActive) =>
        LExposureStepSet(LWorkVideoStep.LWorkExposureCreate(lActive, lExposureStep.LWorkStepValue));

    public void LExposureValueSet(double lValue) =>
        LExposureStepSet(LWorkVideoStep.LWorkExposureCreate(lExposureStep.LWorkStepActive, lValue));

    public void LExposurePersistentSet(bool lPersistent)
    {
        if (lExposurePersistent == lPersistent)
        {
            return;
        }

        lExposurePersistent = lPersistent;
        LExposureChange?.Invoke();
    }

    public void LExposureCapableSet(bool lCapable, bool lPreview)
    {
        bool lPreviewShown = lCapable && lPreview;
        if (lExposureCapable == lCapable && lExposurePreview == lPreviewShown)
        {
            return;
        }

        lExposureCapable = lCapable;
        lExposurePreview = lPreviewShown;
        LExposureChange?.Invoke();
    }
}
