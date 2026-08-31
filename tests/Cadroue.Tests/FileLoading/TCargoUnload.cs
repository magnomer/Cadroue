using Xunit;

namespace Cadroue.Tests;

[Collection("MediaLoad")]
public sealed class TCargoUnload
{
    [Fact]
    public async Task Unload_ClearsCurrentMediaIdentity()
    {
        using var media = new TMediaLoad();
        await media.TMediaLoadRun(media.TSourceCreate("loaded.mp4"));

        Assert.True(media.TMediaClose());

        Assert.Null(media.TMediaCurrentPath);
        Assert.Null(media.TMediaCurrentDuration);
    }

    [Fact]
    public async Task RepeatedLoadUnloadCycles_EmitOneCompletionPerOperation()
    {
        using var media = new TMediaLoad();
        string source = media.TSourceCreate("cycle.mp4");

        for (int cycle = 0; cycle < 5; cycle++)
        {
            await media.TMediaLoadRun(source);
            Assert.True(media.TMediaClose());
        }

        Assert.Equal(10, media.TMediaEvents.Count);
        Assert.Equal(5, media.TMediaEvents.Count(item => item.TMediaSuccess));
        Assert.Equal(5, media.TMediaEvents.Count(item => item.TMediaKind == "Unloaded"));
    }
}
