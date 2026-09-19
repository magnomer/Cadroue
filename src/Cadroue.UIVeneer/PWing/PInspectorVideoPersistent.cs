using Cadroue.Core;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private static readonly LColorKind[] PInspectorVideoKinds =
    {
        LColorKind.LColorKindBrightness,
        LColorKind.LColorKindContrast,
        LColorKind.LColorKindSaturation,
        LColorKind.LColorKindGamma,
        LColorKind.LColorKindWhitebalance,
        LColorKind.LColorKindExposure,
        LColorKind.LColorKindCurve
    };

    public bool PTonePersistentCheck() => PInspectorVideoKinds.Any(PTonePersistentRead);

    private bool PTonePersistentRead(LColorKind pKind) => pKind switch
    {
        LColorKind.LColorKindGamma => LGamma.LGammaPersistent,
        LColorKind.LColorKindWhitebalance => LWhitebalance.LWhitebalancePersistent,
        LColorKind.LColorKindExposure => LExposure.LExposurePersistent,
        LColorKind.LColorKindCurve => LCurve.LCurvePersistent,
        _ => LTone.LTonePersistentRead(pKind)
    };

    private void PTonePersistentSet(LColorKind pKind, bool pPersistent)
    {
        switch (pKind)
        {
            case LColorKind.LColorKindGamma:
                LGamma.LGammaPersistentSet(pPersistent);
                break;
            case LColorKind.LColorKindWhitebalance:
                LWhitebalance.LWhitebalancePersistentSet(pPersistent);
                break;
            case LColorKind.LColorKindExposure:
                LExposure.LExposurePersistentSet(pPersistent);
                break;
            case LColorKind.LColorKindCurve:
                LCurve.LCurvePersistentSet(pPersistent);
                break;
            default:
                LTone.LTonePersistentSet(pKind, pPersistent);
                break;
        }
    }

    public void PTonePersistentApply(LWorkVideo pVideo)
    {
        foreach (LWorkVideoStep pStep in pVideo.LWorkVideoSteps)
        {
            PTonePersistentSet(pStep.LWorkStepKind, true);
        }
    }

    public LWorkVideo PTonePersistentRead()
    {
        var pSteps = new List<LWorkVideoStep>();
        foreach (LColorKind pKind in PInspectorVideoKinds)
        {
            if (PTonePersistentRead(pKind))
            {
                pSteps.Add(PToneStepRead(pKind));
            }
        }

        return new LWorkVideo(pSteps);
    }
}
