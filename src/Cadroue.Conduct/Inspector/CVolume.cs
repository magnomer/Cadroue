using System;

namespace Cadroue.Conduct;

// The volume tool decisions: the switch and gain it holds, their range, when the clip notice shows and when a change
// is told. The owner of the audio step converts it to these values and back.
public sealed class CVolume
{
    private const double _cVolumeLeast = -24;
    private const double _cVolumeMost = 24;

    private bool _cVolumeActive;
    private double _cVolumeGain;
    private bool _cVolumePersistent;

    internal CVolume()
    {
    }

    public event Action? CVolumeChanged;

    public bool CVolumeActive => _cVolumeActive;

    public double CVolumeGain => _cVolumeGain;

    public bool CVolumePersistent => _cVolumePersistent;

    public bool CVolumeClipShown => _cVolumeGain > 0;

    // The gain is held inside its range; a step equal to the held one is not told.
    public void CVolumeStepSet(bool active, double gain)
    {
        double normal = Math.Clamp(gain, _cVolumeLeast, _cVolumeMost);
        if (_cVolumeActive == active && _cVolumeGain.Equals(normal))
        {
            return;
        }

        _cVolumeActive = active;
        _cVolumeGain = normal;
        CVolumeChanged?.Invoke();
    }

    public void CVolumeActiveSet(bool active) => CVolumeStepSet(active, _cVolumeGain);

    public void CVolumeGainSet(double gain) => CVolumeStepSet(_cVolumeActive, gain);

    public void CVolumePersistentSet(bool persistent)
    {
        if (_cVolumePersistent == persistent)
        {
            return;
        }

        _cVolumePersistent = persistent;
        CVolumeChanged?.Invoke();
    }
}
