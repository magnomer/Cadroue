using System;

namespace Cadroue.Conduct;

// The exposure tool decisions: the switch and stops it holds, their range, and when a change is told. The owner of
// the video step converts it to these values and back.
public sealed class CExposure
{
    private const double _cExposureLeast = -3;
    private const double _cExposureMost = 3;

    private bool _cExposureActive;
    private double _cExposureValue;
    private bool _cExposurePersistent;
    private bool _cExposureCapable;
    private bool _cExposurePreview;

    internal CExposure()
    {
    }

    public event Action? CExposureChanged;

    public bool CExposureActive => _cExposureActive;

    public double CExposureValue => _cExposureValue;

    public bool CExposurePersistent => _cExposurePersistent;

    public bool CExposureCapable => _cExposureCapable;

    public bool CExposurePreview => _cExposurePreview;

    // The value is held inside its range; a step equal to the held one is not told.
    public void CExposureStepSet(bool active, double value)
    {
        double normal = Math.Clamp(value, _cExposureLeast, _cExposureMost);
        if (_cExposureActive == active && _cExposureValue.Equals(normal))
        {
            return;
        }

        _cExposureActive = active;
        _cExposureValue = normal;
        CExposureChanged?.Invoke();
    }

    public void CExposureActiveSet(bool active) => CExposureStepSet(active, _cExposureValue);

    public void CExposureValueSet(double value) => CExposureStepSet(_cExposureActive, value);

    public void CExposurePersistentSet(bool persistent)
    {
        if (_cExposurePersistent == persistent)
        {
            return;
        }

        _cExposurePersistent = persistent;
        CExposureChanged?.Invoke();
    }

    // The preview shows only when the editor can run exposure at all.
    public void CExposureCapableSet(bool capable, bool preview)
    {
        bool previewShown = capable && preview;
        if (_cExposureCapable == capable && _cExposurePreview == previewShown)
        {
            return;
        }

        _cExposureCapable = capable;
        _cExposurePreview = previewShown;
        CExposureChanged?.Invoke();
    }
}
