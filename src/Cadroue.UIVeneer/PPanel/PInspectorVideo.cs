using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    public event Action? PInspectorVideoChange;

    public LTone LTone { get; } = new();

    public LGamma LGamma { get; } = new();

    public LExposure LExposure { get; } = new();

    public LCurve LCurve { get; } = new();

    public LWhitebalance LWhitebalance { get; } = new();

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

    public LWorkVideoStep PToneStepRead(LColorKind pStepKind) => pStepKind switch
    {
        LColorKind.LColorKindGamma => LGamma.LGammaStep,
        LColorKind.LColorKindWhitebalance => LWhitebalance.LWhitebalanceStep,
        LColorKind.LColorKindExposure => LExposure.LExposureStep,
        LColorKind.LColorKindCurve => LCurve.LCurveStepRead(),
        _ => LTone.LToneStepRead(pStepKind)
    };

    public void PTonePlanApply(LWorkVideo pVideo)
    {
        LTone.LToneStepSet(PToneStepFind(pVideo, LColorKind.LColorKindBrightness)
            ?? LWorkVideoStep.LWorkBrightnessCreate(false, 0));
        LTone.LToneStepSet(PToneStepFind(pVideo, LColorKind.LColorKindContrast)
            ?? LWorkVideoStep.LWorkContrastCreate(false, 100));
        LTone.LToneStepSet(PToneStepFind(pVideo, LColorKind.LColorKindSaturation)
            ?? LWorkVideoStep.LWorkSaturationCreate(false, 100));
        LGamma.LGammaStepSet(PToneStepFind(pVideo, LColorKind.LColorKindGamma)
            ?? LWorkVideoStep.LWorkGammaCreate(false, 0));
        LWhitebalance.LWhitebalanceStepSet(PToneStepFind(pVideo, LColorKind.LColorKindWhitebalance)
            ?? LWorkVideoStep.LWorkWhitebalanceCreate(false));
        LExposure.LExposureStepSet(PToneStepFind(pVideo, LColorKind.LColorKindExposure)
            ?? LWorkVideoStep.LWorkExposureCreate(false, 0));
        LCurve.LCurveStepSet(PToneStepFind(pVideo, LColorKind.LColorKindCurve)
            ?? LWorkVideoStep.LWorkCurveCreate(false));
        PInspectorVideoRaise();
    }

    private static LWorkVideoStep? PToneStepFind(LWorkVideo pVideo, LColorKind pKind) =>
        pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == pKind);

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
