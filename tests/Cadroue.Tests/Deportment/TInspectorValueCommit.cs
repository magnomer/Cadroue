using Xunit;

namespace Cadroue.Tests;

public sealed class TInspectorValueCommit
{
    [Fact]
    public void ValueCommit_AboveFloor_KeepsTypedNumber()
    {
        Assert.Equal(2000, TInterface.TInspectorValueCommit("2000", 100, 20, 20000));
    }

    [Fact]
    public void ValueCommit_Clamps_FallsBackWhenUnparsable()
    {
        Assert.Equal(20, TInterface.TInspectorValueCommit("2", 100, 20, 20000));
        Assert.Equal(20000, TInterface.TInspectorValueCommit("99999", 100, 20, 20000));
        Assert.Equal(100, TInterface.TInspectorValueCommit("abc", 100, 20, 20000));
        Assert.Equal(-7.5, TInterface.TInspectorValueCommit("-7.5", 100, null, null));
    }

    [Fact]
    public void ValueFormat_EchoesEquivalentText_ElseFormats()
    {
        Assert.Equal("2000", TInterface.TInspectorValueFormat("2000", 2000, "0"));
        Assert.Equal("2000.0", TInterface.TInspectorValueFormat("2000.0", 2000, "0"));
        Assert.Equal("1.5", TInterface.TInspectorValueFormat("x", 1.5, "0.###"));
        Assert.Equal("3", TInterface.TInspectorValueFormat(string.Empty, 3, "0"));
    }

    [Fact]
    public void SlideCommit_OnlyWhenSliderLeavesClampedCurrent()
    {
        var set = new List<double>();

        TInterface.TInspectorSlideCommit(1000, 2000, 0, 1000, set.Add);
        TInterface.TInspectorSlideCommit(500, 2000, 0, 1000, set.Add);

        Assert.Equal(new[] { 500.0 }, set);
    }
}
