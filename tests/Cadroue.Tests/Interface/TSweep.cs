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

        IReadOnlyList<LSweepSpan> lIntervals = LSweep.LSweepOutputParse(lLines);

        Assert.Equal(2, lIntervals.Count);
        Assert.Equal(TimeSpan.FromSeconds(1.5), lIntervals[0].LSweepSpanOrigin);
        Assert.Equal(TimeSpan.FromSeconds(3.0), lIntervals[0].LSweepSpanEnd);
        Assert.Equal(TimeSpan.FromSeconds(5), lIntervals[1].LSweepSpanOrigin);
        Assert.Equal(TimeSpan.FromSeconds(6.25), lIntervals[1].LSweepSpanEnd);
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

        IReadOnlyList<LSweepSpan> lIntervals = LSweep.LSweepStillParse(lLines);

        LSweepSpan lInterval = Assert.Single(lIntervals);
        Assert.Equal(TimeSpan.FromSeconds(2.5), lInterval.LSweepSpanOrigin);
        Assert.Equal(TimeSpan.FromSeconds(4.0), lInterval.LSweepSpanEnd);
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

        LSweepSpan lInterval =
            Assert.Single(LSweep.LSweepStillParse(lLines, TimeSpan.FromSeconds(12)));
        Assert.Equal(TimeSpan.FromSeconds(8), lInterval.LSweepSpanOrigin);
        Assert.Equal(TimeSpan.FromSeconds(12), lInterval.LSweepSpanEnd);
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
}
