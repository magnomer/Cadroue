using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The tone tool decisions: each slot's switch, value and persistence, its range and origin, the editor fact, whether
// the contrast notice shows, and when a change is told. Slots run 0 to 2: brightness, contrast, saturation. The owner
// of the video steps converts them to these values and back.
public sealed class CTone
{
    private static readonly IReadOnlyList<double> _cToneLeast = Array.AsReadOnly(new double[] { -100, 0, 0 });

    private static readonly IReadOnlyList<double> _cToneMost = Array.AsReadOnly(new double[] { 100, 200, 200 });

    private static readonly IReadOnlyList<double> _cToneOrigin = Array.AsReadOnly(new double[] { 0, 100, 100 });

    private readonly LTonePort _cTonePort;
    private readonly bool[] _cToneActive = new bool[3];
    private readonly double[] _cToneValue = { 0, 100, 100 };
    private readonly bool[] _cTonePersistent = new bool[3];
    private bool _cToneCapable = true;

    internal CTone(LTonePort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cTonePort = port;
        CToneActives = Array.AsReadOnly(_cToneActive);
        CToneValues = Array.AsReadOnly(_cToneValue);
        CTonePersistents = Array.AsReadOnly(_cTonePersistent);
    }

    public event Action? CToneChanged;

    public IReadOnlyList<bool> CToneActives { get; }

    public IReadOnlyList<double> CToneValues { get; }

    public IReadOnlyList<bool> CTonePersistents { get; }

    public bool CToneCapable => _cToneCapable;

    public IReadOnlyList<double> CToneLeasts => _cToneLeast;

    public IReadOnlyList<double> CToneMosts => _cToneMost;

    public IReadOnlyList<double> CToneOrigins => _cToneOrigin;

    // The contrast notice shows when the preview runs on neither Flyleaf nor mpv.
    public bool CToneNoticeShown
    {
        get
        {
            (bool flyleaf, bool mpv) = _cTonePort.LTonePreviewRead();
            return !flyleaf && !mpv;
        }
    }

    // Every slot takes its step at once; a step equal to the held one is not told.
    public void CToneStepSet(IReadOnlyList<bool> actives, IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(actives);
        ArgumentNullException.ThrowIfNull(values);
        bool changed = false;
        for (int slot = 0; slot < _cToneValue.Length; slot++)
        {
            changed |= CToneApply(slot, actives[slot], CToneNormalize(slot, values[slot]));
        }

        CToneRaise(changed);
    }

    public void CToneActiveSet(int slot, bool active) => CToneRaise(CToneApply(slot, active, _cToneValue[slot]));

    // Brightness is held as given; contrast and saturation are held inside their range.
    public void CToneValueSet(int slot, double value) =>
        CToneRaise(CToneApply(slot, _cToneActive[slot], CToneNormalize(slot, value)));

    public void CToneBrightnessSet(double value) => CToneValueSet(0, value);

    public void CToneContrastSet(double value) => CToneValueSet(1, value);

    public void CToneSaturationSet(double value) => CToneValueSet(2, value);

    public void CTonePersistentSet(int slot, bool persistent)
    {
        if (_cTonePersistent[slot] == persistent)
        {
            return;
        }

        _cTonePersistent[slot] = persistent;
        CToneChanged?.Invoke();
    }

    public void CToneCapableSet(bool capable)
    {
        if (_cToneCapable == capable)
        {
            return;
        }

        _cToneCapable = capable;
        CToneChanged?.Invoke();
    }

    private static double CToneNormalize(int slot, double value) =>
        slot == 0 ? value : Math.Clamp(value, _cToneLeast[slot], _cToneMost[slot]);

    private bool CToneApply(int slot, bool active, double normal)
    {
        if (_cToneActive[slot] == active && _cToneValue[slot].Equals(normal))
        {
            return false;
        }

        _cToneActive[slot] = active;
        _cToneValue[slot] = normal;
        return true;
    }

    private void CToneRaise(bool changed)
    {
        if (changed)
        {
            CToneChanged?.Invoke();
        }
    }
}
