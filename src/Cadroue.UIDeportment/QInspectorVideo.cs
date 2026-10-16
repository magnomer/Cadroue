using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QInspectorVideo
{
    private static readonly LColorKind[] qInspectorKinds =
    {
        LColorKind.LColorKindBrightness,
        LColorKind.LColorKindContrast,
        LColorKind.LColorKindSaturation,
        LColorKind.LColorKindGamma,
        LColorKind.LColorKindWhitebalance,
        LColorKind.LColorKindExposure,
        LColorKind.LColorKindCurve
    };

    private int qInspectorVideoDepth;
    private bool qInspectorVideoPending;

    public QInspectorVideo(CGamma lGamma)
    {
        QInspectorGamma = lGamma;
        QInspectorTone.QToneChanged += QInspectorVideoHandle;
        QInspectorGamma.CGammaChanged += QInspectorVideoHandle;
        QInspectorExposure.QExposureChanged += QInspectorVideoHandle;
        QInspectorCurve.QCurveChanged += QInspectorVideoHandle;
        QInspectorWhitebalance.QWhitebalanceChanged += QInspectorVideoHandle;
    }

    public event Action? QInspectorSectionChanged;
    public event Action? QInspectorVideoChanged;

    public QTone QInspectorTone { get; } = new();

    public CGamma QInspectorGamma { get; }

    public QExposure QInspectorExposure { get; } = new();

    public QCurve QInspectorCurve { get; } = new();

    public QWhitebalance QInspectorWhitebalance { get; } = new();

    public LWorkVideoStep QInspectorStepRead(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindGamma => QInspectorGammaBuild(),
        LColorKind.LColorKindWhitebalance => QInspectorWhitebalance.QWhitebalanceStep,
        LColorKind.LColorKindExposure => QInspectorExposure.QExposureStep,
        LColorKind.LColorKindCurve => QInspectorCurve.QCurveStepRead(),
        _ => QInspectorTone.QToneStepRead(lKind)
    };

    public void QInspectorVideoSuspend() => qInspectorVideoDepth++;

    public void QInspectorVideoResume()
    {
        qInspectorVideoDepth--;
        if (qInspectorVideoDepth > 0 || !qInspectorVideoPending)
        {
            return;
        }

        qInspectorVideoPending = false;
        QInspectorVideoChanged?.Invoke();
    }

    public void QInspectorVideoApply(LWorkVideo lVideo)
    {
        qInspectorVideoDepth++;
        try
        {
            QInspectorTone.QToneStepSet(QInspectorStepFind(lVideo, LColorKind.LColorKindBrightness)
                ?? LWorkVideoStep.LWorkBrightnessCreate(false, 0));
            QInspectorTone.QToneStepSet(QInspectorStepFind(lVideo, LColorKind.LColorKindContrast)
                ?? LWorkVideoStep.LWorkContrastCreate(false, 100));
            QInspectorTone.QToneStepSet(QInspectorStepFind(lVideo, LColorKind.LColorKindSaturation)
                ?? LWorkVideoStep.LWorkSaturationCreate(false, 100));
            QInspectorGammaAccept(QInspectorStepFind(lVideo, LColorKind.LColorKindGamma)
                ?? LWorkVideoStep.LWorkGammaCreate(false, 0));
            QInspectorWhitebalance.QWhitebalanceStepSet(QInspectorStepFind(lVideo, LColorKind.LColorKindWhitebalance)
                ?? LWorkVideoStep.LWorkWhitebalanceCreate(false));
            QInspectorExposure.QExposureStepSet(QInspectorStepFind(lVideo, LColorKind.LColorKindExposure)
                ?? LWorkVideoStep.LWorkExposureCreate(false, 0));
            QInspectorCurve.QCurveStepSet(QInspectorStepFind(lVideo, LColorKind.LColorKindCurve)
                ?? LWorkVideoStep.LWorkCurveCreate(false));
            qInspectorVideoPending = true;
        }
        finally
        {
            QInspectorVideoResume();
        }
    }

    public bool QInspectorPersistentCheck() => qInspectorKinds.Any(QInspectorPersistentCheck);

    public bool QInspectorPersistentCheck(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindGamma => QInspectorGamma.CGammaPersistent,
        LColorKind.LColorKindWhitebalance => QInspectorWhitebalance.QWhitebalancePersistent,
        LColorKind.LColorKindExposure => QInspectorExposure.QExposurePersistent,
        LColorKind.LColorKindCurve => QInspectorCurve.QCurvePersistent,
        _ => QInspectorTone.QTonePersistentRead(lKind)
    };

    public void QInspectorPersistentSet(LColorKind lKind, bool lPersistent)
    {
        switch (lKind)
        {
            case LColorKind.LColorKindGamma:
                QInspectorGamma.CGammaPersistentSet(lPersistent);
                break;
            case LColorKind.LColorKindWhitebalance:
                QInspectorWhitebalance.QWhitebalancePersistentSet(lPersistent);
                break;
            case LColorKind.LColorKindExposure:
                QInspectorExposure.QExposurePersistentSet(lPersistent);
                break;
            case LColorKind.LColorKindCurve:
                QInspectorCurve.QCurvePersistentSet(lPersistent);
                break;
            default:
                QInspectorTone.QTonePersistentSet(lKind, lPersistent);
                break;
        }
    }

    public void QInspectorPersistentApply(LWorkVideo lVideo)
    {
        foreach (LWorkVideoStep lStep in lVideo.LWorkVideoSteps)
        {
            QInspectorPersistentSet(lStep.LWorkStepKind, true);
        }
    }

    public LWorkVideo QInspectorPersistentRead() =>
        new(qInspectorKinds.Where(QInspectorPersistentCheck).Select(QInspectorStepRead).ToList());

    private void QInspectorVideoHandle()
    {
        QInspectorSectionChanged?.Invoke();
        if (qInspectorVideoDepth > 0)
        {
            qInspectorVideoPending = true;
            return;
        }

        QInspectorVideoChanged?.Invoke();
    }

    // The gamma gate holds plain values; the step is built from them and read back into them here.
    private LWorkVideoStep QInspectorGammaBuild() =>
        LWorkVideoStep.LWorkGammaCreate(
            QInspectorGamma.CGammaActive,
            QInspectorGamma.CGammaValues[0],
            QInspectorGamma.CGammaValues[1],
            QInspectorGamma.CGammaValues[2],
            QInspectorGamma.CGammaValues[3],
            QInspectorGamma.CGammaValues[4]);

    private void QInspectorGammaAccept(LWorkVideoStep lStep) =>
        QInspectorGamma.CGammaStepSet(lStep.LWorkStepActive, lStep.LWorkGammaRead().LWorkSlotRead());

    private static LWorkVideoStep? QInspectorStepFind(LWorkVideo lVideo, LColorKind lKind) =>
        lVideo.LWorkVideoSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
}
