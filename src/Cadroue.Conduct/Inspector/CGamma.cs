using System;
using System.Collections.Generic;

namespace Cadroue.Conduct;

// The gamma tool decisions: the switch and values it holds, each slot's range, the reset, and when a change is told.
// Slots run 0 to 4: midtone, red, green, blue, highlight protection. The owner of the video step converts it to
// these values and back.
public sealed class CGamma
{
    private const double _cGammaMost = 100;

    private static readonly IReadOnlyList<double> _cGammaLeast =
        Array.AsReadOnly(new double[] { -100, -100, -100, -100, 0 });

    private readonly double[] _cGammaValues = new double[5];
    private readonly IReadOnlyList<double> _cGammaValuesView;
    private bool _cGammaActive;
    private bool _cGammaPersistent;
    private bool _cGammaCapable;
    private bool _cGammaPreview;

    internal CGamma()
    {
        _cGammaValuesView = Array.AsReadOnly(_cGammaValues);
    }

    public event Action? CGammaChanged;

    public bool CGammaActive => _cGammaActive;

    public bool CGammaPersistent => _cGammaPersistent;

    public bool CGammaCapable => _cGammaCapable;

    public bool CGammaPreview => _cGammaPreview;

    public IReadOnlyList<double> CGammaLeasts => _cGammaLeast;

    public IReadOnlyList<double> CGammaValues => _cGammaValuesView;

    // Every value is held inside its slot's range; a step equal to the held one is not told.
    public void CGammaStepSet(bool active, IReadOnlyList<double> values)
    {
        double[] normal = new double[_cGammaValues.Length];
        for (int slot = 0; slot < normal.Length; slot++)
        {
            normal[slot] = Math.Clamp(values[slot], _cGammaLeast[slot], _cGammaMost);
        }

        CGammaApply(active, normal);
    }

    public void CGammaActiveSet(bool active) => CGammaApply(active, (double[])_cGammaValues.Clone());

    public void CGammaValueSet(int slot, double value)
    {
        double[] normal = (double[])_cGammaValues.Clone();
        normal[slot] = Math.Clamp(value, _cGammaLeast[slot], _cGammaMost);
        CGammaApply(_cGammaActive, normal);
    }

    public void CGammaReset() => CGammaApply(_cGammaActive, new double[_cGammaValues.Length]);

    private void CGammaApply(bool active, double[] normal)
    {
        if (_cGammaActive == active && _cGammaValues.AsSpan().SequenceEqual(normal))
        {
            return;
        }

        _cGammaActive = active;
        normal.CopyTo(_cGammaValues, 0);
        CGammaChanged?.Invoke();
    }

    public void CGammaPersistentSet(bool persistent)
    {
        if (_cGammaPersistent == persistent)
        {
            return;
        }

        _cGammaPersistent = persistent;
        CGammaChanged?.Invoke();
    }

    // The preview shows only when the editor can run gamma at all.
    public void CGammaCapableSet(bool capable, bool preview)
    {
        bool previewShown = capable && preview;
        if (_cGammaCapable == capable && _cGammaPreview == previewShown)
        {
            return;
        }

        _cGammaCapable = capable;
        _cGammaPreview = previewShown;
        CGammaChanged?.Invoke();
    }
}
