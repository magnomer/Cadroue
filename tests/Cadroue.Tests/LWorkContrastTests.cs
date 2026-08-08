using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LWorkContrastTests
{
    [Theory]
    [InlineData(-10, 0)]
    [InlineData(50, 50)]
    [InlineData(500, 200)]
    public void Create_OutOfRange_ClampsToBound(double lStepValue, double lExpected)
    {
        var step = LWorkVideoStep.LWorkContrastCreate(true, lStepValue);

        Assert.Equal(lExpected, step.LWorkStepValue);
    }
}
