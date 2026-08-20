using Xunit;

namespace Cadroue.Tests;

public sealed class ProcessOwnershipTests
{
    [Fact]
    public void LiveOwnerRecognised()
    {
        TProcessing processing = new();
        Assert.True(processing.CurrentOwnerLives());
    }

    [Fact]
    public void DeadOwnerRejected()
    {
        TProcessing processing = new();
        Assert.True(processing.MissingOwnerDetected());
    }

    [Fact]
    public void LiveRunnerOwnerRecognised()
    {
        TProcessing processing = new();
        Assert.True(processing.LiveRunnerOwnerLives());
    }

    [Fact]
    public void DeadRunnerOwnerRejected()
    {
        TProcessing processing = new();
        Assert.True(processing.DeadRunnerOwnerDetected());
    }
}
