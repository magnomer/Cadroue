using System.Globalization;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static partial class LSweep
{
    public static IReadOnlyList<LPiece> LSweepStillResolve(
        IReadOnlyList<LPiece> lSweepExisting,
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepStills,
        TimeSpan lSweepDuration,
        int lSweepColorCount,
        LDetectorStillMode lSweepMode)
    {
        var lSweepKept = new List<LPiece>();
        foreach (LPiece lSweepPiece in lSweepExisting)
        {
            if (!lSweepPiece.LPieceDetected)
            {
                lSweepKept.Add(lSweepPiece);
            }
        }

        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepMerged = LSweepIntervalNormalize(lSweepStills, lSweepDuration);
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lSweepContent = LSweepComplementResolve(lSweepMerged, lSweepDuration);

        List<(TimeSpan Start, TimeSpan End)> lSweepSpans = lSweepMode == LDetectorStillMode.LDetectorStillTreat
            ? lSweepContent.Concat(lSweepMerged).OrderBy(lSweepSpan => lSweepSpan.Start).ToList()
            : lSweepContent.ToList();

        int lSweepColorIndex = 0;
        int lSweepPalette = Math.Max(lSweepColorCount, 1);
        var lSweepResult = new List<LPiece>(lSweepKept);
        foreach ((TimeSpan lSweepFrom, TimeSpan lSweepTo) in lSweepSpans)
        {
            lSweepResult.Add(new LPiece(lSweepFrom, lSweepTo, lSweepColorIndex % lSweepPalette, string.Empty)
            {
                LPieceDetected = true
            });
            lSweepColorIndex++;
        }

        return lSweepResult.Count > LPiece.LPieceCeiling
            ? lSweepResult.GetRange(0, LPiece.LPieceCeiling)
            : lSweepResult;
    }

    public static string LSweepStillFormat(string lSweepSource, double lSweepTolerance, double lSweepMinimum)
    {
        string lSweepFilter = string.Create(CultureInfo.InvariantCulture,
            $"freezedetect=n={lSweepTolerance / 100:0.#####}:d={lSweepMinimum:0.###}");
        return $"-hide_banner -stats -i {LEncode.LEncodeFormat(lSweepSource)} -map 0:v:0 -vf {LEncode.LEncodeFormat(lSweepFilter)} -an -f null -";
    }

    public static IReadOnlyList<(TimeSpan Start, TimeSpan End)> LSweepStillParse(IEnumerable<string> lSweepLines) =>
        LSweepStillParse(lSweepLines, TimeSpan.Zero);

    public static IReadOnlyList<(TimeSpan Start, TimeSpan End)> LSweepStillParse(
        IEnumerable<string> lSweepLines, TimeSpan lSweepDuration)
    {
        var lSweepIntervals = new List<(TimeSpan, TimeSpan)>();
        double? lSweepPending = null;
        foreach (string lSweepLine in lSweepLines)
        {
            if (lSweepLine is null)
            {
                continue;
            }

            double? lSweepStart = LSweepFieldRead(lSweepLine, "freeze_start: ");
            if (lSweepStart is { } lSweepFrom)
            {
                lSweepPending = lSweepFrom;
                continue;
            }

            double? lSweepEnd = LSweepFieldRead(lSweepLine, "freeze_end: ");
            if (lSweepEnd is { } lSweepTo && lSweepPending is { } lSweepOpen && lSweepTo > lSweepOpen)
            {
                lSweepIntervals.Add((TimeSpan.FromSeconds(lSweepOpen), TimeSpan.FromSeconds(lSweepTo)));
                lSweepPending = null;
            }
        }

        if (lSweepPending is { } lSweepTail && lSweepDuration > TimeSpan.FromSeconds(lSweepTail))
        {
            lSweepIntervals.Add((TimeSpan.FromSeconds(lSweepTail), lSweepDuration));
        }

        return lSweepIntervals;
    }
}
