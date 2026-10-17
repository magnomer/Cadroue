using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The high- and low-pass rules from the passband catalogue, on plain values in slot order: frequency, stages,
// resonance. Every value is held inside its side's range, the stages as a whole number and the poles as 1 or 2.
public sealed class LFilterOutlet : LFilterPort
{
    public IReadOnlyList<double> LFilterLeastsRead(bool lHigh) => Array.AsReadOnly(new[]
    {
        lHigh ? LPassband.LPassbandHighFloor : LPassband.LPassbandLowFloor,
        LPassband.LPassbandStagesLeast,
        LPassband.LPassbandResonanceLeast
    });

    public IReadOnlyList<double> LFilterMostsRead(bool lHigh) => Array.AsReadOnly(new[]
    {
        lHigh ? LPassband.LPassbandHighCeiling : LPassband.LPassbandLowCeiling,
        LPassband.LPassbandStagesMost,
        LPassband.LPassbandResonanceMost
    });

    public IReadOnlyList<string> LFilterTokensRead(bool lHigh) =>
        (lHigh ? LPassband.LPassbandHighPresets : LPassband.LPassbandLowPresets)
        .Select(lPreset => lPreset.LPassbandToken)
        .ToList();

    // The default step: the side's default preset switched off, with the preset it matches.
    public (IReadOnlyList<double> LFilterValues, int LFilterPoles, string? LFilterPreset) LFilterOriginRead(bool lHigh)
    {
        var lStep = (LWorkPassStep)LPassband.LPassbandStepCreate(lHigh, false);
        IReadOnlyList<double> lValues = LFilterValuesRead(lStep);
        return (lValues, lStep.LWorkPassPoles, LFilterMatchFind(lHigh, lValues, lStep.LWorkPassPoles));
    }

    // The values a reset returns to: the chosen preset's; without a known preset, the held frequency, one stage and
    // a resonance of 0.707.
    public IReadOnlyList<double> LFilterDefaultRead(bool lHigh, string? lToken, double lFrequency)
    {
        LPassbandPreset? lPreset = lToken is { } lKnown ? LPassband.LPassbandRead(lHigh, lKnown) : null;
        return Array.AsReadOnly(new[]
        {
            lPreset?.LPassbandCutoff ?? lFrequency,
            lPreset?.LPassbandStages ?? 1,
            lPreset?.LPassbandResonance ?? 0.707
        });
    }

    public (IReadOnlyList<double> LFilterValues, int LFilterPoles) LFilterNormalize(
        bool lHigh, IReadOnlyList<double> lValues, int lPoles)
    {
        ArgumentNullException.ThrowIfNull(lValues);
        var lStep = (LWorkPassStep)LFilterStepCreate(lHigh, false, lValues, lPoles);
        return (LFilterValuesRead(lStep), lStep.LWorkPassPoles);
    }

    // The preset's values held inside their ranges, with its poles; null for an unknown preset.
    public (IReadOnlyList<double> LFilterValues, int LFilterPoles)? LFilterPresetRead(bool lHigh, string lToken)
    {
        if (LPassband.LPassbandRead(lHigh, lToken) is not { } lPreset)
        {
            return null;
        }

        return LFilterNormalize(
            lHigh,
            new[] { lPreset.LPassbandCutoff, lPreset.LPassbandStages, lPreset.LPassbandResonance },
            lPreset.LPassbandPoles);
    }

    public string? LFilterMatchFind(bool lHigh, IReadOnlyList<double> lValues, int lPoles)
    {
        ArgumentNullException.ThrowIfNull(lValues);
        return LPassband.LPassbandMatch(lHigh, lValues[0], (int)Math.Round(lValues[1]), lPoles, lValues[2]);
    }

    // The audio step for the gate's side, switch, values and poles; the stages round to a whole number and every
    // value is held inside its range.
    public static LWorkAudioStep LFilterStepCreate(bool lHigh, bool lActive, IReadOnlyList<double> lValues, int lPoles)
    {
        ArgumentNullException.ThrowIfNull(lValues);
        int lStages = (int)Math.Round(lValues[1]);
        return lHigh
            ? LWorkAudioStep.LWorkHighCreate(lActive, lValues[0], lStages, lPoles, lValues[2])
            : LWorkAudioStep.LWorkLowCreate(lActive, lValues[0], lStages, lPoles, lValues[2]);
    }

    internal static IReadOnlyList<double> LFilterValuesRead(LWorkPassStep lStep) =>
        Array.AsReadOnly(new[] { lStep.LWorkPassFrequency, lStep.LWorkPassStages, lStep.LWorkPassResonance });
}
