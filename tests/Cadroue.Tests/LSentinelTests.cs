using Cadroue.Infrastructure;
using Xunit;

namespace Cadroue.Tests;

public sealed class LSentinelTests
{
    [Fact]
    public void LSentinelOwnerCheckAcceptsCurrentProcessStamp()
    {
        Assert.True(LSentinel.LSentinelOwnerCheck(
            Environment.ProcessId, LSentinel.LSentinelStampRead()));
    }

    [Fact]
    public void LSentinelOwnerCheckRejectsMissingProcess()
    {
        Assert.False(LSentinel.LSentinelOwnerCheck(int.MaxValue, 1));
    }
}
