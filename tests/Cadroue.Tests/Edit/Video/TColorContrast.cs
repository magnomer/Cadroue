using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TColorContrast
{
    [Theory]
    [InlineData(-10, 0)]
    [InlineData(50, 50)]
    [InlineData(500, 200)]
    public void Contrast_OutOfRange_IsClampedToBounds(double lStepValue, double lExpected)
    {
        var step = TInterface.TWorkContrastCreate(true, lStepValue);

        Assert.Equal(lExpected, step.LWorkStepValue);
    }
}
