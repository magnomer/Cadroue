using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadroue.Conduct;

// The curve tool decisions: the switch, the points of each channel and the keep switch it holds, and when a change is
// told. Channels run 0 to 3: master, red, green, blue. A point is its input and output, each from 0 to 1. Each channel
// is replaced whole, so a points list once answered never changes. The owner of the video step converts it to these
// values and back.
public sealed class CCurve
{
    private static readonly IReadOnlyList<(double, double)> _cCurveIdentity =
        Array.AsReadOnly(new (double, double)[] { (0, 0), (1, 1) });

    private readonly IReadOnlyList<(double, double)>[] _cCurveChannels =
    {
        _cCurveIdentity, _cCurveIdentity, _cCurveIdentity, _cCurveIdentity
    };

    private readonly IReadOnlyList<IReadOnlyList<(double, double)>> _cCurveChannelsView;
    private bool _cCurveActive;
    private bool _cCurvePersistent;

    internal CCurve()
    {
        _cCurveChannelsView = Array.AsReadOnly(_cCurveChannels);
    }

    public event Action? CCurveChanged;

    public bool CCurveActive => _cCurveActive;

    public bool CCurvePersistent => _cCurvePersistent;

    public IReadOnlyList<IReadOnlyList<(double, double)>> CCurveChannels => _cCurveChannelsView;

    // A step equal to the held one is not told; points that match the held ones keep the held lists.
    public void CCurveStepSet(bool active, IReadOnlyList<IReadOnlyList<(double, double)>> channels)
    {
        ArgumentNullException.ThrowIfNull(channels);
        bool pointsChanged = Enumerable.Range(0, _cCurveChannels.Length)
            .Any(channel => !_cCurveChannels[channel].SequenceEqual(channels[channel]));
        if (!pointsChanged && _cCurveActive == active)
        {
            return;
        }

        _cCurveActive = active;
        if (pointsChanged)
        {
            for (int channel = 0; channel < _cCurveChannels.Length; channel++)
            {
                _cCurveChannels[channel] = Array.AsReadOnly(channels[channel].ToArray());
            }
        }

        CCurveChanged?.Invoke();
    }

    public void CCurveActiveSet(bool active)
    {
        if (_cCurveActive == active)
        {
            return;
        }

        _cCurveActive = active;
        CCurveChanged?.Invoke();
    }

    // The channel takes the points whole; the change is told even when they match the held ones.
    public void CCurveChannelSet(int channel, IReadOnlyList<(double, double)> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        _cCurveChannels[channel] = Array.AsReadOnly(points.ToArray());
        CCurveChanged?.Invoke();
    }

    public void CCurveChannelReset(int channel)
    {
        _cCurveChannels[channel] = _cCurveIdentity;
        CCurveChanged?.Invoke();
    }

    // Every channel goes back to the straight line; the switch keeps its state.
    public void CCurveReset()
    {
        Array.Fill(_cCurveChannels, _cCurveIdentity);
        CCurveChanged?.Invoke();
    }

    public void CCurvePersistentSet(bool persistent)
    {
        if (_cCurvePersistent == persistent)
        {
            return;
        }

        _cCurvePersistent = persistent;
        CCurveChanged?.Invoke();
    }
}
