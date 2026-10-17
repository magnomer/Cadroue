using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The inspector's gate: one decision object per tool, each exposed as one property. CAtelier makes one per inspector.
// Every decision that spans its tools is one member here, so an owner asks once whatever the number of tools. The
// clinic is the fix tab's inspector, so it is one more tool here.
public sealed class CInspector
{
    private readonly LAudioPort _cInspectorAudioPort;
    private readonly LVideoPort _cInspectorVideoPort;
    private LInspectorPort? _cInspectorAudioPlan;

    internal CInspector(
        LTonePort tone,
        LEqualizerPort equalizer,
        LClinicPort clinic,
        LNoisePort noise,
        LWhitebalancePort whitebalance,
        LFilterPort filter,
        LAudioPort audio,
        LLoudnessPort loudness,
        LVideoPort video)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(video);
        _cInspectorAudioPort = audio;
        _cInspectorVideoPort = video;
        CInspectorTone = new CTone(tone);
        CInspectorEqualizer = new CEqualizer(equalizer);
        CInspectorClinic = new CClinic(clinic);
        CInspectorNoise = new CNoise(noise);
        CInspectorWhitebalance = new CWhitebalance(whitebalance);
        CInspectorHighpass = new CFilter(filter, true);
        CInspectorLowpass = new CFilter(filter, false);
        CInspectorLoudness = new CLoudness(loudness);
    }

    public CTone CInspectorTone { get; }

    public CWhitebalance CInspectorWhitebalance { get; }

    public CGamma CInspectorGamma { get; } = new();

    public CExposure CInspectorExposure { get; } = new();

    public CCurve CInspectorCurve { get; } = new();

    public CVolume CInspectorVolume { get; } = new();

    public CCrop CInspectorCrop { get; } = new();

    public CEqualizer CInspectorEqualizer { get; }

    public CClinic CInspectorClinic { get; }

    public CNoise CInspectorNoise { get; }

    public CFilter CInspectorHighpass { get; }

    public CFilter CInspectorLowpass { get; }

    public CLoudness CInspectorLoudness { get; }

    public CSkip CInspectorSkip { get; } = new();

    // The owner reports which filters the editor has and whether the preview runs on mpv; each tool takes its fact.
    public void CInspectorCapableSet(bool eq, bool exposure, bool whitebalance, bool preview)
    {
        CInspectorExposure.CExposureCapableSet(exposure, preview);
        CInspectorGamma.CGammaCapableSet(eq, preview);
        CInspectorTone.CToneCapableSet(eq);
        CInspectorWhitebalance.CWhitebalanceCapableSet(whitebalance, preview);
    }

    // A video plan sets every video tool, each from its read; a step the plan lacks reads as the Core default.
    public void CInspectorVideoAccept(LInspectorPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        (IReadOnlyList<bool> toneActives, IReadOnlyList<double> toneValues) = plan.LInspectorToneRead();
        CInspectorTone.CToneStepSet(toneActives, toneValues);
        CInspectorWhitebalance.CWhitebalanceStepSet(plan.LInspectorWhitebalanceRead());
        (bool gammaActive, IReadOnlyList<double> gammaValues) = plan.LInspectorGammaRead();
        CInspectorGamma.CGammaStepSet(gammaActive, gammaValues);
        (bool exposureActive, double exposureValue) = plan.LInspectorExposureRead();
        CInspectorExposure.CExposureStepSet(exposureActive, exposureValue);
    }

    // An edit plan sets the crop and its ratio, then every video tool as a video plan, then the skip switch; a plan
    // that keeps the skip switch keeps it as told, last.
    public void CInspectorEditAccept(LInspectorPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        CInspectorCrop.CCropPlanAccept(plan);
        CInspectorVideoAccept(plan);
        CInspectorSkip.CSkipActiveSet(plan.LInspectorSkipRead());
        if (plan.LInspectorKeepRead().LInspectorSkip is bool skip)
        {
            CInspectorSkip.CSkipPersistentSet(skip);
        }
    }

    // An audio plan sets every audio tool, each from its read; a step the plan lacks reads as the Core default. The
    // skip switch follows the plan, last. The plan stays held for a keep that follows.
    public void CInspectorAudioAccept(LInspectorPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        _cInspectorAudioPlan = plan;
        (bool highActive, IReadOnlyList<double> highValues, int highPoles, string? highPreset) =
            plan.LInspectorFilterRead(true);
        CInspectorHighpass.CFilterStepSet(highActive, highValues, highPoles, highPreset);
        (bool lowActive, IReadOnlyList<double> lowValues, int lowPoles, string? lowPreset) =
            plan.LInspectorFilterRead(false);
        CInspectorLowpass.CFilterStepSet(lowActive, lowValues, lowPoles, lowPreset);
        (bool noiseActive, IReadOnlyList<double> noiseValues, bool noiseTrack, int noiseType, string? noisePreset) =
            plan.LInspectorNoiseRead();
        CInspectorNoise.CNoiseStepSet(noiseActive, noiseValues, noiseTrack, noiseType, noisePreset);
        (bool equalizerActive, IReadOnlyList<(double, double)> equalizerBands, string? equalizerPreset) =
            plan.LInspectorEqualizerRead();
        CInspectorEqualizer.CEqualizerStepSet(equalizerActive, equalizerBands, equalizerPreset);
        (bool volumeActive, double volumeGain) = plan.LInspectorVolumeRead();
        CInspectorVolume.CVolumeStepSet(volumeActive, volumeGain);
        (bool levelActive, bool levelDynamic, IReadOnlyList<double> levelValues, bool levelTwoPass,
            string? levelPreset) = plan.LInspectorLevelingRead();
        CInspectorLoudness.CLoudnessStepSet(levelActive, levelDynamic, levelValues, levelTwoPass, levelPreset);
        CInspectorSkip.CSkipActiveSet(plan.LInspectorSkipRead());
    }

    // The last accepted audio plan is kept: every gated audio tool it carries a step for is kept, leveling last; a
    // tool it lacks stays as it is. Before any audio plan, no tool changes. The skip switch is kept as told, after.
    public void CInspectorPersistentAccept(bool skip)
    {
        CInspectorToolsApply();
        CInspectorSkip.CSkipPersistentSet(skip);
    }

    // The persistent audio plan: each gated audio tool the user keeps, from its held values, and the skip switch when
    // it is kept and on, built by the engine and answered as a plan read.
    public LInspectorPort CInspectorPersistentRead()
    {
        (bool, IReadOnlyList<double>, int)? highpass = CInspectorHighpass.CFilterPersistent
            ? (CInspectorHighpass.CFilterActive, CInspectorHighpass.CFilterValues, CInspectorHighpass.CFilterPoles)
            : null;
        (bool, IReadOnlyList<double>, int)? lowpass = CInspectorLowpass.CFilterPersistent
            ? (CInspectorLowpass.CFilterActive, CInspectorLowpass.CFilterValues, CInspectorLowpass.CFilterPoles)
            : null;
        (bool, IReadOnlyList<double>, bool, int)? noise = CInspectorNoise.CNoisePersistent
            ? (CInspectorNoise.CNoiseActive, CInspectorNoise.CNoiseValues, CInspectorNoise.CNoiseTrack,
                CInspectorNoise.CNoiseType)
            : null;
        (bool, double)? volume = CInspectorVolume.CVolumePersistent
            ? (CInspectorVolume.CVolumeActive, CInspectorVolume.CVolumeGain)
            : null;
        (bool, IReadOnlyList<(double, double)>)? equalizer = CInspectorEqualizer.CEqualizerPersistent
            ? (CInspectorEqualizer.CEqualizerActive, CInspectorEqualizer.CEqualizerBands)
            : null;
        (bool, bool, IReadOnlyList<double>, bool)? leveling = CInspectorLoudness.CLoudnessPersistent
            ? (CInspectorLoudness.CLoudnessActive, CInspectorLoudness.CLoudnessDynamic,
                CInspectorLoudness.CLoudnessValues, CInspectorLoudness.CLoudnessTwoPass)
            : null;
        return _cInspectorAudioPort.LAudioPersistentCreate(
            highpass,
            lowpass,
            noise,
            volume,
            leveling,
            equalizer,
            CInspectorSkip.CSkipPersistent && CInspectorSkip.CSkipActive);
    }

    // The audio plan in the caller's order: a step from every gated audio tool's held values for each processing step
    // that names one, with the skip switch as it stands, built by the engine and answered as a plan read.
    public LInspectorPort CInspectorAudioRead(IReadOnlyList<string> processing) =>
        _cInspectorAudioPort.LAudioPlanCreate(
            processing,
            (CInspectorHighpass.CFilterActive, CInspectorHighpass.CFilterValues, CInspectorHighpass.CFilterPoles),
            (CInspectorLowpass.CFilterActive, CInspectorLowpass.CFilterValues, CInspectorLowpass.CFilterPoles),
            (CInspectorNoise.CNoiseActive, CInspectorNoise.CNoiseValues, CInspectorNoise.CNoiseTrack,
                CInspectorNoise.CNoiseType),
            (CInspectorVolume.CVolumeActive, CInspectorVolume.CVolumeGain),
            (CInspectorLoudness.CLoudnessActive, CInspectorLoudness.CLoudnessDynamic,
                CInspectorLoudness.CLoudnessValues, CInspectorLoudness.CLoudnessTwoPass),
            (CInspectorEqualizer.CEqualizerActive, CInspectorEqualizer.CEqualizerBands),
            CInspectorSkip.CSkipActive);

    // The video plan in the caller's order: a step from every video tool's held values for each processing step that
    // names one, built by the engine and answered as a plan read. Without the mpv-only filters, gamma, white balance
    // and exposure are left out.
    public LInspectorPort CInspectorVideoRead(IReadOnlyList<string> processing, bool mpvOnly)
    {
        ArgumentNullException.ThrowIfNull(processing);
        return _cInspectorVideoPort.LVideoPlanCreate(
            processing,
            CInspectorToneRead(0),
            CInspectorToneRead(1),
            CInspectorToneRead(2),
            mpvOnly ? (CInspectorGamma.CGammaActive, CInspectorGamma.CGammaValues) : null,
            mpvOnly ? CInspectorWhitebalance.CWhitebalanceAnswer : null,
            mpvOnly ? (CInspectorExposure.CExposureActive, CInspectorExposure.CExposureValue) : null,
            (CInspectorCurve.CCurveActive, CInspectorCurve.CCurveChannels));
    }

    // The carried video plan: each video tool the user keeps, from its held values, in the tools' order, built by the
    // engine and answered as a plan read.
    public LInspectorPort CInspectorCarriedRead() =>
        _cInspectorVideoPort.LVideoPersistentCreate(
            CInspectorTone.CTonePersistents[0] ? CInspectorToneRead(0) : null,
            CInspectorTone.CTonePersistents[1] ? CInspectorToneRead(1) : null,
            CInspectorTone.CTonePersistents[2] ? CInspectorToneRead(2) : null,
            CInspectorGamma.CGammaPersistent ? (CInspectorGamma.CGammaActive, CInspectorGamma.CGammaValues) : null,
            CInspectorWhitebalance.CWhitebalancePersistent ? CInspectorWhitebalance.CWhitebalanceAnswer : null,
            CInspectorExposure.CExposurePersistent
                ? (CInspectorExposure.CExposureActive, CInspectorExposure.CExposureValue)
                : null,
            CInspectorCurve.CCurvePersistent ? (CInspectorCurve.CCurveActive, CInspectorCurve.CCurveChannels) : null);

    // A tone slot's switch and value: 0 brightness, 1 contrast, 2 saturation.
    private (bool, double) CInspectorToneRead(int slot) =>
        (CInspectorTone.CToneActives[slot], CInspectorTone.CToneValues[slot]);

    // Each gated audio tool the held audio plan carries a step for is kept.
    private void CInspectorToolsApply()
    {
        if (_cInspectorAudioPlan is not { } plan)
        {
            return;
        }

        var present = plan.LInspectorPresentRead();
        if (present.LInspectorHighpass)
        {
            CInspectorHighpass.CFilterPersistentSet(true);
        }

        if (present.LInspectorLowpass)
        {
            CInspectorLowpass.CFilterPersistentSet(true);
        }

        if (present.LInspectorNoise)
        {
            CInspectorNoise.CNoisePersistentSet(true);
        }

        if (present.LInspectorVolume)
        {
            CInspectorVolume.CVolumePersistentSet(true);
        }

        if (present.LInspectorEqualizer)
        {
            CInspectorEqualizer.CEqualizerPersistentSet(true);
        }

        if (present.LInspectorLeveling)
        {
            CInspectorLoudness.CLoudnessPersistentSet(true);
        }
    }
}
