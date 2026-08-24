using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class DrawAggregationTests
{
    [Fact]
    public void RepeatedDraws_AggregateCountTimingGlyphsAndTriggers()
    {
        using var logging = new TLogging();
        logging.VerboseSet(true);
        logging.Reset();

        logging.Draw("Timeline", "resize", 2, 3);
        logging.Draw("Timeline", "resize", 4, 6);
        logging.Draw("Timeline", "scroll", 6);
        logging.DrawFlush();

        TLoggingEntry entry = Assert.Single(logging.Entries);
        Assert.Equal("UI", entry.Kind);
        Assert.Equal("Timeline drew 3x in the last second", entry.Summary);
        Assert.Contains("avg 4.00ms, peak 6.00ms, total 12.0ms", entry.Detail);
        Assert.Contains("9 FormattedText built (3/draw)", entry.Detail);
        Assert.Contains("triggers: resize 2, scroll 1", entry.Detail);
    }

    [Fact]
    public void TimelineDraws_CombineSurfacesWithCursorAndSourceContext()
    {
        using var logging = new TLogging();
        logging.VerboseSet(true);
        logging.Reset();
        const string sourcePath = @"D:\Media\clip.mp4";

        logging.TimelineDraw("Map", TimeSpan.FromSeconds(12.345), sourcePath, "cursor", 0.8);
        logging.TimelineDraw("Viewfinder", TimeSpan.FromSeconds(12.345), sourcePath, "cursor", 1.2, 1);
        logging.TimelineDraw("Map", TimeSpan.FromSeconds(13.5), sourcePath, "keyframes", 1.0);
        logging.TimelineDraw("Viewfinder", TimeSpan.FromSeconds(13.5), sourcePath, "keyframes", 1.4, 1);
        logging.DrawFlush();

        TLoggingEntry entry = Assert.Single(logging.Entries);
        Assert.Equal("UI", entry.Kind);
        Assert.Equal($"Timeline redrawn for 00:00:13.500; {sourcePath}", entry.Summary);
        Assert.Contains("Map: 2 redraws", entry.Detail);
        Assert.Contains("avg 0.90ms, peak 1.00ms, total 1.8ms", entry.Detail);
        Assert.Contains("Viewfinder: 2 redraws", entry.Detail);
        Assert.Contains("2 FormattedText built (1/draw)", entry.Detail);
        Assert.DoesNotContain("PMap", entry.Detail);
        Assert.DoesNotContain("PViewfinder", entry.Detail);
    }

    [Fact]
    public void Reset_PreventsEarlierDrawsFromContaminatingLaterReport()
    {
        using var logging = new TLogging();
        logging.VerboseSet(true);
        logging.Reset();
        logging.Draw("Timeline", "old", 99, 50);

        logging.Reset();
        logging.Draw("Timeline", "new", 5, 2);
        logging.DrawFlush();

        TLoggingEntry entry = Assert.Single(logging.Entries);
        Assert.Equal("Timeline drew 1x in the last second", entry.Summary);
        Assert.Contains("avg 5.00ms, peak 5.00ms, total 5.0ms", entry.Detail);
        Assert.Contains("triggers: new 1", entry.Detail);
        Assert.DoesNotContain("old", entry.Detail);
        Assert.DoesNotContain("99", entry.Detail);
    }

    [Fact]
    public void DisablingVerbose_FlushesDrawsAcceptedBeforeTheTransition()
    {
        using var logging = new TLogging();
        logging.VerboseSet(true);
        logging.Reset();
        logging.Draw("Timeline", "shutdown", 7, 4);

        logging.VerboseSet(false);

        Assert.Collection(
            logging.Entries,
            entry =>
            {
                Assert.Equal("UI", entry.Kind);
                Assert.Equal("Timeline drew 1x in the last second", entry.Summary);
                Assert.Contains("triggers: shutdown 1", entry.Detail);
            },
            entry => Assert.Equal(("Info", "Verbose logging off"), (entry.Kind, entry.Summary)));
    }
}
