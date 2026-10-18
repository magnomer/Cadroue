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
        QInspectorTone = lInspector.CInspectorTone;
        QInspectorGamma = lInspector.CInspectorGamma;
        QInspectorExposure = lInspector.CInspectorExposure;
        QInspectorWhitebalance = lInspector.CInspectorWhitebalance;
        QInspectorCurve = new QCurve(lInspector.CInspectorCurve);
        QInspectorVideoAttach();
    }

    private void QInspectorVideoAttach()
    {
        QInspectorTone.CToneChanged += QInspectorVideoHandle;
        QInspectorGamma.CGammaChanged += QInspectorVideoHandle;
        QInspectorExposure.CExposureChanged += QInspectorVideoHandle;
        QInspectorCurve.QCurveChanged += QInspectorVideoHandle;
        QInspectorWhitebalance.CWhitebalanceChanged += QInspectorVideoHandle;
    }

    public event Action? QInspectorSectionChanged;
    public event Action? QInspectorVideoChanged;

    public CTone QInspectorTone { get; }

    public CGamma QInspectorGamma { get; }

    public CExposure QInspectorExposure { get; }

    public QCurve QInspectorCurve { get; }

    public CWhitebalance QInspectorWhitebalance { get; }

    public LWorkVideoStep QInspectorStepRead(LColorKind lKind) => lKind switch
    {
        LColorKind.LColorKindGamma => QInspectorGammaBuild(),
        LColorKind.LColorKindWhitebalance =>
            LWhitebalancePort.LWhitebalanceStepCreate(QInspectorWhitebalance.CWhitebalanceAnswer),
        LColorKind.LColorKindExposure => QInspectorExposureBuild(),
        LColorKind.LColorKindCurve => QInspectorCurve.QCurveStepRead(),
        LColorKind.LColorKindContrast => LWorkVideoStep.LWorkContrastCreate(
            QInspectorTone.CToneActives[1], QInspectorTone.CToneValues[1]),
        LColorKind.LColorKindSaturation => LWorkVideoStep.LWorkSaturationCreate(
            QInspectorTone.CToneActives[2], QInspectorTone.CToneValues[2]),
        _ => LWorkVideoStep.LWorkBrightnessCreate(QInspectorTone.CToneActives[0], QInspectorTone.CToneValues[0])
    };

    // The gate answers the plan of its tools in the order of these processing steps; without the mpv-only filters it
    // leaves out their steps.
    public LWorkVideo QInspectorVideoRead(IReadOnlyList<string> lProcessing, bool lMpvOnlyCapable) =>
        qInspectorVideoGate.CInspectorVideo.CInspectorVideoRead(lProcessing, lMpvOnlyCapable).LInspectorVideoRead();

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
            qInspectorVideoGate.CInspectorVideo.CInspectorVideoAccept(new LInspectorOutlet(lVideo));
            QInspectorCurveApply(lVideo);
        }
        finally
        {
            QInspectorVideoResume();
        }
    }

    // The gate does not read a plan's curve yet: the curve takes the plan's curve step here, after the gate set the
    // other video tools.
    public void QInspectorCurveApply(LWorkVideo lVideo)
    {
        qInspectorVideoDepth++;
        try
        {
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
        LColorKind.LColorKindWhitebalance => QInspectorWhitebalance.CWhitebalancePersistent,
        LColorKind.LColorKindExposure => QInspectorExposure.CExposurePersistent,
        LColorKind.LColorKindCurve => QInspectorCurve.QCurvePersistent,
        LColorKind.LColorKindContrast => QInspectorTone.CTonePersistents[1],
        LColorKind.LColorKindSaturation => QInspectorTone.CTonePersistents[2],
        _ => QInspectorTone.CTonePersistents[0]
    };

    public void QInspectorPersistentSet(LColorKind lKind, bool lPersistent)
    {
        switch (lKind)
        {
            case LColorKind.LColorKindGamma:
                QInspectorGamma.CGammaPersistentSet(lPersistent);
                break;
            case LColorKind.LColorKindWhitebalance:
                QInspectorWhitebalance.CWhitebalancePersistentSet(lPersistent);
                break;
            case LColorKind.LColorKindExposure:
                QInspectorExposure.CExposurePersistentSet(lPersistent);
                break;
            case LColorKind.LColorKindCurve:
                QInspectorCurve.QCurvePersistentSet(lPersistent);
                break;
            case LColorKind.LColorKindContrast:
                QInspectorTone.CTonePersistentSet(1, lPersistent);
                break;
            case LColorKind.LColorKindSaturation:
                QInspectorTone.CTonePersistentSet(2, lPersistent);
                break;
            default:
                QInspectorTone.CTonePersistentSet(0, lPersistent);
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

    // The gate answers the carried plan of its tools.
    public LWorkVideo QInspectorPersistentRead() =>
        qInspectorVideoGate.CInspectorVideo.CInspectorCarriedRead().LInspectorVideoRead();

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

        return LWorkVideoStep.LWorkCurveCreate(false);
    }

    private static LWorkVideoStep? QInspectorStepFind(LWorkVideo lVideo, LColorKind lKind) =>
        lVideo.LWorkVideoSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
}
