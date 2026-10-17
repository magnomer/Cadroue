using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The persistent audio plan built from the tools' plain values, in plan order: high pass, low pass, noise, volume,
// leveling, equalizer, with the skip switch set. The plan comes back as a plan read, so the gate hands it on
// without naming the plan record. A plan in the caller's order runs its processing step tokens instead.
public sealed class LAudioFacade
{
    private static readonly IReadOnlyDictionary<string, LAudioKind> _lAudioKinds = new Dictionary<string, LAudioKind>
    {
        ["Volume"] = LAudioKind.LAudioKindVolume,
        ["Normalize"] = LAudioKind.LAudioKindLeveling,
        ["Noise Reduction"] = LAudioKind.LAudioKindDenoise,
        ["High Pass"] = LAudioKind.LAudioKindHighpass,
        ["Low Pass"] = LAudioKind.LAudioKindLowpass,
        ["Equalizer"] = LAudioKind.LAudioKindEqualizer,
    };

    internal LAudioFacade()
    {
    }

    // The audio tool a processing step token names; none for a token that names no audio tool.
    internal static LAudioKind? LAudioKindRead(string lToken) =>
        _lAudioKinds.TryGetValue(lToken, out LAudioKind lKind) ? lKind : null;

    // One step per token that names an audio tool, in the tokens' order; a token that names none is passed over.
    public LInspectorPort LAudioPlanCreate(
        IReadOnlyList<string> lProcessing,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles) lHighpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, int LAudioPoles) lLowpass,
        (bool LAudioActive, IReadOnlyList<double> LAudioValues, bool LAudioTrack, int LAudioType) lNoise,
        (bool LAudioActive, double LAudioGain) lVolume,
        (bool LAudioActive, bool LAudioDynamic, IReadOnlyList<double> LAudioValues, bool LAudioTwoPass) lLeveling,
        (bool LAudioActive, IReadOnlyList<(double, double)> LAudioBands) lEqualizer,
        bool lSkip)
    {
        ArgumentNullException.ThrowIfNull(lProcessing);
        var lSteps = new List<LWorkAudioStep>();
        foreach (string lToken in lProcessing)
        {
            LWorkAudioStep? lStep = LAudioKindRead(lToken) switch
            {
                LAudioKind.LAudioKindLeveling => LLoudnessFacade.LLoudnessStepCreate(
                    lLeveling.LAudioActive, lLeveling.LAudioDynamic, lLeveling.LAudioValues, lLeveling.LAudioTwoPass),
                LAudioKind.LAudioKindDenoise => LNoiseFacade.LNoiseStepCreate(
                    lNoise.LAudioActive, lNoise.LAudioValues, lNoise.LAudioTrack, lNoise.LAudioType),
                LAudioKind.LAudioKindHighpass => LFilterFacade.LFilterStepCreate(
                    true, lHighpass.LAudioActive, lHighpass.LAudioValues, lHighpass.LAudioPoles),
                LAudioKind.LAudioKindLowpass => LFilterFacade.LFilterStepCreate(
                    false, lLowpass.LAudioActive, lLowpass.LAudioValues, lLowpass.LAudioPoles),
                LAudioKind.LAudioKindEqualizer => LEqualizerFacade.LEqualizerStepCreate(
                    lEqualizer.LAudioActive, lEqualizer.LAudioBands),
                LAudioKind.LAudioKindVolume => LWorkAudioStep.LWorkVolumeCreate(
                    lVolume.LAudioActive, lVolume.LAudioGain),
                _ => null
            };
            if (lStep is not null)
            {
                lSteps.Add(lStep);
            }
        }

        return new LInspectorOutlet(new LWorkAudio(lSteps) { LWorkAudioSkip = lSkip });
    }

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
            lSteps.Add(LFilterFacade.LFilterStepCreate(
                true, lHigh.LAudioActive, lHigh.LAudioValues, lHigh.LAudioPoles));
        }

        if (lLowpass is { } lLow)
        {
            lSteps.Add(LFilterFacade.LFilterStepCreate(
                false, lLow.LAudioActive, lLow.LAudioValues, lLow.LAudioPoles));
        }

        if (lNoise is { } lDenoise)
        {
            lSteps.Add(LNoiseFacade.LNoiseStepCreate(
                lDenoise.LAudioActive, lDenoise.LAudioValues, lDenoise.LAudioTrack, lDenoise.LAudioType));
        }

        if (lVolume is { } lGain)
        {
            lSteps.Add(LWorkAudioStep.LWorkVolumeCreate(lGain.LAudioActive, lGain.LAudioGain));
        }

        if (lLeveling is { } lLevel)
        {
            lSteps.Add(LLoudnessFacade.LLoudnessStepCreate(
                lLevel.LAudioActive, lLevel.LAudioDynamic, lLevel.LAudioValues, lLevel.LAudioTwoPass));
        }

        if (lEqualizer is { } lBands)
        {
            lSteps.Add(LEqualizerFacade.LEqualizerStepCreate(lBands.LAudioActive, lBands.LAudioBands));
        }

        return new LInspectorOutlet(new LWorkAudio(lSteps) { LWorkAudioSkip = lSkip });
    }
}
