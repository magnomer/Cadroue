using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TInspectorValueBox
{
    [Fact]
    public void ValueCommit_AboveFloor_KeepsTypedNumber()
    {
        Assert.Equal(2000, TInterfaceInspector.TInspectorValueCommit("2000", 100, 20, 20000));
    }

    [Fact]
    public void ValueCommit_Clamps_FallsBackWhenUnparsable()
    {
        Assert.Equal(20, TInterfaceInspector.TInspectorValueCommit("2", 100, 20, 20000));
        Assert.Equal(20000, TInterfaceInspector.TInspectorValueCommit("99999", 100, 20, 20000));
        Assert.Equal(100, TInterfaceInspector.TInspectorValueCommit("abc", 100, 20, 20000));
        Assert.Equal(-7.5, TInterfaceInspector.TInspectorValueCommit("-7.5", 100, null, null));
    }

    [Fact]
    public void ValueFormat_EchoesEquivalentText_ElseFormats()
    {
        Assert.Equal("2000", TInterfaceInspector.TInspectorValueFormat("2000", 2000, "0"));
        Assert.Equal("2000.0", TInterfaceInspector.TInspectorValueFormat("2000.0", 2000, "0"));
        Assert.Equal("1.5", TInterfaceInspector.TInspectorValueFormat("x", 1.5, "0.###"));
        Assert.Equal("3", TInterfaceInspector.TInspectorValueFormat(string.Empty, 3, "0"));
    }

    [Fact]
    public void SlideCommit_OnlyWhenSliderLeavesClampedCurrent()
    {
        var set = new List<double>();

        TInterfaceInspector.TInspectorSlideCommit(1000, 2000, 0, 1000, set.Add);
        TInterfaceInspector.TInspectorSlideCommit(500, 2000, 0, 1000, set.Add);

        Assert.Equal(new[] { 500.0 }, set);
    }
}
