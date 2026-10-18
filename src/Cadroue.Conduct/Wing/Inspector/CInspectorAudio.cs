using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The inspector's audio plans: an audio plan sets every audio tool, the held plan's tools are kept, and the audio and
// persistent plans are built from the tools' held values. CInspector makes one and holds the tools it reads and sets.
public sealed class CInspectorAudio
{
    private readonly LAudioPort _cInspectorAudioPort;
    private readonly CInspector _cInspectorAudioOwner;
    private LInspectorPort? _cInspectorAudioPlan;

    internal CInspectorAudio(LAudioPort port, CInspector owner)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(owner);
        _cInspectorAudioPort = port;
        _cInspectorAudioOwner = owner;
    }

    // An audio plan sets every audio tool, each from its read; a step the plan lacks reads as the Core default. The
    // skip switch follows the plan, last. The plan stays held for a keep that follows.
    public void CInspectorAudioAccept(LInspectorPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        _cInspectorAudioPlan = plan;
        CInspector owner = _cInspectorAudioOwner;
        (bool highActive, IReadOnlyList<double> highValues, int highPoles, string? highPreset) =
            plan.LInspectorFilterRead(true);
        owner.CInspectorHighpass.CFilterStepSet(highActive, highValues, highPoles, highPreset);
        (bool lowActive, IReadOnlyList<double> lowValues, int lowPoles, string? lowPreset) =
            plan.LInspectorFilterRead(false);
        owner.CInspectorLowpass.CFilterStepSet(lowActive, lowValues, lowPoles, lowPreset);
        (bool noiseActive, IReadOnlyList<double> noiseValues, bool noiseTrack, int noiseType, string? noisePreset) =
            plan.LInspectorNoiseRead();
        owner.CInspectorNoise.CNoiseStepSet(noiseActive, noiseValues, noiseTrack, noiseType, noisePreset);
        (bool equalizerActive, IReadOnlyList<(double, double)> equalizerBands, string? equalizerPreset) =
            plan.LInspectorEqualizerRead();
        owner.CInspectorEqualizer.CEqualizerStepSet(equalizerActive, equalizerBands, equalizerPreset);
        (bool volumeActive, double volumeGain) = plan.LInspectorVolumeRead();
        owner.CInspectorVolume.CVolumeStepSet(volumeActive, volumeGain);
        (bool levelActive, bool levelDynamic, IReadOnlyList<double> levelValues, bool levelTwoPass,
            string? levelPreset) = plan.LInspectorLevelingRead();
        owner.CInspectorLoudness.CLoudnessStepSet(levelActive, levelDynamic, levelValues, levelTwoPass, levelPreset);
        owner.CInspectorSkip.CSkipActiveSet(plan.LInspectorSkipRead());
    }

    // The last accepted audio plan is kept: every gated audio tool it carries a step for is kept, leveling last; a
    // tool it lacks stays as it is. Before any audio plan, no tool changes. The skip switch is kept as told, after.
    public void CInspectorPersistentAccept(bool skip)
    {
        CInspectorToolsApply();
        _cInspectorAudioOwner.CInspectorSkip.CSkipPersistentSet(skip);
    }

    // The persistent audio plan: each gated audio tool the user keeps, from its held values, and the skip switch when
    // it is kept and on, built by the engine and answered as a plan read.
    public LInspectorPort CInspectorPersistentRead()
    {
        CInspector owner = _cInspectorAudioOwner;
        (bool, IReadOnlyList<double>, int)? highpass = owner.CInspectorHighpass.CFilterPersistent
            ? (owner.CInspectorHighpass.CFilterActive, owner.CInspectorHighpass.CFilterValues,
                owner.CInspectorHighpass.CFilterPoles)
            : null;
        (bool, IReadOnlyList<double>, int)? lowpass = owner.CInspectorLowpass.CFilterPersistent
            ? (owner.CInspectorLowpass.CFilterActive, owner.CInspectorLowpass.CFilterValues,
                owner.CInspectorLowpass.CFilterPoles)
            : null;
        (bool, IReadOnlyList<double>, bool, int)? noise = owner.CInspectorNoise.CNoisePersistent
            ? (owner.CInspectorNoise.CNoiseActive, owner.CInspectorNoise.CNoiseValues,
                owner.CInspectorNoise.CNoiseTrack, owner.CInspectorNoise.CNoiseType)
            : null;
        (bool, double)? volume = owner.CInspectorVolume.CVolumePersistent
            ? (owner.CInspectorVolume.CVolumeActive, owner.CInspectorVolume.CVolumeGain)
            : null;
        (bool, IReadOnlyList<(double, double)>)? equalizer = owner.CInspectorEqualizer.CEqualizerPersistent
            ? (owner.CInspectorEqualizer.CEqualizerActive, owner.CInspectorEqualizer.CEqualizerBands)
            : null;
        (bool, bool, IReadOnlyList<double>, bool)? leveling = owner.CInspectorLoudness.CLoudnessPersistent
            ? (owner.CInspectorLoudness.CLoudnessActive, owner.CInspectorLoudness.CLoudnessDynamic,
                owner.CInspectorLoudness.CLoudnessValues, owner.CInspectorLoudness.CLoudnessTwoPass)
            : null;
        return _cInspectorAudioPort.LAudioPersistentCreate(
            highpass,
            lowpass,
            noise,
            volume,
            leveling,
            equalizer,
            owner.CInspectorSkip.CSkipPersistent && owner.CInspectorSkip.CSkipActive);
    }

    // The audio plan in the caller's order: a step from every gated audio tool's held values for each processing step
    // that names one, with the skip switch as it stands, built by the engine and answered as a plan read.
    public LInspectorPort CInspectorAudioRead(IReadOnlyList<string> processing)
    {
        CInspector owner = _cInspectorAudioOwner;
        return _cInspectorAudioPort.LAudioPlanCreate(
            processing,
            (owner.CInspectorHighpass.CFilterActive, owner.CInspectorHighpass.CFilterValues,
                owner.CInspectorHighpass.CFilterPoles),
            (owner.CInspectorLowpass.CFilterActive, owner.CInspectorLowpass.CFilterValues,
                owner.CInspectorLowpass.CFilterPoles),
            (owner.CInspectorNoise.CNoiseActive, owner.CInspectorNoise.CNoiseValues, owner.CInspectorNoise.CNoiseTrack,
                owner.CInspectorNoise.CNoiseType),
            (owner.CInspectorVolume.CVolumeActive, owner.CInspectorVolume.CVolumeGain),
            (owner.CInspectorLoudness.CLoudnessActive, owner.CInspectorLoudness.CLoudnessDynamic,
                owner.CInspectorLoudness.CLoudnessValues, owner.CInspectorLoudness.CLoudnessTwoPass),
            (owner.CInspectorEqualizer.CEqualizerActive, owner.CInspectorEqualizer.CEqualizerBands),
            owner.CInspectorSkip.CSkipActive);
    }

    // Each gated audio tool the held audio plan carries a step for is kept.
    private void CInspectorToolsApply()
    {
        if (_cInspectorAudioPlan is not { } plan)
        {
            return;
        }

        CInspector owner = _cInspectorAudioOwner;
        var present = plan.LInspectorPresentRead();
        if (present.LInspectorHighpass)
        {
            owner.CInspectorHighpass.CFilterPersistentSet(true);
        }

        if (present.LInspectorLowpass)
        {
            owner.CInspectorLowpass.CFilterPersistentSet(true);
        }

        if (present.LInspectorNoise)
        {
            owner.CInspectorNoise.CNoisePersistentSet(true);
        }

        if (present.LInspectorVolume)
        {
            owner.CInspectorVolume.CVolumePersistentSet(true);
        }

        if (present.LInspectorEqualizer)
        {
            owner.CInspectorEqualizer.CEqualizerPersistentSet(true);
        }

        if (present.LInspectorLeveling)
        {
            owner.CInspectorLoudness.CLoudnessPersistentSet(true);
        }
    }
}
