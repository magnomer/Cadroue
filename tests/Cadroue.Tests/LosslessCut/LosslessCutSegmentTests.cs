using Xunit;

namespace Cadroue.Tests;

public sealed class LosslessCutSegmentTests
{
    private const string SourcePath = "clip.mp4";

    [Fact]
    public void BothBoundaries_ConvertToMilliseconds()
    {
        TLosslessCut.TResult result = Convert("""{"start":1.25,"end":3.75,"name":"middle"}""");

        TLosslessCut.TSection section = Assert.Single(result.Sections);
        Assert.Equal(1_250, section.StartMilliseconds);
        Assert.Equal(3_750, section.EndMilliseconds);
        Assert.Equal("middle", section.Name);
    }

    [Fact]
    public void StartOnly_EndsAtKnownMediaDuration()
    {
        TLosslessCut.TSection section = Assert.Single(
            Convert("""{"start":2.5,"name":"tail"}""", 10_000).Sections);

        Assert.Equal(2_500, section.StartMilliseconds);
        Assert.Equal(10_000, section.EndMilliseconds);
    }

    [Fact]
    public void EndOnly_StartsAtZero()
    {
        TLosslessCut.TSection section = Assert.Single(
            Convert("""{"end":4.25,"name":"head"}""").Sections);

        Assert.Equal(0, section.StartMilliseconds);
        Assert.Equal(4_250, section.EndMilliseconds);
    }

    [Fact]
    public void NeitherBoundary_SpansKnownMediaDuration()
    {
        TLosslessCut.TSection section = Assert.Single(
            Convert("""{"name":"whole"}""", 8_000).Sections);

        Assert.Equal(0, section.StartMilliseconds);
        Assert.Equal(8_000, section.EndMilliseconds);
    }

    [Fact]
    public void DecimalSeconds_AreConvertedExactlyOnce()
    {
        TLosslessCut.TSection section = Assert.Single(
            Convert("""{"start":12.345,"end":23.456}""", 30_000).Sections);

        Assert.Equal(12_345, section.StartMilliseconds);
        Assert.Equal(23_456, section.EndMilliseconds);
    }

    [Fact]
    public void SegmentNamesAndAcceptedOrder_ArePreserved()
    {
        TLosslessCut.TProject project = TLosslessCut.Parse(
            """
            {"version":1,"cutSegments":[
              {"start":6,"end":7,"name":"later"},
              {"start":1,"end":2,"name":"earlier"}
            ]}
            """);

        TLosslessCut.TResult result = TLosslessCut.Validate(project, SourcePath, TimeSpan.FromSeconds(10));

        Assert.Equal(new[] { "later", "earlier" }, result.Sections.Select(section => section.Name));
        Assert.Equal(new long[] { 6_000, 1_000 }, result.Sections.Select(section => section.StartMilliseconds));
    }

    private static TLosslessCut.TResult Convert(string segment, long durationMilliseconds = 10_000)
    {
        TLosslessCut.TProject project = TLosslessCut.Parse(
            $$"""{"version":1,"mediaFileName":"clip.mp4","cutSegments":[{{segment}}]}""");
        return TLosslessCut.Validate(
            project,
            SourcePath,
            TimeSpan.FromMilliseconds(durationMilliseconds));
    }
}
