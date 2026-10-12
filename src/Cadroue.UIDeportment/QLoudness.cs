using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QLoudness
{
    private static readonly string[] QLoudnessModeKeys =
    {
        "Inspector.Normalize.Loudness", "Inspector.Normalize.Dynamic"
    };

    private static readonly string[] QLoudnessLabelKeys =
    {
        "Inspector.Normalize.Target", "Inspector.Normalize.Peak", "Inspector.Normalize.Range"
    };

    private static readonly string[] QLoudnessUnits = { "LUFS", "dBTP", "LU" };

    private static readonly double[] QLoudnessLeast =
    {
        LLevelingCatalog.LLevelingTargetLeast, LLevelingCatalog.LLevelingPeakLeast, LLevelingCatalog.LLevelingRangeLeast
    };

    private static readonly double[] QLoudnessMost =
    {
        LLevelingCatalog.LLevelingTargetMost, LLevelingCatalog.LLevelingPeakMost, LLevelingCatalog.LLevelingRangeMost
    };

    private LWorkNormalizeStep qLoudnessStep = QLoudnessDefaultCreate(false);
    private string? qLoudnessToken;
    private bool qLoudnessPersistent;

    public QLoudness()
    {
        qLoudnessToken = QLoudnessMatchRead();
    }

    public event Action? QLoudnessChanged;

    public LWorkNormalizeStep QLoudnessStep => qLoudnessStep;

    public string? QLoudnessToken => qLoudnessToken;

    public bool QLoudnessPersistent => qLoudnessPersistent;

    public bool QLoudnessDynamic => qLoudnessStep.LWorkNormalizeMode == LLeveling.LLevelingDynamic;

    public int QLoudnessModeIndex => QLoudnessDynamic ? 1 : 0;

    public IReadOnlyList<string> QLoudnessModeNames =>
        QLoudnessModeKeys.Select(LLocalization.LLocalizationTextRead).ToList();

    public IReadOnlyList<QInspectorRow> QLoudnessRowsRead(bool lDynamic) => lDynamic
        ? QDynamic.QDynamicRowsRead()
        : QLoudnessLabelKeys
            .Select((lKey, lIndex) => QInspectorPlan.QInspectorRowCreate(
                lIndex, lKey, QLoudnessUnits[lIndex], "0.###", QLoudnessLeast[lIndex], QLoudnessMost[lIndex]))
            .ToList();

    public double QLoudnessValueRead(bool lDynamic, int lIndex) => lDynamic
        ? QDynamic.QDynamicValueRead(qLoudnessStep, lIndex)
        : lIndex switch
        {
            1 => qLoudnessStep.LWorkNormalizePeak,
            2 => qLoudnessStep.LWorkNormalizeRange,
            _ => qLoudnessStep.LWorkNormalizeTarget
        };

    public double QLoudnessDefaultRead(bool lDynamic, int lIndex)
    {
        if (lDynamic)
        {
            return QDynamic.QDynamicDefaultRead(qLoudnessToken, lIndex);
        }

        LLevelingDefault lDefault = LLevelingCatalog.LLevelingDefaultRead();
        LLevelingLoudnessPreset? lPreset = QLoudnessPresetRead();
        return lIndex switch
        {
            1 => lPreset?.LLevelingPeak ?? lDefault.LLevelingPeak,
            2 => lPreset?.LLevelingRange ?? lDefault.LLevelingRange,
            _ => lPreset?.LLevelingTarget ?? lDefault.LLevelingTarget
        };
    }

    public QInspectorChoice QLoudnessChoiceRead(bool lDynamic) => lDynamic
        ? QInspectorPlan.QInspectorChoiceRead(
            LLevelingCatalog.LLevelingDynamicTokens,
            QDynamic.QDynamicKeyRead,
            qLoudnessToken,
            QDynamic.QDynamicMatch(qLoudnessStep))
        : QInspectorPlan.QInspectorChoiceRead(
            LLevelingCatalog.LLevelingLoudnessTokens,
            QLoudnessKeyRead,
            qLoudnessToken,
            LLevelingCatalog.LLevelingLoudnessMatch(
                qLoudnessStep.LWorkNormalizeTarget,
                qLoudnessStep.LWorkNormalizePeak,
                qLoudnessStep.LWorkNormalizeRange));

    public static string QLoudnessKeyRead(string lToken) => lToken switch
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

    public LLevelingLoudnessPreset? QLoudnessPresetRead() =>
        qLoudnessToken is { } lToken ? LLevelingCatalog.LLevelingLoudnessRead(lToken) : null;

    public string? QLoudnessMatchRead() => QLoudnessDynamic
        ? QDynamic.QDynamicMatch(qLoudnessStep)
        : LLevelingCatalog.LLevelingLoudnessMatch(
            qLoudnessStep.LWorkNormalizeTarget,
            qLoudnessStep.LWorkNormalizePeak,
            qLoudnessStep.LWorkNormalizeRange);

    public void QLoudnessStepSet(LWorkAudioStep lStep)
    {
        LWorkNormalizeStep lSource = lStep as LWorkNormalizeStep ?? QLoudnessDefaultCreate(lStep.LWorkStepActive);
        LWorkNormalizeStep lNormal = QLoudnessNormalize(lSource);
        QLoudnessStepApply(lNormal, QLoudnessTokenResolve(lNormal, null));
    }

    public void QLoudnessActiveSet(bool lActive) =>
        QLoudnessStepApply(qLoudnessStep with { LWorkStepActive = lActive }, qLoudnessToken);

    public void QLoudnessModeSet(LLeveling lMode)
    {
        LWorkNormalizeStep lNormal = qLoudnessStep with { LWorkNormalizeMode = lMode };
        QLoudnessStepApply(lNormal, QLoudnessTokenResolve(lNormal, null));
    }

    public void QLoudnessModeSelect(int lIndex)
    {
        if (lIndex >= 0 && lIndex < QLoudnessModeKeys.Length)
        {
            QLoudnessModeSet(lIndex == 1 ? LLeveling.LLevelingDynamic : LLeveling.LLevelingLoudness);
        }
    }

    public void QLoudnessValueSet(bool lDynamic, int lIndex, double lValue)
    {
        if (lDynamic)
        {
            QLoudnessDynamicSet(lIndex, lValue);
        }
        else
        {
            QLoudnessValueSet(lIndex, lValue);
        }
    }

    public void QLoudnessChoiceSelect(bool lDynamic, int lIndex)
    {
        if (lDynamic != QLoudnessDynamic)
        {
            return;
        }

        string? lToken = QInspectorPlan.QInspectorChoiceResolve(
            lDynamic ? LLevelingCatalog.LLevelingDynamicTokens : LLevelingCatalog.LLevelingLoudnessTokens,
            lIndex,
            qLoudnessToken,
            QLoudnessMatchRead());
        if (lToken is not null)
        {
            QLoudnessPresetSelect(lToken);
        }
    }

    public void QLoudnessValueSet(int lIndex, double lValue) =>
        QLoudnessStepApply(
            QLoudnessNormalize(qLoudnessStep with
            {
                LWorkNormalizeTarget = lIndex == 0 ? lValue : qLoudnessStep.LWorkNormalizeTarget,
                LWorkNormalizePeak = lIndex == 1 ? lValue : qLoudnessStep.LWorkNormalizePeak,
                LWorkNormalizeRange = lIndex == 2 ? lValue : qLoudnessStep.LWorkNormalizeRange
            }),
            qLoudnessToken);

    public void QLoudnessDynamicSet(int lIndex, double lValue) =>
        QLoudnessStepApply(
            QLoudnessNormalize(QDynamic.QDynamicValueSet(qLoudnessStep, lIndex, lValue)),
            qLoudnessToken);

    public void QLoudnessTwopassSet(bool lTwoPass) =>
        QLoudnessStepApply(qLoudnessStep with { LWorkTwoPass = lTwoPass }, qLoudnessToken);

    public void QLoudnessPresetSelect(string lToken)
    {
        LWorkNormalizeStep? lNormal = QLoudnessDynamic
            ? QDynamic.QDynamicPresetApply(qLoudnessStep, lToken)
            : LLevelingCatalog.LLevelingLoudnessRead(lToken) is { } lPreset
                ? qLoudnessStep with
                {
                    LWorkNormalizeTarget = lPreset.LLevelingTarget,
                    LWorkNormalizePeak = lPreset.LLevelingPeak,
                    LWorkNormalizeRange = lPreset.LLevelingRange
                }
                : null;
        if (lNormal is not null)
        {
            QLoudnessStepApply(QLoudnessNormalize(lNormal), lToken);
        }
    }

    public void QLoudnessPersistentSet(bool lPersistent)
    {
        if (qLoudnessPersistent == lPersistent)
        {
            return;
        }

        qLoudnessPersistent = lPersistent;
        QLoudnessChanged?.Invoke();
    }

    private void QLoudnessStepApply(LWorkNormalizeStep lNormal, string? lToken)
    {
        if (qLoudnessStep == lNormal && qLoudnessToken == lToken)
        {
            return;
        }

        qLoudnessStep = lNormal;
        qLoudnessToken = lToken;
        QLoudnessChanged?.Invoke();
    }

    private static string? QLoudnessTokenResolve(LWorkNormalizeStep lStep, string? lFallback) =>
        (lStep.LWorkNormalizeMode == LLeveling.LLevelingDynamic
            ? QDynamic.QDynamicMatch(lStep)
            : LLevelingCatalog.LLevelingLoudnessMatch(
                lStep.LWorkNormalizeTarget, lStep.LWorkNormalizePeak, lStep.LWorkNormalizeRange))
        ?? lFallback;

    private static LWorkNormalizeStep QLoudnessDefaultCreate(bool lActive)
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

    private static LWorkNormalizeStep QLoudnessNormalize(LWorkNormalizeStep lStep) =>
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
