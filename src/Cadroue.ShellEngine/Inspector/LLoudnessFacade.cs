using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The leveling rules from the leveling catalogue, on plain values in slot order: target, peak, range, frame,
// smoothness, maximum gain, compression. Every value is held inside its range; the loudness mode owns slots 0 to 2
// and the dynamic mode slots 3 to 6.
public sealed class LLoudnessFacade : LLoudnessPort
{
    private const int LLoudnessDynamicFirst = 3;

    internal LLoudnessFacade()
    {
    }

    public IReadOnlyList<double> LLoudnessLeastsRead(bool lDynamic) => Array.AsReadOnly(lDynamic
        ? new[]
        {
            LLevelingCatalog.LLevelingFrameLeast,
            LLevelingCatalog.LLevelingGaussLeast,
            LLevelingCatalog.LLevelingGainLeast,
            LLevelingCatalog.LLevelingCompressLeast
        }
        : new[]
        {
            LLevelingCatalog.LLevelingTargetLeast,
            LLevelingCatalog.LLevelingPeakLeast,
            LLevelingCatalog.LLevelingRangeLeast
        });

    public IReadOnlyList<double> LLoudnessMostsRead(bool lDynamic) => Array.AsReadOnly(lDynamic
        ? new[]
        {
            LLevelingCatalog.LLevelingFrameMost,
            LLevelingCatalog.LLevelingGaussMost,
            LLevelingCatalog.LLevelingGainMost,
            LLevelingCatalog.LLevelingCompressMost
        }
        : new[]
        {
            LLevelingCatalog.LLevelingTargetMost,
            LLevelingCatalog.LLevelingPeakMost,
            LLevelingCatalog.LLevelingRangeMost
        });

    public IReadOnlyList<string> LLoudnessTokensRead(bool lDynamic) =>
        lDynamic ? LLevelingCatalog.LLevelingDynamicTokens : LLevelingCatalog.LLevelingLoudnessTokens;

    // The default step switched off, with the preset its mode matches.
    public (bool LLoudnessDynamic, IReadOnlyList<double> LLoudnessValues, bool LLoudnessTwoPass,
        string? LLoudnessPreset) LLoudnessOriginRead()
    {
        LWorkNormalizeStep lStep = LLoudnessDefaultCreate(false);
        bool lDynamic = lStep.LWorkNormalizeMode == LLeveling.LLevelingDynamic;
        IReadOnlyList<double> lValues = LLoudnessValuesRead(lStep);
        return (lDynamic, lValues, lStep.LWorkTwoPass, LLoudnessMatchFind(lDynamic, lValues));
    }

    // The mode's values a reset returns to: the token's preset of that mode, or the catalogue default without one.
    public IReadOnlyList<double> LLoudnessDefaultRead(bool lDynamic, string? lToken)
    {
        LLevelingDefault lDefault = LLevelingCatalog.LLevelingDefaultRead();
        if (lDynamic)
        {
            LLevelingDynamicPreset? lDynamicPreset =
                lToken is { } lName ? LLevelingCatalog.LLevelingDynamicRead(lName) : null;
            return Array.AsReadOnly(new[]
            {
                lDynamicPreset?.LLevelingFrame ?? lDefault.LLevelingFrame,
                lDynamicPreset?.LLevelingGauss ?? lDefault.LLevelingGauss,
                lDynamicPreset?.LLevelingMaxGain ?? lDefault.LLevelingMaxGain,
                lDynamicPreset?.LLevelingCompress ?? lDefault.LLevelingCompress
            });
        }

        LLevelingLoudnessPreset? lPreset = lToken is { } lKnown ? LLevelingCatalog.LLevelingLoudnessRead(lKnown) : null;
        return Array.AsReadOnly(new[]
        {
            lPreset?.LLevelingTarget ?? lDefault.LLevelingTarget,
            lPreset?.LLevelingPeak ?? lDefault.LLevelingPeak,
            lPreset?.LLevelingRange ?? lDefault.LLevelingRange
        });
    }

    public IReadOnlyList<double> LLoudnessNormalize(IReadOnlyList<double> lValues)
    {
        ArgumentNullException.ThrowIfNull(lValues);
        return LLoudnessValuesRead((LWorkNormalizeStep)LLoudnessStepCreate(false, false, lValues, false));
    }

    // The mode's preset laid over the held values, every value held inside its range; null for an unknown preset.
    public IReadOnlyList<double>? LLoudnessPresetRead(bool lDynamic, string lToken, IReadOnlyList<double> lValues)
    {
        ArgumentNullException.ThrowIfNull(lValues);
        double[] lLaid = lValues.ToArray();
        if (lDynamic)
        {
            if (LLevelingCatalog.LLevelingDynamicRead(lToken) is not { } lDynamicPreset)
            {
                return null;
            }

            lLaid[LLoudnessDynamicFirst] = lDynamicPreset.LLevelingFrame;
            lLaid[LLoudnessDynamicFirst + 1] = lDynamicPreset.LLevelingGauss;
            lLaid[LLoudnessDynamicFirst + 2] = lDynamicPreset.LLevelingMaxGain;
            lLaid[LLoudnessDynamicFirst + 3] = lDynamicPreset.LLevelingCompress;
            return LLoudnessNormalize(lLaid);
        }

        if (LLevelingCatalog.LLevelingLoudnessRead(lToken) is not { } lPreset)
        {
            return null;
        }

        lLaid[0] = lPreset.LLevelingTarget;
        lLaid[1] = lPreset.LLevelingPeak;
        lLaid[2] = lPreset.LLevelingRange;
        return LLoudnessNormalize(lLaid);
    }

    // The preset of the mode the mode's values match, or null.
    public string? LLoudnessMatchFind(bool lDynamic, IReadOnlyList<double> lValues)
    {
        ArgumentNullException.ThrowIfNull(lValues);
        return lDynamic
            ? LLevelingCatalog.LLevelingDynamicMatch(
                lValues[LLoudnessDynamicFirst],
                lValues[LLoudnessDynamicFirst + 1],
                lValues[LLoudnessDynamicFirst + 2],
                lValues[LLoudnessDynamicFirst + 3])
            : LLevelingCatalog.LLevelingLoudnessMatch(lValues[0], lValues[1], lValues[2]);
    }

    // The leveling step for the gate's switch, mode, values and two-pass; every value is held inside its range.
    internal static LWorkAudioStep LLoudnessStepCreate(
        bool lActive, bool lDynamic, IReadOnlyList<double> lValues, bool lTwoPass)
    {
        ArgumentNullException.ThrowIfNull(lValues);
        return LWorkAudioStep.LWorkNormalizeCreate(
            lActive,
            lDynamic ? LLeveling.LLevelingDynamic : LLeveling.LLevelingLoudness,
            lValues[0],
            lValues[1],
            lValues[2],
            lTwoPass,
            lValues[LLoudnessDynamicFirst],
            lValues[LLoudnessDynamicFirst + 1],
            lValues[LLoudnessDynamicFirst + 2],
            lValues[LLoudnessDynamicFirst + 3]);
    }

    // The catalogue default step in the loudness mode, with the given switch.
    internal static LWorkNormalizeStep LLoudnessDefaultCreate(bool lActive)
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

    internal static IReadOnlyList<double> LLoudnessValuesRead(LWorkNormalizeStep lStep) => Array.AsReadOnly(new[]
    {
        lStep.LWorkNormalizeTarget,
        lStep.LWorkNormalizePeak,
        lStep.LWorkNormalizeRange,
        lStep.LWorkNormalizeFrame,
        lStep.LWorkNormalizeGauss,
        lStep.LWorkNormalizeGain,
        lStep.LWorkNormalizeCompress
    });
}
