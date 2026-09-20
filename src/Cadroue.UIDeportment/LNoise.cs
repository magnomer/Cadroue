using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LNoise
{
    private static readonly string[] LNoiseLabelKeys =
    {
        "Inspector.Common.Amount",
        "Inspector.Noise.Floor",
        "Inspector.Noise.Smoothing",
        "Inspector.Noise.Adaptivity",
        "Inspector.Noise.Residual"
    };

    private static readonly string[] LNoiseUnits = { "dB", "dB", "gs", "0-1", "dB" };

    private static readonly string[] LNoiseFormats = { "0.#", "0.#", "0.#", "0.###", "0.#" };

    private static readonly double[] LNoiseLeast =
    {
        LGrainCatalog.LGrainReductionLeast,
        LGrainCatalog.LGrainFloorLeast,
        LGrainCatalog.LGrainSmoothLeast,
        LGrainCatalog.LGrainAdaptivityLeast,
        LGrainCatalog.LGrainFloorLeast
    };

    private static readonly double[] LNoiseMost =
    {
        LGrainCatalog.LGrainReductionMost,
        LGrainCatalog.LGrainFloorMost,
        LGrainCatalog.LGrainSmoothMost,
        LGrainCatalog.LGrainAdaptivityMost,
        LGrainCatalog.LGrainFloorMost
    };

    private static readonly int[] LNoiseOrder = { 0, 2, 1, 4, 3 };

    private static readonly LGrain[] LNoiseTypes = { LGrain.LGrainWhite, LGrain.LGrainVinyl, LGrain.LGrainShellac };

    private static readonly string[] LNoiseTypeKeys =
    {
        "Inspector.Noise.White", "Inspector.Noise.Vinyl", "Inspector.Noise.Shellac"
    };

    private static readonly IReadOnlyList<string> LNoiseTokens =
        LGrainCatalog.LGrainPresets.Select(lPreset => lPreset.LGrainToken).ToList();

    private LWorkNoiseStep lNoiseStep = LNoiseDefaultCreate(false);
    private string? lNoiseToken = "Medium";
    private bool lNoisePersistent;

    public event Action? LNoiseChange;

    public LWorkNoiseStep LNoiseStep => lNoiseStep;

    public string? LNoiseToken => lNoiseToken;

    public bool LNoisePersistent => lNoisePersistent;

    public int LNoiseTypeIndex => Array.IndexOf(LNoiseTypes, lNoiseStep.LWorkNoiseType);

    public IReadOnlyList<string> LNoiseTypeNames => LNoiseTypeKeys.Select(LLocalization.LLocalizationTextRead).ToList();

    public IReadOnlyList<LInspectorRow> LNoiseRows => LNoiseOrder
        .Select(lIndex => LInspectorPlan.LInspectorRowCreate(
            lIndex,
            LNoiseLabelKeys[lIndex],
            LNoiseUnits[lIndex],
            LNoiseFormats[lIndex],
            LNoiseLeast[lIndex],
            LNoiseMost[lIndex]))
        .ToList();

    public double LNoiseValueRead(int lIndex) => lIndex switch
    {
        1 => lNoiseStep.LWorkNoiseFloor,
        2 => lNoiseStep.LWorkNoiseSmooth,
        3 => lNoiseStep.LWorkNoiseAdaptivity,
        4 => lNoiseStep.LWorkNoiseResidual,
        _ => lNoiseStep.LWorkNoiseReduction
    };

    public double LNoiseDefaultRead(int lIndex)
    {
        LGrainPreset? lPreset = LNoisePresetRead();
        return lIndex switch
        {
            1 => lPreset?.LGrainFloor ?? -50,
            2 => lPreset?.LGrainSmooth ?? 6,
            3 => lPreset?.LGrainAdaptivity ?? 0.5,
            4 => lPreset?.LGrainResidual ?? -38,
            _ => lPreset?.LGrainReduction ?? 12
        };
    }

    public LInspectorChoice LNoiseChoiceRead() =>
        LInspectorPlan.LInspectorChoiceRead(LNoiseTokens, LNoiseKeyRead, lNoiseToken, LNoiseMatchRead());

    public static string LNoiseKeyRead(string lToken) => lToken switch
    {
        "Light" => "Inspector.Noise.Light",
        "Medium" => "Inspector.Noise.Medium",
        "Strong" => "Inspector.Noise.Strong",
        "Dialogue" => "Inspector.Noise.Dialogue",
        "Vinyl" => "Inspector.Noise.Vinyl",
        "Shellac" => "Inspector.Noise.Shellac",
        _ => "Inspector.Common.Custom"
    };

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

    public void LNoiseTypeSelect(int lIndex)
    {
        if (lIndex >= 0 && lIndex < LNoiseTypes.Length)
        {
            LNoiseTypeSet(LNoiseTypes[lIndex]);
        }
    }

    public void LNoiseChoiceSelect(int lIndex)
    {
        string? lToken = LInspectorPlan.LInspectorChoiceResolve(LNoiseTokens, lIndex, lNoiseToken, LNoiseMatchRead());
        if (lToken is not null)
        {
            LNoisePresetSelect(lToken);
        }
    }

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
