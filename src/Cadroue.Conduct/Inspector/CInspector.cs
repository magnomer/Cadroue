using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The inspector's gate: one decision object per tool, each exposed as one property. CAtelier makes one per inspector.
// Every decision that spans its tools is one member here, so an owner asks once whatever the number of tools. The
// clinic is the fix tab's inspector, so it is one more tool here.
public sealed class CInspector
{
    internal CInspector(
        LTonePort tone,
        LEqualizerPort equalizer,
        LClinicPort clinic,
        LNoisePort noise,
        LWhitebalancePort whitebalance,
        LFilterPort filter)
    {
        CInspectorTone = new CTone(tone);
        CInspectorEqualizer = new CEqualizer(equalizer);
        CInspectorClinic = new CClinic(clinic);
        CInspectorNoise = new CNoise(noise);
        CInspectorWhitebalance = new CWhitebalance(whitebalance);
        CInspectorHighpass = new CFilter(filter, true);
        CInspectorLowpass = new CFilter(filter, false);
    }

    public CTone CInspectorTone { get; }

    public CWhitebalance CInspectorWhitebalance { get; }

    public CGamma CInspectorGamma { get; } = new();

    public CExposure CInspectorExposure { get; } = new();

    public CVolume CInspectorVolume { get; } = new();

    public CCrop CInspectorCrop { get; } = new();

    public CEqualizer CInspectorEqualizer { get; }

    public CClinic CInspectorClinic { get; }

    public CNoise CInspectorNoise { get; }

    public CFilter CInspectorHighpass { get; }

    public CFilter CInspectorLowpass { get; }

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

    // An audio plan sets every audio tool, each from its read; a step the plan lacks reads as the Core default.
    public void CInspectorAudioAccept(LInspectorPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
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
    }
}
