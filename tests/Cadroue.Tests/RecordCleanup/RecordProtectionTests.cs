using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

public sealed class RecordProtectionTests
{
    [Theory]
    [InlineData("scheduled/x.json")]
    [InlineData("running/y.json")]
    [InlineData("palettes/set.json")]
    [InlineData("work.db")]
    [InlineData("work.db-wal")]
    [InlineData("work.db-shm")]
    public void Excluded_ProtectedPaths_True(string path)
    {
        Assert.True(TInterface.RetentionExcludedCheck(path));
    }

    [Theory]
    [InlineData("done/z.json")]
    [InlineData("audiowork/a.mp4")]
    [InlineData("relayplans/p.json")]
    public void Excluded_OrdinaryPaths_False(string path)
    {
        Assert.False(TInterface.RetentionExcludedCheck(path));
    }

    [Fact]
    public void Excluded_CaseInsensitiveRoot_True()
    {
        Assert.True(TInterface.RetentionExcludedCheck("Scheduled/x.json"));
    }

    [Fact]
    public void Excluded_Backslash_Root_True()
    {
        Assert.True(TInterface.RetentionExcludedCheck("running\\y.json"));
    }
}
