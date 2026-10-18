using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LSweepCombine
{
    public static IReadOnlyList<LPiece> LSweepCombineResolve(
        IReadOnlyList<LPiece> lSweepExisting,
        IReadOnlyList<LSweepSpan> lSweepExcluded,
        IReadOnlyList<LSweepSpan> lSweepKept,
        IReadOnlyList<LSweepBoundary> lSweepBoundaries,
        TimeSpan lSweepDuration,
        int lSweepColorCount)
    {
        var lSweepUser = new List<LPiece>();
        foreach (LPiece lSweepPiece in lSweepExisting)
        {
            if (!lSweepPiece.LPieceDetected)
            {
                lSweepUser.Add(lSweepPiece);
            }
        }

        IReadOnlyList<LSweepSpan> lSweepHoles = LSweepIntervalNormalize(lSweepExcluded, lSweepDuration);
        IReadOnlyList<LSweepSpan> lSweepContent = LSweepComplementResolve(lSweepHoles, lSweepDuration);

        var lSweepHard = new SortedSet<TimeSpan>();
        foreach ((TimeSpan lSweepFrom, TimeSpan lSweepTo) in lSweepKept)
        {
            lSweepHard.Add(LSweepClamp(lSweepFrom, lSweepDuration));
            lSweepHard.Add(LSweepClamp(lSweepTo, lSweepDuration));
        }

        var lSweepSoft = new List<LSweepBoundary>();
        foreach ((TimeSpan lSweepTime, TimeSpan lSweepMinimum) in lSweepBoundaries)
        {
            lSweepSoft.Add(new LSweepBoundary(LSweepClamp(lSweepTime, lSweepDuration), lSweepMinimum));
        }

        lSweepSoft.Sort(
            (lSweepLeft, lSweepRight) => lSweepLeft.LSweepBoundaryTime.CompareTo(lSweepRight.LSweepBoundaryTime));

        int lSweepColorIndex = 0;
        int lSweepPalette = Math.Max(lSweepColorCount, 1);
        var lSweepResult = new List<LPiece>();
        foreach ((TimeSpan lSweepStart, TimeSpan lSweepEnd) in lSweepContent)
        {
            var lSweepStops = new List<TimeSpan>();
            foreach (TimeSpan lSweepHardCut in lSweepHard)
            {
                if (lSweepHardCut > lSweepStart && lSweepHardCut < lSweepEnd)
                {
                    lSweepStops.Add(lSweepHardCut);
                }
            }

            lSweepStops.Add(lSweepEnd);

            TimeSpan lSweepSubStart = lSweepStart;
            foreach (TimeSpan lSweepSubEnd in lSweepStops)
            {
                TimeSpan lSweepCursor = lSweepSubStart;
                foreach ((TimeSpan lSweepTime, TimeSpan lSweepMinimum) in lSweepSoft)
                {
                    if (lSweepTime <= lSweepCursor || lSweepTime >= lSweepSubEnd)
                    {
                        continue;
                    }

                    if (lSweepTime - lSweepCursor < lSweepMinimum || lSweepSubEnd - lSweepTime < lSweepMinimum)
                    {
                        continue;
                    }

                    lSweepResult.Add(
                        new LPiece(lSweepCursor, lSweepTime, lSweepColorIndex % lSweepPalette, string.Empty)
                        {
                            LPieceDetected = true
                        });
                    lSweepColorIndex++;
                    lSweepCursor = lSweepTime;
                }

                lSweepResult.Add(new LPiece(lSweepCursor, lSweepSubEnd, lSweepColorIndex % lSweepPalette, string.Empty)
                {
                    LPieceDetected = true
                });
                lSweepColorIndex++;
                lSweepSubStart = lSweepSubEnd;
            }
        }

        lSweepResult.AddRange(lSweepUser);
        return lSweepResult;
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
