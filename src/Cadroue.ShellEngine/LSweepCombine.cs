using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static partial class LSweep
{
    public static IReadOnlyList<LPiece> LSweepCombineResolve(
        IReadOnlyList<LPiece> lSweepExisting,
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepExcluded,
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepKept,
        IReadOnlyList<TimeSpan> lSweepBoundaries,
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

        var lSweepCuts = new SortedSet<TimeSpan>();
        foreach (TimeSpan lSweepBoundary in lSweepBoundaries)
        {
            lSweepCuts.Add(LSweepClamp(lSweepBoundary, lSweepDuration));
        }

        foreach ((TimeSpan lSweepFrom, TimeSpan lSweepTo) in lSweepKept)
        {
            lSweepCuts.Add(LSweepClamp(lSweepFrom, lSweepDuration));
            lSweepCuts.Add(LSweepClamp(lSweepTo, lSweepDuration));
        }

        int lSweepColorIndex = 0;
        int lSweepPalette = Math.Max(lSweepColorCount, 1);
        var lSweepResult = new List<LPiece>();
        foreach ((TimeSpan lSweepStart, TimeSpan lSweepEnd) in lSweepContent)
        {
            TimeSpan lSweepCursor = lSweepStart;
            foreach (TimeSpan lSweepCut in lSweepCuts)
            {
                if (lSweepCut <= lSweepCursor || lSweepCut >= lSweepEnd)
                {
                    continue;
                }

                lSweepResult.Add(new LPiece(lSweepCursor, lSweepCut, lSweepColorIndex % lSweepPalette, string.Empty)
                {
                    LPieceDetected = true
                });
                lSweepColorIndex++;
                lSweepCursor = lSweepCut;
            }

            lSweepResult.Add(new LPiece(lSweepCursor, lSweepEnd, lSweepColorIndex % lSweepPalette, string.Empty)
            {
                LPieceDetected = true
            });
            lSweepColorIndex++;
        }

        lSweepResult.AddRange(lSweepUser);

        return lSweepResult.Count > LPiece.LPieceCeiling
            ? lSweepResult.GetRange(0, LPiece.LPieceCeiling)
            : lSweepResult;
    }
}
