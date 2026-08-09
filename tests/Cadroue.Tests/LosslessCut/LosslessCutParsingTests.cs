using Xunit;

namespace Cadroue.Tests;

public sealed class LosslessCutParsingTests
{
    [Fact]
    public void ValidSupportedProject_ParsesSuccessfully()
    {
        TLosslessCut.TProject project = TLosslessCut.Parse(
            """{"version":1,"mediaFileName":"clip.mp4","cutSegments":[]}""");

        Assert.Equal(1, project.Version);
        Assert.Equal("clip.mp4", project.Media);
        Assert.True(project.VersionSupported);
        Assert.Empty(project.Segments);
    }

    [Fact]
    public void MalformedProjectText_FailsSafely()
    {
        Assert.ThrowsAny<Exception>(() => TLosslessCut.Parse("{ definitely not json"));
    }

    [Fact]
    public void UnsupportedProjectVersion_IsRejectedByVersionRules()
    {
        TLosslessCut.TProject project = TLosslessCut.Parse(
            """{"version":2,"mediaFileName":"clip.mp4","cutSegments":[]}""");

        Assert.False(project.VersionSupported);
    }

    [Fact]
    public void MultipleSegments_PreserveSourceOrder()
    {
        TLosslessCut.TProject project = TLosslessCut.Parse(
            """
            {
              "version": 1,
              "cutSegments": [
                {"start": 8, "end": 9, "name": "third in time"},
                {"start": 1, "end": 2, "name": "first in time"},
                {"start": 4, "end": 5, "name": "second in time"}
              ]
            }
            """);

        Assert.Equal(
            new[] { "third in time", "first in time", "second in time" },
            project.Segments.Select(segment => segment.Name));
        Assert.Equal(new[] { 0, 1, 2 }, project.Segments.Select(segment => segment.Index));
    }
}
