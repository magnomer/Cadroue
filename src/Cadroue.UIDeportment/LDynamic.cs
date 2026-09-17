using Cadroue.Core;

namespace Cadroue.UIDeportment;

public static class LDynamic
{
    public static string? LDynamicMatch(LWorkNormalizeStep lStep) => LLevelingCatalog.LLevelingDynamicMatch(
        lStep.LWorkNormalizeFrame,
        lStep.LWorkNormalizeGauss,
        lStep.LWorkNormalizeGain,
        lStep.LWorkNormalizeCompress);

    public static LLevelingDynamicPreset? LDynamicPresetRead(string? lToken) =>
        lToken is { } lName ? LLevelingCatalog.LLevelingDynamicRead(lName) : null;

    public static LWorkNormalizeStep? LDynamicPresetApply(LWorkNormalizeStep lStep, string lToken) =>
        LLevelingCatalog.LLevelingDynamicRead(lToken) is { } lPreset
            ? lStep with
            {
                LWorkNormalizeFrame = lPreset.LLevelingFrame,
                LWorkNormalizeGauss = lPreset.LLevelingGauss,
                LWorkNormalizeGain = lPreset.LLevelingMaxGain,
                LWorkNormalizeCompress = lPreset.LLevelingCompress
            }
            : null;

    public static LWorkNormalizeStep LDynamicValueSet(LWorkNormalizeStep lStep, int lIndex, double lValue) =>
        lStep with
        {
            LWorkNormalizeFrame = lIndex == 0 ? lValue : lStep.LWorkNormalizeFrame,
            LWorkNormalizeGauss = lIndex == 1 ? lValue : lStep.LWorkNormalizeGauss,
            LWorkNormalizeGain = lIndex == 2 ? lValue : lStep.LWorkNormalizeGain,
            LWorkNormalizeCompress = lIndex == 3 ? lValue : lStep.LWorkNormalizeCompress
        };
}
