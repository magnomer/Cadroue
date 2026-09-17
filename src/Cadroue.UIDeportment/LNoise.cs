using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LNoise
{
    private LWorkNoiseStep lNoiseStep = LNoiseDefaultCreate(false);
    private string? lNoiseToken = "Medium";
    private bool lNoisePersistent;

    public event Action? LNoiseChange;

    public LWorkNoiseStep LNoiseStep => lNoiseStep;

    public string? LNoiseToken => lNoiseToken;

    public bool LNoisePersistent => lNoisePersistent;

    public LGrainPreset? LNoisePresetRead() =>
        lNoiseToken is { } lToken ? LGrainCatalog.LGrainRead(lToken) : null;

    public string? LNoiseMatchRead() => LGrainCatalog.LGrainMatch(
        lNoiseStep.LWorkNoiseReduction,
        lNoiseStep.LWorkNoiseFloor,
        lNoiseStep.LWorkNoiseSmooth,
        lNoiseStep.LWorkNoiseAdaptivity,
        lNoiseStep.LWorkNoiseResidual,
        lNoiseStep.LWorkNoiseType);

    public void LNoiseStepSet(LWorkAudioStep lStep)
    {
        LWorkNoiseStep lSource = lStep as LWorkNoiseStep ?? LNoiseDefaultCreate(lStep.LWorkStepActive);
        LWorkNoiseStep lNormal = LNoiseNormalize(
            lSource.LWorkStepActive,
            lSource.LWorkNoiseReduction,
            lSource.LWorkNoiseFloor,
            lSource.LWorkNoiseTrack,
            lSource.LWorkNoiseType,
            lSource.LWorkNoiseSmooth,
            lSource.LWorkNoiseAdaptivity,
            lSource.LWorkNoiseResidual);
        string? lToken = LGrainCatalog.LGrainMatch(
            lNormal.LWorkNoiseReduction,
            lNormal.LWorkNoiseFloor,
            lNormal.LWorkNoiseSmooth,
            lNormal.LWorkNoiseAdaptivity,
            lNormal.LWorkNoiseResidual,
            lNormal.LWorkNoiseType);
        if (lNoiseStep == lNormal && lNoiseToken == lToken)
        {
            return;
        }

        lNoiseStep = lNormal;
        lNoiseToken = lToken;
        LNoiseChange?.Invoke();
    }

    public void LNoiseActiveSet(bool lActive) =>
        LNoiseStepApply(lNoiseStep with { LWorkStepActive = lActive });

    public void LNoiseValueSet(int lIndex, double lValue) =>
        LNoiseStepApply(LNoiseNormalize(
            lNoiseStep.LWorkStepActive,
            lIndex == 0 ? lValue : lNoiseStep.LWorkNoiseReduction,
            lIndex == 1 ? lValue : lNoiseStep.LWorkNoiseFloor,
            lNoiseStep.LWorkNoiseTrack,
            lNoiseStep.LWorkNoiseType,
            lIndex == 2 ? lValue : lNoiseStep.LWorkNoiseSmooth,
            lIndex == 3 ? lValue : lNoiseStep.LWorkNoiseAdaptivity,
            lIndex == 4 ? lValue : lNoiseStep.LWorkNoiseResidual));

    public void LNoiseTypeSet(LGrain lType) =>
        LNoiseStepApply(lNoiseStep with { LWorkNoiseType = lType });

    public void LNoiseTrackSet(bool lTrack) =>
        LNoiseStepApply(lNoiseStep with { LWorkNoiseTrack = lTrack });

    public void LNoisePresetSelect(string lToken)
    {
        if (LGrainCatalog.LGrainRead(lToken) is not { } lPreset)
        {
            return;
        }

        LWorkNoiseStep lNormal = LNoiseNormalize(
            lNoiseStep.LWorkStepActive,
            lPreset.LGrainReduction,
            lPreset.LGrainFloor,
            lNoiseStep.LWorkNoiseTrack,
            lPreset.LGrainType,
            lPreset.LGrainSmooth,
            lPreset.LGrainAdaptivity,
            lPreset.LGrainResidual);
        if (lNoiseStep == lNormal && lNoiseToken == lToken)
        {
            return;
        }

        lNoiseStep = lNormal;
        lNoiseToken = lToken;
        LNoiseChange?.Invoke();
    }

    public void LNoisePersistentSet(bool lPersistent)
    {
        if (lNoisePersistent == lPersistent)
        {
            return;
        }

        lNoisePersistent = lPersistent;
        LNoiseChange?.Invoke();
    }

    private void LNoiseStepApply(LWorkNoiseStep lNormal)
    {
        if (lNoiseStep == lNormal)
        {
            return;
        }

        lNoiseStep = lNormal;
        LNoiseChange?.Invoke();
    }

    private static LWorkNoiseStep LNoiseDefaultCreate(bool lActive) =>
        LNoiseNormalize(lActive, 12, -50, false, LGrain.LGrainWhite, 6, 0.5, -38);

    private static LWorkNoiseStep LNoiseNormalize(
        bool lActive,
        double lReduction,
        double lFloor,
        bool lTrack,
        LGrain lType,
        double lSmooth,
        double lAdaptivity,
        double lResidual) =>
        (LWorkNoiseStep)LWorkAudioStep.LWorkNoiseCreate(
            lActive, lReduction, lFloor, lTrack, lType, lSmooth, lAdaptivity, lResidual);
}
