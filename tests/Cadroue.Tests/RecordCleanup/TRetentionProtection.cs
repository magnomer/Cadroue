using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

public sealed class TRetentionProtection
{
    [Theory]
    [InlineData("scheduled/x.json")]
    [InlineData("running/y.json")]
    [InlineData("palettes/set.json")]
    [InlineData("work.db")]
    [InlineData("work.db-wal")]
    [InlineData("work.db-shm")]
    [InlineData("placement.json")]
    [InlineData("log/Cadroue-20260809-154741-72804.log")]
    [InlineData("filerecord/clip.cad")]
    [InlineData("filerecord/clip.cadcache")]
    [InlineData("relayplans/p.json")]
    [InlineData("local-mpv/libmpv-2.dll")]
    [InlineData("local-flyleaf/FlyleafLib.dll")]
    [InlineData("done")]
    public void Swept_ProtectedPaths_False(string path)
    {
        Assert.False(TInterface.TRetentionSweptCheck(path));
    }

    [Theory]
    [InlineData("done/z.json")]
    [InlineData("failed/z.json")]
    [InlineData("cancelled/z.json")]
    [InlineData("audiowork/a.mp4")]
    [InlineData("mergework/m.txt")]
    [InlineData("bridgework/b.concat.txt")]
    [InlineData("passwork/id/ffmpeg2pass-0.log")]
    public void Swept_WorkPaths_True(string path)
    {
        Assert.True(TInterface.TRetentionSweptCheck(path));
    }

    [Fact]
    public void Swept_CaseInsensitiveRoot_True()
    {
        Assert.True(TInterface.TRetentionSweptCheck("Done/x.json"));
    }

    [Fact]
    public void Swept_Backslash_Root_True()
    {
        Assert.True(TInterface.TRetentionSweptCheck("failed\\y.json"));
    }
}
