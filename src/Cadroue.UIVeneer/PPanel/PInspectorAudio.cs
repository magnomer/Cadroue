using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    public event Action? PInspectorAudioChange;

    public LVolume LVolume { get; } = new();

    public LLoudness LLoudness { get; } = new();

    public LNoise LNoise { get; } = new();

    public LFilter LFilterHigh { get; } = new(true);

    public LFilter LFilterLow { get; } = new(false);

    public LEqualizer LEqualizer { get; } = new();

    public LSkip LSkip { get; } = new();

    private void PInspectorAudioAttach()
    {
        LVolume.LVolumeChange += PVolumeUpdate;
        LVolume.LVolumeChange += PInspectorActiveRaise;
        LLoudness.LLoudnessChange += PLoudnessUpdate;
        LLoudness.LLoudnessChange += PInspectorActiveRaise;
        LNoise.LNoiseChange += PNoiseUpdate;
        LNoise.LNoiseChange += PInspectorActiveRaise;
        LFilterHigh.LFilterChange += () => PFilterUpdate(pInspectorHighPass);
        LFilterHigh.LFilterChange += PInspectorActiveRaise;
        LFilterLow.LFilterChange += () => PFilterUpdate(pInspectorLowPass);
        LFilterLow.LFilterChange += PInspectorActiveRaise;
        LEqualizer.LEqualizerChange += PEqualizerUpdate;
        LEqualizer.LEqualizerChange += PInspectorActiveRaise;
        LSkip.LSkipChange += PSkipUpdate;
        PVolumeUpdate();
        PLoudnessUpdate();
        PNoiseUpdate();
        PFilterUpdate(pInspectorHighPass);
        PFilterUpdate(pInspectorLowPass);
        PEqualizerUpdate();
        PSkipUpdate();
    }

    private void PInspectorActiveRaise() => PInspectorAudioChange?.Invoke();

    public LWorkAudioStep PInspectorStepRead(LAudioKind pStepKind) => pStepKind switch
    {
        LAudioKind.LAudioKindLeveling => LLoudness.LLoudnessStep,
        LAudioKind.LAudioKindDenoise => LNoise.LNoiseStep,
        LAudioKind.LAudioKindHighpass => LFilterHigh.LFilterStep,
        LAudioKind.LAudioKindLowpass => LFilterLow.LFilterStep,
        LAudioKind.LAudioKindEqualizer => LEqualizer.LEqualizerStepRead(),
        _ => LVolume.LVolumeStep
    };

    public void PInspectorPlanApply(LWorkAudio pInspectorPlan)
    {
        LFilterHigh.LFilterStepSet(PInspectorStepFind(pInspectorPlan, LAudioKind.LAudioKindHighpass)
            ?? LPassband.LPassbandStepCreate(true, false));
        LFilterLow.LFilterStepSet(PInspectorStepFind(pInspectorPlan, LAudioKind.LAudioKindLowpass)
            ?? LPassband.LPassbandStepCreate(false, false));
        LNoise.LNoiseStepSet(PInspectorStepFind(pInspectorPlan, LAudioKind.LAudioKindDenoise)
            ?? LWorkAudioStep.LWorkNoiseCreate(false, 12, -50, false, LGrain.LGrainWhite, 6, 0.5, -38));
        LEqualizer.LEqualizerStepSet(PInspectorStepFind(pInspectorPlan, LAudioKind.LAudioKindEqualizer)
            ?? LWorkAudioStep.LWorkEqualizerCreate(false, LWorkEqualizerStep.LWorkBandsCreate()));
        LVolume.LVolumeStepSet(PInspectorStepFind(pInspectorPlan, LAudioKind.LAudioKindVolume)
            ?? LWorkAudioStep.LWorkVolumeCreate(false, 0));
        LLoudness.LLoudnessStepSet(PInspectorStepFind(pInspectorPlan, LAudioKind.LAudioKindLeveling)
            ?? LAudio.LAudioNormalizeCreate());
        PInspectorActiveRaise();
    }

    private static LWorkAudioStep? PInspectorStepFind(LWorkAudio pPlan, LAudioKind pKind) =>
        pPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == pKind);

    public void PInspectorMediaReset() => PInspectorPlanApply(PInspectorPersistentRead());

    public bool PInspectorPersistentCheck() =>
        LVolume.LVolumePersistent
        || LLoudness.LLoudnessPersistent
        || LNoise.LNoisePersistent
        || LFilterHigh.LFilterPersistent
        || LFilterLow.LFilterPersistent
        || LEqualizer.LEqualizerPersistent
        || LSkip.LSkipPersistent;

    public void PInspectorPersistentApply(LWorkAudio pInspectorPlan, bool pSkipPersistent)
    {
        foreach (LWorkAudioStep pStep in pInspectorPlan.LWorkAudioSteps)
        {
            switch (pStep.LWorkStepKind)
            {
                case LAudioKind.LAudioKindHighpass:
                    LFilterHigh.LFilterPersistentSet(true);
                    break;
                case LAudioKind.LAudioKindLowpass:
                    LFilterLow.LFilterPersistentSet(true);
                    break;
                case LAudioKind.LAudioKindDenoise:
                    LNoise.LNoisePersistentSet(true);
                    break;
                case LAudioKind.LAudioKindVolume:
                    LVolume.LVolumePersistentSet(true);
                    break;
                case LAudioKind.LAudioKindLeveling:
                    LLoudness.LLoudnessPersistentSet(true);
                    break;
                case LAudioKind.LAudioKindEqualizer:
                    LEqualizer.LEqualizerPersistentSet(true);
                    break;
            }
        }

        LSkip.LSkipPersistentSet(pSkipPersistent);
    }

    public LWorkAudio PInspectorPersistentRead()
    {
        var pSteps = new List<LWorkAudioStep>();
        if (LFilterHigh.LFilterPersistent)
        {
            pSteps.Add(LFilterHigh.LFilterStep);
        }

        if (LFilterLow.LFilterPersistent)
        {
            pSteps.Add(LFilterLow.LFilterStep);
        }

        if (LNoise.LNoisePersistent)
        {
            pSteps.Add(LNoise.LNoiseStep);
        }

        if (LVolume.LVolumePersistent)
        {
            pSteps.Add(LVolume.LVolumeStep);
        }

        if (LLoudness.LLoudnessPersistent)
        {
            pSteps.Add(LLoudness.LLoudnessStep);
        }

        if (LEqualizer.LEqualizerPersistent)
        {
            pSteps.Add(LEqualizer.LEqualizerStepRead());
        }

        return new LWorkAudio(pSteps)
        {
            LWorkAudioSkip = LSkip.LSkipPersistent && LSkip.LSkipActive
        };
    }
}
