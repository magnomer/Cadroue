using Cadroue.Core;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private static readonly string[] PLoudnessLabelKeys =
    {
        "Inspector.Normalize.Target",
        "Inspector.Normalize.Peak",
        "Inspector.Normalize.Range"
    };

    private static readonly string[] PLoudnessUnits = { "LUFS", "dBTP", "LU" };

    private static readonly double[] PLoudnessLeast =
    {
        LLevelingCatalog.LLevelingTargetLeast,
        LLevelingCatalog.LLevelingPeakLeast,
        LLevelingCatalog.LLevelingRangeLeast
    };

    private static readonly double[] PLoudnessMost =
    {
        LLevelingCatalog.LLevelingTargetMost,
        LLevelingCatalog.LLevelingPeakMost,
        LLevelingCatalog.LLevelingRangeMost
    };

    private static string PLoudnessKeyRead(string pToken) => pToken switch
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

    private double PLoudnessValueRead(int pSlot)
    {
        LWorkNormalizeStep pStep = LLoudness.LLoudnessStep;
        return pSlot switch
        {
            1 => pStep.LWorkNormalizePeak,
            2 => pStep.LWorkNormalizeRange,
            _ => pStep.LWorkNormalizeTarget
        };
    }

    private double PLoudnessDefaultRead(int pSlot)
    {
        LLevelingDefault pDefault = LLevelingCatalog.LLevelingDefaultRead();
        LLevelingLoudnessPreset? pPreset = LLoudness.LLoudnessPresetRead();
        return pSlot switch
        {
            1 => pPreset?.LLevelingPeak ?? pDefault.LLevelingPeak,
            2 => pPreset?.LLevelingRange ?? pDefault.LLevelingRange,
            _ => pPreset?.LLevelingTarget ?? pDefault.LLevelingTarget
        };
    }
}
