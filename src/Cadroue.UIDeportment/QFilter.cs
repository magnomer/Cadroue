using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QFilter
{
    private static readonly IReadOnlyList<string> QFilterPoleNames = new[] { "1 (6 dB)", "2 (12 dB)" };

    private readonly bool qFilterHigh;
    private readonly IReadOnlyList<string> qFilterTokens;
    private LWorkPassStep qFilterStep;
    private string? qFilterToken;
    private bool qFilterPersistent;

    public QFilter(bool lHigh)
    {
        qFilterHigh = lHigh;
        qFilterTokens = (lHigh ? LPassband.LPassbandHighPresets : LPassband.LPassbandLowPresets)
            .Select(lPreset => lPreset.LPassbandToken)
            .ToList();
        qFilterStep = (LWorkPassStep)LPassband.LPassbandStepCreate(lHigh, false);
        qFilterToken = QFilterMatchRead();
    }

    public event Action? QFilterChanged;

    public bool QFilterHigh => qFilterHigh;

    public LWorkPassStep QFilterStep => qFilterStep;

    public string? QFilterToken => qFilterToken;

    public bool QFilterPersistent => qFilterPersistent;

    public double QFilterFloor => qFilterHigh ? LPassband.LPassbandHighFloor : LPassband.LPassbandLowFloor;

    public double QFilterCeiling => qFilterHigh ? LPassband.LPassbandHighCeiling : LPassband.LPassbandLowCeiling;

    public double QFilterDefaultRead(int lIndex)
    {
        LPassbandPreset? lPreset = QFilterPresetRead();
        return lIndex switch
        {
            1 => lPreset?.LPassbandStages ?? 1,
            2 => lPreset?.LPassbandResonance ?? 0.707,
            _ => lPreset?.LPassbandCutoff ?? qFilterStep.LWorkPassFrequency
        };
    }

    public IReadOnlyList<string> QFilterPoles => QFilterPoleNames;

    public int QFilterPolesIndex => qFilterStep.LWorkPassPoles == 1 ? 0 : 1;

    public bool QFilterResonanceActive => qFilterStep.LWorkPassPoles == 2;

    public QInspectorChoice QFilterChoiceRead() =>
        QInspectorPlan.QInspectorChoiceRead(qFilterTokens, QFilterKeyRead, qFilterToken, QFilterMatchRead());

    public static string QFilterKeyRead(string lToken) => lToken switch
    {
        "Rumble" => "Inspector.Pass.Preset.Rumble",
        "Wind" => "Inspector.Pass.Preset.Wind",
        "Voice" => "Inspector.Pass.Preset.Voice",
        "Speech (tight)" => "Inspector.Pass.Preset.SpeechTight",
        "Tighten" => "Inspector.Pass.Preset.Tighten",
        "Air tame" => "Inspector.Pass.Preset.Airtame",
        "Soften" => "Inspector.Pass.Preset.Soften",
        "Warm" => "Inspector.Pass.Preset.Warm",
        "AM radio" => "Inspector.Pass.Preset.AmRadio",
        "Telephone" => "Inspector.Pass.Preset.Telephone",
        _ => "Inspector.Common.Custom"
    };

    public LPassbandPreset? QFilterPresetRead() =>
        qFilterToken is { } lToken ? LPassband.LPassbandRead(qFilterHigh, lToken) : null;

    public string? QFilterMatchRead() => LPassband.LPassbandMatch(
        qFilterHigh,
        qFilterStep.LWorkPassFrequency,
        qFilterStep.LWorkPassStages,
        qFilterStep.LWorkPassPoles,
        qFilterStep.LWorkPassResonance);

    public void QFilterStepSet(LWorkAudioStep lStep)
    {
        LWorkPassStep lNormal = QFilterStepResolve(lStep);
        QFilterStepApply(lNormal, LPassband.LPassbandMatch(
            qFilterHigh,
            lNormal.LWorkPassFrequency,
            lNormal.LWorkPassStages,
            lNormal.LWorkPassPoles,
            lNormal.LWorkPassResonance));
    }

    public void QFilterActiveSet(bool lActive) =>
        QFilterStepApply(qFilterStep with { LWorkStepActive = lActive }, qFilterToken);

    public void QFilterFrequencySet(double lFrequency) =>
        QFilterStepApply(
            QFilterNormalize(
                qFilterStep.LWorkStepActive,
                lFrequency,
                qFilterStep.LWorkPassStages,
                qFilterStep.LWorkPassPoles,
                qFilterStep.LWorkPassResonance),
            qFilterToken);

    public void QFilterStagesSet(int lStages) =>
        QFilterStepApply(
            QFilterNormalize(
                qFilterStep.LWorkStepActive,
                qFilterStep.LWorkPassFrequency,
                lStages,
                qFilterStep.LWorkPassPoles,
                qFilterStep.LWorkPassResonance),
            qFilterToken);

    public void QFilterPolesSet(int lPoles) =>
        QFilterStepApply(
            QFilterNormalize(
                qFilterStep.LWorkStepActive,
                qFilterStep.LWorkPassFrequency,
                qFilterStep.LWorkPassStages,
                lPoles,
                qFilterStep.LWorkPassResonance),
            qFilterToken);

    public void QFilterPolesSelect(int lIndex)
    {
        if (QFilterPolesResolve(lIndex) is { } lPoles)
        {
            QFilterPolesSet(lPoles);
        }
    }

    public void QFilterChoiceSelect(int lIndex)
    {
        string? lToken = QInspectorPlan.QInspectorChoiceResolve(
            qFilterTokens, lIndex, qFilterToken, QFilterMatchRead());
        if (lToken is not null)
        {
            QFilterPresetSelect(lToken);
        }
    }

    public void QFilterResonanceSet(double lResonance) =>
        QFilterStepApply(
            QFilterNormalize(
                qFilterStep.LWorkStepActive,
                qFilterStep.LWorkPassFrequency,
                qFilterStep.LWorkPassStages,
                qFilterStep.LWorkPassPoles,
                lResonance),
            qFilterToken);

    public void QFilterPresetSelect(string lToken)
    {
        if (LPassband.LPassbandRead(qFilterHigh, lToken) is not { } lPreset)
        {
            return;
        }

        QFilterStepApply(
            QFilterNormalize(
                qFilterStep.LWorkStepActive,
                lPreset.LPassbandCutoff,
                lPreset.LPassbandStages,
                lPreset.LPassbandPoles,
                lPreset.LPassbandResonance),
            lToken);
    }

    public void QFilterPersistentSet(bool lPersistent)
    {
        if (qFilterPersistent == lPersistent)
        {
            return;
        }

        qFilterPersistent = lPersistent;
        QFilterChanged?.Invoke();
    }

    private void QFilterStepApply(LWorkPassStep lNormal, string? lToken)
    {
        if (qFilterStep == lNormal && qFilterToken == lToken)
        {
            return;
        }

        qFilterStep = lNormal;
        qFilterToken = lToken;
        QFilterChanged?.Invoke();
    }

    // A step that is not a pass step becomes this filter's default step; either way it is normalized.
    private LWorkPassStep QFilterStepResolve(LWorkAudioStep lStep)
    {
        LWorkPassStep lSource = lStep as LWorkPassStep
            ?? (LWorkPassStep)LPassband.LPassbandStepCreate(qFilterHigh, lStep.LWorkStepActive);
        return QFilterNormalize(
            lSource.LWorkStepActive,
            lSource.LWorkPassFrequency,
            lSource.LWorkPassStages,
            lSource.LWorkPassPoles,
            lSource.LWorkPassResonance);
    }

    // The pole count of a listed choice; null for an index out of range.
    private static int? QFilterPolesResolve(int lIndex) =>
        lIndex >= 0 && lIndex < QFilterPoleNames.Count ? (lIndex == 0 ? 1 : 2) : null;

    private LWorkPassStep QFilterNormalize(
        bool lActive, double lFrequency, int lStages, int lPoles, double lResonance) =>
        (LWorkPassStep)(qFilterHigh
            ? LWorkAudioStep.LWorkHighCreate(lActive, lFrequency, lStages, lPoles, lResonance)
            : LWorkAudioStep.LWorkLowCreate(lActive, lFrequency, lStages, lPoles, lResonance));
}
