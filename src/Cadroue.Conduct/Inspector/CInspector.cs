using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The inspector's gate: one decision object per tool, each exposed as one property. CAtelier makes one per inspector.
// Every decision that spans its tools is one member here, so an owner asks once whatever the number of tools. The
// clinic is the fix tab's inspector, so it is one more tool here. The audio and video plans are each one object.
public sealed class CInspector
{
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
        CInspectorAudio = new CInspectorAudio(audio, this);
        CInspectorVideo = new CInspectorVideo(video, this);
        CInspectorTone = new CTone(tone);
        CInspectorEqualizer = new CEqualizer(equalizer);
        CInspectorClinic = new CClinic(clinic);
        CInspectorNoise = new CNoise(noise);
        CInspectorWhitebalance = new CWhitebalance(whitebalance);
        CInspectorHighpass = new CFilter(filter, true);
        CInspectorLowpass = new CFilter(filter, false);
        CInspectorLoudness = new CLoudness(loudness);
    }

    public CInspectorAudio CInspectorAudio { get; }

    public CInspectorVideo CInspectorVideo { get; }

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

    // An edit plan sets the crop and its ratio, then every video tool as a video plan, then the skip switch; a plan
    // that keeps the skip switch keeps it as told, last.
    public void CInspectorEditAccept(LInspectorPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        CInspectorCrop.CCropPlanAccept(plan);
        CInspectorVideo.CInspectorVideoAccept(plan);
        CInspectorSkip.CSkipActiveSet(plan.LInspectorSkipRead());
        if (plan.LInspectorKeepRead().LInspectorSkip is bool skip)
        {
            CInspectorSkip.CSkipPersistentSet(skip);
        }
    }
}
