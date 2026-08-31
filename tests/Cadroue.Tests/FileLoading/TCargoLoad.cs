using Xunit;

namespace Cadroue.Tests;

[Collection("MediaLoad")]
public sealed class TCargoLoad
{
    [Fact]
    public async Task ValidSupportedSource_BecomesCurrent()
    {
        using var media = new TMediaLoad();
        string source = media.TSourceCreate("valid.mp4", 4_250);

        TMediaLoadOutcome outcome = await media.TMediaLoadRun(source);

        Assert.True(outcome.TMediaSuccess);
        Assert.Equal(source, media.TMediaCurrentPath);
        Assert.Equal(4_250, media.TMediaCurrentDuration);
        Assert.Single(media.TMediaEvents);
    }

    [Fact]
    public async Task SourceSpecificInformation_IsReadForEachSource()
    {
        using var media = new TMediaLoad();
        string first = media.TSourceCreate("first.mp4", 1_000);
        string second = media.TSourceCreate("second.mp4", 9_000);

        Assert.True((await media.TMediaLoadRun(first)).TMediaSuccess);
        Assert.True((await media.TMediaLoadRun(second)).TMediaSuccess);

        Assert.Equal(second, media.TMediaCurrentPath);
        Assert.Equal(9_000, media.TMediaCurrentDuration);
    }
}
