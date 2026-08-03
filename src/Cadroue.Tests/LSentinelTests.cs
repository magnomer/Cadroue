using Cadroue.Infrastructure;
using Xunit;

namespace Cadroue.Tests;

public sealed class LSentinelTests
{
    [Fact]
    public void LSentinelOwnerAliveCheckAcceptsCurrentProcessStamp()
    {
        Assert.True(LSentinel.LSentinelOwnerAliveCheck(
            Environment.ProcessId, LSentinel.LSentinelStampRead()));
    }

    [Fact]
    public void LSentinelOwnerAliveCheckRejectsMissingProcess()
    {
        Assert.False(LSentinel.LSentinelOwnerAliveCheck(int.MaxValue, 1));
    }
}
