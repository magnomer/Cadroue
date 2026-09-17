using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private static readonly string[] PDynamicLabelKeys =
    {
        "Inspector.Dynamic.Frame",
        "Inspector.Dynamic.Smoothness",
        "Inspector.Dynamic.MaxGain",
        "Inspector.Dynamic.Compress"
    };

    private static readonly string[] PDynamicUnits = { "ms", "g", "×", "s" };

    private static readonly double[] PDynamicLeast =
    {
        LLevelingCatalog.LLevelingFrameLeast,
        LLevelingCatalog.LLevelingGaussLeast,
        LLevelingCatalog.LLevelingGainLeast,
        LLevelingCatalog.LLevelingCompressLeast
    };

    private static readonly double[] PDynamicMost =
    {
        LLevelingCatalog.LLevelingFrameMost,
        LLevelingCatalog.LLevelingGaussMost,
        LLevelingCatalog.LLevelingGainMost,
        LLevelingCatalog.LLevelingCompressMost
    };

    private static string PDynamicKeyRead(string pToken) => pToken switch
    {
        "Gentle" => "Inspector.Dynamic.Gentle",
        "Leveler" => "Inspector.Dynamic.Leveler",
        "Voice" => "Inspector.Dynamic.Voice",
        "Aggressive" => "Inspector.Dynamic.Aggressive",
        "Music" => "Inspector.Dynamic.Music",
        _ => "Inspector.Common.Custom"
    };

    private double PDynamicValueRead(int pSlot)
    {
        LWorkNormalizeStep pStep = LLoudness.LLoudnessStep;
        return pSlot switch
        {
            1 => pStep.LWorkNormalizeGauss,
            2 => pStep.LWorkNormalizeGain,
            3 => pStep.LWorkNormalizeCompress,
            _ => pStep.LWorkNormalizeFrame
        };
    }

    private double PDynamicDefaultRead(int pSlot)
    {
        LLevelingDefault pDefault = LLevelingCatalog.LLevelingDefaultRead();
        LLevelingDynamicPreset? pPreset = LDynamic.LDynamicPresetRead(LLoudness.LLoudnessToken);
        return pSlot switch
        {
            1 => pPreset?.LLevelingGauss ?? pDefault.LLevelingGauss,
            2 => pPreset?.LLevelingMaxGain ?? pDefault.LLevelingMaxGain,
            3 => pPreset?.LLevelingCompress ?? pDefault.LLevelingCompress,
            _ => pPreset?.LLevelingFrame ?? pDefault.LLevelingFrame
        };
    }
}
