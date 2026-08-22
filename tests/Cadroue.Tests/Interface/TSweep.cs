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
    public void LSweepSectionResolve_KeepsUserSectionsAndCarvesAroundThem()
    {
        var lUser = new LPiece(TimeSpan.Zero, TimeSpan.FromSeconds(5), 0, "keep") { LPieceDetected = false };

        IReadOnlyList<LPiece> lSections = LSweep.LSweepSectionResolve(
            new[] { lUser },
            new[] { (TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)) },
            TimeSpan.FromSeconds(5),
            4);

        Assert.Single(lSections);
        Assert.Equal("keep", lSections[0].LPieceName);
        Assert.False(lSections[0].LPieceDetected);
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
