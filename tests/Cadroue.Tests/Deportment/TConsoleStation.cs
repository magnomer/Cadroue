using Xunit;

namespace Cadroue.Tests;

public sealed class TConsoleStation
{
    [Theory]
    [InlineData(0, 1, 3, 1)]
    [InlineData(2, 1, 3, 0)]
    [InlineData(0, -1, 3, 2)]
    [InlineData(1, -1, 3, 0)]
    [InlineData(0, 1, 1, 0)]
    [InlineData(0, -1, 1, 0)]
    public void Index_WrapsAroundTheBoard(int index, int step, int count, int expected) =>
        Assert.Equal(expected, TInterface.TConsoleIndexResolve(index, step, count));
}
