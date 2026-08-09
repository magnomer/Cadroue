using Xunit;

namespace Cadroue.Tests;

[Collection("MediaLoad")]
public sealed class MediaUnloadTests
{
    [Fact]
    public async Task Unload_ClearsCurrentMediaIdentity()
    {
        using var media = new TMediaLoad();
        await media.LoadAsync(media.SourceCreate("loaded.mp4"));

        Assert.True(media.Unload());

        Assert.Null(media.CurrentPath);
        Assert.Null(media.CurrentDurationMilliseconds);
    }

    [Fact]
    public async Task RepeatedLoadUnloadCycles_EmitOneCompletionPerOperation()
    {
        using var media = new TMediaLoad();
        string source = media.SourceCreate("cycle.mp4");

        for (int cycle = 0; cycle < 5; cycle++)
        {
            await media.LoadAsync(source);
            Assert.True(media.Unload());
        }

        Assert.Equal(10, media.Events.Count);
        Assert.Equal(5, media.Events.Count(item => item.Success));
        Assert.Equal(5, media.Events.Count(item => item.Kind == "Unloaded"));
    }
}
