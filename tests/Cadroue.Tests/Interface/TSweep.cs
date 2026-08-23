using System.Linq;

using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

public sealed class TSweep
{
    [Fact]
    public void LSweepOutputParse_ReadsBlackStartAndEndPairs()
    {
        string[] lLines =
        {
            "[blackdetect @ 0x1] black_start:1.5 black_end:3.0 black_duration:1.5",
            "frame= 10 fps=0.0",
            "[blackdetect @ 0x1] black_start:5 black_end:6.25 black_duration:1.25"
        };

        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lIntervals = LSweep.LSweepOutputParse(lLines);

        Assert.Equal(2, lIntervals.Count);
        Assert.Equal(TimeSpan.FromSeconds(1.5), lIntervals[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(3.0), lIntervals[0].End);
        Assert.Equal(TimeSpan.FromSeconds(5), lIntervals[1].Start);
        Assert.Equal(TimeSpan.FromSeconds(6.25), lIntervals[1].End);
    }

    [Fact]
    public void LSweepStillParse_ReadsOneFreezeIntervalFromPair()
    {
        string[] lLines =
        {
            "[freezedetect @ 0x1] lavfi.freezedetect.freeze_start: 2.5",
            "[freezedetect @ 0x1] lavfi.freezedetect.freeze_duration: 1.5",
            "[freezedetect @ 0x1] lavfi.freezedetect.freeze_end: 4.0"
        };

        IReadOnlyList<(TimeSpan Start, TimeSpan End)> lIntervals = LSweep.LSweepStillParse(lLines);

        (TimeSpan Start, TimeSpan End) lInterval = Assert.Single(lIntervals);
        Assert.Equal(TimeSpan.FromSeconds(2.5), lInterval.Start);
        Assert.Equal(TimeSpan.FromSeconds(4.0), lInterval.End);
    }

    [Fact]
    public void LSweepStillParse_IgnoresUnterminatedStart()
    {
        string[] lLines =
        {
            "[freezedetect @ 0x1] lavfi.freezedetect.freeze_start: 2.5",
            "frame= 10 fps=0.0"
        };

        Assert.Empty(LSweep.LSweepStillParse(lLines));
    }

    [Fact]
    public void LSweepStillParse_ClosesFreezeToEofOnDuration()
    {
        string[] lLines =
        {
            "[freezedetect @ 0x1] lavfi.freezedetect.freeze_start: 8.0",
            "frame= 10 fps=0.0"
        };

        (TimeSpan Start, TimeSpan End) lInterval =
            Assert.Single(LSweep.LSweepStillParse(lLines, TimeSpan.FromSeconds(12)));
        Assert.Equal(TimeSpan.FromSeconds(8), lInterval.Start);
        Assert.Equal(TimeSpan.FromSeconds(12), lInterval.End);
    }

    [Fact]
    public void LSweepStillParse_ReturnsEmptyWithoutFreezeKeys()
    {
        string[] lLines =
        {
            "frame= 10 fps=0.0 time=00:00:03.50",
            "[blackdetect @ 0x1] black_start:1.5 black_end:3.0"
        };

        Assert.Empty(LSweep.LSweepStillParse(lLines));
    }

    [Fact]
    public void LSweepSceneParse_ReadsAscendingUniqueTimesAndIgnoresOtherLines()
    {
        string[] lLines =
        {
            "[Parsed_metadata_1 @ 0x1] lavfi.scd.time=3.5",
            "frame= 10 fps=0.0 time=00:00:03.50",
            "[Parsed_metadata_1 @ 0x1] lavfi.scd.time=1.25",
            "[Parsed_metadata_1 @ 0x1] lavfi.scd.time=3.5",
            "[Parsed_metadata_1 @ 0x1] lavfi.scd.score=0.42"
        };

        IReadOnlyList<TimeSpan> lTimes = LSweep.LSweepSceneParse(lLines);

        Assert.Equal(2, lTimes.Count);
        Assert.Equal(TimeSpan.FromSeconds(1.25), lTimes[0]);
        Assert.Equal(TimeSpan.FromSeconds(3.5), lTimes[1]);
    }

    [Fact]
    public void LSweepSectionResolve_InvertsBlanksIntoDetectedContent()
    {
        IReadOnlyList<LPiece> lSections = LSweep.LSweepSectionResolve(
            Array.Empty<LPiece>(),
            new[] { (TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)) },
            TimeSpan.FromSeconds(5),
            4);

        Assert.Equal(2, lSections.Count);
        Assert.All(lSections, lSection => Assert.True(lSection.LPieceDetected));
        Assert.Equal(TimeSpan.Zero, lSections[0].LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(1), lSections[0].LPieceEnd);
        Assert.Equal(TimeSpan.FromSeconds(2), lSections[1].LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(5), lSections[1].LPieceEnd);
    }

    [Fact]
    public void LSweepSectionResolve_KeepsUserSectionsAndOverlaysDetected()
    {
        var lUser = new LPiece(TimeSpan.Zero, TimeSpan.FromSeconds(5), 0, "keep") { LPieceDetected = false };

        IReadOnlyList<LPiece> lSections = LSweep.LSweepSectionResolve(
            new[] { lUser },
            new[] { (TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)) },
            TimeSpan.FromSeconds(5),
            4);

        Assert.Equal(3, lSections.Count);

        LPiece lKept = Assert.Single(lSections, lSection => lSection.LPieceName == "keep");
        Assert.False(lKept.LPieceDetected);
        Assert.Equal(TimeSpan.Zero, lKept.LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(5), lKept.LPieceEnd);

        IReadOnlyList<LPiece> lDetected = lSections.Where(lSection => lSection.LPieceDetected).ToList();
        Assert.Equal(2, lDetected.Count);
        Assert.Equal(TimeSpan.Zero, lDetected[0].LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(1), lDetected[0].LPieceEnd);
        Assert.Equal(TimeSpan.FromSeconds(2), lDetected[1].LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(5), lDetected[1].LPieceEnd);
    }

    [Fact]
    public void LSweepSectionResolve_ReplacesUntouchedDetectedSections()
    {
        var lStale = new LPiece(TimeSpan.Zero, TimeSpan.FromSeconds(5), 0, "old") { LPieceDetected = true };

        IReadOnlyList<LPiece> lSections = LSweep.LSweepSectionResolve(
            new[] { lStale },
            new[] { (TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)) },
            TimeSpan.FromSeconds(5),
            4);

        Assert.Equal(2, lSections.Count);
        Assert.DoesNotContain(lSections, lSection => lSection.LPieceName == "old");
        Assert.All(lSections, lSection => Assert.True(lSection.LPieceDetected));
    }

    [Fact]
    public void LSweepSectionResolve_TruncatesToCeiling()
    {
        var lBlanks = new List<(TimeSpan Start, TimeSpan End)>();
        for (int lIndex = 0; lIndex < 6000; lIndex++)
        {
            lBlanks.Add((TimeSpan.FromSeconds(2 * lIndex + 1), TimeSpan.FromSeconds(2 * lIndex + 2)));
        }

        IReadOnlyList<LPiece> lSections = LSweep.LSweepSectionResolve(
            Array.Empty<LPiece>(),
            lBlanks,
            TimeSpan.FromSeconds(2 * 6000 + 2),
            4);

        Assert.Equal(LPiece.LPieceCeiling, lSections.Count);
    }

    [Fact]
    public void LSweepStillResolve_DiscardYieldsTwoContentSections()
    {
        IReadOnlyList<LPiece> lSections = LSweep.LSweepStillResolve(
            Array.Empty<LPiece>(),
            new[] { (TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)) },
            TimeSpan.FromSeconds(5),
            4,
            LDetectorStillMode.LDetectorStillDiscard);

        Assert.Equal(2, lSections.Count);
        Assert.All(lSections, lSection => Assert.True(lSection.LPieceDetected));
        Assert.Equal(TimeSpan.Zero, lSections[0].LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(2), lSections[0].LPieceEnd);
        Assert.Equal(TimeSpan.FromSeconds(3), lSections[1].LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(5), lSections[1].LPieceEnd);
    }

    [Fact]
    public void LSweepStillResolve_TreatPartitionsContentStillContent()
    {
        IReadOnlyList<LPiece> lSections = LSweep.LSweepStillResolve(
            Array.Empty<LPiece>(),
            new[] { (TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)) },
            TimeSpan.FromSeconds(5),
            4,
            LDetectorStillMode.LDetectorStillTreat);

        Assert.Equal(3, lSections.Count);
        Assert.All(lSections, lSection => Assert.True(lSection.LPieceDetected));
        Assert.Equal(TimeSpan.Zero, lSections[0].LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(2), lSections[0].LPieceEnd);
        Assert.Equal(TimeSpan.FromSeconds(2), lSections[1].LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(3), lSections[1].LPieceEnd);
        Assert.Equal(TimeSpan.FromSeconds(3), lSections[2].LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(5), lSections[2].LPieceEnd);
    }

    [Fact]
    public void LSweepStillResolve_TreatStillToEofClosesOnDuration()
    {
        IReadOnlyList<LPiece> lSections = LSweep.LSweepStillResolve(
            Array.Empty<LPiece>(),
            new[] { (TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5)) },
            TimeSpan.FromSeconds(5),
            4,
            LDetectorStillMode.LDetectorStillTreat);

        Assert.Equal(2, lSections.Count);
        Assert.Equal(TimeSpan.Zero, lSections[0].LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(3), lSections[0].LPieceEnd);
        Assert.Equal(TimeSpan.FromSeconds(3), lSections[1].LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(5), lSections[1].LPieceEnd);
    }

    [Fact]
    public void LDetectorBlankClamp_BoundsEveryAxis()
    {
        LDetectorBlank lClamped = LDetectorBlank.LDetectorBlankClamp(new LDetectorBlank(
            true, LDetectorType.LDetectorTypeColor, 400, 2.0, 3.0, 5.0, 0.1, 999));

        Assert.Equal(40, lClamped.LDetectorBlankHue);
        Assert.Equal(1, lClamped.LDetectorBlankSaturation);
        Assert.Equal(1, lClamped.LDetectorBlankBrightness);
        Assert.Equal(0.5, lClamped.LDetectorBlankTolerance);
        Assert.Equal(0.5, lClamped.LDetectorBlankCoverage);
        Assert.Equal(60, lClamped.LDetectorBlankMinimum);
    }
}
