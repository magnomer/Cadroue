using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QExposure
{
    private LWorkVideoStep qExposureStep = LWorkVideoStep.LWorkExposureCreate(false, 0);
    private bool qExposurePersistent;
    private bool qExposureCapable;
    private bool qExposurePreview;

    public event Action? QExposureChanged;

    public LWorkVideoStep QExposureStep => qExposureStep;

    public bool QExposurePersistent => qExposurePersistent;

    public bool QExposureCapable => qExposureCapable;

    public bool QExposurePreview => qExposurePreview;

    public void QExposureStepSet(LWorkVideoStep lStep)
    {
        LWorkVideoStep lNormal = LWorkVideoStep.LWorkExposureCreate(lStep.LWorkStepActive, lStep.LWorkStepValue);
        if (qExposureStep == lNormal)
        {
            return;
        }

        qExposureStep = lNormal;
        QExposureChanged?.Invoke();
    }

    public void QExposureActiveSet(bool lActive) =>
        QExposureStepSet(LWorkVideoStep.LWorkExposureCreate(lActive, qExposureStep.LWorkStepValue));

    public void QExposureValueSet(double lValue) =>
        QExposureStepSet(LWorkVideoStep.LWorkExposureCreate(qExposureStep.LWorkStepActive, lValue));

    public void QExposurePersistentSet(bool lPersistent)
    {
        if (qExposurePersistent == lPersistent)
        {
            return;
        }

        qExposurePersistent = lPersistent;
        QExposureChanged?.Invoke();
    }

    public void QExposureCapableSet(bool lCapable, bool lPreview)
    {
        bool lPreviewShown = lCapable && lPreview;
        if (qExposureCapable == lCapable && qExposurePreview == lPreviewShown)
        {
            return;
        }

        qExposureCapable = lCapable;
        qExposurePreview = lPreviewShown;
        QExposureChanged?.Invoke();
    }
}
