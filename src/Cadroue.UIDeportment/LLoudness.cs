using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LLoudness
{
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
