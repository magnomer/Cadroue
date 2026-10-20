using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed class CSEncoderSpeed
{
    private readonly LSEncoderPort _csEncoderSpeedPort;
    private readonly string _csEncoderSpeedRate;
    private readonly string _csEncoderSpeedDefault;

    internal CSEncoderSpeed(LCapabilitySpeed codec, LSEncoderPort draft, string rate)
    {
        _csEncoderSpeedPort = draft;
        _csEncoderSpeedRate = rate;
        _csEncoderSpeedDefault = codec.LCapabilitySpeedDefault;
        CSEncoderSpeedLabel = codec.LCapabilitySpeedLabel;
        CSEncoderSpeedLabels = codec.LCapabilitySpeedValues.Select(choice => choice.LCapabilityChoiceLabel).ToArray();
        CSEncoderSpeedValues = codec.LCapabilitySpeedValues.Select(choice => choice.LCapabilityChoiceValue).ToArray();
    }

    public string CSEncoderSpeedLabel { get; }

    public IReadOnlyList<string> CSEncoderSpeedLabels { get; }

    public IReadOnlyList<string> CSEncoderSpeedValues { get; }

    public int CSEncoderSpeedIndex
    {
        get
        {
            (string rate, _, string speed, _) = _csEncoderSpeedPort.LSEncoderRateRead();
            string selected = CSEncoder.CSEncoderStoredRead(
                _csEncoderSpeedRate, rate, speed, _csEncoderSpeedDefault);
            return Math.Max(0, Array.IndexOf(CSEncoderSpeedValues.ToArray(), selected));
        }
    }

    public double CSEncoderSpeedMost => Math.Max(CSEncoderSpeedLabels.Count - 1, 0);

    public double CSEncoderSpeedOrigin =>
        Math.Clamp(CSEncoderSpeedLabels.Count - 1 - CSEncoderSpeedIndex, 0, CSEncoderSpeedMost);

    public string CSEncoderSpeedShown => CSEncoderSpeedLabels[CSEncoderSpeedIndex];

    public string CSEncoderSpeedFormat(double position) =>
        CSEncoderSpeedLabels[CSEncoderSpeedClamp(position, CSEncoderSpeedLabels.Count - 1)];

    public string CSEncoderSpeedRead(double position) =>
        CSEncoderSpeedValues.Count == 0
            ? string.Empty
            : CSEncoderSpeedValues[CSEncoderSpeedClamp(position, CSEncoderSpeedValues.Count - 1)];

    private static int CSEncoderSpeedClamp(double position, int last) =>
        last - Math.Clamp((int)Math.Round(position), 0, last);
}
