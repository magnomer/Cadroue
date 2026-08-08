using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LSpoolTests
{
    [Fact]
    public void StepResolve_FullRange_SpanOverForty()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        Assert.Equal(10.0, spool.LSpoolStepResolve(1).TotalSeconds, 6);
    }

    [Fact]
    public void StepResolve_SmallRange_FloorsAtFloor()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(1));
        Assert.Equal(0.04, spool.LSpoolStepResolve(1).TotalSeconds, 6);
    }

    [Fact]
    public void StepResolve_NegativeSteps_NegativeDelta()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        Assert.Equal(-10.0, spool.LSpoolStepResolve(-1).TotalSeconds, 6);
    }

    [Fact]
    public void StepResolve_MagnitudeScalesLinearly()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        Assert.Equal(
            spool.LSpoolStepResolve(1).TotalSeconds * 2,
            spool.LSpoolStepResolve(2).TotalSeconds,
            6);
    }
}
