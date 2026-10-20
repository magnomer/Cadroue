using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Cadroue.Conduct;

public sealed class CSFader
{
    private const double CSFaderSpan = 1000;

    private CSFader()
    {
    }

    public double CSFaderLeast { get; private init; }

    public double CSFaderMost { get; private init; }

    public double CSFaderStep { get; private init; } = 1;

    public double CSFaderLeap { get; private init; } = 1;

    public IReadOnlyList<double> CSFaderTicks { get; private init; } = [];

    public bool CSFaderNotched { get; private init; }

    public bool CSFaderSnap { get; private init; }

    public double CSFaderOrigin { get; private init; }

    public string CSFaderShown { get; private init; } = string.Empty;

    public bool CSFaderLocked { get; private init; }

    public bool CSFaderNoticeOrigin => CSFaderNotifier(CSFaderOrigin);

    private Func<double, string> CSFaderFormatter { get; init; } = _ => string.Empty;

    private Func<string, double?> CSFaderMatcher { get; init; } = _ => null;

    private Func<string, double, (double, string)?> CSFaderCommitter { get; init; } = (_, _) => null;

    private Func<double, bool> CSFaderNotifier { get; init; } = _ => true;

    public string CSFaderFormat(double position) => CSFaderFormatter(position);

    public double? CSFaderMatch(string text) => CSFaderMatcher(text);

    public (double, string)? CSFaderCommit(string text, double position) => CSFaderCommitter(text, position);

    public bool CSFaderNoticeCheck(double position) => CSFaderNotifier(position);

    public static CSFader? CSFaderQualityCreate(
        string text,
        double? minimum,
        double? maximum,
        double step,
        bool ascending,
        bool bitrate)
    {
        if (minimum is not double least || maximum is not double most)
        {
            return null;
        }

        return bitrate
            ? CSFaderBitrateCreate(text, least, most)
            : CSFaderStepCreate(text, least, most, step, ascending);
    }

    public static CSFader CSFaderDetentCreate(
        string text,
        IReadOnlyList<int> rates,
        bool snap,
        double maximum,
        string zero)
    {
        double most = Math.Max(maximum, 1);
        double start = int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                       && parsed > 0
            ? CSFaderDetentResolve(parsed, rates, snap)
            : 0;
        return new CSFader
        {
            CSFaderMost = most,
            CSFaderTicks = [0, .. rates.Select(rate => (double)rate)],
            CSFaderNotched = true,
            CSFaderSnap = snap,
            CSFaderOrigin = Math.Min(start, most),
            CSFaderShown = CSFaderDetentFormat(start, zero),
            CSFaderLocked = snap,
            CSFaderFormatter = position => CSFaderDetentFormat(position, zero),
            CSFaderCommitter = (typed, position) =>
            {
                typed = typed.Trim();
                double resolved = position;
                if (typed.Length == 0 || string.Equals(typed, zero, StringComparison.OrdinalIgnoreCase))
                {
                    resolved = 0;
                }
                else if (int.TryParse(typed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hertz)
                         && hertz > 0)
                {
                    resolved = CSFaderDetentResolve(hertz, rates, snap);
                }

                return (Math.Min(resolved, most), CSFaderDetentFormat(resolved, zero));
            },
            CSFaderNotifier = position => position <= 0
        };
    }

    public static CSFader CSFaderLayoutCreate(IReadOnlyList<string> labels, int index)
    {
        int last = Math.Max(labels.Count - 1, 0);
        int start = CSFaderIndexClamp(index, last);
        return new CSFader
        {
            CSFaderMost = last,
            CSFaderTicks = Enumerable.Range(0, last + 1).Select(tick => (double)tick).ToArray(),
            CSFaderNotched = true,
            CSFaderSnap = true,
            CSFaderOrigin = start,
            CSFaderShown = CSFaderLabelResolve(labels, start),
            CSFaderLocked = true,
            CSFaderFormatter = position => CSFaderLabelResolve(labels, CSFaderIndexClamp(position, last)),
            CSFaderNotifier = position => CSFaderIndexClamp(position, last) <= 0
        };
    }

    private static CSFader CSFaderStepCreate(string text, double least, double most, double step, bool ascending)
    {
        double start = CSFaderParse(text, least, least, most, step);
        return new CSFader
        {
            CSFaderLeast = least,
            CSFaderMost = most,
            CSFaderStep = step,
            CSFaderLeap = step,
            CSFaderSnap = true,
            CSFaderOrigin = CSFaderQualityResolve(start, ascending, least, most),
            CSFaderShown = CSFaderNumberFormat(start, step),
            CSFaderFormatter = position =>
                CSFaderNumberFormat(CSFaderQualityResolve(position, ascending, least, most), step),
            CSFaderCommitter = (typed, position) =>
            {
                double quality = CSFaderParse(
                    typed.Trim(), CSFaderQualityResolve(position, ascending, least, most), least, most, step);
                return (CSFaderQualityResolve(quality, ascending, least, most), CSFaderNumberFormat(quality, step));
            }
        };
    }

    private static CSFader CSFaderBitrateCreate(string text, double least, double most)
    {
        double start = Math.Clamp(CSFaderRateParse(text) ?? least, least, most);
        return new CSFader
        {
            CSFaderMost = CSFaderSpan,
            CSFaderLeap = 50,
            CSFaderOrigin = CSFaderPositionResolve(start, least, most),
            CSFaderShown = CSFaderRateFormat(start),
            CSFaderFormatter = position =>
                CSFaderRateFormat(least * Math.Pow(most / least, position / CSFaderSpan)),
            CSFaderMatcher = typed => CSFaderRateParse(typed) is double kbps
                ? CSFaderPositionResolve(Math.Clamp(kbps, least, most), least, most)
                : null
        };
    }

    private static double CSFaderParse(string text, double fallback, double least, double most, double step) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
            && double.IsFinite(parsed)
            ? Math.Clamp(
                least + Math.Round((Math.Clamp(parsed, least, most) - least) / step, MidpointRounding.AwayFromZero)
                * step,
                least,
                most)
            : fallback;

    private static double CSFaderQualityResolve(double position, bool ascending, double least, double most) =>
        ascending ? position : least + most - position;

    private static string CSFaderNumberFormat(double value, double step) =>
        step >= 1 && value == Math.Floor(value)
            ? ((long)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);

    private static double CSFaderPositionResolve(double kbps, double least, double most) =>
        Math.Log(kbps / least) / Math.Log(most / least) * CSFaderSpan;

    private static double? CSFaderRateParse(string text)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            return null;
        }

        char unit = text[^1];
        string number = char.IsDigit(unit) || unit == '.' ? text : text[..^1];
        if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            || value <= 0)
        {
            return null;
        }

        return unit switch
        {
            'k' or 'K' => value,
            'm' or 'M' => value * 1000,
            _ => value / 1000
        };
    }

    private static string CSFaderRateFormat(double kbps)
    {
        double rounded = Math.Round(kbps);
        return rounded >= 1000
            ? (rounded / 1000).ToString("0.##", CultureInfo.InvariantCulture) + "M"
            : ((long)rounded).ToString(CultureInfo.InvariantCulture) + "k";
    }

    private static double CSFaderDetentResolve(double hertz, IReadOnlyList<int> rates, bool snap)
    {
        if (hertz <= 0 || !snap || rates.Count == 0)
        {
            return hertz <= 0 ? 0 : hertz;
        }

        double best = rates[0];
        double bestGap = double.MaxValue;
        foreach (int rate in rates)
        {
            double gap = Math.Abs(rate - hertz);
            if (gap < bestGap)
            {
                bestGap = gap;
                best = rate;
            }
        }

        return best;
    }

    private static string CSFaderDetentFormat(double value, string zero) =>
        value <= 0 ? zero : ((long)Math.Round(value)).ToString(CultureInfo.InvariantCulture);

    private static int CSFaderIndexClamp(double position, int last) =>
        Math.Clamp((int)Math.Round(position), 0, last);

    private static string CSFaderLabelResolve(IReadOnlyList<string> labels, int at) =>
        labels.Count > 0 ? labels[at] : string.Empty;
}
