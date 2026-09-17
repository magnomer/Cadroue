using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LFilter
{
    private readonly bool lFilterHigh;
    private LWorkPassStep lFilterStep;
    private string? lFilterToken;
    private bool lFilterPersistent;

    public LFilter(bool lHigh)
    {
        lFilterHigh = lHigh;
        lFilterStep = (LWorkPassStep)LPassband.LPassbandStepCreate(lHigh, false);
        lFilterToken = LFilterMatchRead();
    }

    public event Action? LFilterChange;

    public bool LFilterHigh => lFilterHigh;

    public LWorkPassStep LFilterStep => lFilterStep;

    public string? LFilterToken => lFilterToken;

    public bool LFilterPersistent => lFilterPersistent;

    public double LFilterFloor => lFilterHigh ? LPassband.LPassbandHighFloor : LPassband.LPassbandLowFloor;

    public double LFilterCeiling => lFilterHigh ? LPassband.LPassbandHighCeiling : LPassband.LPassbandLowCeiling;

    public LPassbandPreset? LFilterPresetRead() =>
        lFilterToken is { } lToken ? LPassband.LPassbandRead(lFilterHigh, lToken) : null;

    public string? LFilterMatchRead() => LPassband.LPassbandMatch(
        lFilterHigh,
        lFilterStep.LWorkPassFrequency,
        lFilterStep.LWorkPassStages,
        lFilterStep.LWorkPassPoles,
        lFilterStep.LWorkPassResonance);

    public void LFilterStepSet(LWorkAudioStep lStep)
    {
        LWorkPassStep lSource = lStep as LWorkPassStep
            ?? (LWorkPassStep)LPassband.LPassbandStepCreate(lFilterHigh, lStep.LWorkStepActive);
        LWorkPassStep lNormal = LFilterNormalize(
            lSource.LWorkStepActive,
            lSource.LWorkPassFrequency,
            lSource.LWorkPassStages,
            lSource.LWorkPassPoles,
            lSource.LWorkPassResonance);
        LFilterStepApply(lNormal, LPassband.LPassbandMatch(
            lFilterHigh,
            lNormal.LWorkPassFrequency,
            lNormal.LWorkPassStages,
            lNormal.LWorkPassPoles,
            lNormal.LWorkPassResonance));
    }

    public void LFilterActiveSet(bool lActive) =>
        LFilterStepApply(lFilterStep with { LWorkStepActive = lActive }, lFilterToken);

    public void LFilterFrequencySet(double lFrequency) =>
        LFilterStepApply(
            LFilterNormalize(
                lFilterStep.LWorkStepActive,
                lFrequency,
                lFilterStep.LWorkPassStages,
                lFilterStep.LWorkPassPoles,
                lFilterStep.LWorkPassResonance),
            lFilterToken);

    public void LFilterStagesSet(int lStages) =>
        LFilterStepApply(
            LFilterNormalize(
                lFilterStep.LWorkStepActive,
                lFilterStep.LWorkPassFrequency,
                lStages,
                lFilterStep.LWorkPassPoles,
                lFilterStep.LWorkPassResonance),
            lFilterToken);

    public void LFilterPolesSet(int lPoles) =>
        LFilterStepApply(
            LFilterNormalize(
                lFilterStep.LWorkStepActive,
                lFilterStep.LWorkPassFrequency,
                lFilterStep.LWorkPassStages,
                lPoles,
                lFilterStep.LWorkPassResonance),
            lFilterToken);

    public void LFilterResonanceSet(double lResonance) =>
        LFilterStepApply(
            LFilterNormalize(
                lFilterStep.LWorkStepActive,
                lFilterStep.LWorkPassFrequency,
                lFilterStep.LWorkPassStages,
                lFilterStep.LWorkPassPoles,
                lResonance),
            lFilterToken);

    public void LFilterPresetSelect(string lToken)
    {
        if (LPassband.LPassbandRead(lFilterHigh, lToken) is not { } lPreset)
        {
            return;
        }

        LFilterStepApply(
            LFilterNormalize(
                lFilterStep.LWorkStepActive,
                lPreset.LPassbandCutoff,
                lPreset.LPassbandStages,
                lPreset.LPassbandPoles,
                lPreset.LPassbandResonance),
            lToken);
    }

    public void LFilterPersistentSet(bool lPersistent)
    {
        if (lFilterPersistent == lPersistent)
        {
            return;
        }

        lFilterPersistent = lPersistent;
        LFilterChange?.Invoke();
    }

    private void LFilterStepApply(LWorkPassStep lNormal, string? lToken)
    {
        if (lFilterStep == lNormal && lFilterToken == lToken)
        {
            return;
        }

        lFilterStep = lNormal;
        lFilterToken = lToken;
        LFilterChange?.Invoke();
    }

    private LWorkPassStep LFilterNormalize(
        bool lActive, double lFrequency, int lStages, int lPoles, double lResonance) =>
        (LWorkPassStep)(lFilterHigh
            ? LWorkAudioStep.LWorkHighCreate(lActive, lFrequency, lStages, lPoles, lResonance)
            : LWorkAudioStep.LWorkLowCreate(lActive, lFrequency, lStages, lPoles, lResonance));
}
