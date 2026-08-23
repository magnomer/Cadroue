using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static partial class LSweep
{
    public static IReadOnlyList<LPiece> LSweepCombineResolve(
        IReadOnlyList<LPiece> lSweepExisting,
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepExcluded,
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepKept,
        IReadOnlyList<(TimeSpan Time, TimeSpan Minimum)> lSweepBoundaries,
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

        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepHoles = LSweepIntervalNormalize(lSweepExcluded, lSweepDuration);
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepContent = LSweepComplementResolve(lSweepHoles, lSweepDuration);

        var lSweepHard = new SortedSet<TimeSpan>();
        foreach ((TimeSpan lSweepFrom, TimeSpan lSweepTo) in lSweepKept)
        {
            lSweepHard.Add(LSweepClamp(lSweepFrom, lSweepDuration));
            lSweepHard.Add(LSweepClamp(lSweepTo, lSweepDuration));
        }

        var lSweepSoft = new List<(TimeSpan Time, TimeSpan Minimum)>();
        foreach ((TimeSpan lSweepTime, TimeSpan lSweepMinimum) in lSweepBoundaries)
        {
            lSweepSoft.Add((LSweepClamp(lSweepTime, lSweepDuration), lSweepMinimum));
        }

        lSweepSoft.Sort((lSweepLeft, lSweepRight) => lSweepLeft.Time.CompareTo(lSweepRight.Time));

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

                    lSweepResult.Add(new LPiece(lSweepCursor, lSweepTime, lSweepColorIndex % lSweepPalette, string.Empty)
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

        return lSweepResult.Count > LPiece.LPieceCeiling
            ? lSweepResult.GetRange(0, LPiece.LPieceCeiling)
            : lSweepResult;
    }
}
