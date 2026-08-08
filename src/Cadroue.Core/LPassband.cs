using System;
using System.Collections.Generic;

namespace Cadroue.Core;

public sealed record LPassbandPreset(
    string LPassbandToken,
    double LPassbandCutoff,
    int LPassbandStages,
    int LPassbandPoles,
    double LPassbandResonance);

public static class LPassband
{
    public const string LPassbandHighDefault = "Voice";
    public const string LPassbandLowDefault = "Air tame";

    public const int LPassbandStagesLeast = 1;
    public const int LPassbandStagesMost = 8;
    public const double LPassbandResonanceLeast = 0.1;
    public const double LPassbandResonanceMost = 2;

    public static readonly IReadOnlyList<LPassbandPreset> LPassbandHighPresets = new[]
    {
        new LPassbandPreset("Rumble", 30, 2, 2, 0.707),
        new LPassbandPreset("Wind", 60, 4, 2, 0.707),
        new LPassbandPreset("Voice", 80, 2, 2, 0.707),
        new LPassbandPreset("Speech (tight)", 100, 4, 2, 0.707),
        new LPassbandPreset("Tighten", 200, 2, 2, 0.707)
    };

    public static readonly IReadOnlyList<LPassbandPreset> LPassbandLowPresets = new[]
    {
        new LPassbandPreset("Air tame", 16000, 2, 2, 0.707),
        new LPassbandPreset("Soften", 10000, 2, 2, 0.707),
        new LPassbandPreset("Warm", 8000, 3, 2, 0.707),
        new LPassbandPreset("AM radio", 5000, 4, 2, 0.707),
        new LPassbandPreset("Telephone", 3400, 4, 2, 0.707)
    };

    public static LWorkAudioStep LPassbandStepCreate(bool lHigh, bool lActive)
    {
        LPassbandPreset lPreset = LPassbandRead(lHigh, lHigh ? LPassbandHighDefault : LPassbandLowDefault)!;
        return lHigh
            ? LWorkAudioStep.LWorkHighCreate(
                lActive, lPreset.LPassbandCutoff, lPreset.LPassbandStages, lPreset.LPassbandPoles, lPreset.LPassbandResonance)
            : LWorkAudioStep.LWorkLowCreate(
                lActive, lPreset.LPassbandCutoff, lPreset.LPassbandStages, lPreset.LPassbandPoles, lPreset.LPassbandResonance);
    }

    public static LPassbandPreset? LPassbandRead(bool lHigh, string lToken)
    {
        foreach (LPassbandPreset lPreset in lHigh ? LPassbandHighPresets : LPassbandLowPresets)
        {
            if (lPreset.LPassbandToken == lToken)
            {
                return lPreset;
            }
        }

        return null;
    }

    public static string? LPassbandMatch(bool lHigh, double lFrequency, int lStages, int lPoles, double lResonance)
    {
        foreach (LPassbandPreset lPreset in lHigh ? LPassbandHighPresets : LPassbandLowPresets)
        {
            if (Math.Abs(lFrequency - lPreset.LPassbandCutoff) < 0.5
                && lStages == lPreset.LPassbandStages
                && lPoles == lPreset.LPassbandPoles
                && Math.Abs(lResonance - lPreset.LPassbandResonance) < 0.001)
            {
                return lPreset.LPassbandToken;
            }
        }

        return null;
    }
}
