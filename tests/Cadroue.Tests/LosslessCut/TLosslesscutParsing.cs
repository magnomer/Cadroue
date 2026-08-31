using Xunit;

namespace Cadroue.Tests;

public sealed class TLosslesscutParsing
{
    [Fact]
    public void ValidSupportedProject_ParsesSuccessfully()
    {
        TLosslesscut.TLosslesscutProject project = TLosslesscut.TLosslesscutParse(
            """{"version":1,"mediaFileName":"clip.mp4","cutSegments":[]}""");

        Assert.Equal(1, project.TLosslesscutVersion);
        Assert.Equal("clip.mp4", project.TLosslesscutMedia);
        Assert.True(project.TLosslesscutSupported);
        Assert.Empty(project.TLosslesscutSegments);
    }

    [Fact]
    public void MalformedProjectText_FailsSafely()
    {
        Assert.ThrowsAny<Exception>(() => TLosslesscut.TLosslesscutParse("{ definitely not json"));
    }

    [Fact]
    public void UnsupportedProjectVersion_IsRejectedByVersionRules()
    {
        TLosslesscut.TLosslesscutProject project = TLosslesscut.TLosslesscutParse(
            """{"version":2,"mediaFileName":"clip.mp4","cutSegments":[]}""");

        Assert.False(project.TLosslesscutSupported);
    }

    [Fact]
    public void MultipleSegments_PreserveSourceOrder()
    {
        TLosslesscut.TLosslesscutProject project = TLosslesscut.TLosslesscutParse(
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
            project.TLosslesscutSegments.Select(segment => segment.TClipName));
        Assert.Equal(new[] { 0, 1, 2 }, project.TLosslesscutSegments.Select(segment => segment.TClipIndex));
    }
}
