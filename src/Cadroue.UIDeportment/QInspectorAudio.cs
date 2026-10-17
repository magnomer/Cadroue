using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QInspectorAudio
{
    private readonly CInspector qInspectorAudioGate;
    private int qInspectorAudioDepth;

    public QInspectorAudio(CInspector lInspector)
    {
        qInspectorAudioGate = lInspector;
        QInspectorVolume = lInspector.CInspectorVolume;
        QInspectorLoudness = new QLoudness(lInspector.CInspectorLoudness);
        QInspectorEqualizer = new QEqualizer(lInspector.CInspectorEqualizer);
        QInspectorNoise = new QNoise(lInspector.CInspectorNoise);
        QInspectorHighpass = new QFilter(lInspector.CInspectorHighpass);
        QInspectorLowpass = new QFilter(lInspector.CInspectorLowpass);
        QInspectorAudioAttach();
    }

    private void QInspectorAudioAttach()
    {
        QInspectorVolume.CVolumeChanged += QInspectorAudioRaise;
        qInspectorAudioGate.CInspectorLoudness.CLoudnessChanged += QInspectorAudioRaise;
        QInspectorNoise.QNoiseChanged += QInspectorAudioRaise;
        qInspectorAudioGate.CInspectorHighpass.CFilterChanged += QInspectorAudioRaise;
        qInspectorAudioGate.CInspectorLowpass.CFilterChanged += QInspectorAudioRaise;
        QInspectorEqualizer.QEqualizerChanged += QInspectorAudioRaise;
    }

    public event Action? QInspectorAudioChanged;

    public CVolume QInspectorVolume { get; }

    public QLoudness QInspectorLoudness { get; }

    public QNoise QInspectorNoise { get; }

    public QFilter QInspectorHighpass { get; }

    public QFilter QInspectorLowpass { get; }

    public QEqualizer QInspectorEqualizer { get; }

    public LWorkAudioStep QInspectorStepRead(LAudioKind lKind) => lKind switch
    {
        LAudioKind.LAudioKindLeveling => QInspectorLevelingBuild(),
        LAudioKind.LAudioKindDenoise => QInspectorNoiseBuild(),
        LAudioKind.LAudioKindHighpass => QInspectorFilterBuild(qInspectorAudioGate.CInspectorHighpass, true),
        LAudioKind.LAudioKindLowpass => QInspectorFilterBuild(qInspectorAudioGate.CInspectorLowpass, false),
        LAudioKind.LAudioKindEqualizer => QInspectorEqualizerBuild(),
        _ => QInspectorVolumeBuild()
    };

    public LWorkAudio QInspectorAudioRead(IEnumerable<LAudioKind> lOrder) =>
        new(lOrder.Select(QInspectorStepRead).ToList())
        {
            LWorkAudioSkip = qInspectorAudioGate.CInspectorSkip.CSkipActive
        };

    public void QInspectorAudioApply(LWorkAudio lPlan)
    {
        qInspectorAudioDepth++;
        qInspectorAudioGate.CInspectorAudioAccept(new LInspectorOutlet(lPlan));
        qInspectorAudioDepth--;
        QInspectorAudioChanged?.Invoke();
    }

    public bool QInspectorPersistentCheck() =>
        QInspectorVolume.CVolumePersistent
        || qInspectorAudioGate.CInspectorLoudness.CLoudnessPersistent
        || QInspectorNoise.QNoisePersistent
        || qInspectorAudioGate.CInspectorHighpass.CFilterPersistent
        || qInspectorAudioGate.CInspectorLowpass.CFilterPersistent
        || QInspectorEqualizer.QEqualizerPersistent
        || qInspectorAudioGate.CInspectorSkip.CSkipPersistent;

    // The plan was applied just before: the gate keeps each gated tool of the plan it holds, then the skip switch.
    public void QInspectorPersistentApply(bool lSkipPersistent) =>
        qInspectorAudioGate.CInspectorPersistentAccept(lSkipPersistent);

    // The gate answers the kept plan of its tools with the kept skip switch.
    public LWorkAudio QInspectorPersistentRead() =>
        qInspectorAudioGate.CInspectorPersistentRead().LInspectorAudioRead();

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

    // The engine builds the leveling step from the gate's plain values, as the noise step below.
    private LWorkAudioStep QInspectorLevelingBuild() =>
        LLoudnessOutlet.LLoudnessStepCreate(
            qInspectorAudioGate.CInspectorLoudness.CLoudnessActive,
            qInspectorAudioGate.CInspectorLoudness.CLoudnessDynamic,
            qInspectorAudioGate.CInspectorLoudness.CLoudnessValues,
            qInspectorAudioGate.CInspectorLoudness.CLoudnessTwoPass);

    // The engine builds a pass step from its gate's plain values, as the noise step above.
    private static LWorkAudioStep QInspectorFilterBuild(CFilter lFilter, bool lHigh) =>
        LFilterOutlet.LFilterStepCreate(lHigh, lFilter.CFilterActive, lFilter.CFilterValues, lFilter.CFilterPoles);
}
