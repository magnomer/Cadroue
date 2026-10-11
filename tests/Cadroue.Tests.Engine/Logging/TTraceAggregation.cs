using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Engine;

[CollectionDefinition("Logging", DisableParallelization = true)]
public sealed class TTraceCollection
{
}

[Collection("Logging")]
public sealed class TTraceAggregation
{
    [Fact]
    public void RepeatedDraws_AggregateCountTimingGlyphsAndTriggers()
    {
        using var logging = new TTrace();
        logging.TTraceVerboseSet(true);
        logging.TTraceReset();
        const string sourcePath = @"D:\Media\clip.mp4";

        logging.TTimelineDraw("Map", TimeSpan.FromSeconds(1), sourcePath, "resize", 2, 3);
        logging.TTimelineDraw("Map", TimeSpan.FromSeconds(1), sourcePath, "resize", 4, 6);
        logging.TTimelineDraw("Map", TimeSpan.FromSeconds(1), sourcePath, "scroll", 6);
        logging.TTraceDrawCommit();

        TTraceEntry entry = Assert.Single(logging.TTraceEntries);
        Assert.Equal("UI", entry.TTraceKind);
        Assert.Equal($"Timeline redrawn for 00:00:01.000; {sourcePath}", entry.TTraceSummary);
        Assert.Contains("Map: 3 redraws", entry.TTraceDetail);
        Assert.Contains("avg 4.00ms, peak 6.00ms, total 12.0ms", entry.TTraceDetail);
        Assert.Contains("9 FormattedText built (3/draw)", entry.TTraceDetail);
        Assert.Contains("triggers: resize 2, scroll 1", entry.TTraceDetail);
    }

    [Fact]
    public void TimelineDraws_CombineSurfacesWithCursorAndSourceContext()
    {
        using var logging = new TTrace();
        logging.TTraceVerboseSet(true);
        logging.TTraceReset();
        const string sourcePath = @"D:\Media\clip.mp4";

        logging.TTimelineDraw("Map", TimeSpan.FromSeconds(12.345), sourcePath, "cursor", 0.8);
        logging.TTimelineDraw("Viewfinder", TimeSpan.FromSeconds(12.345), sourcePath, "cursor", 1.2, 1);
        logging.TTimelineDraw("Map", TimeSpan.FromSeconds(13.5), sourcePath, "keyframes", 1.0);
        logging.TTimelineDraw("Viewfinder", TimeSpan.FromSeconds(13.5), sourcePath, "keyframes", 1.4, 1);
        logging.TTraceDrawCommit();

        TTraceEntry entry = Assert.Single(logging.TTraceEntries);
        Assert.Equal("UI", entry.TTraceKind);
        Assert.Equal($"Timeline redrawn for 00:00:13.500; {sourcePath}", entry.TTraceSummary);
        Assert.Contains("Map: 2 redraws", entry.TTraceDetail);
        Assert.Contains("avg 0.90ms, peak 1.00ms, total 1.8ms", entry.TTraceDetail);
        Assert.Contains("Viewfinder: 2 redraws", entry.TTraceDetail);
        Assert.Contains("2 FormattedText built (1/draw)", entry.TTraceDetail);
        Assert.DoesNotContain("PMap", entry.TTraceDetail);
        Assert.DoesNotContain("PViewfinder", entry.TTraceDetail);
    }

    [Fact]
    public void Reset_PreventsEarlierDrawsFromContaminatingLaterReport()
    {
        using var logging = new TTrace();
        logging.TTraceVerboseSet(true);
        logging.TTraceReset();
        logging.TTimelineDraw("Map", TimeSpan.FromSeconds(1), null, "old", 99, 50);

        logging.TTraceReset();
        logging.TTimelineDraw("Map", TimeSpan.FromSeconds(2), null, "new", 5, 2);
        logging.TTraceDrawCommit();

        TTraceEntry entry = Assert.Single(logging.TTraceEntries);
        Assert.Equal("Timeline redrawn for 00:00:02.000; (no media)", entry.TTraceSummary);
        Assert.Contains("Map: 1 redraw", entry.TTraceDetail);
        Assert.Contains("avg 5.00ms, peak 5.00ms, total 5.0ms", entry.TTraceDetail);
        Assert.Contains("2 FormattedText built (2/draw)", entry.TTraceDetail);
        Assert.Contains("triggers: new 1", entry.TTraceDetail);
        Assert.DoesNotContain("old", entry.TTraceDetail);
        Assert.DoesNotContain("99", entry.TTraceDetail);
    }

    [Fact]
    public void DisablingVerbose_FlushesDrawsAcceptedBeforeTheTransition()
    {
        using var logging = new TTrace();
        logging.TTraceVerboseSet(true);
        logging.TTraceReset();
        logging.TTimelineDraw("Map", TimeSpan.FromSeconds(3), null, "shutdown", 7, 4);

        logging.TTraceVerboseSet(false);

        Assert.Collection(
            logging.TTraceEntries,
            entry =>
            {
                Assert.Equal("UI", entry.TTraceKind);
                Assert.Equal("Timeline redrawn for 00:00:03.000; (no media)", entry.TTraceSummary);
                Assert.Contains("triggers: shutdown 1", entry.TTraceDetail);
            },
            entry => Assert.Equal(("Info", "Verbose logging off"), (entry.TTraceKind, entry.TTraceSummary)));
    }
}
