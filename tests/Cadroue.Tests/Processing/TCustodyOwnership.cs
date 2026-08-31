using Xunit;

namespace Cadroue.Tests;

public sealed class TCustodyOwnership
{
    [Fact]
    public void LiveOwnerRecognised()
    {
        TProcessing processing = new();
        Assert.True(processing.TProcessingCurrentCheck());
    }

    [Fact]
    public void DeadOwnerRejected()
    {
        TProcessing processing = new();
        Assert.True(processing.TProcessingMissingCheck());
    }

    [Fact]
    public void LiveRunnerOwnerRecognised()
    {
        TProcessing processing = new();
        Assert.True(processing.TProcessingLiveCheck());
    }

    [Fact]
    public void DeadRunnerOwnerRejected()
    {
        TProcessing processing = new();
        Assert.True(processing.TProcessingDeadCheck());
    }
}
