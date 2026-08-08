using System;
using System.Collections.Generic;

namespace Cadroue.Core;

public enum LLeveling
{
    LLevelingLoudness,
    LLevelingDynamic
}

public sealed record LLevelingLoudnessPreset(
    string LLevelingToken,
    double LLevelingTarget,
    double LLevelingPeak,
    double LLevelingRange);

public sealed record LLevelingDynamicPreset(
    string LLevelingToken,
    double LLevelingFrame,
    double LLevelingGauss,
    double LLevelingMaxGain,
    double LLevelingCompress);

public static class LLevelingCatalog
{
    public const double LLevelingTargetLeast = -36;
    public const double LLevelingTargetMost = -5;
    public const double LLevelingPeakLeast = -9;
    public const double LLevelingPeakMost = 0;
    public const double LLevelingRangeLeast = 1;
    public const double LLevelingRangeMost = 20;
    public const double LLevelingFrameLeast = 50;
    public const double LLevelingFrameMost = 1000;
    public const double LLevelingGaussLeast = 3;
    public const double LLevelingGaussMost = 101;
    public const double LLevelingGainLeast = 1;
    public const double LLevelingGainMost = 40;
    public const double LLevelingCompressLeast = 0;
    public const double LLevelingCompressMost = 30;

    public static (double Target, double Peak, double Range, bool TwoPass,
        double Frame, double Gauss, double MaxGain, double Compress) LLevelingDefaultRead() =>
        (-21, -2, 6, true, 300, 21, 10, 6);

    public static readonly IReadOnlyList<LLevelingLoudnessPreset> LLevelingLoudnessPresets = new[]
    {
        new LLevelingLoudnessPreset("Loud", -9, -1, 6),
        new LLevelingLoudnessPreset("Streaming", -14, -1, 9),
        new LLevelingLoudnessPreset("Podcast", -16, -1.5, 8),
        new LLevelingLoudnessPreset("Dialogue", -18, -1.5, 7),
        new LLevelingLoudnessPreset("Audiobook", -21, -2, 6),
        new LLevelingLoudnessPreset("Broadcast", -23, -1, 15),
        new LLevelingLoudnessPreset("TV", -24, -2, 20),
        new LLevelingLoudnessPreset("Film", -27, -2, 18)
    };

    public static readonly IReadOnlyList<LLevelingDynamicPreset> LLevelingDynamicPresets = new[]
    {
        new LLevelingDynamicPreset("Gentle", 500, 31, 7, 0),
        new LLevelingDynamicPreset("Leveler", 300, 21, 10, 6),
        new LLevelingDynamicPreset("Voice", 200, 15, 12, 8),
        new LLevelingDynamicPreset("Aggressive", 150, 11, 15, 12),
        new LLevelingDynamicPreset("Music", 400, 31, 8, 0)
    };

    public static readonly IReadOnlyList<string> LLevelingLoudnessTokens = new[]
    {
        "Loud", "Streaming", "Podcast", "Dialogue", "Audiobook", "Broadcast", "TV", "Film"
    };

    public static readonly IReadOnlyList<string> LLevelingDynamicTokens = new[]
    {
        "Gentle", "Leveler", "Voice", "Aggressive", "Music"
    };

    public static (double Target, double Peak, double Range)? LLevelingLoudnessRead(string lToken)
    {
        foreach (LLevelingLoudnessPreset lPreset in LLevelingLoudnessPresets)
        {
            if (lPreset.LLevelingToken == lToken)
            {
                return (lPreset.LLevelingTarget, lPreset.LLevelingPeak, lPreset.LLevelingRange);
            }
        }

        return null;
    }

    public static (double Frame, double Gauss, double MaxGain, double Compress)? LLevelingDynamicRead(string lToken)
    {
        foreach (LLevelingDynamicPreset lPreset in LLevelingDynamicPresets)
        {
            if (lPreset.LLevelingToken == lToken)
            {
                return (lPreset.LLevelingFrame, lPreset.LLevelingGauss, lPreset.LLevelingMaxGain, lPreset.LLevelingCompress);
            }
        }

        return null;
    }

    public static string? LLevelingLoudnessMatch(double lTarget, double lPeak, double lRange)
    {
        foreach (LLevelingLoudnessPreset lPreset in LLevelingLoudnessPresets)
        {
            if (Math.Abs(lTarget - lPreset.LLevelingTarget) < 0.05
                && Math.Abs(lPeak - lPreset.LLevelingPeak) < 0.05
                && Math.Abs(lRange - lPreset.LLevelingRange) < 0.05)
            {
                return lPreset.LLevelingToken;
            }
        }

        return null;
    }

    public static string? LLevelingDynamicMatch(double lFrame, double lGauss, double lMaxGain, double lCompress)
    {
        foreach (LLevelingDynamicPreset lPreset in LLevelingDynamicPresets)
        {
            if (Math.Abs(lFrame - lPreset.LLevelingFrame) < 0.5
                && Math.Abs(lGauss - lPreset.LLevelingGauss) < 0.5
                && Math.Abs(lMaxGain - lPreset.LLevelingMaxGain) < 0.05
                && Math.Abs(lCompress - lPreset.LLevelingCompress) < 0.05)
            {
                return lPreset.LLevelingToken;
            }
        }

        return null;
    }
}
