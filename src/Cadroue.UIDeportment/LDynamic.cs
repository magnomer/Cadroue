using Cadroue.Core;

namespace Cadroue.UIDeportment;

public static class LDynamic
{
    public static readonly IReadOnlyList<string> LDynamicUnits = new[] { "ms", "g", "×", "s" };

    private static readonly string[] LDynamicLabelKeys =
    {
        "Inspector.Dynamic.Frame",
        "Inspector.Dynamic.Smoothness",
        "Inspector.Dynamic.MaxGain",
        "Inspector.Dynamic.Compress"
    };

    private static readonly double[] LDynamicLeast =
    {
        LLevelingCatalog.LLevelingFrameLeast,
        LLevelingCatalog.LLevelingGaussLeast,
        LLevelingCatalog.LLevelingGainLeast,
        LLevelingCatalog.LLevelingCompressLeast
    };

    private static readonly double[] LDynamicMost =
    {
        LLevelingCatalog.LLevelingFrameMost,
        LLevelingCatalog.LLevelingGaussMost,
        LLevelingCatalog.LLevelingGainMost,
        LLevelingCatalog.LLevelingCompressMost
    };

    public static IReadOnlyList<LInspectorRow> LDynamicRowsRead() => LDynamicLabelKeys
        .Select((lKey, lIndex) => LInspectorPlan.LInspectorRowCreate(
            lIndex, lKey, LDynamicUnits[lIndex], "0.###", LDynamicLeast[lIndex], LDynamicMost[lIndex]))
        .ToList();

    public static string LDynamicKeyRead(string lToken) => lToken switch
    {
        "Gentle" => "Inspector.Dynamic.Gentle",
        "Leveler" => "Inspector.Dynamic.Leveler",
        "Voice" => "Inspector.Dynamic.Voice",
        "Aggressive" => "Inspector.Dynamic.Aggressive",
        "Music" => "Inspector.Dynamic.Music",
        _ => "Inspector.Common.Custom"
    };

    public static string? LDynamicMatch(LWorkNormalizeStep lStep) => LLevelingCatalog.LLevelingDynamicMatch(
        lStep.LWorkNormalizeFrame,
        lStep.LWorkNormalizeGauss,
        lStep.LWorkNormalizeGain,
        lStep.LWorkNormalizeCompress);

    public static LLevelingDynamicPreset? LDynamicPresetRead(string? lToken) =>
        lToken is { } lName ? LLevelingCatalog.LLevelingDynamicRead(lName) : null;

    public static double LDynamicValueRead(LWorkNormalizeStep lStep, int lIndex) => lIndex switch
    {
        1 => lStep.LWorkNormalizeGauss,
        2 => lStep.LWorkNormalizeGain,
        3 => lStep.LWorkNormalizeCompress,
        _ => lStep.LWorkNormalizeFrame
    };

    public static double LDynamicDefaultRead(string? lToken, int lIndex)
    {
        LLevelingDefault lDefault = LLevelingCatalog.LLevelingDefaultRead();
        LLevelingDynamicPreset? lPreset = LDynamicPresetRead(lToken);
        return lIndex switch
        {
            1 => lPreset?.LLevelingGauss ?? lDefault.LLevelingGauss,
            2 => lPreset?.LLevelingMaxGain ?? lDefault.LLevelingMaxGain,
            3 => lPreset?.LLevelingCompress ?? lDefault.LLevelingCompress,
            _ => lPreset?.LLevelingFrame ?? lDefault.LLevelingFrame
        };
    }

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
