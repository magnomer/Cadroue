using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadroue.Conduct;

// The curve tool decisions: the switch, shown channel and point, the points of each channel, the keep switch, and when
// a change is told. Channels run 0 to 3: master, red, green, blue. A point is its input and output, each from 0 to 1.
// Each channel is replaced whole, so a points list once answered never changes.
public sealed class CCurve
{
    private const double CCurveGap = 1.0 / 150;
    private static readonly IReadOnlyList<(double, double)> _cCurveIdentity =
        Array.AsReadOnly(new (double, double)[] { (0, 0), (1, 1) });

    private readonly IReadOnlyList<(double, double)>[] _cCurveChannels;

    private readonly IReadOnlyList<IReadOnlyList<(double, double)>> _cCurveChannelsView;
    private bool _cCurveActive;
    private bool _cCurvePersistent;
    private int _cCurveChannel;
    private int _cCurveSelected = 1;

    internal CCurve()
    {
        _cCurveChannels = [_cCurveIdentity, _cCurveIdentity, _cCurveIdentity, _cCurveIdentity];
        _cCurveChannelsView = Array.AsReadOnly(_cCurveChannels);
    }

    public event Action? CCurveChanged;

    public bool CCurveActive => _cCurveActive;

    public bool CCurvePersistent => _cCurvePersistent;

    public int CCurveChannel => _cCurveChannel;

    public int CCurveSelected => _cCurveSelected;

    public IReadOnlyList<IReadOnlyList<(double, double)>> CCurveChannels => _cCurveChannelsView;

    public IReadOnlyList<(double, double)> CCurvePoints => _cCurveChannels[_cCurveChannel];

    private (double, double) CCurvePointRead() => CCurvePoints[_cCurveSelected];

    public double CCurveInputPercent => CCurvePointRead().Item1 * 100;

    public double CCurveOutputPercent => CCurvePointRead().Item2 * 100;

    public (bool CCurveActive, IReadOnlyList<IReadOnlyList<(double, double)>> CCurveChannels) CCurveStepRead() =>
        (_cCurveActive, _cCurveChannelsView);

    public IReadOnlyList<(double, double)> CCurveTrackRead(int sampleCount)
    {
        double[] inputs = CCurvePoints.Select(point => point.Item1).ToArray();
        double[] outputs = CCurvePoints.Select(point => point.Item2).ToArray();
        double[] slopes = CCurveSlopeResolve(inputs, outputs);
        var track = new List<(double, double)>();
        for (int sample = 0; sample <= sampleCount; sample++)
        {
            double input = (double)sample / sampleCount;
            track.Add((input, CCurveOutputResolve(inputs, outputs, slopes, input)));
        }

        return track;
    }

    private static double[] CCurveSlopeResolve(double[] inputs, double[] outputs)
    {
        int count = inputs.Length;
        var slopes = new double[count];
        if (count < 2)
        {
            return slopes;
        }

        var deltas = new double[count - 1];
        for (int index = 0; index < count - 1; index++)
        {
            double run = inputs[index + 1] - inputs[index];
            deltas[index] = run <= 0 ? 0 : (outputs[index + 1] - outputs[index]) / run;
        }

        slopes[0] = deltas[0];
        slopes[count - 1] = deltas[count - 2];
        for (int index = 1; index < count - 1; index++)
        {
            double left = deltas[index - 1];
            double right = deltas[index];
            if (left * right <= 0)
            {
                slopes[index] = 0;
                continue;
            }

            double leftSpan = inputs[index] - inputs[index - 1];
            double rightSpan = inputs[index + 1] - inputs[index];
            double leftWeight = (2 * rightSpan) + leftSpan;
            double rightWeight = rightSpan + (2 * leftSpan);
            slopes[index] = (leftWeight + rightWeight) / ((leftWeight / left) + (rightWeight / right));
        }

        return slopes;
    }

    private static double CCurveOutputResolve(double[] inputs, double[] outputs, double[] slopes, double input)
    {
        int count = inputs.Length;
        if (count == 0)
        {
            return input;
        }

        if (input <= inputs[0])
        {
            return outputs[0];
        }

        if (input >= inputs[count - 1])
        {
            return outputs[count - 1];
        }

        int segment = 0;
        while (segment < count - 2 && input > inputs[segment + 1])
        {
            segment++;
        }

        double span = inputs[segment + 1] - inputs[segment];
        if (span <= 0)
        {
            return outputs[segment];
        }

        double step = (input - inputs[segment]) / span;
        double stepSquare = step * step;
        double stepCube = stepSquare * step;
        double hermite00 = (2 * stepCube) - (3 * stepSquare) + 1;
        double hermite10 = stepCube - (2 * stepSquare) + step;
        double hermite01 = (-2 * stepCube) + (3 * stepSquare);
        double hermite11 = stepCube - stepSquare;
        return Math.Clamp(
            (hermite00 * outputs[segment])
            + (hermite10 * span * slopes[segment])
            + (hermite01 * outputs[segment + 1])
            + (hermite11 * span * slopes[segment + 1]),
            0, 1);
    }

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

            _cCurveSelected = Math.Clamp(_cCurveSelected, 0, CCurvePoints.Count - 1);
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

    public void CCurveChannelSelect(int channel)
    {
        if (channel < 0)
        {
            return;
        }

        int clamped = Math.Clamp(channel, 0, _cCurveChannels.Length - 1);
        if (_cCurveChannel == clamped)
        {
            return;
        }

        _cCurveChannel = clamped;
        _cCurveSelected = Math.Clamp(_cCurveSelected, 0, CCurvePoints.Count - 1);
        CCurveChanged?.Invoke();
    }

    private void CCurvePointSelect(int index)
    {
        int clamped = Math.Clamp(index, 0, CCurvePoints.Count - 1);
        if (_cCurveSelected == clamped)
        {
            return;
        }

        _cCurveSelected = clamped;
        CCurveChanged?.Invoke();
    }

    public void CCurvePressHandle(double input, double output, double tolerance)
    {
        int hit = CCurveHitFind(input, output, tolerance);
        if (hit < 0)
        {
            CCurvePointAdd(input / tolerance, 1 - (output / tolerance));
        }
        else
        {
            CCurvePointSelect(hit);
        }
    }

    private int CCurveHitFind(double input, double output, double tolerance)
    {
        int best = -1;
        double bestDistance = 9;
        for (int index = 0; index < CCurvePoints.Count; index++)
        {
            double distance = Math.Sqrt(
                Math.Pow((Math.Clamp(CCurvePoints[index].Item1, 0, 1) * tolerance) - input, 2)
                + Math.Pow(((1 - Math.Clamp(CCurvePoints[index].Item2, 0, 1)) * tolerance) - output, 2));
            if (distance <= bestDistance)
            {
                best = index;
                bestDistance = distance;
            }
        }

        return best;
    }

    private void CCurvePointSet(double input, double output)
    {
        double clampedInput = Math.Clamp(input, 0, 1);
        if (_cCurveSelected <= 0)
        {
            clampedInput = 0;
        }
        else if (_cCurveSelected >= CCurvePoints.Count - 1)
        {
            clampedInput = 1;
        }
        else
        {
            clampedInput = Math.Clamp(
                clampedInput,
                CCurvePoints[_cCurveSelected - 1].Item1 + CCurveGap,
                CCurvePoints[_cCurveSelected + 1].Item1 - CCurveGap);
        }

        var point = (clampedInput, Math.Clamp(output, 0, 1));
        if (CCurvePointRead().Equals(point))
        {
            return;
        }

        List<(double, double)> points = CCurvePoints.ToList();
        points[_cCurveSelected] = point;
        _cCurveChannels[_cCurveChannel] = Array.AsReadOnly(points.ToArray());
        CCurveChanged?.Invoke();
    }

    public bool CCurveMove(double input, double output, bool active)
    {
        if (!active)
        {
            return false;
        }

        CCurvePointSet(input, output);
        return true;
    }

    public void CCurvePointCommit(string inputText, string outputText)
    {
        (double input, double output) = CCurvePointRead();
        CCurvePointSet(
            CInspectorField.CInspectorValueCommit(inputText, input * 100, null, null) / 100,
            CInspectorField.CInspectorValueCommit(outputText, output * 100, null, null) / 100);
    }

    private void CCurvePointAdd(double input, double output)
    {
        var point = (
            Input: Math.Clamp(Math.Clamp(input, 0, 1), CCurveGap, 1 - CCurveGap),
            Output: Math.Clamp(output, 0, 1));
        List<(double, double)> points = CCurvePoints.ToList();
        points.Add(point);
        points.Sort((left, right) => left.Item1.CompareTo(right.Item1));
        _cCurveSelected = points.IndexOf(point);
        _cCurveChannels[_cCurveChannel] = Array.AsReadOnly(points.ToArray());
        CCurveChanged?.Invoke();
    }

    public void CCurvePointDelete()
    {
        if (CCurvePoints.Count <= 2 || _cCurveSelected <= 0 || _cCurveSelected >= CCurvePoints.Count - 1)
        {
            return;
        }

        List<(double, double)> points = CCurvePoints.ToList();
        points.RemoveAt(_cCurveSelected);
        _cCurveChannels[_cCurveChannel] = Array.AsReadOnly(points.ToArray());
        CCurveChanged?.Invoke();
    }

    public void CCurveChannelReset(int channel)
    {
        _cCurveChannels[channel] = _cCurveIdentity;
        _cCurveSelected = Math.Clamp(_cCurveSelected, 0, CCurvePoints.Count - 1);
        CCurveChanged?.Invoke();
    }

    // Every channel goes back to the straight line; the switch keeps its state.
    public void CCurveReset()
    {
        Array.Fill(_cCurveChannels, _cCurveIdentity);
        _cCurveSelected = Math.Clamp(_cCurveSelected, 0, CCurvePoints.Count - 1);
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
