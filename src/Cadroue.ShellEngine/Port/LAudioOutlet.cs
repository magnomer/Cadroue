using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The persistent audio plan built from the tools' plain values, in plan order: high pass, low pass, noise, volume,
// leveling, equalizer, with the skip switch set. The plan comes back as a plan read, so the gate hands it on
// without naming the plan record.
public sealed class LAudioOutlet : LAudioPort
{
    public LInspectorPort LAudioPersistentCreate(
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles)? lHighpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles)? lLowpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, bool LAudioTrack, int LAudioType)? lNoise,
        (bool LAudioActive, double LAudioGain)? lVolume,
        (bool LAudioActive, bool LAudioDynamic, IReadOnlyList<double> LAudioValues, bool LAudioTwoPass)? lLeveling,
        (bool LAudioActive, IReadOnlyList<(double, double)> LAudioBands)? lEqualizer,
        bool lSkip)
    {
        var lSteps = new List<LWorkAudioStep>();
        if (lHighpass is { } lHigh)
        {
            lSteps.Add(LFilterOutlet.LFilterStepCreate(
                true, lHigh.LAudioActive, lHigh.LAudioValues, lHigh.LAudioPoles));
        }

        if (lLowpass is { } lLow)
        {
            lSteps.Add(LFilterOutlet.LFilterStepCreate(
                false, lLow.LAudioActive, lLow.LAudioValues, lLow.LAudioPoles));
        }

        if (lNoise is { } lDenoise)
        {
            lSteps.Add(LNoiseOutlet.LNoiseStepCreate(
                lDenoise.LAudioActive, lDenoise.LAudioValues, lDenoise.LAudioTrack, lDenoise.LAudioType));
        }

        if (lVolume is { } lGain)
        {
            lSteps.Add(LWorkAudioStep.LWorkVolumeCreate(lGain.LAudioActive, lGain.LAudioGain));
        }

        if (lLeveling is { } lLevel)
        {
            lSteps.Add(LLoudnessOutlet.LLoudnessStepCreate(
                lLevel.LAudioActive, lLevel.LAudioDynamic, lLevel.LAudioValues, lLevel.LAudioTwoPass));
        }

        if (lEqualizer is { } lBands)
        {
            lSteps.Add(LEqualizerOutlet.LEqualizerStepCreate(lBands.LAudioActive, lBands.LAudioBands));
        }

        return new LInspectorOutlet(new LWorkAudio(lSteps) { LWorkAudioSkip = lSkip });
    }
}
