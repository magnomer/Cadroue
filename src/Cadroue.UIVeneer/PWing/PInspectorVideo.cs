using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    public event Action? PInspectorVideoChange;

    public LTone LTone => LInspector.LInspectorTone;

    public LGamma LGamma => LInspector.LInspectorGamma;

    public LExposure LExposure => LInspector.LInspectorExposure;

    public LCurve LCurve => LInspector.LInspectorCurve;

    public LWhitebalance LWhitebalance => LInspector.LInspectorWhitebalance;

    private void PInspectorVideoAttach()
    {
        LTone.LToneChange += PToneUpdate;
        LTone.LToneChange += PInspectorVideoRaise;
        LGamma.LGammaChange += PGammaUpdate;
        LGamma.LGammaChange += PInspectorVideoRaise;
        LExposure.LExposureChange += PExposureUpdate;
        LExposure.LExposureChange += PInspectorVideoRaise;
        LCurve.LCurveChange += PCurveUpdate;
        LCurve.LCurveChange += PInspectorVideoRaise;
        LWhitebalance.LWhitebalanceChange += PWhitebalanceUpdate;
        LWhitebalance.LWhitebalanceChange += PInspectorVideoRaise;
        PToneUpdate();
        PGammaUpdate();
        PExposureUpdate();
        PCurveUpdate();
        PWhitebalanceUpdate();
    }

    private void PInspectorVideoRaise() => PInspectorVideoChange?.Invoke();

    public LWorkVideoStep PToneStepRead(LColorKind pStepKind) => LInspector.LInspectorStepRead(pStepKind);

    public void PTonePlanApply(LWorkVideo pVideo)
    {
        LInspector.LInspectorVideoApply(pVideo);
        PInspectorVideoRaise();
    }

    public void PToneCapabilitySet(bool pCapable) => LTone.LToneCapableSet(pCapable);

    public void PGammaCapabilitySet(bool pGammaCapable, bool pGammaPreview) =>
        LGamma.LGammaCapableSet(pGammaCapable, pGammaPreview);

    public void PExposureCapabilitySet(bool pExposureCapable, bool pExposurePreview) =>
        LExposure.LExposureCapableSet(pExposureCapable, pExposurePreview);

    public void PCurveCapabilitySet(bool pCurveCapable, bool pCurvePreview) =>
        LCurve.LCurveCapableSet(pCurveCapable, pCurvePreview);

    public void PWhitebalanceCapabilitySet(bool pWhitebalanceCapable, bool pWhitebalancePreview) =>
        LWhitebalance.LWhitebalanceCapableSet(pWhitebalanceCapable, pWhitebalancePreview);
}
