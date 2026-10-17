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
        QInspectorEqualizer = new QEqualizer(lInspector.CInspectorEqualizer);
        QInspectorNoise = new QNoise(lInspector.CInspectorNoise);
        QInspectorHighpass = new QFilter(lInspector.CInspectorHighpass);
        QInspectorLowpass = new QFilter(lInspector.CInspectorLowpass);
        QInspectorAudioAttach();
    }

    private void QInspectorAudioAttach()
    {
        QInspectorVolume.CVolumeChanged += QInspectorAudioRaise;
        QInspectorLoudness.QLoudnessChanged += QInspectorAudioRaise;
        QInspectorNoise.QNoiseChanged += QInspectorAudioRaise;
        qInspectorAudioGate.CInspectorHighpass.CFilterChanged += QInspectorAudioRaise;
        qInspectorAudioGate.CInspectorLowpass.CFilterChanged += QInspectorAudioRaise;
        QInspectorEqualizer.QEqualizerChanged += QInspectorAudioRaise;
    }

    public event Action? QInspectorAudioChanged;

    public CVolume QInspectorVolume { get; }

    public QLoudness QInspectorLoudness { get; } = new();

    public QNoise QInspectorNoise { get; }

    public QFilter QInspectorHighpass { get; }

    public QFilter QInspectorLowpass { get; }

    public QEqualizer QInspectorEqualizer { get; }

    public LWorkAudioStep QInspectorStepRead(LAudioKind lKind) => lKind switch
    {
        LAudioKind.LAudioKindLeveling => QInspectorLoudness.QLoudnessStep,
        LAudioKind.LAudioKindDenoise => QInspectorNoiseBuild(),
        LAudioKind.LAudioKindHighpass => QInspectorFilterBuild(qInspectorAudioGate.CInspectorHighpass, true),
        LAudioKind.LAudioKindLowpass => QInspectorFilterBuild(qInspectorAudioGate.CInspectorLowpass, false),
        LAudioKind.LAudioKindEqualizer => QInspectorEqualizerBuild(),
        _ => QInspectorVolumeBuild()
    };

    public LWorkAudio QInspectorAudioRead(IEnumerable<LAudioKind> lOrder) =>
        new(lOrder.Select(QInspectorStepRead).ToList()) { LWorkAudioSkip = qInspectorSkip.QSkipActive };

    public void QInspectorAudioApply(LWorkAudio lPlan)
    {
        qInspectorAudioDepth++;
        qInspectorAudioGate.CInspectorAudioAccept(new LInspectorOutlet(lPlan));
        QInspectorLoudness.QLoudnessStepSet(QInspectorStepResolve(lPlan, LAudioKind.LAudioKindLeveling));
        qInspectorAudioDepth--;
        QInspectorAudioChanged?.Invoke();
    }

    public bool QInspectorPersistentCheck() =>
        QInspectorVolume.CVolumePersistent
        || QInspectorLoudness.QLoudnessPersistent
        || QInspectorNoise.QNoisePersistent
        || qInspectorAudioGate.CInspectorHighpass.CFilterPersistent
        || qInspectorAudioGate.CInspectorLowpass.CFilterPersistent
        || QInspectorEqualizer.QEqualizerPersistent
        || qInspectorSkip.QSkipPersistent;

    public void QInspectorPersistentApply(LWorkAudio lPlan, bool lSkipPersistent)
    {
        foreach (LWorkAudioStep lStep in lPlan.LWorkAudioSteps)
        {
            switch (lStep.LWorkStepKind)
            {
                case LAudioKind.LAudioKindHighpass:
                    qInspectorAudioGate.CInspectorHighpass.CFilterPersistentSet(true);
                    break;
                case LAudioKind.LAudioKindLowpass:
                    qInspectorAudioGate.CInspectorLowpass.CFilterPersistentSet(true);
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
        if (qInspectorAudioGate.CInspectorHighpass.CFilterPersistent)
        {
            lSteps.Add(QInspectorFilterBuild(qInspectorAudioGate.CInspectorHighpass, true));
        }

        if (qInspectorAudioGate.CInspectorLowpass.CFilterPersistent)
        {
            lSteps.Add(QInspectorFilterBuild(qInspectorAudioGate.CInspectorLowpass, false));
        }

        if (QInspectorNoise.QNoisePersistent)
        {
            lSteps.Add(QInspectorNoiseBuild());
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
            lSteps.Add(QInspectorEqualizerBuild());
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

    // The engine builds the equalizer step from the gate's plain values, as the volume step above.
    private LWorkAudioStep QInspectorEqualizerBuild() =>
        LEqualizerOutlet.LEqualizerStepCreate(
            qInspectorAudioGate.CInspectorEqualizer.CEqualizerActive,
            qInspectorAudioGate.CInspectorEqualizer.CEqualizerBands);

    // The engine builds the noise step from the gate's plain values, as the equalizer step above.
    private LWorkAudioStep QInspectorNoiseBuild() =>
        LNoiseOutlet.LNoiseStepCreate(
            qInspectorAudioGate.CInspectorNoise.CNoiseActive,
            qInspectorAudioGate.CInspectorNoise.CNoiseValues,
            qInspectorAudioGate.CInspectorNoise.CNoiseTrack,
            qInspectorAudioGate.CInspectorNoise.CNoiseType);

    // The engine builds a pass step from its gate's plain values, as the noise step above.
    private static LWorkAudioStep QInspectorFilterBuild(CFilter lFilter, bool lHigh) =>
        LFilterOutlet.LFilterStepCreate(lHigh, lFilter.CFilterActive, lFilter.CFilterValues, lFilter.CFilterPoles);

    private static LWorkAudioStep QInspectorStepResolve(LWorkAudio lPlan, LAudioKind lKind) =>
        QInspectorStepFind(lPlan, lKind) ?? LAudio.LAudioNormalizeCreate();

    private static LWorkAudioStep? QInspectorStepFind(LWorkAudio lPlan, LAudioKind lKind) =>
        lPlan.LWorkAudioSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
}
