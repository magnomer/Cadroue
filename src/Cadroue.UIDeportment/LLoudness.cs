using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LLoudness
{
    private static readonly string[] LLoudnessModeKeys =
    {
        "Inspector.Normalize.Loudness", "Inspector.Normalize.Dynamic"
    };

    private static readonly string[] LLoudnessLabelKeys =
    {
        "Inspector.Normalize.Target", "Inspector.Normalize.Peak", "Inspector.Normalize.Range"
    };

    private static readonly string[] LLoudnessUnits = { "LUFS", "dBTP", "LU" };

    private static readonly double[] LLoudnessLeast =
    {
        LLevelingCatalog.LLevelingTargetLeast, LLevelingCatalog.LLevelingPeakLeast, LLevelingCatalog.LLevelingRangeLeast
    };

    private static readonly double[] LLoudnessMost =
    {
        LLevelingCatalog.LLevelingTargetMost, LLevelingCatalog.LLevelingPeakMost, LLevelingCatalog.LLevelingRangeMost
    };

    private LWorkNormalizeStep lLoudnessStep = LLoudnessDefaultCreate(false);
    private string? lLoudnessToken;
    private bool lLoudnessPersistent;

    public LLoudness()
    {
        lLoudnessToken = LLoudnessMatchRead();
    }

    public event Action? LLoudnessChange;

    public LWorkNormalizeStep LLoudnessStep => lLoudnessStep;

    public string? LLoudnessToken => lLoudnessToken;

    public bool LLoudnessPersistent => lLoudnessPersistent;

    public bool LLoudnessDynamic => lLoudnessStep.LWorkNormalizeMode == LLeveling.LLevelingDynamic;

    public int LLoudnessModeIndex => LLoudnessDynamic ? 1 : 0;

    public IReadOnlyList<string> LLoudnessModeNames =>
        LLoudnessModeKeys.Select(LLocalization.LLocalizationTextRead).ToList();

    public IReadOnlyList<LInspectorRow> LLoudnessRowsRead(bool lDynamic) => lDynamic
        ? LDynamic.LDynamicRowsRead()
        : LLoudnessLabelKeys
            .Select((lKey, lIndex) => LInspectorPlan.LInspectorRowCreate(
                lIndex, lKey, LLoudnessUnits[lIndex], "0.###", LLoudnessLeast[lIndex], LLoudnessMost[lIndex]))
            .ToList();

    public double LLoudnessValueRead(bool lDynamic, int lIndex) => lDynamic
        ? LDynamic.LDynamicValueRead(lLoudnessStep, lIndex)
        : lIndex switch
        {
            1 => lLoudnessStep.LWorkNormalizePeak,
            2 => lLoudnessStep.LWorkNormalizeRange,
            _ => lLoudnessStep.LWorkNormalizeTarget
        };

    public double LLoudnessDefaultRead(bool lDynamic, int lIndex)
    {
        if (lDynamic)
        {
            return LDynamic.LDynamicDefaultRead(lLoudnessToken, lIndex);
        }

        LLevelingDefault lDefault = LLevelingCatalog.LLevelingDefaultRead();
        LLevelingLoudnessPreset? lPreset = LLoudnessPresetRead();
        return lIndex switch
        {
            1 => lPreset?.LLevelingPeak ?? lDefault.LLevelingPeak,
            2 => lPreset?.LLevelingRange ?? lDefault.LLevelingRange,
            _ => lPreset?.LLevelingTarget ?? lDefault.LLevelingTarget
        };
    }

    public LInspectorChoice LLoudnessChoiceRead(bool lDynamic) => lDynamic
        ? LInspectorPlan.LInspectorChoiceRead(
            LLevelingCatalog.LLevelingDynamicTokens,
            LDynamic.LDynamicKeyRead,
            lLoudnessToken,
            LDynamic.LDynamicMatch(lLoudnessStep))
        : LInspectorPlan.LInspectorChoiceRead(
            LLevelingCatalog.LLevelingLoudnessTokens,
            LLoudnessKeyRead,
            lLoudnessToken,
            LLevelingCatalog.LLevelingLoudnessMatch(
                lLoudnessStep.LWorkNormalizeTarget,
                lLoudnessStep.LWorkNormalizePeak,
                lLoudnessStep.LWorkNormalizeRange));

    public static string LLoudnessKeyRead(string lToken) => lToken switch
    {
        "Loud" => "Inspector.Normalize.Loud",
        "Streaming" => "Inspector.Normalize.Streaming",
        "Podcast" => "Inspector.Normalize.Podcast",
        "Dialogue" => "Inspector.Normalize.Dialogue",
        "Audiobook" => "Inspector.Normalize.Audiobook",
        "Broadcast" => "Inspector.Normalize.Broadcast",
        "TV" => "Inspector.Normalize.TV",
        "Film" => "Inspector.Normalize.Film",
        _ => "Inspector.Common.Custom"
    };

    public LLevelingLoudnessPreset? LLoudnessPresetRead() =>
        lLoudnessToken is { } lToken ? LLevelingCatalog.LLevelingLoudnessRead(lToken) : null;

    public string? LLoudnessMatchRead() => LLoudnessDynamic
        ? LDynamic.LDynamicMatch(lLoudnessStep)
        : LLevelingCatalog.LLevelingLoudnessMatch(
            lLoudnessStep.LWorkNormalizeTarget,
            lLoudnessStep.LWorkNormalizePeak,
            lLoudnessStep.LWorkNormalizeRange);

    public void LLoudnessStepSet(LWorkAudioStep lStep)
    {
        LWorkNormalizeStep lSource = lStep as LWorkNormalizeStep ?? LLoudnessDefaultCreate(lStep.LWorkStepActive);
        LWorkNormalizeStep lNormal = LLoudnessNormalize(lSource);
        LLoudnessStepApply(lNormal, LLoudnessTokenResolve(lNormal, null));
    }

    public void LLoudnessActiveSet(bool lActive) =>
        LLoudnessStepApply(lLoudnessStep with { LWorkStepActive = lActive }, lLoudnessToken);

    public void LLoudnessModeSet(LLeveling lMode)
    {
        LWorkNormalizeStep lNormal = lLoudnessStep with { LWorkNormalizeMode = lMode };
        LLoudnessStepApply(lNormal, LLoudnessTokenResolve(lNormal, null));
    }

    public void LLoudnessModeSelect(int lIndex)
    {
        if (lIndex >= 0 && lIndex < LLoudnessModeKeys.Length)
        {
            LLoudnessModeSet(lIndex == 1 ? LLeveling.LLevelingDynamic : LLeveling.LLevelingLoudness);
        }
    }

    public void LLoudnessValueSet(bool lDynamic, int lIndex, double lValue)
    {
        if (lDynamic)
        {
            LLoudnessDynamicSet(lIndex, lValue);
        }
        else
        {
            LLoudnessValueSet(lIndex, lValue);
        }
    }

    public void LLoudnessChoiceSelect(bool lDynamic, int lIndex)
    {
        if (lDynamic != LLoudnessDynamic)
        {
            return;
        }

        string? lToken = LInspectorPlan.LInspectorChoiceResolve(
            lDynamic ? LLevelingCatalog.LLevelingDynamicTokens : LLevelingCatalog.LLevelingLoudnessTokens,
            lIndex,
            lLoudnessToken,
            LLoudnessMatchRead());
        if (lToken is not null)
        {
            LLoudnessPresetSelect(lToken);
        }
    }

    public void LLoudnessValueSet(int lIndex, double lValue) =>
        LLoudnessStepApply(
            LLoudnessNormalize(lLoudnessStep with
            {
                LWorkNormalizeTarget = lIndex == 0 ? lValue : lLoudnessStep.LWorkNormalizeTarget,
                LWorkNormalizePeak = lIndex == 1 ? lValue : lLoudnessStep.LWorkNormalizePeak,
                LWorkNormalizeRange = lIndex == 2 ? lValue : lLoudnessStep.LWorkNormalizeRange
            }),
            lLoudnessToken);

    public void LLoudnessDynamicSet(int lIndex, double lValue) =>
        LLoudnessStepApply(
            LLoudnessNormalize(LDynamic.LDynamicValueSet(lLoudnessStep, lIndex, lValue)),
            lLoudnessToken);

    public void LLoudnessTwopassSet(bool lTwoPass) =>
        LLoudnessStepApply(lLoudnessStep with { LWorkTwoPass = lTwoPass }, lLoudnessToken);

    public void LLoudnessPresetSelect(string lToken)
    {
        LWorkNormalizeStep? lNormal = LLoudnessDynamic
            ? LDynamic.LDynamicPresetApply(lLoudnessStep, lToken)
            : LLevelingCatalog.LLevelingLoudnessRead(lToken) is { } lPreset
                ? lLoudnessStep with
                {
                    LWorkNormalizeTarget = lPreset.LLevelingTarget,
                    LWorkNormalizePeak = lPreset.LLevelingPeak,
                    LWorkNormalizeRange = lPreset.LLevelingRange
                }
                : null;
        if (lNormal is not null)
        {
            LLoudnessStepApply(LLoudnessNormalize(lNormal), lToken);
        }
    }

    public void LLoudnessPersistentSet(bool lPersistent)
    {
        if (lLoudnessPersistent == lPersistent)
        {
            return;
        }

        lLoudnessPersistent = lPersistent;
        LLoudnessChange?.Invoke();
    }

    private void LLoudnessStepApply(LWorkNormalizeStep lNormal, string? lToken)
    {
        if (lLoudnessStep == lNormal && lLoudnessToken == lToken)
        {
            return;
        }

        lLoudnessStep = lNormal;
        lLoudnessToken = lToken;
        LLoudnessChange?.Invoke();
    }

    private static string? LLoudnessTokenResolve(LWorkNormalizeStep lStep, string? lFallback) =>
        (lStep.LWorkNormalizeMode == LLeveling.LLevelingDynamic
            ? LDynamic.LDynamicMatch(lStep)
            : LLevelingCatalog.LLevelingLoudnessMatch(
                lStep.LWorkNormalizeTarget, lStep.LWorkNormalizePeak, lStep.LWorkNormalizeRange))
        ?? lFallback;

    private static LWorkNormalizeStep LLoudnessDefaultCreate(bool lActive)
    {
        LLevelingDefault lDefault = LLevelingCatalog.LLevelingDefaultRead();
        return (LWorkNormalizeStep)LWorkAudioStep.LWorkNormalizeCreate(
            lActive,
            LLeveling.LLevelingLoudness,
            lDefault.LLevelingTarget,
            lDefault.LLevelingPeak,
            lDefault.LLevelingRange,
            lDefault.LLevelingTwoPass,
            lDefault.LLevelingFrame,
            lDefault.LLevelingGauss,
            lDefault.LLevelingMaxGain,
            lDefault.LLevelingCompress);
    }

    private static LWorkNormalizeStep LLoudnessNormalize(LWorkNormalizeStep lStep) =>
        (LWorkNormalizeStep)LWorkAudioStep.LWorkNormalizeCreate(
            lStep.LWorkStepActive,
            lStep.LWorkNormalizeMode,
            lStep.LWorkNormalizeTarget,
            lStep.LWorkNormalizePeak,
            lStep.LWorkNormalizeRange,
            lStep.LWorkTwoPass,
            lStep.LWorkNormalizeFrame,
            lStep.LWorkNormalizeGauss,
            lStep.LWorkNormalizeGain,
            lStep.LWorkNormalizeCompress);
}
