using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LInspectorAudio
{
    private readonly LSkip lInspectorSkip;

    public LInspectorAudio(LSkip lSkip)
    {
        lInspectorSkip = lSkip;
        LInspectorVolume.LVolumeChange += LInspectorAudioRaise;
        LInspectorLoudness.LLoudnessChange += LInspectorAudioRaise;
        LInspectorNoise.LNoiseChange += LInspectorAudioRaise;
        LInspectorHighpass.LFilterChange += LInspectorAudioRaise;
        LInspectorLowpass.LFilterChange += LInspectorAudioRaise;
        LInspectorEqualizer.LEqualizerChange += LInspectorAudioRaise;
    }

    public event Action? LInspectorAudioChange;

    public LVolume LInspectorVolume { get; } = new();

    public LLoudness LInspectorLoudness { get; } = new();

    public LNoise LInspectorNoise { get; } = new();

    public LFilter LInspectorHighpass { get; } = new(true);

    public LFilter LInspectorLowpass { get; } = new(false);

    public LEqualizer LInspectorEqualizer { get; } = new();

    public LWorkAudioStep LInspectorStepRead(LAudioKind lKind) => lKind switch
    {
        LAudioKind.LAudioKindLeveling => LInspectorLoudness.LLoudnessStep,
        LAudioKind.LAudioKindDenoise => LInspectorNoise.LNoiseStep,
        LAudioKind.LAudioKindHighpass => LInspectorHighpass.LFilterStep,
        LAudioKind.LAudioKindLowpass => LInspectorLowpass.LFilterStep,
        LAudioKind.LAudioKindEqualizer => LInspectorEqualizer.LEqualizerStepRead(),
        _ => LInspectorVolume.LVolumeStep
    };

    public LWorkAudio LInspectorAudioRead(IEnumerable<LAudioKind> lOrder) =>
        new(lOrder.Select(LInspectorStepRead).ToList()) { LWorkAudioSkip = lInspectorSkip.LSkipActive };

    public void LInspectorAudioApply(LWorkAudio lPlan)
    {
        LInspectorHighpass.LFilterStepSet(LInspectorStepFind(lPlan, LAudioKind.LAudioKindHighpass)
            ?? LPassband.LPassbandStepCreate(true, false));
        LInspectorLowpass.LFilterStepSet(LInspectorStepFind(lPlan, LAudioKind.LAudioKindLowpass)
            ?? LPassband.LPassbandStepCreate(false, false));
        LInspectorNoise.LNoiseStepSet(LInspectorStepFind(lPlan, LAudioKind.LAudioKindDenoise)
            ?? LWorkAudioStep.LWorkNoiseCreate(false, 12, -50, false, LGrain.LGrainWhite, 6, 0.5, -38));
        LInspectorEqualizer.LEqualizerStepSet(LInspectorStepFind(lPlan, LAudioKind.LAudioKindEqualizer)
            ?? LWorkAudioStep.LWorkEqualizerCreate(false, LWorkEqualizerStep.LWorkBandsCreate()));
        LInspectorVolume.LVolumeStepSet(LInspectorStepFind(lPlan, LAudioKind.LAudioKindVolume)
            ?? LWorkAudioStep.LWorkVolumeCreate(false, 0));
        LInspectorLoudness.LLoudnessStepSet(LInspectorStepFind(lPlan, LAudioKind.LAudioKindLeveling)
            ?? LAudio.LAudioNormalizeCreate());
        LInspectorAudioRaise();
    }

    public bool LInspectorPersistentCheck() =>
        LInspectorVolume.LVolumePersistent
        || LInspectorLoudness.LLoudnessPersistent
        || LInspectorNoise.LNoisePersistent
        || LInspectorHighpass.LFilterPersistent
        || LInspectorLowpass.LFilterPersistent
        || LInspectorEqualizer.LEqualizerPersistent
        || lInspectorSkip.LSkipPersistent;

    public void LInspectorPersistentApply(LWorkAudio lPlan, bool lSkipPersistent)
    {
        foreach (LWorkAudioStep lStep in lPlan.LWorkAudioSteps)
        {
            switch (lStep.LWorkStepKind)
            {
                case LAudioKind.LAudioKindHighpass:
                    LInspectorHighpass.LFilterPersistentSet(true);
                    break;
                case LAudioKind.LAudioKindLowpass:
                    LInspectorLowpass.LFilterPersistentSet(true);
                    break;
                case LAudioKind.LAudioKindDenoise:
                    LInspectorNoise.LNoisePersistentSet(true);
                    break;
                case LAudioKind.LAudioKindVolume:
                    LInspectorVolume.LVolumePersistentSet(true);
                    break;
                case LAudioKind.LAudioKindLeveling:
                    LInspectorLoudness.LLoudnessPersistentSet(true);
                    break;
                case LAudioKind.LAudioKindEqualizer:
                    LInspectorEqualizer.LEqualizerPersistentSet(true);
                    break;
            }
        }

        lInspectorSkip.LSkipPersistentSet(lSkipPersistent);
    }

    public LWorkAudio LInspectorPersistentRead()
    {
        var lSteps = new List<LWorkAudioStep>();
        if (LInspectorHighpass.LFilterPersistent)
        {
            lSteps.Add(LInspectorHighpass.LFilterStep);
        }

        if (LInspectorLowpass.LFilterPersistent)
        {
            lSteps.Add(LInspectorLowpass.LFilterStep);
        }

        if (LInspectorNoise.LNoisePersistent)
        {
            lSteps.Add(LInspectorNoise.LNoiseStep);
        }

        if (LInspectorVolume.LVolumePersistent)
        {
            lSteps.Add(LInspectorVolume.LVolumeStep);
        }

        if (LInspectorLoudness.LLoudnessPersistent)
        {
            lSteps.Add(LInspectorLoudness.LLoudnessStep);
        }

        if (LInspectorEqualizer.LEqualizerPersistent)
        {
            lSteps.Add(LInspectorEqualizer.LEqualizerStepRead());
        }

        return new LWorkAudio(lSteps)
        {
            LWorkAudioSkip = lInspectorSkip.LSkipPersistent && lInspectorSkip.LSkipActive
        };
    }

    private void LInspectorAudioRaise() => LInspectorAudioChange?.Invoke();

    private static LWorkAudioStep? LInspectorStepFind(LWorkAudio lPlan, LAudioKind lKind) =>
        lPlan.LWorkAudioSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
}
