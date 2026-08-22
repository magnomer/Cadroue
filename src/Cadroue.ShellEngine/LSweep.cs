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
        return $"-hide_banner -nostats -i {LEncode.LEncodeFormat(lSweepSource)} -map 0:v:0 -vf {LEncode.LEncodeFormat(lSweepFilter)} -an -f null -";
    }

    private static string LSweepBlackFormat(LDetectorBlank lSweepSpec) =>
        string.Create(CultureInfo.InvariantCulture,
            $"blackdetect=d={lSweepSpec.LDetectorBlankMinimum:0.###}:pic_th={lSweepSpec.LDetectorBlankCoverage:0.###}:pix_th={lSweepSpec.LDetectorBlankTolerance:0.###}");

    private static string LSweepColorFormat(LDetectorBlank lSweepSpec)
    {
        (int lSweepRed, int lSweepGreen, int lSweepBlue) = LNeutral.LNeutralRgbResolve(
            lSweepSpec.LDetectorBlankHue,
            lSweepSpec.LDetectorBlankSaturation,
            Math.Max(lSweepSpec.LDetectorBlankBrightness, 0.001));
        int lSweepThreshold = (int)Math.Round(lSweepSpec.LDetectorBlankTolerance * 255);
        string lSweepMatch = string.Create(CultureInfo.InvariantCulture,
            $"lt(abs(r(X,Y)-{lSweepRed}),{lSweepThreshold})*lt(abs(g(X,Y)-{lSweepGreen}),{lSweepThreshold})*lt(abs(b(X,Y)-{lSweepBlue}),{lSweepThreshold})");
        string lSweepExpr = $"if({lSweepMatch},0,255)";
        return string.Create(CultureInfo.InvariantCulture,
            $"format=gbrp,geq=r='{lSweepExpr}':g='{lSweepExpr}':b='{lSweepExpr}',format=gray,blackdetect=d={lSweepSpec.LDetectorBlankMinimum:0.###}:pic_th={lSweepSpec.LDetectorBlankCoverage:0.###}:pix_th=0.1");
    }

    public static IReadOnlyList<(TimeSpan Start, TimeSpan End)> LSweepOutputParse(IEnumerable<string> lSweepLines)
    {
        var lSweepIntervals = new List<(TimeSpan, TimeSpan)>();
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
                lSweepIntervals.Add((TimeSpan.FromSeconds(lSweepFrom), TimeSpan.FromSeconds(lSweepTo)));
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

    public static IReadOnlyList<LPiece> LSweepSectionResolve(
        IReadOnlyList<LPiece> lSweepExisting,
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepBlanks,
        TimeSpan lSweepDuration,
        int lSweepColorCount)
    {
        var lSweepKept = new List<LPiece>();
        foreach (LPiece lSweepPiece in lSweepExisting)
        {
            if (!lSweepPiece.LPieceDetected)
            {
                lSweepKept.Add(lSweepPiece);
            }
        }

        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepMerged = LSweepIntervalNormalize(lSweepBlanks, lSweepDuration);
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepContent = LSweepComplementResolve(lSweepMerged, lSweepDuration);
        var lSweepReserved = lSweepKept
            .Select(lSweepPiece => (lSweepPiece.LPieceOrigin, lSweepPiece.LPieceEnd))
            .ToList();

        int lSweepColorIndex = 0;
        int lSweepPalette = Math.Max(lSweepColorCount, 1);
        var lSweepResult = new List<LPiece>(lSweepKept);
        foreach ((TimeSpan lSweepFrom, TimeSpan lSweepTo) in lSweepContent)
        {
            foreach ((TimeSpan lSweepFree, TimeSpan lSweepFreeEnd) in LSweepFreeResolve(lSweepFrom, lSweepTo, lSweepReserved))
            {
                lSweepResult.Add(new LPiece(lSweepFree, lSweepFreeEnd, lSweepColorIndex % lSweepPalette, string.Empty)
                {
                    LPieceDetected = true
                });
                lSweepColorIndex++;
            }
        }

        lSweepResult.Sort((lSweepLeft, lSweepRight) => lSweepLeft.LPieceOrigin.CompareTo(lSweepRight.LPieceOrigin));
        return lSweepResult;
    }

    private static IReadOnlyList<(TimeSpan Start, TimeSpan End)> LSweepIntervalNormalize(
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepBlanks, TimeSpan lSweepDuration)
    {
        var lSweepOrdered = lSweepBlanks
            .Select(lSweepBlank => (
                Start: LSweepClamp(lSweepBlank.Start, lSweepDuration),
                End: LSweepClamp(lSweepBlank.End, lSweepDuration)))
            .Where(lSweepBlank => lSweepBlank.End > lSweepBlank.Start)
            .OrderBy(lSweepBlank => lSweepBlank.Start)
            .ToList();

        var lSweepMerged = new List<(TimeSpan Start, TimeSpan End)>();
        foreach ((TimeSpan lSweepStart, TimeSpan lSweepEnd) in lSweepOrdered)
        {
            if (lSweepMerged.Count > 0 && lSweepStart <= lSweepMerged[^1].End)
            {
                if (lSweepEnd > lSweepMerged[^1].End)
                {
                    lSweepMerged[^1] = (lSweepMerged[^1].Start, lSweepEnd);
                }
            }
            else
            {
                lSweepMerged.Add((lSweepStart, lSweepEnd));
            }
        }

        return lSweepMerged;
    }

    private static IReadOnlyList<(TimeSpan Start, TimeSpan End)> LSweepComplementResolve(
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepBlanks, TimeSpan lSweepDuration)
    {
        var lSweepContent = new List<(TimeSpan Start, TimeSpan End)>();
        TimeSpan lSweepCursor = TimeSpan.Zero;
        foreach ((TimeSpan lSweepStart, TimeSpan lSweepEnd) in lSweepBlanks)
        {
            if (lSweepStart > lSweepCursor)
            {
                lSweepContent.Add((lSweepCursor, lSweepStart));
            }

            if (lSweepEnd > lSweepCursor)
            {
                lSweepCursor = lSweepEnd;
            }
        }

        if (lSweepDuration > lSweepCursor)
        {
            lSweepContent.Add((lSweepCursor, lSweepDuration));
        }

        return lSweepContent;
    }

    private static IReadOnlyList<(TimeSpan Start, TimeSpan End)> LSweepFreeResolve(
        TimeSpan lSweepFrom, TimeSpan lSweepTo, IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepReserved)
    {
        var lSweepFree = new List<(TimeSpan Start, TimeSpan End)>();
        TimeSpan lSweepCursor = lSweepFrom;
        foreach ((TimeSpan lSweepStart, TimeSpan lSweepEnd) in lSweepReserved
            .Where(lSweepRange => lSweepRange.End > lSweepFrom && lSweepRange.Start < lSweepTo)
            .OrderBy(lSweepRange => lSweepRange.Start))
        {
            if (lSweepStart > lSweepCursor)
            {
                lSweepFree.Add((lSweepCursor, lSweepStart));
            }

            if (lSweepEnd > lSweepCursor)
            {
                lSweepCursor = lSweepEnd;
            }
        }

        if (lSweepTo > lSweepCursor)
        {
            lSweepFree.Add((lSweepCursor, lSweepTo));
        }

        return lSweepFree;
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
