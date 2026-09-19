using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TCursorChip
{
    [Fact]
    public void TimeFormat_DropsTheHourUntilItIsNeeded()
    {
        Assert.Equal("0:00", TInterface.TCursorTimeFormat(TimeSpan.Zero));
        Assert.Equal("12:05", TInterface.TCursorTimeFormat(TimeSpan.FromSeconds(725)));
        Assert.Equal("1:02:03", TInterface.TCursorTimeFormat(new TimeSpan(1, 2, 3)));
    }

    [Fact]
    public void Wheel_ResolvesNotches_AndNeverZero()
    {
        Assert.Equal(2, TInterface.TCursorWheelResolve(240));
        Assert.Equal(-1, TInterface.TCursorWheelResolve(-120));
        Assert.Equal(1, TInterface.TCursorWheelResolve(30));
        Assert.Equal(-1, TInterface.TCursorWheelResolve(-30));
    }

    [Fact]
    public void Chip_StaysInsideTheStrip_AndCentresOnTheLine()
    {
        Assert.Equal((0, 5), TInterface.TCursorChipResolve(2, 20, 10, 0, 20, 100));
        Assert.Equal((80, 5), TInterface.TCursorChipResolve(99, 20, 10, 0, 20, 100));
        Assert.Equal((40, 5), TInterface.TCursorChipResolve(50, 20, 10, 0, 20, 100));
        Assert.Equal((0, 5), TInterface.TCursorChipResolve(50, 200, 10, 0, 20, 100));
        Assert.Equal((9.5, 10.5), TInterface.TCursorGuideResolve(10, 1));
    }

    [Fact]
    public void Lines_SplitAroundTheChip_AndSkipEmptySpans()
    {
        IReadOnlyList<LCursorLine> whole = TInterface.TCursorLinesResolve(0, 50, true, 0, 0, 0);
        Assert.Single(whole);
        Assert.Equal(50, whole[0].LCursorLineBottom);

        IReadOnlyList<LCursorLine> split = TInterface.TCursorLinesResolve(0, 50, false, 20, 30, 10);
        Assert.Equal(2, split.Count);
        Assert.Equal(18, split[0].LCursorLineBottom);
        Assert.Equal(32, split[1].LCursorLineTop);

        IReadOnlyList<LCursorLine> top = TInterface.TCursorLinesResolve(0, 50, false, 0, 10, 10);
        Assert.Single(top);
        Assert.Equal(12, top[0].LCursorLineTop);

        Assert.Empty(TInterface.TCursorLinesResolve(10, 10, true, 0, 0, 0));
        Assert.Single(TInterface.TCursorLinesResolve(0, 50, false, 20, 30, 0));
    }
}
