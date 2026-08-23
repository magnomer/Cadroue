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
    public void LSweepLuminanceParse_PairsTimeWithFollowingLuma()
    {
        string[] lLines =
        {
            "lavfi.signalstats.YAVG=99.000000",
            "garbage line without tokens",
            "frame:0    pts_time:0.000000",
            "lavfi.signalstats.YAVG=158.000000",
            "frame:1    pts_time:0.500000",
            "lavfi.signalstats.YAVG=122.000000",
            "frame:2    pts_time:1.000000"
        };

        IReadOnlyList<LSweepSample> lSamples = LSweep.LSweepLuminanceParse(lLines);

        Assert.Equal(2, lSamples.Count);
        Assert.Equal(TimeSpan.Zero, lSamples[0].LSweepSampleTime);
        Assert.Equal(158.0, lSamples[0].LSweepSampleLuma);
        Assert.Equal(TimeSpan.FromSeconds(0.5), lSamples[1].LSweepSampleTime);
        Assert.Equal(122.0, lSamples[1].LSweepSampleLuma);
    }

    [Fact]
    public void LSweepLuminanceResolve_FlatSeriesYieldsNoBoundaryEvenAtZero()
    {
        var lSamples = new List<LSweepSample>();
        for (int lIndex = 0; lIndex < 20; lIndex++)
        {
            lSamples.Add(new LSweepSample(TimeSpan.FromSeconds(lIndex * 0.1), 158.0));
        }

        Assert.Empty(LSweep.LSweepLuminanceResolve(lSamples, 0.5, 0));
    }

    [Fact]
    public void LSweepLuminanceResolve_SustainedStepYieldsOneBoundary()
    {
        var lSamples = new List<LSweepSample>();
        for (int lIndex = 0; lIndex < 20; lIndex++)
        {
            double lLuma = lIndex < 10 ? 158.0 : 122.0;
            lSamples.Add(new LSweepSample(TimeSpan.FromSeconds(lIndex * 0.1), lLuma));
        }

        IReadOnlyList<TimeSpan> lBoundaries = LSweep.LSweepLuminanceResolve(lSamples, 0.5, 10);

        TimeSpan lBoundary = Assert.Single(lBoundaries);
        Assert.Equal(TimeSpan.FromSeconds(1.0), lBoundary);
    }

    [Fact]
    public void LSweepLuminanceResolve_SubThresholdStepYieldsNoBoundary()
    {
        var lSamples = new List<LSweepSample>();
        for (int lIndex = 0; lIndex < 20; lIndex++)
        {
            double lLuma = lIndex < 10 ? 158.0 : 150.0;
            lSamples.Add(new LSweepSample(TimeSpan.FromSeconds(lIndex * 0.1), lLuma));
        }

        Assert.Empty(LSweep.LSweepLuminanceResolve(lSamples, 0.5, 10));
    }

    [Fact]
    public void LSweepLuminanceResolve_LargeInputResolvesToSingleStep()
    {
        const int lHalf = 20000;
        var lSamples = new List<LSweepSample>(lHalf * 2);
        for (int lIndex = 0; lIndex < lHalf * 2; lIndex++)
        {
            double lLuma = lIndex < lHalf ? 100.0 : 200.0;
            lSamples.Add(new LSweepSample(TimeSpan.FromSeconds(lIndex * 0.04), lLuma));
        }

        IReadOnlyList<TimeSpan> lBoundaries = LSweep.LSweepLuminanceResolve(lSamples, 0.5, 10);

        TimeSpan lBoundary = Assert.Single(lBoundaries);
        Assert.Equal(lSamples[lHalf].LSweepSampleTime, lBoundary);
    }

    [Fact]
    public void LSweepCombineResolve_UnionsHolesTreatSpansBoundariesAndUserPieces()
    {
        var lUser = new LPiece(TimeSpan.Zero, TimeSpan.FromSeconds(10), 0, "keep") { LPieceDetected = false };

        IReadOnlyList<LPiece> lResult = LSweep.LSweepCombineResolve(
            new[] { lUser },
            new[] { (TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)) },
            new[] { (TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(7)) },
            new[] { (TimeSpan.FromSeconds(4), TimeSpan.Zero), (TimeSpan.FromSeconds(8), TimeSpan.Zero) },
            TimeSpan.FromSeconds(10),
            4);

        Assert.Equal(7, lResult.Count);

        LPiece lKept = Assert.Single(lResult, lSection => lSection.LPieceName == "keep");
        Assert.False(lKept.LPieceDetected);
        Assert.Equal(TimeSpan.Zero, lKept.LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(10), lKept.LPieceEnd);

        IReadOnlyList<LPiece> lDetected = lResult.Where(lSection => lSection.LPieceDetected).ToList();
        Assert.Equal(6, lDetected.Count);

        Assert.DoesNotContain(lDetected, lSection =>
            lSection.LPieceOrigin < TimeSpan.FromSeconds(3) && lSection.LPieceEnd > TimeSpan.FromSeconds(2));

        Assert.Contains(lDetected, lSection =>
            lSection.LPieceOrigin == TimeSpan.FromSeconds(6) && lSection.LPieceEnd == TimeSpan.FromSeconds(7));

        Assert.Contains(lDetected, lSection =>
            lSection.LPieceOrigin == TimeSpan.FromSeconds(3) && lSection.LPieceEnd == TimeSpan.FromSeconds(4));
        Assert.Contains(lDetected, lSection =>
            lSection.LPieceOrigin == TimeSpan.FromSeconds(4) && lSection.LPieceEnd == TimeSpan.FromSeconds(6));
        Assert.Contains(lDetected, lSection =>
            lSection.LPieceOrigin == TimeSpan.FromSeconds(7) && lSection.LPieceEnd == TimeSpan.FromSeconds(8));
        Assert.Contains(lDetected, lSection =>
            lSection.LPieceOrigin == TimeSpan.FromSeconds(8) && lSection.LPieceEnd == TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void LSweepCombineResolve_DropsSceneCutBelowMinimumButKeepsZeroMinimum()
    {
        IReadOnlyList<LPiece> lResult = LSweep.LSweepCombineResolve(
            Array.Empty<LPiece>(),
            Array.Empty<(TimeSpan Start, TimeSpan End)>(),
            Array.Empty<(TimeSpan Start, TimeSpan End)>(),
            new[]
            {
                (TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)),
                (TimeSpan.FromSeconds(5), TimeSpan.Zero)
            },
            TimeSpan.FromSeconds(10),
            4);

        Assert.All(lResult, lSection => Assert.True(lSection.LPieceDetected));

        Assert.DoesNotContain(lResult, lSection => lSection.LPieceEnd == TimeSpan.FromSeconds(1));
        Assert.Contains(lResult, lSection =>
            lSection.LPieceOrigin == TimeSpan.Zero && lSection.LPieceEnd == TimeSpan.FromSeconds(5));
        Assert.Contains(lResult, lSection =>
            lSection.LPieceOrigin == TimeSpan.FromSeconds(5) && lSection.LPieceEnd == TimeSpan.FromSeconds(10));
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
