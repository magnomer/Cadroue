using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QNoise
{
    private static readonly string[] QNoiseLabelKeys =
    {
        "Inspector.Common.Amount",
        "Inspector.Noise.Floor",
        "Inspector.Noise.Smoothing",
        "Inspector.Noise.Adaptivity",
        "Inspector.Noise.Residual"
    };

    private static readonly string[] QNoiseUnits = { "dB", "dB", "gs", "0-1", "dB" };

    private static readonly string[] QNoiseFormats = { "0.#", "0.#", "0.#", "0.###", "0.#" };

    private static readonly double[] QNoiseLeast =
    {
        LGrainCatalog.LGrainReductionLeast,
        LGrainCatalog.LGrainFloorLeast,
        LGrainCatalog.LGrainSmoothLeast,
        LGrainCatalog.LGrainAdaptivityLeast,
        LGrainCatalog.LGrainFloorLeast
    };

    private static readonly double[] QNoiseMost =
    {
        LGrainCatalog.LGrainReductionMost,
        LGrainCatalog.LGrainFloorMost,
        LGrainCatalog.LGrainSmoothMost,
        LGrainCatalog.LGrainAdaptivityMost,
        LGrainCatalog.LGrainFloorMost
    };

    private static readonly int[] QNoiseOrder = { 0, 2, 1, 4, 3 };

    private static readonly LGrain[] QNoiseTypes = { LGrain.LGrainWhite, LGrain.LGrainVinyl, LGrain.LGrainShellac };

    private static readonly string[] QNoiseTypeKeys =
    {
        "Inspector.Noise.White", "Inspector.Noise.Vinyl", "Inspector.Noise.Shellac"
    };

    private static readonly IReadOnlyList<string> QNoiseTokens =
        LGrainCatalog.LGrainPresets.Select(lPreset => lPreset.LGrainToken).ToList();

    private LWorkNoiseStep qNoiseStep = QNoiseDefaultCreate(false);
    private string? qNoiseToken = "Medium";
    private bool qNoisePersistent;

    public event Action? QNoiseChanged;

    public LWorkNoiseStep QNoiseStep => qNoiseStep;

    public string? QNoiseToken => qNoiseToken;

    public bool QNoisePersistent => qNoisePersistent;

    public int QNoiseTypeIndex => Array.IndexOf(QNoiseTypes, qNoiseStep.LWorkNoiseType);

    public IReadOnlyList<string> QNoiseTypeNames => QNoiseTypeKeys.Select(LLocalization.LLocalizationTextRead).ToList();

    public IReadOnlyList<QInspectorRow> QNoiseRows => QNoiseOrder
        .Select(lIndex => QInspectorPlan.QInspectorRowCreate(
            lIndex,
            QNoiseLabelKeys[lIndex],
            QNoiseUnits[lIndex],
            QNoiseFormats[lIndex],
            QNoiseLeast[lIndex],
            QNoiseMost[lIndex]))
        .ToList();

    public double QNoiseValueRead(int lIndex) => lIndex switch
    {
        1 => qNoiseStep.LWorkNoiseFloor,
        2 => qNoiseStep.LWorkNoiseSmooth,
        3 => qNoiseStep.LWorkNoiseAdaptivity,
        4 => qNoiseStep.LWorkNoiseResidual,
        _ => qNoiseStep.LWorkNoiseReduction
    };

    public double QNoiseDefaultRead(int lIndex)
    {
        LGrainPreset? lPreset = QNoisePresetRead();
        return lIndex switch
        {
            1 => lPreset?.LGrainFloor ?? -50,
            2 => lPreset?.LGrainSmooth ?? 6,
            3 => lPreset?.LGrainAdaptivity ?? 0.5,
            4 => lPreset?.LGrainResidual ?? -38,
            _ => lPreset?.LGrainReduction ?? 12
        };
    }

    public QInspectorChoice QNoiseChoiceRead() =>
        QInspectorPlan.QInspectorChoiceRead(QNoiseTokens, QNoiseKeyRead, qNoiseToken, QNoiseMatchRead());

    public static string QNoiseKeyRead(string lToken) => lToken switch
    {
        "Light" => "Inspector.Noise.Light",
        "Medium" => "Inspector.Noise.Medium",
        "Strong" => "Inspector.Noise.Strong",
        "Dialogue" => "Inspector.Noise.Dialogue",
        "Vinyl" => "Inspector.Noise.Vinyl",
        "Shellac" => "Inspector.Noise.Shellac",
        _ => "Inspector.Common.Custom"
    };

    public LGrainPreset? QNoisePresetRead() =>
        qNoiseToken is { } lToken ? LGrainCatalog.LGrainRead(lToken) : null;

    public string? QNoiseMatchRead() => LGrainCatalog.LGrainMatch(
        qNoiseStep.LWorkNoiseReduction,
        qNoiseStep.LWorkNoiseFloor,
        qNoiseStep.LWorkNoiseSmooth,
        qNoiseStep.LWorkNoiseAdaptivity,
        qNoiseStep.LWorkNoiseResidual,
        qNoiseStep.LWorkNoiseType);

    public void QNoiseStepSet(LWorkAudioStep lStep)
    {
        LWorkNoiseStep lSource = lStep as LWorkNoiseStep ?? QNoiseDefaultCreate(lStep.LWorkStepActive);
        LWorkNoiseStep lNormal = QNoiseNormalize(
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
        if (qNoiseStep == lNormal && qNoiseToken == lToken)
        {
            return;
        }

        qNoiseStep = lNormal;
        qNoiseToken = lToken;
        QNoiseChanged?.Invoke();
    }

    public void QNoiseActiveSet(bool lActive) =>
        QNoiseStepApply(qNoiseStep with { LWorkStepActive = lActive });

    public void QNoiseValueSet(int lIndex, double lValue) =>
        QNoiseStepApply(QNoiseNormalize(
            qNoiseStep.LWorkStepActive,
            lIndex == 0 ? lValue : qNoiseStep.LWorkNoiseReduction,
            lIndex == 1 ? lValue : qNoiseStep.LWorkNoiseFloor,
            qNoiseStep.LWorkNoiseTrack,
            qNoiseStep.LWorkNoiseType,
            lIndex == 2 ? lValue : qNoiseStep.LWorkNoiseSmooth,
            lIndex == 3 ? lValue : qNoiseStep.LWorkNoiseAdaptivity,
            lIndex == 4 ? lValue : qNoiseStep.LWorkNoiseResidual));

    public void QNoiseTypeSet(LGrain lType) =>
        QNoiseStepApply(qNoiseStep with { LWorkNoiseType = lType });

    public void QNoiseTypeSelect(int lIndex)
    {
        if (lIndex >= 0 && lIndex < QNoiseTypes.Length)
        {
            QNoiseTypeSet(QNoiseTypes[lIndex]);
        }
    }

    public void QNoiseChoiceSelect(int lIndex)
    {
        string? lToken = QInspectorPlan.QInspectorChoiceResolve(QNoiseTokens, lIndex, qNoiseToken, QNoiseMatchRead());
        if (lToken is not null)
        {
            QNoisePresetSelect(lToken);
        }
    }

    public void QNoiseTrackSet(bool lTrack) =>
        QNoiseStepApply(qNoiseStep with { LWorkNoiseTrack = lTrack });

    public void QNoisePresetSelect(string lToken)
    {
        if (LGrainCatalog.LGrainRead(lToken) is not { } lPreset)
        {
            return;
        }

        LWorkNoiseStep lNormal = QNoiseNormalize(
            qNoiseStep.LWorkStepActive,
            lPreset.LGrainReduction,
            lPreset.LGrainFloor,
            qNoiseStep.LWorkNoiseTrack,
            lPreset.LGrainType,
            lPreset.LGrainSmooth,
            lPreset.LGrainAdaptivity,
            lPreset.LGrainResidual);
        if (qNoiseStep == lNormal && qNoiseToken == lToken)
        {
            return;
        }

        qNoiseStep = lNormal;
        qNoiseToken = lToken;
        QNoiseChanged?.Invoke();
    }

    public void QNoisePersistentSet(bool lPersistent)
    {
        if (qNoisePersistent == lPersistent)
        {
            return;
        }

        qNoisePersistent = lPersistent;
        QNoiseChanged?.Invoke();
    }

    private void QNoiseStepApply(LWorkNoiseStep lNormal)
    {
        if (qNoiseStep == lNormal)
        {
            return;
        }

        qNoiseStep = lNormal;
        QNoiseChanged?.Invoke();
    }

    private static LWorkNoiseStep QNoiseDefaultCreate(bool lActive) =>
        QNoiseNormalize(lActive, 12, -50, false, LGrain.LGrainWhite, 6, 0.5, -38);

    private static LWorkNoiseStep QNoiseNormalize(
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
