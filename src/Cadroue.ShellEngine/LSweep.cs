using System.Globalization;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static partial class LSweep
{
    public static string LSweepArgsFormat(string lSweepSource, LDetectorBlank lSweepBlank)
    {
        LDetectorBlank lSweepSpec = LDetectorBlank.LDetectorBlankClamp(lSweepBlank);
        string lSweepFilter = lSweepSpec.LDetectorBlankType == LDetectorType.LDetectorTypeColor
            ? LSweepColorFormat(lSweepSpec)
            : LSweepBlackFormat(lSweepSpec);
        return $"-hide_banner -stats -i {LEncode.LEncodeFormat(lSweepSource)} " +
            $"-map 0:v:0 -vf {LEncode.LEncodeFormat(lSweepFilter)} -an -f null -";
    }

    private static string LSweepBlackFormat(LDetectorBlank lSweepSpec) =>
        string.Create(CultureInfo.InvariantCulture,
            $"blackdetect=d={lSweepSpec.LDetectorBlankMinimum:0.###}:" +
            $"pic_th={lSweepSpec.LDetectorBlankCoverage:0.###}:" +
            $"pix_th={lSweepSpec.LDetectorBlankTolerance:0.###}");

    private static string LSweepColorFormat(LDetectorBlank lSweepSpec)
    {
        (int lSweepRed, int lSweepGreen, int lSweepBlue) = LNeutral.LNeutralRgbResolve(
            lSweepSpec.LDetectorBlankHue,
            lSweepSpec.LDetectorBlankSaturation,
            Math.Max(lSweepSpec.LDetectorBlankBrightness, 0.001));
        int lSweepThreshold = (int)Math.Round(lSweepSpec.LDetectorBlankTolerance * 255);
        string lSweepMatch = string.Create(CultureInfo.InvariantCulture,
            $"lt(abs(r(X,Y)-{lSweepRed}),{lSweepThreshold})*" +
            $"lt(abs(g(X,Y)-{lSweepGreen}),{lSweepThreshold})*" +
            $"lt(abs(b(X,Y)-{lSweepBlue}),{lSweepThreshold})");
        string lSweepExpr = $"if({lSweepMatch},0,255)";
        return string.Create(CultureInfo.InvariantCulture,
            $"format=gbrp,geq=r='{lSweepExpr}':g='{lSweepExpr}':b='{lSweepExpr}',format=gray," +
            $"blackdetect=d={lSweepSpec.LDetectorBlankMinimum:0.###}:" +
            $"pic_th={lSweepSpec.LDetectorBlankCoverage:0.###}:pix_th=0.1");
    }

    public static string LSweepSceneFormat(string lSweepSource, double lSweepThreshold)
    {
        string lSweepFilter = string.Create(CultureInfo.InvariantCulture,
            $"scdet=threshold={lSweepThreshold:0.###},metadata=print:key=lavfi.scd.time");
        return $"-hide_banner -stats -i {LEncode.LEncodeFormat(lSweepSource)} " +
            $"-map 0:v:0 -vf {LEncode.LEncodeFormat(lSweepFilter)} -an -f null -";
    }

    public static IReadOnlyList<TimeSpan> LSweepSceneParse(IEnumerable<string> lSweepLines)
    {
        var lSweepSeen = new SortedSet<double>();
        foreach (string lSweepLine in lSweepLines)
        {
            if (lSweepLine is null)
            {
                continue;
            }

            double? lSweepTime = LSweepFieldRead(lSweepLine, "lavfi.scd.time=");
            if (lSweepTime is { } lSweepAt)
            {
                lSweepSeen.Add(lSweepAt);
            }
        }

        return lSweepSeen.Select(TimeSpan.FromSeconds).ToList();
    }

    public static IReadOnlyList<LSweepSpan> LSweepOutputParse(IEnumerable<string> lSweepLines)
    {
        var lSweepIntervals = new List<LSweepSpan>();
        foreach (string lSweepLine in lSweepLines)
        {
            if (lSweepLine is null || !lSweepLine.Contains("black_start:", StringComparison.Ordinal))
            {
                continue;
            }

            double? lSweepStart = LSweepFieldRead(lSweepLine, "black_start:");
            double? lSweepEnd = LSweepFieldRead(lSweepLine, "black_end:");
            if (lSweepStart is { } lSweepFrom && lSweepEnd is { } lSweepTo && lSweepTo > lSweepFrom)
            {
                lSweepIntervals.Add(new LSweepSpan(TimeSpan.FromSeconds(lSweepFrom), TimeSpan.FromSeconds(lSweepTo)));
            }
        }

        return lSweepIntervals;
    }

    private static double? LSweepFieldRead(string lSweepLine, string lSweepKey)
    {
        int lSweepAt = lSweepLine.IndexOf(lSweepKey, StringComparison.Ordinal);
        if (lSweepAt < 0)
        {
            return null;
        }

        int lSweepFrom = lSweepAt + lSweepKey.Length;
        int lSweepTo = lSweepFrom;
        while (lSweepTo < lSweepLine.Length && !char.IsWhiteSpace(lSweepLine[lSweepTo]))
        {
            lSweepTo++;
        }

        return double.TryParse(
            lSweepLine.AsSpan(lSweepFrom, lSweepTo - lSweepFrom),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double lSweepValue)
            ? lSweepValue
            : null;
    }

    private static IReadOnlyList<LSweepSpan> LSweepIntervalNormalize(
        IReadOnlyList<LSweepSpan> lSweepBlanks, TimeSpan lSweepDuration)
    {
        var lSweepOrdered = lSweepBlanks
            .Select(lSweepBlank => new LSweepSpan(
                LSweepClamp(lSweepBlank.LSweepSpanOrigin, lSweepDuration),
                LSweepClamp(lSweepBlank.LSweepSpanEnd, lSweepDuration)))
            .Where(lSweepBlank => lSweepBlank.LSweepSpanEnd > lSweepBlank.LSweepSpanOrigin)
            .OrderBy(lSweepBlank => lSweepBlank.LSweepSpanOrigin)
            .ToList();

        var lSweepMerged = new List<LSweepSpan>();
        foreach ((TimeSpan lSweepStart, TimeSpan lSweepEnd) in lSweepOrdered)
        {
            if (lSweepMerged.Count > 0 && lSweepStart <= lSweepMerged[^1].LSweepSpanEnd)
            {
                if (lSweepEnd > lSweepMerged[^1].LSweepSpanEnd)
                {
                    lSweepMerged[^1] = new LSweepSpan(lSweepMerged[^1].LSweepSpanOrigin, lSweepEnd);
                }
            }
            else
            {
                lSweepMerged.Add(new LSweepSpan(lSweepStart, lSweepEnd));
            }
        }

        return lSweepMerged;
    }

    private static IReadOnlyList<LSweepSpan> LSweepComplementResolve(
        IReadOnlyList<LSweepSpan> lSweepBlanks, TimeSpan lSweepDuration)
    {
        var lSweepContent = new List<LSweepSpan>();
        TimeSpan lSweepCursor = TimeSpan.Zero;
        foreach ((TimeSpan lSweepStart, TimeSpan lSweepEnd) in lSweepBlanks)
        {
            if (lSweepStart > lSweepCursor)
            {
                lSweepContent.Add(new LSweepSpan(lSweepCursor, lSweepStart));
            }

            if (lSweepEnd > lSweepCursor)
            {
                lSweepCursor = lSweepEnd;
            }
        }

        if (lSweepDuration > lSweepCursor)
        {
            lSweepContent.Add(new LSweepSpan(lSweepCursor, lSweepDuration));
        }

        return lSweepContent;
    }

    private static TimeSpan LSweepClamp(TimeSpan lSweepValue, TimeSpan lSweepDuration)
    {
        if (lSweepValue < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return lSweepValue > lSweepDuration ? lSweepDuration : lSweepValue;
    }
}
