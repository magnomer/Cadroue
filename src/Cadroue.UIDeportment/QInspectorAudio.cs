using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QInspectorAudio
{
    private readonly QSkip qInspectorSkip;
    private readonly CInspector qInspectorAudioGate;
    private int qInspectorAudioDepth;

    public QInspectorAudio(QSkip lSkip, CInspector lInspector)
    {
        qInspectorSkip = lSkip;
        qInspectorAudioGate = lInspector;
        QInspectorVolume = lInspector.CInspectorVolume;
        QInspectorVolume.CVolumeChanged += QInspectorAudioRaise;
        QInspectorLoudness.QLoudnessChanged += QInspectorAudioRaise;
        QInspectorNoise.QNoiseChanged += QInspectorAudioRaise;
        QInspectorHighpass.QFilterChanged += QInspectorAudioRaise;
        QInspectorLowpass.QFilterChanged += QInspectorAudioRaise;
        QInspectorEqualizer.QEqualizerChanged += QInspectorAudioRaise;
    }

    public event Action? QInspectorAudioChanged;

    public CVolume QInspectorVolume { get; }

    public QLoudness QInspectorLoudness { get; } = new();

    public QNoise QInspectorNoise { get; } = new();

    public QFilter QInspectorHighpass { get; } = new(true);

    public QFilter QInspectorLowpass { get; } = new(false);

    public QEqualizer QInspectorEqualizer { get; } = new();

    public LWorkAudioStep QInspectorStepRead(LAudioKind lKind) => lKind switch
    {
        LAudioKind.LAudioKindLeveling => QInspectorLoudness.QLoudnessStep,
        LAudioKind.LAudioKindDenoise => QInspectorNoise.QNoiseStep,
        LAudioKind.LAudioKindHighpass => QInspectorHighpass.QFilterStep,
        LAudioKind.LAudioKindLowpass => QInspectorLowpass.QFilterStep,
        LAudioKind.LAudioKindEqualizer => QInspectorEqualizer.QEqualizerStepRead(),
        _ => QInspectorVolumeBuild()
    };

    public LWorkAudio QInspectorAudioRead(IEnumerable<LAudioKind> lOrder) =>
        new(lOrder.Select(QInspectorStepRead).ToList()) { LWorkAudioSkip = qInspectorSkip.QSkipActive };

    public void QInspectorAudioApply(LWorkAudio lPlan)
    {
        qInspectorAudioDepth++;
        QInspectorHighpass.QFilterStepSet(QInspectorStepFind(lPlan, LAudioKind.LAudioKindHighpass)
            ?? LPassband.LPassbandStepCreate(true, false));
        QInspectorLowpass.QFilterStepSet(QInspectorStepFind(lPlan, LAudioKind.LAudioKindLowpass)
            ?? LPassband.LPassbandStepCreate(false, false));
        QInspectorNoise.QNoiseStepSet(QInspectorStepFind(lPlan, LAudioKind.LAudioKindDenoise)
            ?? LWorkAudioStep.LWorkNoiseCreate(false, 12, -50, false, LGrain.LGrainWhite, 6, 0.5, -38));
        QInspectorEqualizer.QEqualizerStepSet(QInspectorStepFind(lPlan, LAudioKind.LAudioKindEqualizer)
            ?? LWorkAudioStep.LWorkEqualizerCreate(false, LWorkEqualizerStep.LWorkBandsCreate()));
        qInspectorAudioGate.CInspectorAudioAccept(new LInspectorOutlet(lPlan));
        QInspectorLoudness.QLoudnessStepSet(QInspectorStepFind(lPlan, LAudioKind.LAudioKindLeveling)
            ?? LAudio.LAudioNormalizeCreate());
        qInspectorAudioDepth--;
        QInspectorAudioChanged?.Invoke();
    }

    public bool QInspectorPersistentCheck() =>
        QInspectorVolume.CVolumePersistent
        || QInspectorLoudness.QLoudnessPersistent
        || QInspectorNoise.QNoisePersistent
        || QInspectorHighpass.QFilterPersistent
        || QInspectorLowpass.QFilterPersistent
        || QInspectorEqualizer.QEqualizerPersistent
        || qInspectorSkip.QSkipPersistent;

    public void QInspectorPersistentApply(LWorkAudio lPlan, bool lSkipPersistent)
    {
        foreach (LWorkAudioStep lStep in lPlan.LWorkAudioSteps)
        {
            switch (lStep.LWorkStepKind)
            {
                case LAudioKind.LAudioKindHighpass:
                    QInspectorHighpass.QFilterPersistentSet(true);
                    break;
                case LAudioKind.LAudioKindLowpass:
                    QInspectorLowpass.QFilterPersistentSet(true);
                    break;
                case LAudioKind.LAudioKindDenoise:
                    QInspectorNoise.QNoisePersistentSet(true);
                    break;
                case LAudioKind.LAudioKindVolume:
                    QInspectorVolume.CVolumePersistentSet(true);
                    break;
                case LAudioKind.LAudioKindLeveling:
                    QInspectorLoudness.QLoudnessPersistentSet(true);
                    break;
                case LAudioKind.LAudioKindEqualizer:
                    QInspectorEqualizer.QEqualizerPersistentSet(true);
                    break;
            }
        }

        qInspectorSkip.QSkipPersistentSet(lSkipPersistent);
    }

    public LWorkAudio QInspectorPersistentRead()
    {
        var lSteps = new List<LWorkAudioStep>();
        if (QInspectorHighpass.QFilterPersistent)
        {
            lSteps.Add(QInspectorHighpass.QFilterStep);
        }

        if (QInspectorLowpass.QFilterPersistent)
        {
            lSteps.Add(QInspectorLowpass.QFilterStep);
        }

        if (QInspectorNoise.QNoisePersistent)
        {
            lSteps.Add(QInspectorNoise.QNoiseStep);
        }

        if (QInspectorVolume.CVolumePersistent)
        {
            lSteps.Add(QInspectorVolumeBuild());
        }

        if (QInspectorLoudness.QLoudnessPersistent)
        {
            lSteps.Add(QInspectorLoudness.QLoudnessStep);
        }

        if (QInspectorEqualizer.QEqualizerPersistent)
        {
            lSteps.Add(QInspectorEqualizer.QEqualizerStepRead());
        }

        return new LWorkAudio(lSteps)
        {
            LWorkAudioSkip = qInspectorSkip.QSkipPersistent && qInspectorSkip.QSkipActive
        };
    }

    private void QInspectorAudioRaise()
    {
        if (qInspectorAudioDepth > 0)
        {
            return;
        }

        QInspectorAudioChanged?.Invoke();
    }

    // The volume gate holds plain values; the step is built from them here. A plan is read back through the gate.
    private LWorkAudioStep QInspectorVolumeBuild() =>
        LWorkAudioStep.LWorkVolumeCreate(QInspectorVolume.CVolumeActive, QInspectorVolume.CVolumeGain);

    private static LWorkAudioStep? QInspectorStepFind(LWorkAudio lPlan, LAudioKind lKind) =>
        lPlan.LWorkAudioSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
}
