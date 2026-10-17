using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;

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

    private readonly CInspector qInspectorVideoGate;
    private int qInspectorVideoDepth;
    private bool qInspectorVideoPending;

    public QInspectorVideo(CInspector lInspector)
    {
        qInspectorVideoGate = lInspector;
        QInspectorGamma = lInspector.CInspectorGamma;
        QInspectorExposure = lInspector.CInspectorExposure;
        QInspectorVideoAttach();
    }

    private void QInspectorVideoAttach()
    {
        QInspectorTone.QToneChanged += QInspectorVideoHandle;
        QInspectorGamma.CGammaChanged += QInspectorVideoHandle;
        QInspectorExposure.CExposureChanged += QInspectorVideoHandle;
        QInspectorCurve.QCurveChanged += QInspectorVideoHandle;
        QInspectorWhitebalance.QWhitebalanceChanged += QInspectorVideoHandle;
    }

    public event Action? QInspectorSectionChanged;
    public event Action? QInspectorVideoChanged;

    public QTone QInspectorTone { get; } = new();

    public CGamma QInspectorGamma { get; }

    public CExposure QInspectorExposure { get; }

    public QCurve QInspectorCurve { get; } = new();

    public QWhitebalance QInspectorWhitebalance { get; } = new();

    public LWorkVideoStep QInspectorStepRead(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindGamma => QInspectorGammaBuild(),
        LColorKind.LColorKindWhitebalance => QInspectorWhitebalance.QWhitebalanceStep,
        LColorKind.LColorKindExposure => QInspectorExposureBuild(),
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
            QInspectorTone.QToneStepSet(QInspectorStepResolve(lVideo, LColorKind.LColorKindBrightness));
            QInspectorTone.QToneStepSet(QInspectorStepResolve(lVideo, LColorKind.LColorKindContrast));
            QInspectorTone.QToneStepSet(QInspectorStepResolve(lVideo, LColorKind.LColorKindSaturation));
            QInspectorWhitebalance.QWhitebalanceStepSet(
                QInspectorStepResolve(lVideo, LColorKind.LColorKindWhitebalance));
            qInspectorVideoGate.CInspectorVideoAccept(new LInspectorOutlet(lVideo));
            QInspectorCurve.QCurveStepSet(QInspectorStepResolve(lVideo, LColorKind.LColorKindCurve));
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
        LColorKind.LColorKindExposure => QInspectorExposure.CExposurePersistent,
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
                QInspectorExposure.CExposurePersistentSet(lPersistent);
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

    // The gamma gate holds plain values; the step is built from them here. A plan is read back through the gate.
    private LWorkVideoStep QInspectorGammaBuild() =>
        LWorkVideoStep.LWorkGammaCreate(
            QInspectorGamma.CGammaActive,
            QInspectorGamma.CGammaValues[0],
            QInspectorGamma.CGammaValues[1],
            QInspectorGamma.CGammaValues[2],
            QInspectorGamma.CGammaValues[3],
            QInspectorGamma.CGammaValues[4]);

    // The exposure gate holds plain values; the step is built from them here.
    private LWorkVideoStep QInspectorExposureBuild() =>
        LWorkVideoStep.LWorkExposureCreate(QInspectorExposure.CExposureActive, QInspectorExposure.CExposureValue);

    private static LWorkVideoStep QInspectorStepResolve(LWorkVideo lVideo, LColorKind lKind)
    {
        if (QInspectorStepFind(lVideo, lKind) is { } lStep)
        {
            return lStep;
        }

        return lKind switch
        {
            LColorKind.LColorKindBrightness => LWorkVideoStep.LWorkBrightnessCreate(false, 0),
            LColorKind.LColorKindContrast => LWorkVideoStep.LWorkContrastCreate(false, 100),
            LColorKind.LColorKindSaturation => LWorkVideoStep.LWorkSaturationCreate(false, 100),
            LColorKind.LColorKindWhitebalance => LWorkVideoStep.LWorkWhitebalanceCreate(false),
            _ => LWorkVideoStep.LWorkCurveCreate(false)
        };
    }

    private static LWorkVideoStep? QInspectorStepFind(LWorkVideo lVideo, LColorKind lKind) =>
        lVideo.LWorkVideoSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
}
