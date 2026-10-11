using Cadroue.Core;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Engine;

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
