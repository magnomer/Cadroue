using Xunit;

namespace Cadroue.Tests;

public sealed class TLosslesscutSegment
{
    private const string TLosslesscutSource = "clip.mp4";

    [Fact]
    public void BothBoundaries_ConvertToMilliseconds()
    {
        TLosslesscut.TLosslesscutResult result = TLosslesscutImport("""{"start":1.25,"end":3.75,"name":"middle"}""");

        TLosslesscut.TSection section = Assert.Single(result.TLosslesscutSections);
        Assert.Equal(1_250, section.TSectionStartMilliseconds);
        Assert.Equal(3_750, section.TSectionEndMilliseconds);
        Assert.Equal("middle", section.TSectionName);
    }

    [Fact]
    public void StartOnly_EndsAtKnownMediaDuration()
    {
        TLosslesscut.TSection section = Assert.Single(
            TLosslesscutImport("""{"start":2.5,"name":"tail"}""", 10_000).TLosslesscutSections);

        Assert.Equal(2_500, section.TSectionStartMilliseconds);
        Assert.Equal(10_000, section.TSectionEndMilliseconds);
    }

    [Fact]
    public void EndOnly_StartsAtZero()
    {
        TLosslesscut.TSection section = Assert.Single(
            TLosslesscutImport("""{"end":4.25,"name":"head"}""").TLosslesscutSections);

        Assert.Equal(0, section.TSectionStartMilliseconds);
        Assert.Equal(4_250, section.TSectionEndMilliseconds);
    }

    [Fact]
    public void NeitherBoundary_SpansKnownMediaDuration()
    {
        TLosslesscut.TSection section = Assert.Single(
            TLosslesscutImport("""{"name":"whole"}""", 8_000).TLosslesscutSections);

        Assert.Equal(0, section.TSectionStartMilliseconds);
        Assert.Equal(8_000, section.TSectionEndMilliseconds);
    }

    [Fact]
    public void DecimalSeconds_AreConvertedExactlyOnce()
    {
        TLosslesscut.TSection section = Assert.Single(
            TLosslesscutImport("""{"start":12.345,"end":23.456}""", 30_000).TLosslesscutSections);

        Assert.Equal(12_345, section.TSectionStartMilliseconds);
        Assert.Equal(23_456, section.TSectionEndMilliseconds);
    }

    [Fact]
    public void SegmentNamesAndAcceptedOrder_ArePreserved()
    {
        TLosslesscut.TLosslesscutProject project = TLosslesscut.TLosslesscutParse(
            """
            {"version":1,"cutSegments":[
              {"start":6,"end":7,"name":"later"},
              {"start":1,"end":2,"name":"earlier"}
            ]}
            """);

        TLosslesscut.TLosslesscutResult result = TLosslesscut.TLosslesscutValidate(project, TLosslesscutSource, TimeSpan.FromSeconds(10));

        Assert.Equal(new[] { "later", "earlier" }, result.TLosslesscutSections.Select(section => section.TSectionName));
        Assert.Equal(new long[] { 6_000, 1_000 }, result.TLosslesscutSections.Select(section => section.TSectionStartMilliseconds));
    }

    private static TLosslesscut.TLosslesscutResult TLosslesscutImport(string segment, long durationMilliseconds = 10_000)
    {
        TLosslesscut.TLosslesscutProject project = TLosslesscut.TLosslesscutParse(
            $$"""{"version":1,"mediaFileName":"clip.mp4","cutSegments":[{{segment}}]}""");
        return TLosslesscut.TLosslesscutValidate(
            project,
            TLosslesscutSource,
            TimeSpan.FromMilliseconds(durationMilliseconds));
    }
}
