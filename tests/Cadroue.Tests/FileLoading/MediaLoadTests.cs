using Xunit;

namespace Cadroue.Tests;

[Collection("MediaLoad")]
public sealed class MediaLoadTests
{
    [Fact]
    public async Task ValidSupportedSource_BecomesCurrent()
    {
        using var media = new TMediaLoad();
        string source = media.SourceCreate("valid.mp4", 4_250);

        TMediaLoadOutcome outcome = await media.LoadAsync(source);

        Assert.True(outcome.Success);
        Assert.Equal(source, media.CurrentPath);
        Assert.Equal(4_250, media.CurrentDurationMilliseconds);
        Assert.Single(media.Events);
    }

    [Fact]
    public async Task SourceSpecificInformation_IsReadForEachSource()
    {
        using var media = new TMediaLoad();
        string first = media.SourceCreate("first.mp4", 1_000);
        string second = media.SourceCreate("second.mp4", 9_000);

        Assert.True((await media.LoadAsync(first)).Success);
        Assert.True((await media.LoadAsync(second)).Success);

        Assert.Equal(second, media.CurrentPath);
        Assert.Equal(9_000, media.CurrentDurationMilliseconds);
    }
}
