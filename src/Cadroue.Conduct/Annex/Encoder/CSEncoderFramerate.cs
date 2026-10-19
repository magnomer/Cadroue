using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Cadroue.Conduct;

public static class CSEncoderFramerate
{
    private const string CSEncoderFramerateSource = "Same as source";

    private static readonly double[] _csEncoderFramerateRates = CSEncoderFramerateCreate();

    public static IReadOnlyList<string> CSEncoderFramerateValues { get; } = _csEncoderFramerateRates
        .Select(rate => rate == 0 ? string.Empty : rate.ToString(CultureInfo.InvariantCulture))
        .ToArray();

    public static int CSEncoderFramerateMost => _csEncoderFramerateRates.Length - 1;

    private static double[] CSEncoderFramerateCreate()
    {
        var rates = new List<double>();
        for (int at = 1; at <= 240; at++)
        {
            rates.Add(at);
        }

        rates.AddRange(new[] { 23.976, 29.97, 59.94, 119.88 });
        var sorted = rates.OrderBy(rate => rate).ToList();
        sorted.Insert(0, 0);
        return sorted.ToArray();
    }

    public static bool CSEncoderFramerateCheck(string fps, string label)
    {
        string trim = fps.Trim();
        return trim.Length == 0
            || string.Equals(trim, CSEncoderFramerateSource, StringComparison.Ordinal)
            || string.Equals(trim, label, StringComparison.Ordinal);
    }

    public static int CSEncoderFramerateResolve(string text, string label)
    {
        if (CSEncoderFramerateCheck(text, label)
            || !double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double rate)
            || !double.IsFinite(rate) || rate <= 0)
        {
            return 0;
        }

        int best = 1;
        double bestDiff = double.MaxValue;
        for (int at = 1; at < _csEncoderFramerateRates.Length; at++)
        {
            double diff = Math.Abs(_csEncoderFramerateRates[at] - rate);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = at;
            }
        }

        return best;
    }

    public static bool CSEncoderFramerateCheck(double position) => CSEncoderFramerateClamp(position) == 0;

    private static int CSEncoderFramerateClamp(double position) =>
        Math.Clamp((int)Math.Round(position), 0, CSEncoderFramerateMost);

    public static string CSEncoderFramerateFormat(string fps, string label) =>
        CSEncoderFramerateCheck(fps, label) ? string.Empty : fps.Trim();

    public static string CSEncoderFramerateFormat(double position, string label)
    {
        int at = CSEncoderFramerateClamp(position);
        return at == 0 ? label : CSEncoderFramerateValues[at];
    }

    public static string CSEncoderFramerateRead(string text, string label)
    {
        string value = text.Trim();
        return CSEncoderFramerateCheck(value, label) ? CSEncoderFramerateSource : value;
    }
}
