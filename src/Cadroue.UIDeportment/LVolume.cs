using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LVolume
{
    private LWorkAudioStep lVolumeStep = LWorkAudioStep.LWorkVolumeCreate(false, 0);
    private bool lVolumePersistent;

    public event Action? LVolumeChange;

    public LWorkAudioStep LVolumeStep => lVolumeStep;

    public double LVolumeGain => ((LWorkVolumeStep)lVolumeStep).LWorkVolumeGain;

    public bool LVolumePersistent => lVolumePersistent;

    public void LVolumeStepSet(LWorkAudioStep lStep)
    {
        double lGain = lStep is LWorkVolumeStep lVolume ? lVolume.LWorkVolumeGain : 0;
        LWorkAudioStep lNormal = LWorkAudioStep.LWorkVolumeCreate(lStep.LWorkStepActive, lGain);
        if (lVolumeStep == lNormal)
        {
            return;
        }

        lVolumeStep = lNormal;
        LVolumeChange?.Invoke();
    }

    public void LVolumeActiveSet(bool lActive) =>
        LVolumeStepSet(LWorkAudioStep.LWorkVolumeCreate(lActive, LVolumeGain));

    public void LVolumeGainSet(double lGain) =>
        LVolumeStepSet(LWorkAudioStep.LWorkVolumeCreate(lVolumeStep.LWorkStepActive, lGain));

    public void LVolumePersistentSet(bool lPersistent)
    {
        if (lVolumePersistent == lPersistent)
        {
            return;
        }

        lVolumePersistent = lPersistent;
        LVolumeChange?.Invoke();
    }
}
