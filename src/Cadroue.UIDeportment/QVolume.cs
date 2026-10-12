using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QVolume
{
    private LWorkAudioStep qVolumeStep = LWorkAudioStep.LWorkVolumeCreate(false, 0);
    private bool qVolumePersistent;

    public event Action? QVolumeChanged;

    public LWorkAudioStep QVolumeStep => qVolumeStep;

    public double QVolumeGain => ((LWorkVolumeStep)qVolumeStep).LWorkVolumeGain;

    public bool QVolumePersistent => qVolumePersistent;

    public bool QVolumeClipShown => QVolumeGain > 0;

    public void QVolumeStepSet(LWorkAudioStep lStep)
    {
        double lGain = lStep is LWorkVolumeStep lVolume ? lVolume.LWorkVolumeGain : 0;
        LWorkAudioStep lNormal = LWorkAudioStep.LWorkVolumeCreate(lStep.LWorkStepActive, lGain);
        if (qVolumeStep == lNormal)
        {
            return;
        }

        qVolumeStep = lNormal;
        QVolumeChanged?.Invoke();
    }

    public void QVolumeActiveSet(bool lActive) =>
        QVolumeStepSet(LWorkAudioStep.LWorkVolumeCreate(lActive, QVolumeGain));

    public void QVolumeGainSet(double lGain) =>
        QVolumeStepSet(LWorkAudioStep.LWorkVolumeCreate(qVolumeStep.LWorkStepActive, lGain));

    public void QVolumePersistentSet(bool lPersistent)
    {
        if (qVolumePersistent == lPersistent)
        {
            return;
        }

        qVolumePersistent = lPersistent;
        QVolumeChanged?.Invoke();
    }
}
